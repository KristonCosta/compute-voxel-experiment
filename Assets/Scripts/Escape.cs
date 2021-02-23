using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Escape : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
     Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);   
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.T))
        {
            Application.Quit();   
        }
    }
}
