using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

//This class creates 3 Sockets for Control, video and data, only the control socket while loops for recieving data at the moment 
public class SocketFile : MonoBehaviour
{
    private TcpListener socket_control, socket_video, socket_data;
    private TcpClient controlClient, videoClient, dataClient;
    private NetworkStream controlStream, videoStream, dataStream;
    private Thread controlThread, videoThread, dataThread;
    private static SocketFile instance;
    
    void Start(){
        controlThread = new Thread(new ThreadStart(StartControlSocket));
        controlThread.IsBackground = true;
        controlThread.Start();

        videoThread = new Thread(new ThreadStart(StartVideoSocket));
        videoThread.IsBackground = true;
        videoThread.Start();

        dataThread = new Thread(new ThreadStart(StartDataSocket));
        dataThread.IsBackground = true;
        dataThread.Start();
    }

    void StartControlSocket(){

        try{
                socket_control = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
                socket_control.Start();
                Debug.Log("control Server gestartet, wartet auf Verbindungen...");

                while(true)
                {
                    controlClient = socket_control.AcceptTcpClient();
                    Debug.Log("control Client verbunden.");


                    controlStream = controlClient.GetStream();
                    byte[] receivedBuffer = new byte[1024];
                    
                    try{

                        while(controlClient.Connected){

                            if (controlStream.DataAvailable){

                                int bytesRead = controlStream.Read(receivedBuffer, 0, receivedBuffer.Length);
                                if (bytesRead == 0)
                                {
                                    // Client hat die Verbindung geschlossen
                                    Debug.Log("control Client hat die Verbindung geschlossen.");
                                    break;
                                }

                                string message = Encoding.UTF8.GetString(receivedBuffer, 0, bytesRead);
                                Debug.Log("Empfangen: " + message);

                                // Nachricht zurücksenden (Echo)
                                byte[] responseData = Encoding.UTF8.GetBytes(message);
                                controlStream.Write(responseData, 0, responseData.Length);
                                Debug.Log("Gesendet: " + message);
                            }

                        }
                    }
                     catch (SocketException se)
                    {
                        // Erkennen von Verbindungsabbruch durch harte Beendigung des Clients
                        Debug.LogError("SocketException: Client hat die Verbindung abrupt geschlossen. " + se.Message);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Fehler bei der Datenübertragung: " + e.Message);
                    }
                }

        }
        catch (Exception e)
        {
            Debug.LogError("Fehler im Server: " + e.Message);
        }
    }

    void StartVideoSocket(){
         try{
                socket_video = new TcpListener(IPAddress.Parse("127.0.0.1"), 5001);
                socket_video.Start();
                Debug.Log("video Server gestartet, wartet auf Verbindungen...");

                while(true)
                {
                    videoClient = socket_video.AcceptTcpClient();
                    Debug.Log("video Client verbunden.");

                    //would send video data to smth 
                }
            }
            catch (Exception e)
        {
            Debug.LogError("Fehler im Server: " + e.Message);
        }

    }

    void StartDataSocket(){
        try{
                socket_data = new TcpListener(IPAddress.Parse("127.0.0.1"), 5002);
                socket_data.Start();
                Debug.Log("data Server gestartet, wartet auf Verbindungen...");

                while(true)
                {
                    dataClient = socket_data.AcceptTcpClient();
                    Debug.Log("data Client verbunden.");

                    //would send data to smth 
                }
            }
            catch (Exception e)
        {
            Debug.LogError("Fehler im Server: " + e.Message);
        }

    }



}

