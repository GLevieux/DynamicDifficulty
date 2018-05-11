using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour {

    public Transform tankPrefab; 
    public Transform [] spawnPositions;
    
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
    };

    public paramsDiff easyDiff;
    public paramsDiff hardDiff;

    private float nextDifficulty = 0;
    private bool waitingForNewLevel = true;
    private int numLevel = 0;

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
        currentDiff.nbEnnemies = (int)Mathf.Lerp((float)easyDiff.nbEnnemies, (float)hardDiff.nbEnnemies, difficulty);
        currentDiff.speedTurn = Mathf.Lerp(easyDiff.speedTurn, hardDiff.speedTurn, difficulty);
        currentDiff.speedMove = Mathf.Lerp(easyDiff.speedMove, hardDiff.speedMove, difficulty);
        currentDiff.timeBetweenShot = Mathf.Lerp(easyDiff.timeBetweenShot, hardDiff.timeBetweenShot, difficulty);

        spawnEverybody(currentDiff.nbEnnemies);
        for (int i = 0; i < ennemies.Length; i++)
        {
            TankAI ai = ennemies[i].GetComponent<TankAI>();
            ai.timeBetweenShot = currentDiff.timeBetweenShot;
            ai.speedTurn = currentDiff.speedTurn;
            ai.speedMove = currentDiff.speedMove;
        }

        waitingForNewLevel = false;
    }

    void setupTank(Transform tank,float patateColor, int num)
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
	void Start () {
        GDiffManager.setActivity(GameDifficultyManager.GDActivityEnum.TANK);
        GDiffManager.setPlayerId("MonSuperJoueur");
        createLevel(0.2f);
    }

    void newEnnemies(int nb, Transform player)
    {
        ennemies = new Transform[nb];
        int spawn = 1;
        for(int i = 0; i < nb; i++)
        {
            ennemies[i] = Instantiate(tankPrefab, spawnPositions[spawn].position, spawnPositions[spawn].rotation) as Transform;
            TankAI ai = ennemies[i].gameObject.AddComponent<TankAI>();
            ai.setPlayer(player,ennemies);
        
            setupTank(ennemies[i], 0.9f,i+3);
            spawn++;
            if (spawn >= spawnPositions.Length)
                spawn = 1;
        }
    }
	
	// Update is called once per frame
	void Update () {
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

        if (end)
        {
            double[] vars = new double[1];
            vars[0] = nextDifficulty;
            GDiffManager.addTry(vars, alldead);

            numLevel++;
            vars = GDiffManager.getDiffParams(numLevel);
            nextDifficulty = (float)vars[0];
            StartCoroutine("nextLevel");

        }
        
            
    }
}
