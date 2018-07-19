using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterSecs : MonoBehaviour {

    public float DestroyTimeout = 10;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        DestroyTimeout -= Time.deltaTime;
        if (DestroyTimeout <= 0)
            Destroy(this.gameObject);

    }
}
