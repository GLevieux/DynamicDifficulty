using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimCrosshair : MonoBehaviour {

    public Transform Ground;
    public Transform Crosshair;
    
    public Plane plane;

	// Use this for initialization
	void Start () {
        plane = new Plane(Ground.position, Ground.up);
        Cursor.visible = false;
    }
	
	// Update is called once per frame
	void Update () {
        plane = new Plane(Ground.up,Ground.position);
        // create a ray from the mousePosition
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // plane.Raycast returns the distance from the ray start to the hit point
        float distance = 100;
       
        if (plane.Raycast(ray, out distance))
        {

            // some point of the plane was hit - get its coordinates
            Vector3 hitPoint = ray.GetPoint(distance);
            
            Crosshair.position = hitPoint + Ground.up;
        }
    }
}
