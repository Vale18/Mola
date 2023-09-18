using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorMoving : MonoBehaviour
{
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
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.01f, transform.position.z);
    }
}
