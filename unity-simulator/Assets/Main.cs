using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Linq;

public class Main 
{
    static private TcpListener controlSocket, videoSocket, dataSocket;
    static private TcpClient controlClient, videoClient, dataClient;
    static private NetworkStream controlStream, videoStream, dataStream;
    static private Thread socketThread;
    static bool connected;
    static string[] commands={"echo"};
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void OnStart()
    {
        // starts thread that initializes control, video and data socket
        socketThread = new Thread(new ThreadStart(StartSocketProcess));
        socketThread.IsBackground = true;
        socketThread.Start();
    }

    static void StartSocketProcess(){

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
        ///<summary>
        ///<param name="client">The TcpClient of the control socket</param>
        ///</summary>

        controlStream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;
         
        while ((bytesRead = controlStream.Read(buffer, 0, buffer.Length)) != 0){
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log("Received: " + message);

            Command json = JsonUtility.FromJson<Command>(message);
            string command= json.command;
            string [] param = json.param;
            Debug.Log($"Command: {command} with parameters: {string.Join(",", param)}");

            if (commands.Contains(command)){
                Type type = typeof(MyListenerInitializer);
                MethodInfo method = type.GetMethod(command, BindingFlags.Static | BindingFlags.NonPublic);
                method.Invoke(null, param);
            }
            else{
                Debug.Log($"Command:{command} is not defined");
            }
        }

    }
    static void HandleVideoClient(TcpClient client){
        ///<summary>
        ///<param name="client">The TcpClient of the video socket</param>
        ///</summary>
        while(true){
            //keep connection open 
        }

    }

    static void HandleDataClient(TcpClient client){
        ///<summary>
        ///<param name="client">The TcpClient of the data socket</param>
        ///</summary>
        while(true){
            //keep connection open 
        }

    }

    static void echo(string[] param){
        byte[] responseData = Encoding.UTF8.GetBytes(param[0]);
        controlStream.Write(responseData, 0, responseData.Length);
    }

}

[System.Serializable]
public class Command{
    public string command;
    public string[] param;
}