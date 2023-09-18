using UnityEngine;

public class FishMover : MonoBehaviour
{
    public float speed = 5f;
    private const float waterSurfaceY = -3f;

    private void Update()
    {
        //if fish is at water surface
        if(transform.position.y >= waterSurfaceY)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.down);
        }

        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
