using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class BottomTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Bottom reached");
            StartAndRestartScene restartObj = FindObjectOfType<StartAndRestartScene>();
            restartObj.setBottomReached(true);
            restartObj.setLock(false);
            GameObject depthTextObject = GameObject.Find("DepthText");
            if (depthTextObject != null)
            {
            TextMeshPro depthTextComponent = depthTextObject.GetComponent<TextMeshPro>();
            depthTextComponent.text = "Touch to\nRestart";
            }
        }
    }
}
