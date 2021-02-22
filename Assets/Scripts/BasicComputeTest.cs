using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class BasicComputeTest : MonoBehaviour
{
    enum State
    {
        Waiting,
        Starting,
        Loading,
        Done
    }

    public const int chunkSize = 16;
    public const int chunkHeight = 256;
    private State state = State.Waiting; 
    public ComputeShader shader;
    public ComputeShader geomShader;
    private RenderTexture texture;
    public Material material;
    ComputeBuffer pointsBuffer;
    
    private ComputeBuffer faceInfoBuffer;
    private ComputeBuffer countBuffer; 
    
    private GameObject myQuad;
    private bool quadInitialized = false;
    private int kernel;

    private Vector3[] output;

    private GameObject[] spheres;
    private AsyncGPUReadbackRequest request;
    private CoroutineQueue queue;
    private bool initialized = false;
    public Vector3Int chunkOffset;
    private bool destroyed = false;
    
    
    private readonly Vector2 uv00 = new Vector2(0f, 0f);
    private readonly Vector2  uv10 = new Vector2(1f, 0f);
    private readonly Vector2  uv01 = new Vector2(0f, 1f);
    private readonly Vector2  uv11 = new Vector2(1f, 1f);
    
    // Start is called before the first frame update
    public void Start()
    {
        int sizeOfChunk = chunkSize + 2;
        int numVoxels = sizeOfChunk * sizeOfChunk * (chunkHeight + 2);
        int maxQuadCount = numVoxels;
        pointsBuffer = new ComputeBuffer(numVoxels, sizeof(float));
        pointsBuffer.SetCounterValue(0);
        faceInfoBuffer = new ComputeBuffer(maxQuadCount, sizeof(int) * 3 + sizeof(uint) * 2, ComputeBufferType.Append);
        faceInfoBuffer.SetCounterValue(0);
        countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        
        
    }

    public void Init(CoroutineQueue q, Vector3Int offset)
    {
        queue = q;
        chunkOffset = offset;
        state = State.Starting;
        MeshFilter f;
        if (quadInitialized && myQuad.TryGetComponent(out f))
        {
            f.mesh = null;
        }
    }
    
    public void Generate()
    {
        
        int sizeOfChunk = chunkSize + 2;
        pointsBuffer.SetCounterValue(0);
        faceInfoBuffer.SetCounterValue(0);
        kernel = shader.FindKernel("Basic");

        shader.SetBuffer(kernel, "points", pointsBuffer);
        shader.SetInt("size_of_chunk", sizeOfChunk);
        shader.SetInt("height_of_chunk", chunkHeight + 2);
        shader.SetVector("offset", new Vector3(chunkOffset.x, chunkOffset.y, chunkOffset.z));
        var groups = Mathf.CeilToInt(sizeOfChunk / 8.0f);
        var heightGroup = Mathf.CeilToInt(chunkHeight / 8.0f);
        shader.Dispatch(kernel, groups, heightGroup, groups);
        kernel = geomShader.FindKernel("Gen");
        geomShader.SetBuffer(kernel, "face_info", faceInfoBuffer);
        geomShader.SetBuffer(kernel, "points", pointsBuffer);
        geomShader.SetInt("size_of_chunk", sizeOfChunk);
        geomShader.SetInt("height_of_chunk", chunkHeight + 2);
        geomShader.Dispatch(kernel, groups, heightGroup, groups);

        request = AsyncGPUReadback.Request(faceInfoBuffer);
        state = State.Loading;
        
        if (destroyed)
        {
            ClearBuffers();
        }
    }

    private void LoadRequest()
    {
        ComputeBuffer.CopyCount(faceInfoBuffer, countBuffer, 0);
        int[] num_voxels_arr = new int[1] { 0 };
        countBuffer.GetData(num_voxels_arr);
        int num_voxels = num_voxels_arr[0];
        
        if (num_voxels == 0)
        {
            state = State.Done;
            return;
        }
        var voxels = request.GetData<FaceInfo>();
        
        if (destroyed)
        {
            return;
        }
        
        uint numQuads = 0;
        for (int i = 0; i < num_voxels; i++)
        {
            numQuads += voxels[i].num_faces;
        }

        

        var p0 = new Vector3(-0.5f, -0.5f, 0.5f);
        var p1 = new Vector3(0.5f, -0.5f, 0.5f);
        var p2 = new Vector3(0.5f, -0.5f, -0.5f);
        var p3 = new Vector3(-0.5f, -0.5f, -0.5f);
        var p4 = new Vector3(-0.5f, 0.5f, 0.5f);
        var p5 = new Vector3(0.5f, 0.5f, 0.5f);
        var p6 = new Vector3(0.5f, 0.5f, -0.5f);
        var p7 = new Vector3(-0.5f, 0.5f, -0.5f);
        
        var vertices = new Vector3[4*numQuads];
        var normals = new Vector3[4*numQuads];
        var uvs = new Vector2[4*numQuads];
        var meshTriangles = new int[6*numQuads];
        
        var vertex_offset = 0;
        var normal_offset = 0;
        var uv_offset = 0;
        var triangle_offset = 0;
        
        int counter = 0;
        for (int i = 0; i < num_voxels; i++)
        {
            var voxel = voxels[i];


            
            if (voxel.num_faces == 0) break;

            if ((voxel.faces & 0x1) != 0)
            {
                counter += 1;
                vertices[vertex_offset] = p7 + voxel.coordinate;
                vertices[vertex_offset + 1] = p6 + voxel.coordinate;
                vertices[vertex_offset + 2] = p5 + voxel.coordinate;
                vertices[vertex_offset + 3] = p4 + voxel.coordinate;

                generate_normals(normal_offset, Vector3.up, normals);
                generate_uvs(uv_offset, uvs);
                generate_triangles(triangle_offset, vertex_offset, meshTriangles);

                vertex_offset += 4;
                normal_offset += 4;
                uv_offset += 4;
                triangle_offset += 6;
            }


            if ((voxel.faces & 0x2) != 0)
            {
                counter += 1;
                vertices[vertex_offset] = p0 + voxel.coordinate;
                vertices[vertex_offset + 1] = p1 + voxel.coordinate;
                vertices[vertex_offset + 2] = p2 + voxel.coordinate;
                vertices[vertex_offset + 3] = p3 + voxel.coordinate;

                generate_normals(normal_offset, Vector3.down, normals);
                generate_uvs(uv_offset, uvs);
                generate_triangles(triangle_offset, vertex_offset, meshTriangles);

                vertex_offset += 4;
                normal_offset += 4;
                uv_offset += 4;
                triangle_offset += 6;
            }


            if ((voxel.faces & 0x4) != 0)
            {
                counter += 1;
                vertices[vertex_offset] = p7 + voxel.coordinate;
                vertices[vertex_offset + 1] = p4 + voxel.coordinate;
                vertices[vertex_offset + 2] = p0 + voxel.coordinate;
                vertices[vertex_offset + 3] = p3 + voxel.coordinate;

                generate_normals(normal_offset, Vector3.left, normals);
                generate_uvs(uv_offset, uvs);
                generate_triangles(triangle_offset, vertex_offset, meshTriangles);

                vertex_offset += 4;
                normal_offset += 4;
                uv_offset += 4;
                triangle_offset += 6;
            }


            if ((voxel.faces & 0x8) != 0)
            {
                counter += 1;
                vertices[vertex_offset] = p5 + voxel.coordinate;
                vertices[vertex_offset + 1] = p6 + voxel.coordinate;
                vertices[vertex_offset + 2] = p2 + voxel.coordinate;
                vertices[vertex_offset + 3] = p1 + voxel.coordinate;

                generate_normals(normal_offset, Vector3.right, normals);
                generate_uvs(uv_offset, uvs);
                generate_triangles(triangle_offset, vertex_offset, meshTriangles);

                vertex_offset += 4;
                normal_offset += 4;
                uv_offset += 4;
                triangle_offset += 6;
            }


            if ((voxel.faces & 0x10) != 0)
            {
                counter += 1;
                vertices[vertex_offset] = p4 + voxel.coordinate;
                vertices[vertex_offset + 1] = p5 + voxel.coordinate;
                vertices[vertex_offset + 2] = p1 + voxel.coordinate;
                vertices[vertex_offset + 3] = p0 + voxel.coordinate;

                generate_normals(normal_offset, Vector3.forward, normals);
                generate_uvs(uv_offset, uvs);
                generate_triangles(triangle_offset, vertex_offset, meshTriangles);

                vertex_offset += 4;
                normal_offset += 4;
                uv_offset += 4;
                triangle_offset += 6;
            }


            if ((voxel.faces & 0x20) != 0)
            {
                counter += 1;
                vertices[vertex_offset] = p6 + voxel.coordinate;
                vertices[vertex_offset + 1] = p7 + voxel.coordinate;
                vertices[vertex_offset + 2] = p3 + voxel.coordinate;
                vertices[vertex_offset + 3] = p2 + voxel.coordinate;

                generate_normals(normal_offset, Vector3.back, normals);
                generate_uvs(uv_offset, uvs);
                generate_triangles(triangle_offset, vertex_offset, meshTriangles);

                vertex_offset += 4;
                normal_offset += 4;
                uv_offset += 4;
                triangle_offset += 6;
            }
        }

        if (!quadInitialized)
        {
            myQuad = new GameObject("quad");
            quadInitialized = true;
        }
        MeshFilter filter;
        Mesh mesh = new Mesh { name = "ScriptedMesh" }; 
        if (!myQuad.TryGetComponent(out filter))
        {
            filter = myQuad.AddComponent<MeshFilter>();
            filter.mesh = null;
        }
        MeshRenderer renderer;
        if (!myQuad.TryGetComponent(out renderer))
        {
            renderer = myQuad.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.material = material;
        }

        MeshCollider meshCollider;
        if (!myQuad.TryGetComponent(out meshCollider))
        {
            meshCollider = myQuad.AddComponent<MeshCollider>();
                
        }

        myQuad.transform.parent = transform;
        myQuad.transform.position = transform.position;
        
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.RecalculateBounds();
        filter.mesh = mesh;
        meshCollider.sharedMesh = filter.mesh;
        state = State.Done;
    }

    private void generate_normals(int offset, Vector3 dir, Vector3[] normals)
    {
        for (int n = 0; n < 4; n++)
        {
            normals[offset + n] = Vector3.up;
        }
    }

    private void generate_uvs(int offset, Vector2[] uvs)
    {
        uvs[offset] = uv11;
        uvs[offset + 1] = uv01;
        uvs[offset + 2] = uv00;
        uvs[offset + 3] = uv10;
    }
    
    private void generate_triangles(int offset, int vert_offset, int[] triangles)
    {
        triangles[offset] = 3 + vert_offset;
        triangles[offset + 1] = 1 + vert_offset;
        triangles[offset + 2] = 0 + vert_offset;
        triangles[offset + 3] = 3 + vert_offset;
        triangles[offset + 4] = 2 + vert_offset;
        triangles[offset + 5] = 1 + vert_offset;
    }
    
    public void ClearBuffers()
    {
        pointsBuffer?.Release();
        faceInfoBuffer?.Release();
        countBuffer?.Release();
    }

    private void OnDisable()
    {
        destroyed = true;
        ClearBuffers();
    }

    private void Update()
    {
        if (state == State.Starting)
        {
            Generate();
        }
        else if (state == State.Loading && request.done && !request.hasError)
        {
            LoadRequest();
            state = State.Done;
        } 
        else if (state == State.Loading && request.hasError)
        {
            Debug.LogError("Request had an error");
        }
        
    }

    struct FaceInfo
    {
        public Vector3Int coordinate;
        public uint faces;
        public uint num_faces;
    }
}
