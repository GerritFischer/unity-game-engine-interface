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

    static string[] commands={"echo", "change_scene", "step_simulation_without_physics"};

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
        while(true){
            //keep connection open, WIP 
        }

    }

    static void HandleDataClient(TcpClient client){
        //---------------------------------------------------------------------
        /// <summary>
        /// Handles the data socket
        /// </summary>
        /// <param name="client">The TcpClient of the data socket</param>
        //---------------------------------------------------------------------
        while(true){
            //keep connection open 
        }

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

    static void step_simulation_without_physics(param){
        float x = param[0];
        float y = param[1];
        float yaw = param[2];
        agent.move(x, y, yaw)        
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