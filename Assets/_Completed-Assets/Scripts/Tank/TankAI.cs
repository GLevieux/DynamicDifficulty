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
    private float cooldownShot = 0;

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
        //Calcul des params de shoot si le joueur ne bougeait pas
        float distance = (transform.GetComponent<Complete.TankShooting>().m_FireTransform.position - player.position).magnitude;
        float theta = Mathf.Asin(transform.GetComponent<Complete.TankShooting>().m_FireTransform.forward.y);
        float g = Mathf.Abs(Physics.gravity.y);
        float speed = Mathf.Sqrt((distance / (Mathf.Sin(2 * theta))) * g);
        float time = (2 * speed * Mathf.Sin(theta)) / g;

        //On prevoit la position de l'ennemi si ce temps s'écoule
        Vector3 nextPos = player.transform.position + (player.GetComponent<Complete.TankMovement>().m_Speed *
            player.GetComponent<Complete.TankMovement>().m_MovementInputValue * time * player.transform.forward);

        //On se base sur la prochaine position
        distance = (nextPos - transform.position).magnitude;
        float angle = Vector3.SignedAngle(transform.forward, nextPos - transform.position, Vector3.up);
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
                if(dist < 15)
                {
                    tooClose = true;
                }
            }

        }

        if (Mathf.Abs(angle) < 10 && cooldownShot <= 0 && !tooClose)
        {
            //On vise le poinit ou va se trouver le joueur
            distance = (transform.GetComponent<Complete.TankShooting>().m_FireTransform.position - nextPos).magnitude;
            speed = Mathf.Sqrt((distance / (Mathf.Sin(2 * theta))) * g);
            
            ts.m_CurrentLaunchForce = Mathf.Max(15.0f, speed);
            ts.Fire();
            cooldownShot = timeBetweenShot;
        }
            


    }
}
