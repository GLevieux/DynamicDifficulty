using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {

    public string PlayerName = "UnknownPlayerName";

    public void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}
