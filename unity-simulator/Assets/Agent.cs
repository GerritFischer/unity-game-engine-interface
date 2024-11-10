using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Agent : MonoBehaviour
{
    private Camera[] cams = {null, null, null, null};
    private GameObject[] camObjects = {null, null, null, null};
    // Start is called before the first frame update
    void Awake(){
        Debug.Log("Agent class initiated");

        // set game object layer to last layer so we can remove 
        // the agent from the camera feed
        gameObject.layer = 31;
        gameObject.transform.position = new Vector3(0,0,0);

        GameObject camObject_n = new GameObject("camera_n");
        GameObject camObject_e = new GameObject("camera_e");
        GameObject camObject_w = new GameObject("camera_w");
        GameObject camObject_s = new GameObject("camera_s");

        //save Gameobject instance
        camObjects[0]=camObject_n;
        camObjects[1]=camObject_e;
        camObjects[2]=camObject_w;
        camObjects[3]=camObject_s;

        //set default settings for camera
        for (int i = 0; i<4; i++){
            //becomes child of agent
            camObjects[i].transform.parent = this.transform;
            camObjects[i].transform.position = gameObject.transform.position;
            cams[i] = camObjects[i].AddComponent<Camera>();
            cams[i].enabled = false;
            //layer 32 becomes hidden
            cams[i].cullingMask = ~(1 << 31);
            //set Camera field of View
            cams[i].fieldOfView=90.0f;
        }
        //set direction of cameras gameobject
        camObjects[0].transform.forward = Vector3.forward;
        camObjects[1].transform.forward = Vector3.right;
        camObjects[2].transform.forward = Vector3.left;
        camObjects[3].transform.forward = Vector3.back;
        //move(1f,1f,(float) (Math.PI/2));
    }

    public void move(float x ,float z, float yaw){
        //set new steps and rotation for agent
        gameObject.transform.position = new Vector3(x,0,z);
        gameObject.transform.Rotate(0.0f, (float)(yaw * 360 / (Mathf.PI * 2)), 0.0f, Space.Self);
        //set new steps and rotation for cameras
        foreach(GameObject camObject in camObjects){
            camObject.transform.position = gameObject.transform.position;
            camObject.transform.Rotate(0.0f, (float)(yaw * 360 / (Mathf.PI * 2)), 0.0f, Space.Self);
        }
    }

    Texture2D get_image(){
        //TODO: Gerrit
        return null;
    }
}
