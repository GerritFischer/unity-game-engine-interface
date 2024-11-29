using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Agent : MonoBehaviour
{
    private Camera[] cams = {null, null, null, null};
    private GameObject[] camObjects = {null, null, null, null};

    private const int IMAGE_HEIGHT = 256;
    private const int IMAGE_WIDTH = 256;

    private const int FINAL_WIDTH = IMAGE_WIDTH * 4;
    // Start is called before the first frame update
    void Awake(){
        //---------------------------------------------------------------------
        /// <summary>
        /// Initiates the agent class, create 4 child objects with camera components
        /// and set various properties of the cameras
        /// </summary>
        //---------------------------------------------------------------------
        Debug.Log("Agent class initiated");

        // set game object layer to last layer so we can remove 
        // the agent from the camera feed
        gameObject.layer = 31;
        gameObject.transform.localPosition = new Vector3(0,0,0);

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
        RenderTexture renderTexture = new RenderTexture(256,256, 16); 
        for (int i = 0; i<4; i++){
            //becomes child of agent
            camObjects[i].transform.parent = this.transform;
            //place at the center of parent
            camObjects[i].transform.localPosition = new Vector3(0,0,0);
            cams[i] = camObjects[i].AddComponent<Camera>();
            cams[i].enabled = false;
            //layer 32 becomes hidden
            cams[i].cullingMask = ~(1 << 31);
            //set Camera field of View
            cams[i].fieldOfView=90.0f;
            //set render texture of cams
            cams[i].targetTexture = renderTexture;
        }
        //set direction of cameras gameobject
        camObjects[0].transform.forward = Vector3.forward;
        camObjects[1].transform.forward = Vector3.right;
        camObjects[2].transform.forward = Vector3.back;
        camObjects[3].transform.forward = Vector3.left;
        //move(1f,1f,90.0f);
    }

    public void move(float x ,float z, float yaw){
        //---------------------------------------------------------------------
        /// <summary>
        /// Moves the agent into a desired pose
        /// </summary>
        /// <param name="x"=Coordinate of the x-axis to teleport to</param>
        /// <param name="z"=Coordinate of the z-axis to teleport to</param>
        /// <param name="yaw"=Rotation of the agent in degree</param>
        //---------------------------------------------------------------------

        //set new steps and rotation for agent
        gameObject.transform.position = new Vector3(x,0,z);
        gameObject.transform.localRotation *= Quaternion.Euler(0, yaw, 0);
    }

    public int[] get_image_info(){
        //---------------------------------------------------------------------
        /// <summary>
        /// Returns the width and height of the final image
        /// </summary>
        //---------------------------------------------------------------------
        return new int[2]{FINAL_WIDTH, IMAGE_HEIGHT};
    }

    public byte[,] get_image(){
        //---------------------------------------------------------------------
        /// <summary>
        /// Retrieves the current images from all 4 cameras and converts them into
        /// one big image. Returns a 2d byte array with the pixels and corresponding
        /// RGB values in the range of 0-255.
        /// </summary>
        //---------------------------------------------------------------------
        
        // retrieve single images
        byte[,,] images = new byte[4, IMAGE_HEIGHT*FINAL_WIDTH, 3];
        for(int i = 0; i < 4; i++){
            RenderTexture.active = cams[i].targetTexture;
            cams[i].Render();
            Texture2D image_raw = new Texture2D(cams[i].targetTexture.width, cams[i].targetTexture.height);
            image_raw.ReadPixels(new Rect(0,0, cams[i].targetTexture.width, cams[i].targetTexture.height), 0,0);
            image_raw.Apply();
            Color[] colors = image_raw.GetPixels();
            for(int x = 0; x < IMAGE_WIDTH; x++){
                for(int y = 0; y < IMAGE_HEIGHT; y++){
                    Color c = colors[y*IMAGE_WIDTH + x];
                    images[i, y*IMAGE_WIDTH+x, 0] = (byte)Mathf.RoundToInt(c.r * 255);
                    images[i, y*IMAGE_WIDTH+x, 1] = (byte)Mathf.RoundToInt(c.g * 255);
                    images[i, y*IMAGE_WIDTH+x, 2] = (byte)Mathf.RoundToInt(c.b * 255);
                }
            }
            Destroy(image_raw);
        }  
        // convert into one image
        byte[,] finalImage = new byte[FINAL_WIDTH * IMAGE_HEIGHT, 3];
        for(int x = 0; x < FINAL_WIDTH; x++){
            for(int y = 0; y < IMAGE_HEIGHT; y++){
                int index = (int) Math.Floor((float) x / IMAGE_WIDTH);
                finalImage[y*FINAL_WIDTH+x,0] = images[index, IMAGE_WIDTH * IMAGE_HEIGHT - IMAGE_WIDTH - y*IMAGE_WIDTH + (x % IMAGE_WIDTH),0];
                finalImage[y*FINAL_WIDTH+x,1] = images[index,IMAGE_WIDTH * IMAGE_HEIGHT - IMAGE_WIDTH - y*IMAGE_WIDTH + (x % IMAGE_WIDTH),1];
                finalImage[y*FINAL_WIDTH+x,2] = images[index,IMAGE_WIDTH * IMAGE_HEIGHT - IMAGE_WIDTH - y*IMAGE_WIDTH + (x % IMAGE_WIDTH),2];
            }
        }
        return finalImage;
    }
}
