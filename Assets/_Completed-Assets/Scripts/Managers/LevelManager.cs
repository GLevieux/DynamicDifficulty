using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{

    public Transform tankPrefab;
    public Transform[] spawnPositions;

    Transform player;
    Transform[] ennemies;

    public DDAModelUnityBridge DDAModelManager;

    [System.Serializable]
    public class DiffCurve
    {
        [Range(0, 1)]
        public float[] DiffStepsPlaying;

        public float getDifficulty(int step)
        {
            if (DiffStepsPlaying == null)
                return 0;

            while (step >= DiffStepsPlaying.Length)
                step -= DiffStepsPlaying.Length;

            if (DiffStepsPlaying != null && step < DiffStepsPlaying.Length)
                return DiffStepsPlaying[step];

            return 0;
        }
    }

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

    private DDAModel.DiffParams lastDiffParams;
    private float nextDifficulty = 0;
    private bool waitingForNewLevel = true;
    private int numLevel = 0;
    private int numLevelLogReg = 0;

    public DiffCurve DifficultyCurve;

    private int Score = 0;
    float answer = -10;
    bool questionAnswered = false;
    bool win = false;

    bool explained = false;
    bool explaining = false;
    bool expLogReg = false;
    bool firstLevel = true;
    bool logged = false;
    bool ask = true;
    int NbLevelTuto = 15;
    int NbLevelRandom = 35;

    GameObject question;
    GameObject Explication;
    GameObject ExpLogReg;
    GameObject Map;

    public Transform AimCursor;

    IEnumerator deuxSecTimer()
    {
        yield return new WaitForSeconds(2);
        Cursor.visible = true;
        AimCursor.gameObject.SetActive(false);
        question.SetActive(true);


    }

    IEnumerator explication()
    {
        explaining = true;
        TWin.enabled = false;
        TFail.enabled = false;
        Map.SetActive(false);
        Cursor.visible = true;
        AimCursor.gameObject.SetActive(false);
        Explication.SetActive(true);
        yield return new WaitForSeconds(2);
        explained = true;
    }

    IEnumerator explicationLogReg()
    {
        expLogReg = true;
        explaining = true;
        TWin.enabled = false;
        TFail.enabled = false;
        Map.SetActive(false);
        Cursor.visible = true;
        AimCursor.gameObject.SetActive(false);
        ExpLogReg.SetActive(true);
        yield return new WaitForSeconds(2);
    }
        IEnumerator nextLevel()
    {
        questionAnswered = false;
        logged = false;
        win = false;
        CompteARebours.enabled = false;
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
    void Start()
    {
        Map = GameObject.Find("Map");
        Explication = GameObject.Find("Explication");
        Explication.SetActive(false);
        ExpLogReg = GameObject.Find("ExpLogReg");
        ExpLogReg.SetActive(false);
        question = GameObject.Find("Question");
        question.SetActive(false);

        DDAModelManager.setDDAAlgorithm(DDAModel.DDAAlgorithm.DDA_PMDELTA);
        DDAModelManager.setPlayerId("MonSuperJoueur");
        GameObject pm = GameObject.Find("PlayerManager");
        if (pm)
        {
            DDAModelManager.setPlayerId(pm.GetComponent<PlayerManager>().PlayerName);
            DDAModelManager.setPlayerAge(pm.GetComponent<PlayerManager>().PlayerAge);
            DDAModelManager.setPlayerGender(pm.GetComponent<PlayerManager>().PlayerGender);
        }

        DDAModelManager.setChallengeId("Tank");
        destroyLevel();

        //Premier niveau
        lastDiffParams = DDAModelManager.computeNewDiffParams();
        nextDifficulty = (float)lastDiffParams.Theta;
        createLevel(nextDifficulty);

        foreach (Button b in question.GetComponentsInChildren<Button>())
            b.onClick.AddListener(delegate { ClickButton(b); });
        Button BValider = Explication.GetComponentInChildren<Button>();
        BValider.onClick.AddListener(delegate { ClickButton(BValider); });

        Button BValider2 = ExpLogReg.GetComponentInChildren<Button>();
        BValider2.onClick.AddListener(delegate { ClickButton(BValider2); });

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
    void Update()
    {
        if (waitingForNewLevel)
            return;

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



            if (!logged)
            {
                ask = true;
                if (numLevel >= NbLevelTuto - 1 && !explained)
                    StartCoroutine("explication");
                if (numLevel > NbLevelTuto && numLevel < NbLevelRandom)
                    DDAModelManager.setDDAAlgorithm(DDAModel.DDAAlgorithm.DDA_RANDOM_LOGREG);
               

                if (win)
                    Score++;
                double[] betas = new double[2];
                if (lastDiffParams.Betas != null && lastDiffParams.Betas.Length > 0)
                {
                    betas = lastDiffParams.Betas;
                }
                if(DDAModelManager.getDDAAlgorithm() == DDAModel.DDAAlgorithm.DDA_LOGREG || DDAModelManager.getDDAAlgorithm() == DDAModel.DDAAlgorithm.DDA_PMDELTA || (DDAModelManager.getDDAAlgorithm() == DDAModel.DDAAlgorithm.DDA_RANDOM_LOGREG && firstLevel))
                {
                    this.log(DDAModelManager.getPlayerId(),
                        betas[0],
                        betas[1],
                        lastDiffParams.LRAccuracy,
                        lastDiffParams.LogRegError.ToString(),
                        lastDiffParams.AlgorithmActuallyUsed.ToString(),
                        (float)lastDiffParams.TargetDiff,
                        (float)lastDiffParams.Theta,
                        win,
                        answer,
                        DDAModelManager.getPlayerAge(),
                        DDAModelManager.getPlayerGender());
                }
                
                logged = true;



                double[] vars = new double[1];
                vars[0] = nextDifficulty;
                DDADataManager.Attempt lastAttempt = new DDADataManager.Attempt();
                if (win)
                    lastAttempt.Result = 1;
                else
                    lastAttempt.Result = 0;
                lastAttempt.Thetas = vars;
                DDAModelManager.addLastAttempt(lastAttempt);

            }
            

            if (DDAModelManager.getDDAAlgorithm() == DDAModel.DDAAlgorithm.DDA_PMDELTA && !explaining)
            {
                lastDiffParams = DDAModelManager.computeNewDiffParams();
                nextDifficulty = (float)lastDiffParams.Theta;
                numLevel++;
                StartCoroutine("nextLevel");
            }
            if (DDAModelManager.getDDAAlgorithm() == DDAModel.DDAAlgorithm.DDA_RANDOM_LOGREG && !explaining)
            {
                if (!firstLevel)
                {
                    if (ask)
                    {
                        ask = false;
                        StartCoroutine("deuxSecTimer");
                    }
                    if (questionAnswered)
                    {
                        double[] betas = new double[2];
                        if (lastDiffParams.Betas != null && lastDiffParams.Betas.Length > 0)
                        {
                            betas = lastDiffParams.Betas;
                        }
                        this.log(DDAModelManager.getPlayerId(),
                            betas[0],
                            betas[1],
                            lastDiffParams.LRAccuracy,
                            lastDiffParams.LogRegError.ToString(),
                            lastDiffParams.AlgorithmActuallyUsed.ToString(),
                            (float)lastDiffParams.TargetDiff,
                            (float)lastDiffParams.Theta,
                            win,
                            answer,
                            DDAModelManager.getPlayerAge(),
                            DDAModelManager.getPlayerGender());

                        lastDiffParams = DDAModelManager.computeNewDiffParams();
                        nextDifficulty = (float)lastDiffParams.Theta;
                        numLevel++;
                        if (numLevel > NbLevelRandom)
                        {
                            if (!expLogReg)
                            {
                                StartCoroutine("explicationLogReg");
                            }
                            DDAModelManager.setDDAAlgorithm(DDAModel.DDAAlgorithm.DDA_LOGREG);
                            answer = -10;
                        }
                        else
                            StartCoroutine("nextLevel");
                    }
                }
                else
                {
                    firstLevel = false;
                    lastDiffParams = DDAModelManager.computeNewDiffParams();
                    nextDifficulty = (float)lastDiffParams.Theta;
                    numLevel++;
                }
            }
            if (DDAModelManager.getDDAAlgorithm() == DDAModel.DDAAlgorithm.DDA_LOGREG && !explaining)
            {
                lastDiffParams = DDAModelManager.computeNewDiffParams(DifficultyCurve.getDifficulty(numLevelLogReg));//0.2 0.5 0.7
                nextDifficulty = (float)lastDiffParams.Theta; ;
                numLevel++;
                numLevelLogReg++;
                StartCoroutine("nextLevel");
            }

            if (numLevel >= 100)
                SceneManager.LoadScene(2);
        }
    }

    public void log(string player, double beta0, double beta1, double accuracy, string logRegError, string usedAlgorithm, float targetDiff, float param, bool win, float answer, string age, string sexe)
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

                sw.Write("Time;beta0;beta1;accuracy;log reg error;used Model;target Diff;param Diff;win;answer;age;sexe\n");

                sw.Flush();
                ofs.Flush();
                sw.Close();
                ofs.Close();
            }

            ofs = new FileStream(csvFile, FileMode.Append);
            sw = new StreamWriter(ofs);

            string dateTime = System.DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
            sw.Write(dateTime + ";");
            sw.Write(beta0 + ";");
            sw.Write(beta1 + ";");
            sw.Write(accuracy + ";");
            sw.Write(logRegError + ";");
            sw.Write(usedAlgorithm + ";");
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
        else if (B.name == "BValider")
        {
            Explication.SetActive(false);
            Map.SetActive(true);
            explaining = false;
        }
        else if (B.name == "BValider2")
        {
            ExpLogReg.SetActive(false);
            Map.SetActive(true);
            explaining = false;
        }

        Cursor.visible = false;
        AimCursor.gameObject.SetActive(true);
        question.SetActive(false);

    }
}
