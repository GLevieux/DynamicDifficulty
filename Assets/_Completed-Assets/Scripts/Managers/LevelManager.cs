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
    public DDAModelUnityBridge DDAModelManager;

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
    public Text CompteARebours;

    private float nextDifficulty = 0;
    private bool waitingForNewLevel = true;
    private int numLevel = 0;

    private int Score = 0;
    float answer = -10;
    bool questionAnswered = false;
    bool win = false;

    bool explained = false;
    bool firstLevel = true;

    GameObject question;
    GameObject Explication;
    GameObject Map;

    public Transform AimCursor;

    IEnumerator explication()
    {
        TWin.enabled = false;
        TFail.enabled = false;
        Map.SetActive(false);
        Explication.SetActive(true);
        yield return new WaitForSeconds(10);
        Explication.SetActive(false);
        Map.SetActive(true);
        explained = true;
    }
        IEnumerator nextLevel()
    {
        win = false;
        CompteARebours.enabled = false;
        questionAnswered = false;
        question.SetActive(false);
        waitingForNewLevel = true;
        CompteARebours.enabled = true;
        CompteARebours.text = "3";
        yield return new WaitForSeconds(1);
        CompteARebours.text = "2";
        yield return new WaitForSeconds(1);
        CompteARebours.text = "1";
        yield return new WaitForSeconds(1);
        CompteARebours.enabled = false;
        TWin.enabled = false;
        TFail.enabled = false;
        createLevel(nextDifficulty);

    }

    void destroyLevel()
    {
        if (player)
        { 
            Destroy(player.gameObject);
            for (int i = 0; i < ennemies.Length; i++)
            {
                Destroy(ennemies[i].gameObject);
            }
        }

    }
    
    void createLevel(float difficulty)
    {
        
        

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
    }

    void setupTank(Transform tank, float patateColor, int num)
    {
        MeshRenderer[] renderers = tank.GetComponentsInChildren<MeshRenderer>();
        
        // Go through all the renderers...
        Color color = Color.HSVToRGB(patateColor, patateColor, patateColor);
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
        Map = GameObject.Find("Map");
        Explication = GameObject.Find("Explication");
        Explication.SetActive(false);
        question = GameObject.Find("Question");
        question.SetActive(false);
        GDiffManager.setPlayerId("MonSuperJoueur");
        //DDAModelManager.setPlayerId("MonSuperJoueur");
        GameObject pm = GameObject.Find("PlayerManager");
        if (pm)
        {
            GDiffManager.setPlayerId(pm.GetComponent<PlayerManager>().PlayerName);
            GDiffManager.setPlayerAge(pm.GetComponent<PlayerManager>().PlayerAge);
            GDiffManager.setPlayerGender(pm.GetComponent<PlayerManager>().PlayerGender);
            /*DDAModelManager.setPlayerId(pm.GetComponent<PlayerManager>().PlayerName);
            DDAModelManager.setPlayerAge(pm.GetComponent<PlayerManager>().PlayerAge);
            DDAModelManager.setPlayerGender(pm.GetComponent<PlayerManager>().PlayerGender);*/
        }

        GDiffManager.setActivity(GameDifficultyManager.GDActivityEnum.TANK);
        //DDAModelManager.setChallengeId("Tank");
        destroyLevel();
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
        foreach (Button b in question.GetComponentsInChildren<Button>())
            b.onClick.AddListener(delegate { ClickButton(b); });
        bool end = false;
        bool alldead = false;
        if (player == null || !player.gameObject.activeSelf)
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
            
            
            if (alldead && player.gameObject.activeSelf)
            {
                win = true;
            }
            if (win)
                TWin.enabled = true;
            else
                TFail.enabled = true;

            destroyLevel();
            if (DDAModelManager.Algorithm == DDAModel.DDAAlgorithm.DDA_PMDELTA)//!GDiffManager.isUsingLRModel())
            {
                if (win)
                    Score++;

                double[] betas = GDiffManager.getBetas(); //DDAModelManager.DdaModel.LogReg.Betas;
                if (betas == null)
                    betas = new double[2];

                this.log(GDiffManager.getPlayerId(),
                    betas[0],
                    betas[1],
                    GDiffManager.getModelQuality(),
                    GDiffManager.isUsingLRModel(),
                    (float)GDiffManager.getTargetDiff(),
                    nextDifficulty,
                    win,
                    answer,
                    GDiffManager.getPlayerAge(),
                    GDiffManager.getPlayerGender());
               /* this.log(DDAModelManager.getPlayerId(),
                    betas[0],
                    betas[1],
                    DDAModelManager.DdaModel.LRAccuracy,
                    GDiffManager.isUsingLRModel(),
                    (float)GDiffManager.getTargetDiff(),
                    nextDifficulty,
                    win,
                    answer,
                    DDAModelManager.getPlayerAge(),
                    DDAModelManager.getPlayerGender());*/

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
            else
            {
                if (!explained)
                {
                    StartCoroutine("explication");
                }
                else
                {
                    if (!firstLevel)
                    {
                        Cursor.visible = true;
                        AimCursor.gameObject.SetActive(false);
                        question.SetActive(true);
                        if (numLevel == 0)
                        {
                            questionAnswered = true;
                            question.SetActive(false);
                        }
                        if (questionAnswered)
                        {
                            Cursor.visible = false;
                            AimCursor.gameObject.SetActive(true);
                            if (win)
                                Score++;

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
                                    win,
                                    answer,
                                    GDiffManager.getPlayerAge(),
                                    GDiffManager.getPlayerGender());

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
                    else
                    {
                        firstLevel = false;
                        if (win)
                            Score++;

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
                                win,
                                answer,
                                GDiffManager.getPlayerAge(),
                                GDiffManager.getPlayerGender());

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
            }
        }
    }

    public void log(string player, double beta0, double beta1, double accuracy, bool usedModel, float targetDiff, float param, bool win, float answer, string age, string sexe)
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

                sw.Write("Time;beta0;beta1;accuracy;used Model;target Diff;param Diff;win;answer;age;sexe\n");

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
            sw.Write((win ? 1 : 0) + ";");
            sw.Write(answer + ";");
            sw.Write(age + ";");
            sw.Write(sexe + "\n");

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

    public void ClickButton(Button B)
    {
        if (B.name == "BFacile")
        {
            answer = -1;
            questionAnswered = true;
        }
        else if (B.name == "BPareil")
        {
            answer = 0;
            questionAnswered = true;
        }
        else if (B.name == "BDur")
        {
            answer = 1;
            questionAnswered = true;
        }

    }
}
