using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using UnityEngine;

public class World : MonoBehaviour
{
    
    public GameObject obj;
    public GameObject player;
    public Vector3Int playerChunk;
    public Dictionary<Vector3Int, GameObject> chunks = new Dictionary<Vector3Int, GameObject>();
    public Queue<GameObject> reusableChunks = new Queue<GameObject>();
    private CoroutineQueue queue;
    
    private const int radius = 8;
    IEnumerator Reload()
    {
        var chunkQueue = new SimplePriorityQueue<Vector3Int, int>();
        var old = chunks.Keys.ToList();
        var currentChunks = new HashSet<Vector3Int>();
        for (int i = -radius; i <= radius; i++)
        {

            for (int j = -radius; j <= radius; j++)
            {
                    var rad = new Vector3Int(i, 0, j);
                    var pos = rad + playerChunk;
                    pos *= Chunk.chunkSize;
                    currentChunks.Add(pos);
                    chunkQueue.Enqueue(rad, rad.sqrMagnitude);
            }
        }

        foreach (var key in old.ToList())
        {
            if (!currentChunks.Contains(key))
            { 
                reusableChunks.Enqueue(chunks[key]);
                chunks.Remove(key);
            }
        }
        
        while (chunkQueue.Count > 0)
        {
            
            var pos = chunkQueue.Dequeue();
            if (pos.sqrMagnitude > radius * radius)
            {
                continue;
            }

            pos.x += playerChunk.x;
            pos.z += playerChunk.z;
            pos *= Chunk.chunkSize;
                    
            if (chunks.ContainsKey(pos))
            {
                continue;
            }
            GameObject chunk;
            if (reusableChunks.Count > 0)
            {
                chunk = reusableChunks.Dequeue();
                if (chunk.GetComponent<Chunk>().chunkOffset == pos)
                {
                    chunks.Add(pos, chunk);
                    continue;
                } 
            }
            else
            {
                chunk = Instantiate(obj);
            }
            
            chunks.Add(pos, chunk);
            chunk.transform.position = pos; 
            chunk.GetComponent<Chunk>().Init(pos);

            yield return null;
        }

        yield return null;
    }

    private void Start()
    {
        queue = new CoroutineQueue(2, StartCoroutine);
    }

    void Generate()
    { 
        StopCoroutine(nameof(Reload));
        StartCoroutine(Reload());
    }

    Vector3Int positionToChunk(Vector3 position)
    {
        Vector3 chunkPos = new Vector3(position.x, 0, position.z);
        chunkPos /= (float)(Chunk.chunkSize);
        chunkPos.x = Mathf.Floor(chunkPos.x);
        chunkPos.z = Mathf.Floor(chunkPos.z);

        return new Vector3Int((int) chunkPos.x, (int)0, (int) chunkPos.z);
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
