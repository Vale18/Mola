using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class StartAndRestartScene : MonoBehaviour
{
    public GameObject player;
    public ElevatorMoving elevatorMoving;
    private TextMeshPro depthTextComponent;
    private GameObject depthTextObject;
    private GameObject nexusListener;
    private bool locked = false;
    private bool bottomReached = false;

 void Awake()
    {
        nexusListener = FindObjectOfType<Nexus_Listener>().gameObject;
        depthTextObject = GameObject.Find("DepthText");
        nexusListener.SetActive(false);
        depthTextObject.GetComponent<DisplayDepth>().enabled = false;
        // Wenn das GameObject gefunden wurde, versuche die TextMeshPro-Komponente zu erhalten
        if (depthTextObject != null)
        {
            depthTextComponent = depthTextObject.GetComponent<TextMeshPro>();
        }
    }

    public void OnEventOccured()
    {
        Debug.Log("Event occured");
        if (locked)
            return;
        if(bottomReached)
        {
            Debug.Log("Restarting Scene");
            SceneManager.LoadScene( SceneManager.GetActiveScene().buildIndex );
        }
        else
        {
            StartCoroutine(initiateElevatorRide());
        }
       
        //TODO: Quietschsound

    }
    IEnumerator initiateElevatorRide()
    {
        Debug.Log("Elevator started");
        //TODO: Starte Erzählung 
        if(depthTextComponent != null)
        {
            depthTextComponent.text = "Preparing\nElevator...";
        }
        yield return new WaitForSeconds(10);
        if(depthTextComponent != null)
        {
            depthTextComponent.text = "ERROR\nElevator not\nresponding";
        }
        Debug.Log("Elevator moving now");
        elevatorMoving.setCanMove(true);
        yield return new WaitForSeconds(10);
        activateUnderwaterStuff();
    }
    public void setLock(bool locked)
    {
        this.locked = locked;
    }
    public void setBottomReached(bool bottomReached)
    {
        this.bottomReached = bottomReached;
    }
    public void activateUnderwaterStuff()
    {
        nexusListener.SetActive(true);
        depthTextObject.GetComponent<DisplayDepth>().enabled = true;
    }
}
