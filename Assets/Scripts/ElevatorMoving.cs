using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorMoving : MonoBehaviour
{
    public float speed = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void FixedUpdate()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y - speed, transform.position.z);
    }
}
