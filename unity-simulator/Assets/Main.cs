using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Main
{
    static private TcpListener controlSocket, videoSocket, dataSocket;
    static private TcpClient controlClient, videoClient, dataClient;
    static private NetworkStream controlStream, videoStream, dataStream;
    static private Thread socketThread;
    static bool connected;

    //actions to be run on the main thread of unity
    private static readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    //context to access the main thread from the client threads
    private static SynchronizationContext mainThreadContext;

    private static byte[] aknBytes = Encoding.UTF8.GetBytes("AKN");
    private static List<AssetBundle> loadedBundles = new List<AssetBundle>();

    static string[] commands={"echo", "change_scene", "step_simulation_without_physics", "get_image", "get_image_info"};

    static Agent agent;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnStart() {
        //---------------------------------------------------------------------
        /// <summary>
        /// Function that runs before scene load, saves the main thread context
        /// and starts up the socket start thread (might refactor later)
        /// </summary>
        //---------------------------------------------------------------------
        string path = Path.Combine(".", "addons");
        LoadAssetBundles(path);
        SceneManager.sceneLoaded+=createAgent;
        mainThreadContext = SynchronizationContext.Current;
        // starts thread that initializes control, video and data socket
        socketThread = new Thread(new ThreadStart(StartSocketProcess));
        socketThread.IsBackground = true;
        socketThread.Start();
    }

    static void StartSocketProcess(){
        //---------------------------------------------------------------------
        /// <summary>
        /// Starts each socket server and waits for client to connect, 
        /// starts client handle threads for each socket 
        /// </summary>
        //---------------------------------------------------------------------
        controlSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
        controlSocket.Start();
        Debug.Log("control server started, waiting for connection ...");

        videoSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), 5001);
        videoSocket.Start();
        Debug.Log("video server started, waiting for connection ......");

        dataSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), 5002);
        dataSocket.Start();
        Debug.Log("data server started, waiting for connection ......");

        while(!connected)
        {
            try{
                controlClient = controlSocket.AcceptTcpClient();
                Debug.Log("control client connected.");

                videoClient = videoSocket.AcceptTcpClient();
                Debug.Log("video client connected.");

                dataClient = dataSocket.AcceptTcpClient();
                Debug.Log("data client connected.");
                connected = true;
            }

            catch (Exception e) {
                Debug.LogError("couldn't connect because of:  " + e.Message );
            }

        }
        // Handle the client connection in a new thread
        Thread controlClientThread = new Thread(() => HandleControlClient(controlClient));
        Thread videoClientThread = new Thread(() => HandleVideoClient(videoClient));
        Thread dataClientThread = new Thread(() => HandleDataClient(dataClient));
        controlClientThread.IsBackground = true;
        videoClientThread.IsBackground = true;
        dataClientThread.IsBackground = true;
        controlClientThread.Start();
        videoClientThread.Start();
        dataClientThread.Start();
                    
    }

    static void HandleControlClient(TcpClient client){
        //---------------------------------------------------------------------
        /// <summary>
        /// Handles the control socket, decodes json into command object, 
        /// invokes the corresponding command
        /// </summary>
        /// <param name="client">The TcpClient of the control socket</param>
        //---------------------------------------------------------------------

        controlStream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;
         
        while ((bytesRead = controlStream.Read(buffer, 0, buffer.Length)) != 0){
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log("Received: " + message);

            Command json = JsonUtility.FromJson<Command>(message);
            string command= json.command;
            string [] param = json.param;
            Debug.Log(param.GetType());
            Debug.Log($"Command: {command} with parameters: {string.Join(",", param)}");

            if (commands.Contains(command)){
                Type type = typeof(Main);
                MethodInfo method = type.GetMethod(command, BindingFlags.Static | BindingFlags.NonPublic);
                method.Invoke(null, new object[]{param});
                Debug.Log($"Command: {command} is done");
            }
            else{
                Debug.Log($"Command: {command} is not defined");
            }

        }

    }
    static void HandleVideoClient(TcpClient client){

        //---------------------------------------------------------------------
        /// <summary>
        /// Handles the video socket
        /// </summary>
        /// <param name="client">The TcpClient of the video socket</param>
        //---------------------------------------------------------------------
        videoStream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;
         
        while ((bytesRead = videoStream.Read(buffer, 0, buffer.Length)) != 0){
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log("Received on vid: " + message);

        }

    }

    static void HandleDataClient(TcpClient client){
        //---------------------------------------------------------------------
        /// <summary>
        /// Handles the data socket
        /// </summary>
        /// <param name="client">The TcpClient of the data socket</param>
        //---------------------------------------------------------------------
        dataStream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;
         
        while ((bytesRead = videoStream.Read(buffer, 0, buffer.Length)) != 0){
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log("Received on data: " + message);

        }
    }

    static void send_data(NetworkStream stream, byte[] data){
        Debug.Log(data != null);
        data = data.Concat(Encoding.Default.GetBytes("!unityservereod!")).ToArray();
        Debug.Log(data != null);
        stream.Write(data, 0, data.Length);
    }


    static void echo(string[] param){
        //---------------------------------------------------------------------
        /// <summary>
        /// Recieves a message as first entry of param array, sends it back
        /// </summary>
        /// <param name="param">The array of parameters</param>
        //---------------------------------------------------------------------
        byte[] responseData = Encoding.UTF8.GetBytes(param[0]);
        controlStream.Write(responseData, 0, responseData.Length);
    }

    static void change_scene(string[] param){
        //---------------------------------------------------------------------
        /// <summary>
        /// Changes the scene to the scene with the name given in param[0]
        /// </summary>
        /// <param name="param">The array of parameters</param>
        //---------------------------------------------------------------------
        string scene_name = param[0];
        RunOnMainThread(() =>  {
            try{
                SceneManager.LoadScene(scene_name);
            }
            catch(Exception e){
                Debug.LogWarning(e.Message);    
            }   
            controlStream.Write(aknBytes, 0, aknBytes.Length);
        });
           
    }

    static void step_simulation_without_physics(string[] param){
        Debug.Log("stepping it up");
        //---------------------------------------------------------------------
        /// <summary>
        /// Invokes the move method of the agent and steps simulation
        /// </summary>
        /// <param name="param">The array of parameters</param>
        //---------------------------------------------------------------------
        float x = float.Parse(param[0]);
        float y = float.Parse(param[1]);
        float yaw = float.Parse(param[2]);
        agent.move(x, y, yaw);
        Debug.Log("stepped it up");
        controlStream.Write(aknBytes, 0, aknBytes.Length);    
        Debug.Log("told everyone");    
    }

    static void get_image_info(string[] param){
        string image_info = JsonUtility.ToJson(new ImageInfo {width = 2*4, height=2, channels=3, format="rgb"});
        Debug.Log(image_info);
        byte[] image_info_bytes = Encoding.UTF8.GetBytes(image_info);
        send_data(dataStream, image_info_bytes);
        //dataStream.Write(image_info_bytes, 0, image_info_bytes.Length);
        controlStream.Write(aknBytes, 0, aknBytes.Length);
        
    }

    static void get_image(string[] param){
        Debug.Log("trying to get image");
        int[,] image = agent.get_image();

        // convert image array into byte array for transfer
        int rows = image.GetLength(0);
        Debug.Log("ROWS" + rows);
        int cols = image.GetLength(1);
        int elements = rows * cols;

        // elements * 4 because each int takes up 4 bytes
        byte[] byteArray = new byte[elements * 4];

        for (int i = 0; i < rows; i++){
            for(int j = 0; j < cols; j++){
                byteArray[i*cols+j] = (byte)image[i,j];
            }
        }

        Debug.Log("trying to send video");
        videoStream.Write(byteArray, 0, byteArray.Length);
        // todo: wait for akn from interface on video stream
        Debug.Log("video sent");
        controlStream.Write(aknBytes, 0, aknBytes.Length);
        Debug.Log("done here");

    }
    
    
    private static void RunOnMainThread(Action action) {
        //---------------------------------------------------------------------
        /// <summary>
        /// Checks if the thread that called this function is already the main
        /// thread, if not, queues the action up to be handled by the main
        /// thread executor
        /// </summary>
        /// <param name="action">The action that will be run on the main thread </param>
        //---------------------------------------------------------------------
        if (SynchronizationContext.Current == mainThreadContext) {  
            // If already on main thread, execute immediately
            action();
        }
        else {
            // Otherwise, enqueue the action for the main thread
            mainThreadActions.Enqueue(action);
            mainThreadContext.Post(_ => ProcessMainThreadActions(), null);
        }

    }

    private static void ProcessMainThreadActions() {   
        //---------------------------------------------------------------------
        /// <summary>
        /// Dequeues the actions and invokes them on the main thread
        /// </summary>
        //---------------------------------------------------------------------
        while (mainThreadActions.TryDequeue(out Action action)){       
            action.Invoke();
        }

    }
    private static void createAgent(Scene scene, LoadSceneMode mode){
        //---------------------------------------------------------------------
        /// <summary>
        /// Creates an agent if he doesn't exits
        /// </summary>
        //---------------------------------------------------------------------

        GameObject agent_object = GameObject.Find("agent");
        // check if name of agent exist
        if (agent_object == null){
            agent_object = GameObject.CreatePrimitive(PrimitiveType.Cube);
            agent_object.name = "agent";
            Debug.Log("agent created");            
        }

        if(agent_object.GetComponent<Agent>() == null){
            agent_object.AddComponent<Agent>();
        }

        agent = agent_object.GetComponent<Agent>();
    }

    private static void LoadAssetBundles(string folderPath){
        //---------------------------------------------------------------------
        /// <summary>
        /// Loads the AssetBundles from the given path into a list to load
        /// scene from them.
        /// </summary>
        /// <param name="folderPath">The path to the folder where the bundles are located </param>
        //---------------------------------------------------------------------
        if(!Directory.Exists(folderPath)){
            Debug.Log("Directory does not exist: " + folderPath);
            return;
        }
        // get all file paths of directory
        string[] bundleFiles = Directory.GetFiles(folderPath);
        foreach(string filePath in bundleFiles)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(filePath);
            loadedBundles.Add(bundle);
        }

    }

}

[System.Serializable]
public class Command{
    //---------------------------------------------------------------------
    /// <summary>
    /// Class for decoding and storing json commands
    /// </summary>
    //---------------------------------------------------------------------
    public string command;
    public string[] param;
}

[System.Serializable]
public class ImageInfo
{
    public int width;
    public int height;
    public int channels;
    public string format;
}