using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class procMain : MonoBehaviour
{
    public Material procMat;
    public Material rawMat;
    public Material rawDepthMat;

    nuitrack.ColorSensor rgbCam;
    nuitrack.ColorFrame rgbFrameRaw;

    nuitrack.DepthSensor depthCam;
    nuitrack.DepthFrame depthFrameRaw;

    Texture2D rgbTexRaw;
    Color[] rgbColRaw;

    Texture2D rgbTexProc;
    Color[] rgbColProc;

    Texture2D depthTexRaw;
    Color[] depthColRaw;

    Texture2D depthTexProc;
    Color[] depthColProc;

    Color[] accumulator;

    // Start is called before the first frame update
    void Start()
    {

        rgbCam = nuitrack.ColorSensor.Create();
        depthCam = nuitrack.DepthSensor.Create();

        //... debug resolutions of camera

        nuitrack.OutputMode rgbMode = rgbCam.GetOutputMode();

        int cWidth = rgbMode.XRes;
        int cHeight = rgbMode.YRes;

        Debug.Log(cWidth);
        Debug.Log(cHeight);

        nuitrack.OutputMode depthMode = depthCam.GetOutputMode();

        int dWidth = depthMode.XRes;
        int dHeight = depthMode.YRes;

        Debug.Log(dWidth);
        Debug.Log(dHeight);

        //... initialze buffer arrays

        rgbColRaw = new Color[cWidth * cHeight];
        rgbColProc = new Color[cWidth * cHeight];

        depthColRaw = new Color[dWidth * dHeight];
        depthColProc = new Color[dWidth * dHeight];

        accumulator = new Color[cWidth * cHeight];

        // set accumultor to blank (transparent + white);

        int k = 0;

        for (int i = 0; i < cWidth; i++)
        {
            for (int j = 0; j < cHeight; j++)
            {
                accumulator[k].r = 0.0f;
                accumulator[k].g = 0.0f;
                accumulator[k].b = 0.0f;
                accumulator[k].a = 0.0f;

                k++;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // get color and depth frames
        rgbFrameRaw = rgbCam.GetColorFrame();
        depthFrameRaw = depthCam.GetDepthFrame();

        // processing depth frame
        if (depthFrameRaw != null)
        {
            depthTexRaw = depthFrameRaw.ToTexture2D();
            //depthColRaw = depthTexRaw.GetPixels();

            int w = depthTexRaw.width;
            int h = depthTexRaw.height;

            int k = 0;

            depthTexProc = new Texture2D(w, h, TextureFormat.RFloat, false);

            for (int i = 0; i < h; i++)
            {
                for(int j = 0; j < w; j++)
                {
                    int curDepth = depthFrameRaw[i, j];

                    if(curDepth == 0)
                    {
                        curDepth = 5000;
                    }

                    // scale depth output
                    depthColProc[k].r = 1.0f - (float)curDepth / 5000.0f;

                    k++;
                }
            }

            // Debug.Log(depthFrameRaw[h/2, w/2]);

            // set depth colors
            depthTexRaw.SetPixels(depthColProc);
            depthTexRaw.Apply();

            rawDepthMat.mainTexture = depthTexRaw;
        }

        // process rgb frame (ghosty)
        if(rgbFrameRaw!= null)
        {
            rgbTexRaw = rgbFrameRaw.ToTexture2D();
            rgbColRaw = rgbTexRaw.GetPixels();

            //... loop through all pixels

            int k = 0;

            int w = rgbTexRaw.width;
            int h = rgbTexRaw.height;

            rgbTexProc = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    float exposure = 0.05f;

                    //... grayscale + accumulation
                    float gray = (rgbColRaw[k].r + rgbColRaw[k].g + rgbColRaw[k].b) / 3.0f;
                    float alpha = depthColProc[k].r * 2.0f - 1.0f;


                    accumulator[k].r = accumulator[k].r * (1.0f - exposure) + gray * exposure;
                    accumulator[k].g = accumulator[k].g * (1.0f - exposure) + gray * exposure;
                    accumulator[k].b = accumulator[k].b * (1.0f - exposure) + gray * exposure;
                    accumulator[k].a = accumulator[k].a * (1.0f - exposure) + alpha * exposure;

                    rgbColProc[k].r = accumulator[k].r;
                    rgbColProc[k].g = accumulator[k].g;
                    rgbColProc[k].b = accumulator[k].b;
                    // rgbColProc[k].a = 1.0f;

                    rgbColProc[k].a = accumulator[k].a;

                    //...

                    ////... grayscale + decay
                    //float gray = (rgbColRaw[k].r + rgbColRaw[k].g + rgbColRaw[k].b) / 3.0f;
                    //float alpha = depthColProc[k].r;
                    //float decay = 0.8f;

                    //// implement decay
                    //accumulator[k].r *= decay;
                    //accumulator[k].g *= decay;
                    //accumulator[k].b *= decay;
                    //accumulator[k].a *= decay;

                    //// add current values
                    //accumulator[k].r += gray;
                    //accumulator[k].g += gray;
                    //accumulator[k].b += gray;
                    //accumulator[k].a += alpha;

                    //// compare current values with accumulated value
                    //if (alpha > accumulator[k].a)
                    //{
                    //    accumulator[k].a = accumulator[k].a * (1.0f - exposure) + alpha * exposure;
                    //}

                    //if(gray > accumulator[k].r)
                    //{
                    //    accumulator[k].r = accumulator[k].r * (1.0f - exposure) + gray * exposure;
                    //    accumulator[k].g = accumulator[k].g * (1.0f - exposure) + gray * exposure;
                    //    accumulator[k].b = accumulator[k].b * (1.0f - exposure) + gray * exposure;
                    //}

                    //rgbColProc[k].r = accumulator[k].r;
                    //rgbColProc[k].g = accumulator[k].g;
                    //rgbColProc[k].b = accumulator[k].b;
                    //rgbColProc[k].a = accumulator[k].a;

                    ////...

                    k++;
                }
            }

            // Debug.Log(depthColProc[(int)(848 * 480 * 0.5)].r);

            //Debug.Log(rgbColProc[0].r);
            //Debug.Log(rgbColProc[w * h - 1].r);

            //... set and apply textures

            // raw color:
            rgbTexRaw.SetPixels(rgbColRaw);
            rgbTexRaw.Apply();

            rawMat.mainTexture = rgbTexRaw;

            // grayscale color:
            rgbTexProc.SetPixels(rgbColProc);
            rgbTexProc.Apply();

            procMat.mainTexture = rgbTexProc;

            //...

            // background.texture = rgbTexProc;
        }

    }

}
