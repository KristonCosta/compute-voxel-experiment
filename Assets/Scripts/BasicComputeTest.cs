using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class BasicComputeTest : MonoBehaviour
{
    enum State
    {
        Starting,
        Loading,
        Done
    }

    public const int chunkSize = 32;
    private State state = State.Starting; 
    public ComputeShader shader;
    public ComputeShader geomShader;
    private RenderTexture texture;
    public Material material;
    private Quad[] quads;
    ComputeBuffer pointsBuffer;
    private ComputeBuffer quadBuffer;
    
    private int kernel;

    private Vector3[] output;

    private GameObject[] spheres;
    private AsyncGPUReadbackRequest request;
    private CoroutineQueue queue;
    private bool initialized = false;

    private bool destroyed = false;
    // Start is called before the first frame update

    public void Generate(CoroutineQueue q, Vector3 offset)
    {
        queue = q;
        var start = Time.timeAsDouble;
        int sizeOfChunk = chunkSize + 2;
        int numVoxels = sizeOfChunk * sizeOfChunk * sizeOfChunk;
        int maxQuadCount = numVoxels * 6;
        pointsBuffer = new ComputeBuffer(numVoxels, sizeof(float));
        pointsBuffer.SetCounterValue(0);
        quadBuffer = new ComputeBuffer(maxQuadCount, sizeof(float) * 3 * 5, ComputeBufferType.Append);
        quadBuffer.SetCounterValue(0);
        kernel = shader.FindKernel("Basic");

        shader.SetBuffer(kernel, "points", pointsBuffer);
        shader.SetInt("size_of_chunk", sizeOfChunk);
        shader.SetVector("offset", offset);
        var groups = Mathf.CeilToInt(sizeOfChunk / 8.0f);
        shader.Dispatch(kernel, groups, groups, groups);

        kernel = geomShader.FindKernel("Gen");

        geomShader.SetBuffer(kernel, "quads", quadBuffer);
        geomShader.SetBuffer(kernel, "points", pointsBuffer);
        geomShader.SetInt("size_of_chunk", sizeOfChunk);
        geomShader.Dispatch(kernel, groups, groups, groups);

        request = AsyncGPUReadback.Request(quadBuffer);
        state = State.Loading;
        var diff = Time.timeAsDouble - start;
        if (destroyed)
        {
            ClearBuffers();
        }
       // yield return null;
    }

    private void LoadRequest()
    {
        Profiler.BeginSample("LoadRequest");
        if (destroyed)
        {
            return;
        }
        
        var start = Time.timeAsDouble;
        var numQuads = quads.Length;   
        
        var vertices = new Vector3[4*numQuads];
        var normals = new Vector3[4*numQuads];
        var uvs = new Vector2[4*numQuads];
        var meshTriangles = new int[6*numQuads];
        
        for (int i = 0; i < numQuads; i++) {
            for (int j = 0; j < 4; j++) {
                vertices[i * 4 + j] = quads[i][j];
                normals[i * 4 + j] = quads[i].normal;
            }

            var varOffset = i * 4;
            var indexOffset = i * 6;
            meshTriangles[indexOffset] = 3 + varOffset;
            meshTriangles[indexOffset + 1] = 1 + varOffset;
            meshTriangles[indexOffset + 2] = 0 + varOffset;
            meshTriangles[indexOffset + 3] = 3 + varOffset;
            meshTriangles[indexOffset + 4] = 2 + varOffset;
            meshTriangles[indexOffset + 5] = 1 + varOffset;

            uvs[varOffset] = Vector2.one;
            uvs[varOffset + 1] = Vector2.up;
            uvs[varOffset + 2] = Vector2.zero;
            uvs[varOffset + 3] = Vector2.right;
        }
        
        var quad = new GameObject("quad");
        quad.transform.parent = transform;
        quad.transform.position = transform.position;
        var mesh = new Mesh { name = "ScriptedMesh" };
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;
        mesh.normals = normals;
        mesh.uv = uvs;
        
        mesh.RecalculateBounds();
        
        var filter = quad.AddComponent<MeshFilter>();
       // var renderer = quad.AddComponent<MeshRenderer>();
       // mesh.OptimizeIndexBuffers();
        filter.mesh = mesh;
        
       // renderer.shadowCastingMode = ShadowCastingMode.On;
       // renderer.material = material;
        state = State.Done;
        Profiler.EndSample();
        return;
    }

    public void ClearBuffers()
    {
        pointsBuffer?.Release();
        quadBuffer?.Release();
    }

    private void OnDisable()
    {
        destroyed = true;
        pointsBuffer?.Release();
        quadBuffer?.Release();
    }

    private void Update()
    {
        if (state == State.Done) return;
        if (state == State.Loading && request.done && !request.hasError)
        {
            quads = request.GetData<Quad>().ToArray();
            pointsBuffer.Dispose();
            quadBuffer.Dispose();
            LoadRequest();
            state = State.Done;
        } else if (state == State.Loading && request.hasError)
        {
            Debug.LogError("Request had an error");
            
        }
        
    }

    struct Quad
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Vector3 d;
        public Vector3 normal;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    case 2:
                        return c;
                    default:
                        return d;
                }
            }
        }
    }
}
