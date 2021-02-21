using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public GameObject obj;
    public GameObject player;
    public Vector3Int playerChunk;
    public ConcurrentDictionary<Vector3Int, GameObject> chunks = new ConcurrentDictionary<Vector3Int, GameObject>();
    
    IEnumerator Gogo()
    {
        for (int i = -5; i <= 5; i++)
        {
            for (int j = -5; j <= 5; j++)
            {
                for (int y = -3; y < 3; y++)
                {
                    var position = new Vector3Int(i, y, j) + playerChunk;

                    if (chunks.ContainsKey(position))
                    {
                        continue;
                    }

                    GameObject chunk = Instantiate(obj);
                    chunks.TryAdd(position, chunk);
                    position *= 16;
                    chunk.transform.position = position;
                    chunk.GetComponent<BasicComputeTest>().Generate(position);
                    Debug.Log("Generating " + position.ToString());
                }
                yield return null;    
            }
            
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Gogo());
    }

    void Generate()
    {
        StartCoroutine(Gogo());
    }

    Vector3Int positionToChunk(Vector3 position)
    {
        Vector3 chunkPos = new Vector3(position.x, position.y, position.z);
        chunkPos /= 16.0f;
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
