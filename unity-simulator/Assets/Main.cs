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
using System.Globalization;

public class Main
{
    private static TcpListener controlSocket, videoSocket, dataSocket;
    private static TcpClient controlClient, videoClient, dataClient;
    private static NetworkStream controlStream, videoStream, dataStream;
    private static Thread socketThread;
    private static bool connected;

    //actions to be run on the main thread of unity
    private static readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    //context to access the main thread from the client threads
    private static SynchronizationContext mainThreadContext;
    private static byte[] aknBytes = Encoding.UTF8.GetBytes("AKN");
    private static List<AssetBundle> loadedBundles = new List<AssetBundle>();
    private static string[] commands ={"echo",
                                      "change_scene",
                                      "step_simulation_without_physics",
                                      "get_image",
                                      "get_image_info",
                                      "get_objects",
                                      "move_object",
                                      "set_illumination",
                                      "get_illumination",
                                      "stop",
                                      "get_position"};
    private static bool img_akn_recieved = false;
    private static bool data_akn_recieved = false;
    private static Agent agent;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnStart()
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Function that runs before scene load, saves the main thread context
        /// and starts up the socket start thread (might refactor later)
        /// </summary>
        //---------------------------------------------------------------------
        string path = Path.Combine(".", "addons");
        LoadAssetBundles(path);
        SceneManager.sceneLoaded += createAgent;
        mainThreadContext = SynchronizationContext.Current;
        // starts thread that initializes control, video and data socket
        // if this function is called without a new thread unity wont compile (for some reason)
        socketThread = new Thread(new ThreadStart(StartSocketProcess));
        socketThread.IsBackground = true;
        socketThread.Start();
    }

    static void StartSocketProcess()
    {
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

        while (!connected)
        {
            try
            {
                controlClient = controlSocket.AcceptTcpClient();
                Debug.Log("control client connected.");

                videoClient = videoSocket.AcceptTcpClient();
                Debug.Log("video client connected.");

                dataClient = dataSocket.AcceptTcpClient();
                Debug.Log("data client connected.");
                connected = true;
            }

            catch (Exception e)
            {
                Debug.LogError("couldn't connect because of:  " + e.Message);
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

    static void HandleControlClient(TcpClient client)
    {
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

        while ((bytesRead = controlStream.Read(buffer, 0, buffer.Length)) != 0)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log("Received: " + message);

            Command json = JsonUtility.FromJson<Command>(message);
            string command = json.command;
            string[] param = json.param;
            Debug.Log(param.GetType());
            Debug.Log($"Command: {command} with parameters: {string.Join(",", param)}");

            if (commands.Contains(command))
            {
                Type type = typeof(Main);
                MethodInfo method = type.GetMethod(command, BindingFlags.Static | BindingFlags.NonPublic);
                method.Invoke(null, new object[] { param });
                Debug.Log($"Command: {command} is done");
            }
            else
            {
                Debug.Log($"Command: {command} is not defined");
            }
        }

    }
    static void HandleVideoClient(TcpClient client)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Handles the video socket
        /// </summary>
        /// <param name="client">The TcpClient of the video socket</param>
        //---------------------------------------------------------------------
        videoStream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = videoStream.Read(buffer, 0, buffer.Length)) != 0)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log("Received on video: " + message);
            img_akn_recieved = true;
        }

    }

    static void HandleDataClient(TcpClient client)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Handles the data socket
        /// </summary>
        /// <param name="client">The TcpClient of the data socket</param>
        //---------------------------------------------------------------------
        dataStream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = dataStream.Read(buffer, 0, buffer.Length)) != 0)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log("Received on data: " + message);
            data_akn_recieved = true;
        }

    }

    static void send_data(NetworkStream stream, byte[] data)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// sends data via the given socket and adds end of data bytes at the end
        /// </summary>
        /// <param name="stream">The stream which the data will be written to</param>
        /// <param name="data">The data that will be send</param>
        //---------------------------------------------------------------------
        data = data.Concat(Encoding.UTF8.GetBytes("!unityservereod!")).ToArray();
        stream.Write(data, 0, data.Length);
    }


    static void echo(string[] param)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Recieves a message as first entry of param array, sends it back
        /// </summary>
        /// <param name="param">The array of parameters</param>
        //---------------------------------------------------------------------
        byte[] responseData = Encoding.UTF8.GetBytes(param[0]);
        controlStream.Write(responseData, 0, responseData.Length);
    }

    static void change_scene(string[] param)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Changes the scene to the scene with the name given in param[0]
        /// </summary>
        /// <param name="param">The array of parameters</param>
        //---------------------------------------------------------------------
        string scene_name = param[0];
        RunOnMainThread(() =>
        {
            try
            {
                SceneManager.LoadScene(scene_name);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }
            controlStream.Write(aknBytes, 0, aknBytes.Length);
        });

    }
    static void move_object(string[] param)
    {//---------------------------------------------------------------------
        /// <summary>
        /// Changes the scene to the scene with the name given in param[0]
        /// </summary>
        /// <param name="param">array contains id and rotation and position</param>
        //---------------------------------------------------------------------

        //converts sended string to float
        float pos_x = float.Parse(param[1], CultureInfo.InvariantCulture);
        float pos_y = float.Parse(param[2], CultureInfo.InvariantCulture);
        float pos_z = float.Parse(param[3], CultureInfo.InvariantCulture);
        float rot_x = float.Parse(param[4], CultureInfo.InvariantCulture);
        float rot_y = float.Parse(param[5], CultureInfo.InvariantCulture);
        float rot_z = float.Parse(param[6], CultureInfo.InvariantCulture);

        //waiting till the main Thread is finished
        ManualResetEventSlim mainFin = new ManualResetEventSlim(false);

        RunOnMainThread(() =>
        {
            //search for id
            GameObject current_object = GameObject.Find("/" + param[0]);
            if (current_object != null)
            {
                //transform the position and rotation
                current_object.transform.localPosition = new Vector3(pos_x, pos_y, pos_z);
                current_object.transform.localRotation *= Quaternion.Euler(rot_x, rot_y, rot_z);
            }
            else
            {
                Debug.Log("Object does not exist");
            }
            mainFin.Set();
        });
        mainFin.Wait();
        controlStream.Write(aknBytes, 0, aknBytes.Length);
    }

    static void step_simulation_without_physics(string[] param)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Invokes the move method of the agent and steps simulation
        /// </summary>
        /// <param name="param">The array of parameters</param>
        //---------------------------------------------------------------------
        float x = float.Parse(param[0], CultureInfo.InvariantCulture);
        float y = float.Parse(param[1], CultureInfo.InvariantCulture);
        float yaw = float.Parse(param[2], CultureInfo.InvariantCulture);

        //waiting till the main Thread is finished
        ManualResetEventSlim mainFin = new ManualResetEventSlim(false);
        RunOnMainThread(() =>
        {
            agent.move(x, y, yaw);
            mainFin.Set();
        });
        mainFin.Wait();
        controlStream.Write(aknBytes, 0, aknBytes.Length);
    }

    static void get_image_info(string[] param)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Retrieves the format data of the image
        /// </summary>
        /// <param name="param">The array of parameters</param>
        //---------------------------------------------------------------------
        string image_info = JsonUtility.ToJson(new ImageInfo { width = agent.get_image_info()[0], height = agent.get_image_info()[1], channels = 3, format = "rgb" });
        byte[] image_info_bytes = Encoding.UTF8.GetBytes(image_info);
        send_data(dataStream, image_info_bytes);
        // wait for akn for data socket 
        while (!data_akn_recieved) { };
        data_akn_recieved = false;
        controlStream.Write(aknBytes, 0, aknBytes.Length);
    }

    static void get_image(string[] param)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Retrieves the image the agent is currently seeing
        /// </summary>
        /// <param name="param">The array of parameters</param>
        //---------------------------------------------------------------------
        byte[,] image = new byte[1,1];
        //waiting till the main Thread is finished
        ManualResetEventSlim mainFin = new ManualResetEventSlim(false);
        RunOnMainThread(() =>
        {
            image = agent.get_image();
            mainFin.Set();
        });
        mainFin.Wait();
        // convert image array into byte array for transfer
        int rows = image.GetLength(0);
        int cols = image.GetLength(1);
        int elements = rows * cols;
        // converting the 2d pixel array to a normal 1d array
        // so it can be sent as a byte string
        byte[] image_as_array = new byte[elements];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                image_as_array[i * cols + j] = image[i, j];
            }
        }
        send_data(videoStream, image_as_array);
        //wait for acknowledge
        while (!img_akn_recieved) { };
        img_akn_recieved = false;
        controlStream.Write(aknBytes, 0, aknBytes.Length);

    }


    private static void RunOnMainThread(Action action)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Checks if the thread that called this function is already the main
        /// thread, if not, queues the action up to be handled by the main
        /// thread executor
        /// </summary>
        /// <param name="action">The action that will be run on the main thread </param>
        //---------------------------------------------------------------------
        if (SynchronizationContext.Current == mainThreadContext)
        {
            // If already on main thread, execute immediately
            action();
        }
        else
        {
            // Otherwise, enqueue the action for the main thread
            mainThreadActions.Enqueue(action);
            mainThreadContext.Post(_ => ProcessMainThreadActions(), null);
        }

    }

    private static void ProcessMainThreadActions()
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Dequeues the actions and invokes them on the main thread
        /// </summary>
        //---------------------------------------------------------------------
        while (mainThreadActions.TryDequeue(out Action action))
        {
            action.Invoke();
        }

    }
    private static void createAgent(Scene scene, LoadSceneMode mode)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Creates an agent if he doesn't exits
        /// </summary>
        //---------------------------------------------------------------------

        GameObject agent_object = GameObject.Find("agent");
        // check if name of agent exist
        if (agent_object == null)
        {
            agent_object = GameObject.CreatePrimitive(PrimitiveType.Cube);
            agent_object.name = "agent";
            Debug.Log("agent created");
        }

        if (agent_object.GetComponent<Agent>() == null)
        {
            agent_object.AddComponent<Agent>();
        }

        agent = agent_object.GetComponent<Agent>();
    }

    private static void LoadAssetBundles(string folderPath)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// Loads the AssetBundles from the given path into a list to load
        /// scene from them.
        /// </summary>
        /// <param name="folderPath">The path to the folder where the bundles are located </param>
        //---------------------------------------------------------------------
        if (!Directory.Exists(folderPath))
        {
            Debug.Log("Directory does not exist: " + folderPath);
            return;
        }
        // get all file paths of directory
        string[] bundleFiles = Directory.GetFiles(folderPath);
        foreach (string filePath in bundleFiles)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(filePath);
            loadedBundles.Add(bundle);
        }

    }

    private static void get_illumination(string[] param)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// retrievees the color of a specified light source
        /// </summary>
        /// <param name="param"> The array of parameters </param>
        //---------------------------------------------------------------------
        float[] color = null;
        ManualResetEventSlim mainFin = new ManualResetEventSlim(false); //waiting till the main Thread is finished
        RunOnMainThread(() =>
        {
            // access to Game Objects and their data can only be on main Thread
            GameObject lightGameObject = GameObject.Find(param[0]);
            if (lightGameObject.GetComponent<Light>() != null)
            {
                Light light = lightGameObject.GetComponent<Light>();
                color = new float[] { light.color.r, light.color.g, light.color.b };
            }
            else
            {
                Debug.Log("Object has no Light source");
            }
            mainFin.Set();
        });

        mainFin.Wait();
        IllumiInfo illumiInfo = new IllumiInfo();
        illumiInfo.colors = color;
        string illumiInfoString = JsonUtility.ToJson(illumiInfo);
        byte[] all_info_bytes = Encoding.UTF8.GetBytes(illumiInfoString);
        send_data(dataStream, all_info_bytes);
        while (!data_akn_recieved) { };
        data_akn_recieved = false;
        controlStream.Write(aknBytes, 0, aknBytes.Length);
    }

    private static void get_objects(string[] param)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// gets all the Game Objects in the current unity scene and sends them back in a Dictionary
        /// </summary>
        /// <param name="param"> The array of parameters </param>
        //---------------------------------------------------------------------
        GameObject[] all_Objects = null;
        ObjectInfo objectInfo = new ObjectInfo();
        ManualResetEventSlim mainFin = new ManualResetEventSlim(false); //waiting till the main Thread is finished
        RunOnMainThread(() =>
        {    //FindObjectsOfType and getter for Game Object can only run on main Thread

            all_Objects = GameObject.FindObjectsOfType<GameObject>();
            List<string> keyList = new List<string>();
            List<string> nameList = new List<string>();
            List<string> valueList = new List<string>();
            List<short> propertyList = new List<short>();
            List<(System.Type, short) > compCheck = new(){
                (typeof(Light), (1 << 0)),       //Bit 0 for Lightsource
            	(typeof(Camera), (1 << 1)),      //Bit 1 for camera 
                (typeof(MeshRenderer), (1 << 2)) //Bit 2 for geometric Forms 
                //can be expanded when nescessary
            }; 

            foreach(GameObject currObject in all_Objects){
                short properties = 0;
                foreach (var (type, bitmask) in compCheck){
                    if(currObject.GetComponent(type) != null){
                        properties |= bitmask;
                    }
                }
                keyList.Add(currObject.GetInstanceID().ToString());
                nameList.Add(currObject.name);
                valueList.Add(currObject.GetType().ToString());
                propertyList.Add(properties);
            }
            objectInfo.properties = propertyList.ToArray();
            objectInfo.keys = keyList.ToArray();
            objectInfo.names = nameList.ToArray();
            objectInfo.values = valueList.ToArray();
            mainFin.Set();
        });
        mainFin.Wait();
        //main thread is finished now turning it into bytes
        string all_info_string = JsonUtility.ToJson(objectInfo);
        byte[] all_info_bytes = Encoding.UTF8.GetBytes(all_info_string);
        string bytesize = all_info_bytes.Length.ToString();    
        byte[] all_info_bytes_length = Encoding.UTF8.GetBytes(bytesize);

        send_data(dataStream, all_info_bytes_length);
        while (!data_akn_recieved) { };
        data_akn_recieved = false;
        send_data(dataStream, all_info_bytes);
        //aknowledgement for sending both
        while (!data_akn_recieved) { };
        data_akn_recieved = false;
        controlStream.Write(aknBytes, 0, aknBytes.Length);

    }
    private static void get_position(string[] param)
    {
        //---------------------------------------------------------------------
        /// <summary>
        /// get the Id of a object and sends the pose back as array
        /// </summary>
        /// <param name="param"> The array of parameters </param>
        //---------------------------------------------------------------------


        //init pose
        float[] coor = new float[] { float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN };

        //waiting till the main Thread is finished
        ManualResetEventSlim mainFin = new ManualResetEventSlim(false);

        RunOnMainThread(() =>
        {
            GameObject current_object = GameObject.Find("/" + param[0]);
            if (current_object != null)
            {
                //get the pose
                coor[0] = current_object.transform.localPosition.x;
                coor[1] = current_object.transform.localPosition.z;
                coor[2] = current_object.transform.localPosition.y;

                //convert Quanterion into EulerAngle
                Vector3 rotation = current_object.transform.localRotation.eulerAngles;
                coor[3] = rotation.x;
                coor[4] = rotation.y;
                coor[5] = rotation.z;
            }
            mainFin.Set();
        });
        mainFin.Wait();
        //init new class
        Pose pose = new Pose();
        pose.poses = coor;
        //convert to Json
        string poseInfo = JsonUtility.ToJson(pose);
        byte[] all_info_bytes = Encoding.UTF8.GetBytes(poseInfo);
        send_data(dataStream, all_info_bytes);
        while (!data_akn_recieved) { };
        data_akn_recieved = false;
        controlStream.Write(aknBytes, 0, aknBytes.Length);
    }

    private static void stop(string[] param){
        //---------------------------------------------------------------------
        /// <summary>
        /// This function will stop the unity simulator. It will not send 
        /// back an acknowledgement.
        /// </summary>
        /// <param name="param"> The array of parameters </param>
        //---------------------------------------------------------------------
        RunOnMainThread(() => {
            Application.Quit();
        });  
    }
}

[System.Serializable]
public class Command
{
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
    //---------------------------------------------------------------------
    /// <summary>
    /// Class for storing image info
    /// </summary>
    //---------------------------------------------------------------------
    public int width;
    public int height;
    public int channels;
    public string format;
}

[System.Serializable]

public class ObjectInfo
{
    //---------------------------------------------------------------------
    /// <summary>
    /// Class for storing object info
    /// </summary>
    //---------------------------------------------------------------------
    public string[] keys;
    public string[] names;
    public string[] values;
    public short[] properties;
}

[System.Serializable]
public class IllumiInfo
{
    //---------------------------------------------------------------------
    /// <summary>
    /// Class for storing color info of light
    /// </summary>
    //---------------------------------------------------------------------
    public float[] colors;
}
[System.Serializable]
public class Pose
{
    //---------------------------------------------------------------------
    /// <summary>
    /// Class for storing position and rotation
    /// </summary>
    //---------------------------------------------------------------------
    public float[] poses;
}