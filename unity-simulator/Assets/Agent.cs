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

    public int[,] get_image(){
        
        int final_height = 2;
        int final_width = 2 * 4;
        const int width = 2;

        int[,] finalImage = new int[final_width * final_height, 3];
        int[,] image_n = {{255,255,0}, {255,255,0}, {255,255,0}, {255,255,0}};
        int[,] image_o = {{255,0,255}, {255,0,255}, {255,0,255}, {255,0,255}};
        int[,] image_s = {{0,255,255}, {0,255,255}, {0,255,255}, {0,255,255}};
        int[,] image_w = {{255,255,255}, {255,255,255}, {255,255,255}, {255,255,255}};

        for(int x = 0; x < final_width; x++){
            for(int y = 0; y < final_height; y++){
                int[] pixel = new int[3];
                switch(x){
                    case < width * 1:
                        pixel[0] = image_n[y*width + (x % width),0];
                        pixel[1] = image_n[y*width + (x % width),1];
                        pixel[2] = image_n[y*width + (x % width),2];
                    break; 
                    case < width * 2:
                        pixel[0] = image_o[y*width + (x % width),0];
                        pixel[1] = image_o[y*width + (x % width),1];
                        pixel[2] = image_o[y*width + (x % width),2];
                        
                    break;
                    case < width * 3:
                        pixel[0] = image_s[y*width + (x % width),0];
                        pixel[1] = image_s[y*width + (x % width),1];
                        pixel[2] = image_s[y*width + (x % width),2];
                    break;    
                    case < width * 4:
                        pixel[0] = image_w[y*width + (x % width),0];
                        pixel[1] = image_w[y*width + (x % width),1];
                        pixel[2] = image_w[y*width + (x % width),2];
                    break;               
                }
                finalImage[y*final_width+x,0] = pixel[0];
                finalImage[y*final_width+x,1] = pixel[1];
                finalImage[y*final_width+x,2] = pixel[2];
            }
        }
        
        foreach (int item in finalImage){
            Debug.Log(item); 
        }
        return finalImage;
    }
}
