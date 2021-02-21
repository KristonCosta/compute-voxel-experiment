using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public GameObject obj;

    IEnumerator Generator(int i)
    {
        for (int j = -1; j <= 1; j++)
        {
            for (int y = 0; y < 1; y++)
            {
                GameObject chunk = Instantiate(obj); 
                var pos = new Vector3(i * 16, y * 16, j * 16);
                chunk.transform.position = pos;
                chunk.GetComponent<BasicComputeTest>().Generate(pos);
                yield return null;    
            }
            
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        for (int i = -1; i <= 1; i++)
        {
            StartCoroutine(Generator(i));    
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
