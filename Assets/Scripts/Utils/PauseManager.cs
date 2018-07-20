using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour {

    public GameObject PausePanel;

	// Use this for initialization
	void Start () {
        PausePanel.SetActive(false);
    }
	
	// Update is called once per frame
	void Update () {
		if(Input.GetButtonDown("Pause"))
        {
            if(Time.timeScale > 0)
            {
                Debug.Log("Pause");
                Time.timeScale = 0;
                PausePanel.SetActive(true);
            }   
            else
            {
                Debug.Log("Fin Pause");
                Time.timeScale = 1;
                PausePanel.SetActive(false);
            }
                
        }
	}
}
