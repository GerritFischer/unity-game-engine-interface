using System;
using UnityEngine;
using UnityEditor;

//when activated and in Unity scene, assets will create object with SocketFile 

public class SocketFileInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeSocketFile()
    {
        if(GameObject.FindObjectOfType<SocketFile>() == null){
          
          
             // Erstelle ein neues GameObject für den MyListener
        GameObject listenerObject = new GameObject("SocketFile");
        listenerObject.AddComponent<SocketFile>();
        }

    }
}