using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicComputeTest : MonoBehaviour
{
    
    public ComputeShader shader;
    public ComputeShader geomShader;
    private RenderTexture texture;
    public Material material;

    ComputeBuffer pointsBuffer;
    private ComputeBuffer quadBuffer;
    
    private int kernel;

    private Vector3[] output;

    private GameObject[] spheres;

    private bool initialized = false;
    // Start is called before the first frame update

    public void Generate(Vector3 offset)
    {
        int sizeOfChunk = 16;
        int numVoxels = sizeOfChunk * sizeOfChunk * sizeOfChunk;
        int maxQuadCount = numVoxels * 6;
        pointsBuffer = new ComputeBuffer(numVoxels, sizeof(float));
        quadBuffer = new ComputeBuffer (maxQuadCount, sizeof (float) * 3 * 5, ComputeBufferType.Append);
        kernel = shader.FindKernel("Basic");
        
        shader.SetBuffer (kernel, "points", pointsBuffer);
        shader.SetInt("size_of_chunk", sizeOfChunk);
        shader.SetVector("offset", offset);
        shader.Dispatch(kernel, sizeOfChunk/8, sizeOfChunk/8, sizeOfChunk/8);
        
        kernel = geomShader.FindKernel("Gen");
        
        geomShader.SetBuffer (kernel, "quads", quadBuffer);
        geomShader.SetBuffer (kernel, "points", pointsBuffer);
        geomShader.SetInt("size_of_chunk", sizeOfChunk);
        geomShader.Dispatch(kernel, sizeOfChunk/8, sizeOfChunk/8, sizeOfChunk/8);
        
        ComputeBuffer quadCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount (quadBuffer, quadCountBuffer, 0);
        int[] quadCountArray = { 0 };
        quadCountBuffer.GetData (quadCountArray);
        int numQuads = quadCountArray[0];
        
        Quad[] quads = new Quad[numQuads];
        quadBuffer.GetData(quads, 0, 0, numQuads);
            
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
        var renderer = quad.AddComponent<MeshRenderer>();

        filter.mesh = mesh;
        renderer.material = material;
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
