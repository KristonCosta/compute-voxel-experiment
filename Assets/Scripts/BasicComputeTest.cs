using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicComputeTest : MonoBehaviour
{
    
    public ComputeShader shader;
    public ComputeShader geomShader;
    public GameObject ob;
    private RenderTexture texture;
    public Material material; 
    public GameObject chunk;
    
    ComputeBuffer pointsBuffer;
    private ComputeBuffer quadBuffer;
    
    private int kernel;

    private Vector3[] output;

    private GameObject[] spheres;

    private bool initialized = false;
    // Start is called before the first frame update

    Vector3 indexToPosition(int index) 
    {
        return new Vector3(index % 8f, Mathf.Floor((index % 64) / 8.0f), Mathf.Floor(index / 64.0f));
    }
    
    void Start()
    {
        
        int numVoxels = 8 * 8 * 8;
        int maxQuadCount = numVoxels * 10;
        pointsBuffer = new ComputeBuffer(numVoxels, sizeof(float));
        quadBuffer = new ComputeBuffer (maxQuadCount, sizeof (float) * 3 * 5, ComputeBufferType.Append);
        kernel = shader.FindKernel("Basic");
        
        shader.SetBuffer (0, "points", pointsBuffer);
        shader.SetFloat("offset", Time.time);
        
        shader.Dispatch(kernel, 1, 1, 1);
        
        kernel = geomShader.FindKernel("Gen");
        
        geomShader.SetBuffer (0, "quads", quadBuffer);
        geomShader.SetBuffer (0, "points", pointsBuffer);
        geomShader.Dispatch(kernel, 1, 1, 1);
        
        ComputeBuffer quadCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount (quadBuffer, quadCountBuffer, 0);
        int[] quadCountArray = { 0 };
        quadCountBuffer.GetData (quadCountArray);
        int numQuads = quadCountArray[0];
        
        Quad[] quads = new Quad[numQuads];
        quadBuffer.GetData(quads, 0, 0, numQuads);
        
        for (int i = 0; i < numQuads; i++) {
            
            var vertices = new Vector3[4];
            var normals = new Vector3[ 4];
            var uvs = new Vector2[ 4];
            var meshTriangles = new int[ 6];

            for (int j = 0; j < 4; j++) {
                vertices[j] = quads[i][j];
                normals[j] = quads[i].normal;
            }

            var offset = 0; // i * 4;
            var indexOffset = 0; // i * 6;
            meshTriangles[indexOffset] = 3 + offset;
            meshTriangles[indexOffset + 1] = 1 + offset;
            meshTriangles[indexOffset + 2] = 0 + offset;
            meshTriangles[indexOffset + 3] = 3 + offset;
            meshTriangles[indexOffset + 4] = 2 + offset;
            meshTriangles[indexOffset + 5] = 1 + offset;

            uvs[offset] = Vector2.one;
            uvs[offset + 1] = Vector2.up;
            uvs[offset + 2] = Vector2.zero;
            uvs[offset + 3] = Vector2.right;
            
            var quad = new GameObject("quad");
            var mesh = new Mesh { name = "ScriptedMesh" };
            mesh.vertices = vertices;
            mesh.triangles = meshTriangles;
            mesh.normals = normals;
            mesh.uv = uvs;
        
            mesh.RecalculateBounds();
            
            var filter = quad.AddComponent<MeshFilter>();
            var renderer = quad.AddComponent<MeshRenderer>();

            filter.mesh = mesh;
            renderer.material = material;

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
