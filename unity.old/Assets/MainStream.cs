using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Intel.RealSense;

public class MainStream : MonoBehaviour
{
    // output material
    public Material cmat;

    // RS camera input
    //public RsFrameProvider Source;

    Texture2D ctex;

    Config cfg;
    Pipeline pipe;

    // debugging frame count
    int count = 0;

    void Start()
    {
        // initialise config with .bag file path
        cfg = new Config();
        cfg.EnableDeviceFromFile("C:/Users/rjgwa/Documents/GitHub/hashtagDeep/sample_data/20190330_093417.bag");
        pipe = new Pipeline();

        //pipe.Start();
        pipe.Start(cfg);
    }

    void Update()
    {
        FrameSet frames = pipe.WaitForFrames();
        VideoFrame frame = frames.ColorFrame;

        if (frame != null)
        {
            ProcessFrame(frame);
            count++;
            Debug.Log(count);
        }

        if (frames != null && frames.Count != 0)
        {
            ProcessFrame(frames.ColorFrame);

            // Debug.Log(frames.ColorFrame.Width);
        }

        //FrameSet frames;

        //if (pipe.PollForFrames(out frames))
        //{
        //    count++;
        //    //Frame f = frames[0];
        //    ProcessFrame(frames.ColorFrame);

        //    // Debug.Log(frames.ColorFrame.Width);
        //    Debug.Log(count); 
        //}
    }

    // function to process frame
    private void ProcessFrame(VideoFrame frame)
    {
        // for first frame
        if (ctex==null)
        {

            ctex = new Texture2D(frame.Width, frame.Height, Convert(frame.Profile.Format), false, true);

            // set output materials texture to ctex
            cmat.mainTexture = ctex;
          //  textureBinding.Invoke(texture);
        }

        // this is unity stuff
        ctex.LoadRawTextureData(frame.Data, frame.Stride * frame.Height);
        ctex.Apply();

    }

    // function to convert RS raw data to Unity format
    private static TextureFormat Convert(Format lrsFormat)
    {
        switch (lrsFormat)
        {
            case Format.Z16: return TextureFormat.R16;
            case Format.Disparity16: return TextureFormat.R16;
            case Format.Rgb8: return TextureFormat.RGB24;
            case Format.Rgba8: return TextureFormat.RGBA32;
            case Format.Bgra8: return TextureFormat.BGRA32;
            case Format.Y8: return TextureFormat.Alpha8;
            case Format.Y16: return TextureFormat.R16;
            case Format.Raw16: return TextureFormat.R16;
            case Format.Raw8: return TextureFormat.Alpha8;
            case Format.Disparity32: return TextureFormat.RFloat;
            case Format.Yuyv:
            case Format.Bgr8:
            case Format.Raw10:
            case Format.Xyz32f:
            case Format.Uyvy:
            case Format.MotionRaw:
            case Format.MotionXyz32f:
            case Format.GpioRaw:
            case Format.Any:
            default:
                return TextureFormat.RGB24;
        }
    }

    private void OnApplicationQuit()
    {
        pipe.Stop();
    }
}
