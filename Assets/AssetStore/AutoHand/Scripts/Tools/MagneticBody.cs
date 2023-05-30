using Autohand;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MagneticBody : MonoBehaviour
{
    public int magneticIndex = 0;
    public float strengthMultiplyer = 1f;
    public UnityMagneticEvent magneticEnter;
    public UnityMagneticEvent magneticExit;

    [HideInInspector]
    public Rigidbody body;
    private void Start() {
        body = GetComponent<Rigidbody>();
    }
}
