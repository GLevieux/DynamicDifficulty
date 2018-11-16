using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankAI : MonoBehaviour {

    Complete.TankMovement tm;
    Complete.TankShooting ts;
    private Transform player;
    private Transform[] ennemies;
    public float timeBetweenShot = 1.0f;
    public float speedTurn = 1.0f;
    public float speedMove = 0.5f;
    public float precision = 0.5f;
    private float cooldownFirstShot = 1;
    private float cooldownShot = 0;
    private float coolDownVariabilityDir = 0;
    private Vector3 dirVariability;

	// Use this for initialization
	void Start () {
        tm = GetComponent<Complete.TankMovement>();
        ts = GetComponent<Complete.TankShooting>();
    }

    public void setPlayer(Transform player, Transform [] ennemies)
    {
        this.player = player;
        this.ennemies = ennemies;
    }
	
	// Update is called once per frame
	void LateUpdate () {
        if(cooldownFirstShot > 0)
            cooldownFirstShot -= Time.deltaTime;
        coolDownVariabilityDir -= Time.deltaTime;
        if(coolDownVariabilityDir <= 0)
        {
            coolDownVariabilityDir = 1.0f;
            dirVariability = Random.onUnitSphere;
            dirVariability.y = 0;
        }

        //On détermine ou il veut aller
        //Il veut aller vers le joueur, mais aussi s'éloigner de ses potes
        //On fait un vecteur de répulsion
        Vector3 repulsion = new Vector3();
        for (int i = 0; i < ennemies.Length; i++)
        {
            if (ennemies[i] != transform)
            {
                Vector3 dirToOtherAi = (transform.position - ennemies[i].position);
                float dist = dirToOtherAi.magnitude;
                repulsion += (dirToOtherAi.normalized * 40) / dist;
            }              
        }        

        //Calcul des params de shoot si le joueur ne bougeait pas
        float distance = (transform.GetComponent<Complete.TankShooting>().m_FireTransform.position - player.position).magnitude;
        float theta = Mathf.Asin(transform.GetComponent<Complete.TankShooting>().m_FireTransform.forward.y);
        float g = Mathf.Abs(Physics.gravity.y);
        float speed = Mathf.Sqrt((distance / (Mathf.Sin(2 * theta))) * g);
        float time = (2 * speed * Mathf.Sin(theta)) / g;

        //On prevoit la position de l'ennemi si ce temps s'écoule
        Vector3 nextPos = player.transform.position + (player.GetComponent<Complete.TankMovement>().m_Speed *
            player.GetComponent<Complete.TankMovement>().m_MovementInputValue * time * player.transform.forward);

        //On perturbe cette prochaine position en fonction de la qualité de l'ia
        Vector3 perturbation = dirVariability;
        perturbation.y = 0;
        perturbation *= Mathf.Clamp01(1 - precision) * 15;
        nextPos += perturbation;

        //On se base sur la prochaine position
        distance = (nextPos - transform.position).magnitude;

        Vector3 dirNextPos = (nextPos - transform.position).normalized;

        Vector3 dirPoint = nextPos + repulsion;
        float angle = Vector3.SignedAngle(transform.forward, dirPoint - transform.position, Vector3.up);
        tm.m_TurnInputValue = angle/50 * speedTurn;
        tm.m_MovementInputValue = speedMove;

        cooldownShot -= Time.deltaTime;

        //Teste le friendly fire
        bool tooClose = false;
        for(int i = 0; i < ennemies.Length; i++)
        {
            float dist = (nextPos - ennemies[i].position).magnitude;
            if(ennemies[i]!= transform)
            {
                if(dist < 10)
                {
                    tooClose = true;
                }
            }
        }

        if (Mathf.Abs(angle) < 5 && cooldownShot <= 0 && !tooClose && cooldownFirstShot <= 0)
        {
            //On vise le poinit ou va se trouver le joueur
            distance = (transform.GetComponent<Complete.TankShooting>().m_FireTransform.position - nextPos).magnitude;
            speed = Mathf.Sqrt((distance / (Mathf.Sin(2 * theta))) * g);
            
            ts.m_CurrentLaunchForce = Mathf.Max(15.0f, speed);
            ts.Fire(false, dirNextPos);
            cooldownShot = timeBetweenShot;
        }
    }
}
