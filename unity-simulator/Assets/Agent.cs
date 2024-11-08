using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    private Camera[] cams = {null, null, null, null};
    // Start is called before the first frame update
    void Awake(){
        Debug.Log("Agent class initiated");

        // set game object layer to last layer so we can remove 
        // the agent from the camera feed
        gameObject.layer = 31;
        gameObject.transform.position= new Vector3(0,1,0);
        
    }

    public void move(float x, float y, float yaw){
        //TODO: Tommy
    }

    Texture2D get_image(){
        //TODO: Gerrit
        return null;
    }
}
