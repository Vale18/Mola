using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleController : MonoBehaviour
{

    public Nexus_Listener nexus_Listener;
    public ParticleSystem breathBubble;

    // Start is called before the first frame update
    void Start()
    {
        nexus_Listener = GetComponent<Nexus_Listener>();
        breathBubble = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        int activate = nexus_Listener.breathe;

        if (activate == 1) {
            breathBubble.Play();
        } else {
            breathBubble.Stop();
        }
    }
}
