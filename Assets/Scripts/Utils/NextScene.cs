using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NextScene : MonoBehaviour {

    public PlayerManager playerManager;
    public Text playerNameText;
    public Text playerAgeText;
    public Text playerGenderText;
    public void loadNextScene()
    {
        if (playerNameText.text != "" && playerAgeText.text != "" && playerGenderText.text != "")
        {
            playerManager.PlayerName = playerNameText.text+"V2";
            playerManager.PlayerAge = playerAgeText.text;
            playerManager.PlayerGender = playerGenderText.text;
            SceneManager.LoadScene(1);
        }
            
        

    }
}
