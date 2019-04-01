using System;
using UnityEngine;
using Intel.RealSense;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RsPointCloudRenderer : MonoBehaviour
{
    public RsFrameProvider Source;
    private Mesh mesh;
    private Texture2D uvmap;

    Texture2D ctex;
    public Material cmat;

    [NonSerialized]
    private Vector3[] vertices;

    FrameQueue depthQueue;
    FrameQueue rgbQueue;

    public GameObject particleProto;
    public GameObject unityObject;
    public GameObject sphere;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += Dispose;

        // particle = Object.Instantiate(particleProto);
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    }

    private void OnStartStreaming(PipelineProfile obj)
    {
        depthQueue = new FrameQueue(1);
        rgbQueue = new FrameQueue(1);

        //mesh stuff
        using (var depth = obj.Streams.FirstOrDefault(s => s.Stream == Stream.Depth && s.Format == Format.Z16).As<VideoStreamProfile>())
            ResetMesh(depth.Width, depth.Height);

        Source.OnNewSample += OnNewSample;
    }

    //more mesh stuff
    private void ResetMesh(int width, int height)
    {
        Assert.IsTrue(SystemInfo.SupportsTextureFormat(TextureFormat.RGFloat));
        uvmap = new Texture2D(width, height, TextureFormat.RGFloat, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
        };
        GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_UVMap", uvmap);

        if (mesh != null)
            mesh.Clear();
        else
            mesh = new Mesh()
            {
                indexFormat = IndexFormat.UInt32,
            };

        vertices = new Vector3[width * height];

        var indices = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            indices[i] = i;

        mesh.MarkDynamic();
        mesh.vertices = vertices;

        var uvs = new Vector2[width * height];
        Array.Clear(uvs, 0, uvs.Length);
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                uvs[i + j * width].x = i / (float)width;
                uvs[i + j * width].y = j / (float)height;
            }
        }

        mesh.uv = uvs;

        mesh.SetIndices(indices, MeshTopology.Points, 0, false);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    void OnDestroy()
    {
        if (depthQueue != null)
        {
            depthQueue.Dispose();
            depthQueue = null;
        }

        if (rgbQueue != null)
        {
            rgbQueue.Dispose();
            rgbQueue = null;
        }

        if (mesh != null)
            Destroy(null);
    }

    private void Dispose()
    {
        Source.OnNewSample -= OnNewSample;

        if (depthQueue != null)
        {
            depthQueue.Dispose();
            depthQueue = null;
        }

        if (rgbQueue != null)
        {
            rgbQueue.Dispose();
            rgbQueue = null;
        }
    }

    // this is where I think it processes the raw frame info
    private void OnNewSample(Frame frame)
    {
        if (depthQueue == null)
            return;
        if (rgbQueue == null)
            return;

        try
        {
            // checking if frame has multiple streams?
            if (frame.IsComposite)
            {
                using (var fs = FrameSet.FromFrame(frame))

                // getting depth data as points
                using (var points = fs.FirstOrDefault<Points>(Stream.Depth, Format.Xyz32f))

                {
                    if (points != null)
                    {
                        depthQueue.Enqueue(points);
                    }
                }

                // getting depth data as frames
                using (var fs = FrameSet.FromFrame(frame))

                using (var videoFrame = fs.FirstOrDefault<VideoFrame>(Stream.Color, Format.Rgb8))
                {
                    if (videoFrame != null)
                        rgbQueue.Enqueue(videoFrame);
                }
                return;
            }

            if (frame.Is(Extension.Points))
            {
                depthQueue.Enqueue(frame);
            }

            if (frame.Is(Extension.VideoFrame))
            {
                rgbQueue.Enqueue(frame);
            }

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }


    protected void Update()
    {
        
        if (depthQueue != null)
        {
            Points points;

            // getting points
            if (depthQueue.PollForFrame<Points>(out points))
                using (points)
                {
                    int count = points.Count;
                    Debug.Log(count);
                    //Debug.Log("hello");

                    if (points.Count != mesh.vertexCount)
                    {
                        using (var p = points.GetProfile<VideoStreamProfile>())
                            ResetMesh(p.Width, p.Height);
                    }

                    if (points.TextureData != IntPtr.Zero)
                    {
                        uvmap.LoadRawTextureData(points.TextureData, points.Count * sizeof(float) * 2);
                        uvmap.Apply();
                    }

                    if (points.VertexData != IntPtr.Zero)
                    {
                        points.CopyVertices(vertices);

                        mesh.vertices = vertices;
                        mesh.UploadMeshData(false);

                        //Vector3 verMid = vertices[(int)count/2];
                        //Debug.Log((float)verMid.y);

                        Vector3 vec = vertices[5000];

                        Debug.Log("x =" + (float)vec.x);
                        Debug.Log("z =" + (float)vec.z);
                        Debug.Log("y =" + (float)vec.y);

                        sphere.transform.position = vec;
                    }
                }

            // Debug.Log("thank you, next");
        }

        if(rgbQueue != null)
        {
            VideoFrame rgbFrame;

            //getting rgb frames
            if (rgbQueue.PollForFrame<VideoFrame>(out rgbFrame))
                using (rgbFrame)
                {
                    // Debug.Log("processed one frame");
                    ProcessFrame(rgbFrame);
                }
        }
    }

    // function to process frame
    private void ProcessFrame(VideoFrame frame)
    {
        // for first frame
        if (ctex == null)
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

    //// temp particle function
    //public CParticle(GameObject particleGraphic)
    //{
    //    unityObject = Object.Instantiate(particleGraphic);
    //}
}
