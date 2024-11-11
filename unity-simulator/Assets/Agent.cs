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
        GameObject camObject_s = new GameObject("camera_s");
        GameObject camObject_w = new GameObject("camera_w");

        //save Gameobject instance
        camObjects[0]=camObject_n;
        camObjects[1]=camObject_e;
        camObjects[2]=camObject_s;
        camObjects[3]=camObject_w;

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
        camObjects[2].transform.forward = Vector3.back;
        camObjects[3].transform.forward = Vector3.left;
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

    public int[,] get_image(){
        
        int final_height = 1080;
        const int width = 1920;
        int final_width = width * 4;
        
        int[,,] images = new int[4, final_height*width, 3];
        for(int i = 0; i < 4; i++){
            RenderTexture renderT = new RenderTexture(1920,1080, 16); 
            cams[i].targetTexture = renderT;
            var currentRT = RenderTexture.active;
            RenderTexture.active = cams[i].targetTexture;
            cams[i].Render();
            Texture2D image_raw = new Texture2D(cams[i].targetTexture.width, cams[i].targetTexture.height);
            image_raw.ReadPixels(new Rect(0,0, cams[i].targetTexture.width, cams[i].targetTexture.height), 0,0);
            image_raw.Apply();
            RenderTexture.active = currentRT;
            Color[] colors = image_raw.GetPixels();
            for(int x = 0; x < width; x++){
                for(int y = 0; y < final_height; y++){
                    Color c = colors[y*width + x];

                    images[i, y*width+x, 0] = Mathf.RoundToInt(c.r * 255);
                    images[i, y*width+x, 1] = Mathf.RoundToInt(c.g * 255);
                    images[i, y*width+x, 2] = Mathf.RoundToInt(c.b * 255);
                }
            }
        }
        
    
    
        int[,] finalImage = new int[final_width * final_height, 3];
        int[,] image_n = {
            {255, 255, 0}, {255, 255, 0}, {255, 255, 0}, {255, 255, 0},
            {255, 255, 0}, {255, 255, 0}, {255, 255, 0}, {255, 255, 0},
            {255, 255, 0}, {255, 255, 0}, {255, 255, 0}, {255, 255, 0},
            {255, 255, 0}, {255, 255, 0}, {255, 255, 0}, {255, 255, 0}
        };

        int[,] image_o = {
            {255, 0, 255}, {255, 0, 255}, {255, 0, 255}, {255, 0, 255},
            {255, 0, 255}, {255, 0, 255}, {255, 0, 255}, {255, 0, 255},
            {255, 0, 255}, {255, 0, 255}, {255, 0, 255}, {255, 0, 255},
            {255, 0, 255}, {255, 0, 255}, {255, 0, 255}, {255, 0, 255}
        };

        int[,] image_s = {
            {0, 255, 255}, {0, 255, 255}, {0, 255, 255}, {0, 255, 255},
            {0, 255, 255}, {0, 255, 255}, {0, 255, 255}, {0, 255, 255},
            {0, 255, 255}, {0, 255, 255}, {0, 255, 255}, {0, 255, 255},
            {0, 255, 255}, {0, 255, 255}, {0, 255, 255}, {0, 255, 255}
        };

        int[,] image_w = {
            {255, 255, 255}, {255, 255, 255}, {255, 255, 255}, {255, 255, 255},
            {255, 255, 255}, {255, 255, 255}, {255, 255, 255}, {255, 255, 255},
            {255, 255, 255}, {255, 255, 255}, {255, 255, 255}, {255, 255, 255},
            {255, 255, 255}, {255, 255, 255}, {255, 255, 255}, {255, 255, 255}
        };

        for(int x = 0; x < final_width; x++){
            for(int y = 0; y < final_height; y++){
                int[] pixel = new int[3];
                switch(x){
                    case < width * 1:
                        pixel[0] = images[0, y*width + (x % width),0];
                        pixel[1] = images[0,y*width + (x % width),1];
                        pixel[2] = images[0,y*width + (x % width),2];
                    break; 
                    case < width * 2:
                        pixel[0] = images[1,y*width + (x % width),0];
                        pixel[1] = images[1,y*width + (x % width),1];
                        pixel[2] = images[1,y*width + (x % width),2];
                        
                    break;
                    case < width * 3:
                        pixel[0] = images[2,y*width + (x % width),0];
                        pixel[1] = images[2,y*width + (x % width),1];
                        pixel[2] = images[2,y*width + (x % width),2];
                    break;    
                    case < width * 4:
                        pixel[0] = images[3,y*width + (x % width),0];
                        pixel[1] = images[3,y*width + (x % width),1];
                        pixel[2] = images[3,y*width + (x % width),2];
                    break;               
                }
                finalImage[y*final_width+x,0] = pixel[0];
                finalImage[y*final_width+x,1] = pixel[1];
                finalImage[y*final_width+x,2] = pixel[2];
            }
        }
        Debug.Log("new image generated");
        return finalImage;
    }
}
