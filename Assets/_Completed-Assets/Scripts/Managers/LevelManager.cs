using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour {

    public Transform tankPrefab;
    public Transform[] spawnPositions;

    Transform player;
    Transform[] ennemies;

    public GameDifficultyManager GDiffManager;

    [System.Serializable]
    public struct paramsDiff
    {
        public int nbEnnemies;
        public float speedMove;
        public float speedTurn;
        public float timeBetweenShot;
        public float precision;
    };

    public paramsDiff easyDiff;
    public paramsDiff hardDiff;

    public Text TScore;
    public Text TWin;
    public Text TFail;

    private float nextDifficulty = 0;
    private bool waitingForNewLevel = true;
    private int numLevel = 0;

    private int Score = 0;

    IEnumerator nextLevel()
    {
        waitingForNewLevel = true;
        yield return new WaitForSeconds(2);
        createLevel(nextDifficulty);

    }

    void createLevel(float difficulty)
    {

        if (player)
        { 
            Destroy(player.gameObject);
            for (int i = 0; i < ennemies.Length; i++)
            {
                Destroy(ennemies[i].gameObject);
            }
        }


        paramsDiff currentDiff;
        currentDiff.nbEnnemies = Mathf.RoundToInt(Mathf.Lerp((float)easyDiff.nbEnnemies, (float)hardDiff.nbEnnemies, difficulty));
        currentDiff.speedTurn = Mathf.Lerp(easyDiff.speedTurn, hardDiff.speedTurn, difficulty);
        currentDiff.speedMove = Mathf.Lerp(easyDiff.speedMove, hardDiff.speedMove, difficulty);
        currentDiff.timeBetweenShot = Mathf.Lerp(easyDiff.timeBetweenShot, hardDiff.timeBetweenShot, difficulty);
        currentDiff.precision = Mathf.Lerp(easyDiff.precision, hardDiff.precision, difficulty);

        spawnEverybody(currentDiff.nbEnnemies);
        for (int i = 0; i < ennemies.Length; i++)
        {
            TankAI ai = ennemies[i].GetComponent<TankAI>();
            ai.timeBetweenShot = currentDiff.timeBetweenShot;
            ai.speedTurn = currentDiff.speedTurn;
            ai.speedMove = currentDiff.speedMove;
            ai.precision = currentDiff.precision;
        }

        waitingForNewLevel = false;

        updateScoreLabel();
        TWin.enabled = false;
        TFail.enabled = false;
    }

    void setupTank(Transform tank, float patateColor, int num)
    {
        MeshRenderer[] renderers = tank.GetComponentsInChildren<MeshRenderer>();

        // Go through all the renderers...
        Color color = Color.HSVToRGB(Random.value, patateColor, patateColor);
        for (int i = 0; i < renderers.Length; i++)
        {
            // ... set their material color to the color specific to this tank.
            renderers[i].material.color = color;
        }

        Complete.TankMovement mvt = tank.GetComponent<Complete.TankMovement>();
        Complete.TankShooting sht = tank.GetComponent<Complete.TankShooting>();

        // Set the player numbers to be consistent across the scripts.
        mvt.m_PlayerNumber = num;
        sht.m_PlayerNumber = num;
    }

    void spawnEverybody(int nbEnnemies)
    {
        //On spawn player 1
        player = Instantiate(tankPrefab, spawnPositions[0].position, spawnPositions[0].rotation) as Transform;
        setupTank(player, 0.5f, 2);
        newEnnemies(nbEnnemies, player);
    }


    // Use this for initialization
    void Start() {
        GDiffManager.setPlayerId("MonSuperJoueur");
        GameObject pm = GameObject.Find("PlayerManager");
        if (pm)
            GDiffManager.setPlayerId(pm.GetComponent<PlayerManager>().PlayerName);
        GDiffManager.setActivity(GameDifficultyManager.GDActivityEnum.TANK);

        createLevel(0.2f);

        Score = 0;
    }

    void newEnnemies(int nb, Transform player)
    {
        ennemies = new Transform[nb];
        int spawn = 1;
        for (int i = 0; i < nb; i++)
        {
            ennemies[i] = Instantiate(tankPrefab, spawnPositions[spawn].position, spawnPositions[spawn].rotation) as Transform;
            TankAI ai = ennemies[i].gameObject.AddComponent<TankAI>();
            ai.setPlayer(player, ennemies);

            setupTank(ennemies[i], 0.9f, i + 3);
            spawn++;
            if (spawn >= spawnPositions.Length)
                spawn = 1;
        }
    }

    void updateScoreLabel()
    {
        TScore.text = "Level:" + numLevel + "\n" + "Score:" + (Score); 
    }

    // Update is called once per frame
    void Update() {
        if (waitingForNewLevel)
            return;

        bool end = false;
        bool alldead = false;
        if (!player.gameObject.activeSelf)
            end = true;
        else
        {
            alldead = true;
            for (int i = 0; i < ennemies.Length; i++)
            {
                if (ennemies[i].gameObject.activeSelf)
                    alldead = false;
                

            }
            if (alldead)
                end = true;
        }

        /**
         * Fin de niveau !!! on log tout ici
         * */
        if (end)
        {
            bool win = false;
            if(alldead && player.gameObject.activeSelf)
            {
                win = true;
                Score++;
            }
            if (win)
                TWin.enabled = true;
            else
                TFail.enabled = true;



            double[] betas = GDiffManager.getBetas();
            if (betas == null)
                betas = new double[2];

            this.log(GDiffManager.getPlayerId(),
                betas[0],
                betas[1],
                GDiffManager.getModelQuality(),
                GDiffManager.isUsingLRModel(),
                (float)GDiffManager.getTargetDiff(),
                nextDifficulty,
                win);

            double[] vars = new double[1];
            vars[0] = nextDifficulty;
            GDiffManager.addTry(vars, win);

            numLevel++;

            if (numLevel >= 62)
                SceneManager.LoadScene(2);
            
            vars = GDiffManager.getDiffParams(numLevel);
            nextDifficulty = (float)vars[0];
            StartCoroutine("nextLevel");

        }


    }

    public void log(string player, double beta0, double beta1, double accuracy, bool usedModel, float targetDiff, float param, bool win)
    {
        string csvFile = Application.persistentDataPath + "/" + player + "_log.csv";

        try
        {
            FileStream ofs;
            StreamWriter sw;

            if (!File.Exists(csvFile))
            {
                ofs = new FileStream(csvFile, FileMode.Create);
                sw = new StreamWriter(ofs);

                sw.Write("Time;beta0;beta1;accuracy;used Model;target Diff;param Diff;win\n");

                sw.Flush();
                ofs.Flush();
                sw.Close();
                ofs.Close();
            }

            ofs = new FileStream(csvFile, FileMode.Append);
            sw = new StreamWriter(ofs);

            string dateTime = System.DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
            sw.Write(dateTime+";");
            sw.Write(beta0 + ";");
            sw.Write(beta1 + ";");
            sw.Write(accuracy + ";");
            sw.Write((usedModel?1:0) + ";");
            sw.Write(targetDiff + ";");
            sw.Write(param + ";");
            sw.Write((win ? 1 : 0) + "\n");
          
            sw.Flush();
            ofs.Flush();
            sw.Close();
            ofs.Close();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }

    }
}
