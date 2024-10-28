using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Linq;

//mono behaviour is needed to access main thread of unity
public class Main : MonoBehaviour
{
    static private TcpListener controlSocket, videoSocket, dataSocket;
    static private TcpClient controlClient, videoClient, dataClient;
    static private NetworkStream controlStream, videoStream, dataStream;
    static private Thread socketThread;
    static bool connected;

    //actions to be run on the main thread of unity
    private static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
    //context to access the main thread from the client threads
    private static SynchronizationContext _mainThreadContext;

    static string[] commands={"echo", "change_scene"};
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void OnStart() {
        //---------------------------------------------------------------------
        /// <summary>
        /// Function that runs before at startup, saves the main thread context
        /// and starts up the socket start thread (might refactor later)
        /// </summary>
        //---------------------------------------------------------------------
        _mainThreadContext = SynchronizationContext.Current;
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
        try {      
            RunOnMainThread(() =>  {
                SceneManager.LoadScene(scene_name);
            });
        }
        catch (Exception e) {     
            Debug.Log("Couldn't load scene: " + e.Message);
        }

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
        if (SynchronizationContext.Current == _mainThreadContext) {  
            // If already on main thread, execute immediately
            action();
        }
        else {
            // Otherwise, enqueue the action for the main thread
            _mainThreadActions.Enqueue(action);
            _mainThreadContext.Post(_ => ProcessMainThreadActions(), null);
        }

    }

    private static void ProcessMainThreadActions() {   
        //---------------------------------------------------------------------
        /// <summary>
        /// Dequeues the actions and invokes them on the main thread
        /// </summary>
        //---------------------------------------------------------------------
        while (_mainThreadActions.TryDequeue(out Action action)){       
            action.Invoke();
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