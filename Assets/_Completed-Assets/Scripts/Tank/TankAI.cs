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
        float distance = (player.position - transform.position).magnitude;
        float angle = Vector3.SignedAngle(transform.forward, player.position - transform.position, Vector3.up);
        tm.m_TurnInputValue = angle/50 * speedTurn;
        tm.m_MovementInputValue = speedMove;

        cooldownShot -= Time.deltaTime;

        //Teste le friendly fire
        bool tooClose = false;
        for(int i = 0; i < ennemies.Length; i++)
        {
            float dist = (player.position - ennemies[i].position).magnitude;
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
            ts.m_CurrentLaunchForce = Mathf.Max(15.0f,distance);
            ts.Fire();
            cooldownShot = timeBetweenShot;
        }
            


    }
}
