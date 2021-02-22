using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class FlyCamera : MonoBehaviour {
 
    /*
    Writen by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.  
    Converted to C# 27-02-13 - no credit wanted.
    Simple flycam I made, since I couldn't find any others made public.  
    Made simple to use (drag and drop, done) for regular keyboard layout  
    wasd : basic movement
    shift : Makes camera accelerate
    space : Moves camera on X and Z axis only.  So camera doesn't gain any height*/
     
     
    float mainSpeed = 100.0f; //regular speed
    float shiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
    float maxShift = 1000.0f; //Maximum speed when holdin gshift
    float camSens = 0.25f; //How sensitive it with mouse
    private Vector3 lastMouse = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
    private float totalRun= 1.0f;

    public float climbSpeed = 4;
    public float slowMoveFactor = 0.25f;
    public float normalMoveSpeed = 10;
    public float fastMoveFactor = 3;
    public float cameraSensitivity = 90;
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update () {
        rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
        rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        rotationY = Mathf.Clamp (rotationY, -90, 90);
 
        transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
        transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);
 
        if (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift))
        {
            transform.position += transform.forward * (normalMoveSpeed * fastMoveFactor * Input.GetAxis("Vertical") * Time.deltaTime);
            transform.position += transform.right * (normalMoveSpeed * fastMoveFactor * Input.GetAxis("Horizontal") * Time.deltaTime);
        }
        else if (Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl))
        {
            transform.position += transform.forward * (normalMoveSpeed * slowMoveFactor * Input.GetAxis("Vertical") * Time.deltaTime);
            transform.position += transform.right * (normalMoveSpeed * slowMoveFactor * Input.GetAxis("Horizontal") * Time.deltaTime);
        }
        else
        {
            transform.position += transform.forward * (normalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime);
            transform.position += transform.right * (normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime);
        }
 
 
        if (Input.GetKey (KeyCode.Q)) {transform.position += transform.up * (climbSpeed * Time.deltaTime);}
        if (Input.GetKey (KeyCode.E)) {transform.position -= transform.up * (climbSpeed * Time.deltaTime);}
 
        if (Input.GetKeyDown (KeyCode.End))
        {
            Cursor.lockState = CursorLockMode.None;
        }
       
    }
     
    private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey (KeyCode.W)){
            p_Velocity += new Vector3(0, 0 , 1);
        }
        if (Input.GetKey (KeyCode.S)){
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey (KeyCode.A)){
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey (KeyCode.D)){
            p_Velocity += new Vector3(1, 0, 0);
        }
        return p_Velocity;
    }
}