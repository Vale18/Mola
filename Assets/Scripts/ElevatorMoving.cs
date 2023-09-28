using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorMoving : MonoBehaviour
{
    public float minSpeed = 1f;
    public float maxSpeed = 50f;

    [Header("Speed Transition Times")]
    public float speedUpTime = 3f;    // Time in seconds to reach maxSpeed
    public float slowDownTime = 3f;   // Time in seconds to reach minSpeed

    [Header("Speed Up Section 1")]
    public float speedUpStartDepth1 = -30f;
    public float speedUpEndDepth1 = -400f;

    [Header("Speed Up Section 2")]
    public float speedUpStartDepth2 = -507f;
    public float speedUpEndDepth2 = -1200f;

    [Header("Speed Up Section 3")]
    public float speedUpStartDepth3 = -1307f;
    public float speedUpEndDepth3 = -4200f;

    [Header("Speed Up Section 4")]
    public float speedUpStartDepth4 = -4307f;
    public float speedUpEndDepth4 = -6200f;

    [Header("Speed Up Section 5")]
    public float speedUpStartDepth5 = -6307f;
    public float speedUpEndDepth5 = -10850f;

    [Header("End")]
    public float stopDepth = -11000f;

    private float currentSpeed;
    private float speedLerpTime;
    private bool canMove = true;
    

    private void Start()
    {

    }


    private void Update()
    {
        if (!canMove) return;

        float currentDepth = transform.position.y;

        bool isInSpeedUpZone = 
            (currentDepth <= speedUpStartDepth1 && currentDepth >= speedUpEndDepth1) ||
            (currentDepth <= speedUpStartDepth2 && currentDepth >= speedUpEndDepth2) ||
            (currentDepth <= speedUpStartDepth3 && currentDepth >= speedUpEndDepth3) ||
            (currentDepth <= speedUpStartDepth4 && currentDepth >= speedUpEndDepth4) ||
            (currentDepth <= speedUpStartDepth5 && currentDepth >= speedUpEndDepth5);

        if ( currentDepth <= stopDepth)
        {
            canMove = false;
        }

        else if (isInSpeedUpZone)
        {
            speedLerpTime += Time.deltaTime / speedUpTime;
        }
        else
        {
            speedLerpTime -= Time.deltaTime / slowDownTime;
        }

        speedLerpTime = Mathf.Clamp01(speedLerpTime);
        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, speedLerpTime);

        // Adjust the elevator's position based on the calculated speed
        transform.position = new Vector3(transform.position.x, transform.position.y - currentSpeed * Time.deltaTime, transform.position.z);
    }
    public void setCanMove(bool canMove)
    {
        this.canMove = canMove;
    }
}
