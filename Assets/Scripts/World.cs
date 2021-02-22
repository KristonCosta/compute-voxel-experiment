using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public class World : MonoBehaviour
{
    public GameObject obj;
    public GameObject player;
    public Vector3Int playerChunk;
    public Dictionary<Vector3Int, GameObject> chunks = new Dictionary<Vector3Int, GameObject>();
    private CoroutineQueue queue;
    private const int radius = 1;
    IEnumerator Gogo()
    {
        
        var old = chunks.Keys.ToList();
        var currentChunks = new HashSet<Vector3Int>();
        for (int i = -radius; i <= radius; i++)
        {
            
            for (int j = -radius; j <= radius; j++)
            {
                Profiler.BeginSample("Gogo");
                for (int y = -radius; y < radius; y++)
                {
                    var position = new Vector3Int(i, y, j) + playerChunk;
                    currentChunks.Add(position);
                    if (chunks.ContainsKey(position))
                    {
                        continue;
                    }

                    GameObject chunk = Instantiate(obj);
                    chunks.Add(position, chunk);
                    position *= BasicComputeTest.chunkSize;
                    chunk.transform.position = position;
                    chunk.GetComponent<BasicComputeTest>().Generate(queue, position);
                    
                }
                Profiler.EndSample();
                yield return null;
                
                          
            }
            
            
        }
        
        foreach (var key in old)
        {
            if (!currentChunks.Contains(key))
            {
                if (chunks.ContainsKey(key))
                {
                    chunks[key].GetComponent<BasicComputeTest>().ClearBuffers();
                    Destroy(chunks[key]);
                    chunks.Remove(key);    
                }
            }
        }
        yield return null;
    }

    private void Start()
    {
        queue = new CoroutineQueue(2, StartCoroutine);
    }

    void Generate()
    { 
        StopCoroutine("Gogo");
        StartCoroutine(Gogo());
    }

    Vector3Int positionToChunk(Vector3 position)
    {
        Vector3 chunkPos = new Vector3(position.x, position.y, position.z);
        chunkPos /= (float)(BasicComputeTest.chunkSize);
        chunkPos.x = Mathf.Floor(chunkPos.x);
        chunkPos.y = Mathf.Floor(chunkPos.y);
        chunkPos.z = Mathf.Floor(chunkPos.z);

        return new Vector3Int((int) chunkPos.x, (int) chunkPos.y, (int) chunkPos.z);
    }

    // Update is called once per frame
    void Update()
    {
        var playerCurrentPosition = positionToChunk(player.transform.position);
        if (playerCurrentPosition != playerChunk)
        {
            playerChunk = playerCurrentPosition;
            Generate();
        }
    }
}
