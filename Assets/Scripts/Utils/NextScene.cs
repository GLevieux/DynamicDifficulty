using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NextScene : MonoBehaviour {

    public PlayerManager playerManager;
    public Text playerNameText;
    public void loadNextScene()
    {
        if (playerNameText.text != "")
        {
            playerManager.PlayerName = playerNameText.text+"V2";
            SceneManager.LoadScene(1);
        }
            
        

    }
}
