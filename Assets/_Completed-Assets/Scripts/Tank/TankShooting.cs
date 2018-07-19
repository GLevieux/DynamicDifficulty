﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class TankShooting : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify the different players.
        public Rigidbody m_Shell;                   // Prefab of the shell.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
        public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
        public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
        public float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
        public float m_MaxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
        public float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.
        public float deltaTimeFire = 0.5f;


        private string m_FireButton;                // The input axis that is used for launching shells.
        public float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
        private bool m_Fired;                       // Whether or not the shell has been launched with this button press.

        public List<Transform> Bullets = new List<Transform>();

        public void OnDestroy()
        {
            foreach (Transform t in Bullets)
            {
                if (t != null)
                {
                    Debug.Log("Disab : Destroy bullet " + t.gameObject);
                    if (t.gameObject != null)
                        Destroy(t.gameObject);
                }
            }
                
        }


        public void OnDisable()
        {
            foreach (Transform t in Bullets)
            {
                if(t != null)
                {
                    Debug.Log("Disab : Destroy bullet " + t.gameObject);
                    if (t.gameObject != null)
                        Destroy(t.gameObject);
                }
                
            }
        }



        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force and the UI
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
        }


        private void Start ()
        {
            // The fire axis is based on the player number.
            m_FireButton = "Fire" + m_PlayerNumber;

            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }


        private void Update ()
        {
            if (m_PlayerNumber > 2)
                return;

            deltaTimeFire -= Time.deltaTime;

            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_MinLaunchForce;

            // If the max force has been exceeded and the shell hasn't yet been launched...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                // ... use the max force and launch the shell.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire ();
            }
            // Otherwise, if the fire button has just started being pressed...
            else if ((Input.GetButtonDown (m_FireButton) || Input.GetButtonDown("Fire1")) && deltaTimeFire < 0)
            {
                // ... reset the fired flag and reset the launch force.
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;

                // Change the clip to the charging clip and start it playing.
                //m_ShootingAudio.clip = m_ChargingClip;
                //m_ShootingAudio.Play ();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if ((Input.GetButton (m_FireButton) || Input.GetButton("Fire1")) && !m_Fired)
            {
                // Increment the launch force and update the slider.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

                //m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            else if (Input.GetButtonUp (m_FireButton) && !m_Fired)
            {
                // ... launch the shell.
                Fire ();
            }
            else if (Input.GetButtonUp("Fire1") && !m_Fired)
            {
                // ... launch the shell.
                Fire(true);
            }
        }

        private float getSpeedToShootPoint(Vector3 point)
        {
            float distance = (transform.position - point).magnitude - 2;
            float theta = Mathf.Asin(transform.GetComponent<Complete.TankShooting>().m_FireTransform.forward.y);
            float g = Mathf.Abs(Physics.gravity.y);
            float speed = Mathf.Sqrt((distance / (Mathf.Sin(2 * theta))) * g);
            return speed;
        }


        public void Fire (bool useMouse = false, Vector3 forcedDir = default(Vector3))
        {
            // Set the fired flag so only Fire is only called once.
            m_Fired = true;
            deltaTimeFire = 0.5f;

            // Create an instance of the shell and store a reference to it's rigidbody.
            Rigidbody shellInstance = null;

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            if (useMouse)
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    Vector3 dir = (hit.point - transform.position).normalized;
                    dir.y = transform.GetComponent<Complete.TankShooting>().m_FireTransform.forward.y;
                    shellInstance = Instantiate(m_Shell, transform.position + dir*2, Quaternion.FromToRotation(Vector3.forward,dir)) as Rigidbody;
                    shellInstance.velocity = dir * getSpeedToShootPoint(hit.point);
                }
            }
            else if (forcedDir != default(Vector3))
            {
                Vector3 forcedDirNorm = forcedDir.normalized;
                forcedDirNorm.y = transform.GetComponent<Complete.TankShooting>().m_FireTransform.forward.y;
                shellInstance = Instantiate(m_Shell, transform.position + forcedDirNorm * 2, Quaternion.FromToRotation(Vector3.forward, forcedDirNorm)) as Rigidbody;
                shellInstance.velocity = forcedDirNorm * m_CurrentLaunchForce;
            } else  {
                shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
                shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
            }

            if(shellInstance != null)
                Bullets.Add(shellInstance.transform);

            // Change the clip to the firing clip and play it.
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play ();

            // Reset the launch force.  This is a precaution in case of missing button events.
            m_CurrentLaunchForce = m_MinLaunchForce;
        }
    }
}