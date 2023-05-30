using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpOSC;

public class OscSend : MonoBehaviour
{
    OscMessage message;
    UDPSender sender;
    void Start()
    {
        message = new OscMessage("/test/1", 23, 42.01f, "hello world");
        sender = new UDPSender("127.0.0.1", 55555);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            sender.Send(message);
        }
    }
}