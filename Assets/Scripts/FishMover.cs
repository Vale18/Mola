using UnityEngine;

public class FishMover : MonoBehaviour
{
    public float speed = 5f;
    private const float waterSurfaceY = -1f;
    private const float minCenterDistance = 15f;
    private Vector3 moveDirection;

    private void Start()
    {
        moveDirection = transform.forward;  // Initial move direction is the direction the fish is facing when spawned
    }

    private void Update()
    {
        // If fish is near or above the water surface, adjust its direction to move it downward
        if (transform.position.y >= waterSurfaceY)
        {
            moveDirection = Vector3.down;
        }
        // If fish is too close to the center, reflect its move direction based on its incidence angle to the center
        else if (Vector3.Distance(transform.position, new Vector3(0, transform.position.y, 0)) < minCenterDistance)
        {
            Vector3 directionToCenter = (transform.position - new Vector3(0, transform.position.y, 0)).normalized;
            moveDirection = Vector3.Reflect(moveDirection, directionToCenter);
        }

        transform.rotation = Quaternion.LookRotation(moveDirection);
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
