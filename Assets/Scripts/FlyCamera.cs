using System;
using UnityEngine;
using System.Collections;

public class FlyCamera : MonoBehaviour {
	
	/*
    Writen by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.  
    Converted to C# 27-02-13 - no credit wanted.
    Simple flycam I made, since I couldn't find any others made public.  
    Made simple to use (drag and drop, done) for regular keyboard layout  
    wasd : basic movement
    shift : Makes camera accelerate
    space : Moves camera on X and Z axis only.  So camera doesn't gain any height*/
	
	
	public float mainSpeed = 100.0f; //regular speed
	public float shiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
	public float maxShift = 1000.0f; //Maximum speed when holdin gshift
	private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    
    public float cameraSensitivity = 90;
    
	private float totalRun= 1.0f;

	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
	}
	
	void Update () {
 
		rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
		rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
		rotationY = Mathf.Clamp (rotationY, -90, 90);
 
		transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
		transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);


		//Keyboard commands
		float f = 0.0f;
		Vector3 p = GetBaseInput();
		if (Input.GetKey (KeyCode.LeftShift)){
			totalRun += Time.deltaTime;
			p  = p * totalRun * shiftAdd;
			p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
			p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
			p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
		}
		else{
			totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
			p = p * mainSpeed;
		}

		transform.position += p * Time.deltaTime;


        if (Input.GetKeyDown (KeyCode.End))
        {
            Cursor.lockState = CursorLockMode.None;
        }
       
	}
	
	private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
		Vector3 p_Velocity = new Vector3();
		if (Input.GetKey (KeyCode.W))
		{
			p_Velocity += transform.forward;
		}
		if (Input.GetKey (KeyCode.S))
		{
			p_Velocity -= transform.forward;
		}
		if (Input.GetKey (KeyCode.A))
		{
			p_Velocity -= transform.right;
		}
		if (Input.GetKey (KeyCode.D)){
			p_Velocity += transform.right;
		}
		return p_Velocity;
	}
}