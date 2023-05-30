using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Hochschule Duesseldorf University of Applied Sciences
/// Department of Media
/// Author:	Till Davin	<till.davin@study.hs-duesseldorf.de>
/// SharpOSC is authored by ValdemarOrn https://github.com/ValdemarOrn/SharpOSC
/// Code partially copied from UnityOSC https://github.com/jorgegarcia/UnityOSC/blob/master/src/OSC/OSCReciever.cs
/// Small helper class to parse messages received from a seperate UDPListener thread to the Unity main thread.
/// Just initialize this class to receive data, check for HasMessagesWaiting and get a message with GetNextMessage.
/// </summary>
namespace SharpOSC
{
    public class OscReceiver : MonoBehaviour
    {
        [SerializeField]
        int port = 55555;

        Queue<OscMessage> messageQueue = new Queue<OscMessage>();
        UDPListener listener;
        HandleOscPacket receiveCallback;

        // constructor
        void OnEnable()
        {
            receiveCallback = AddMessageToQueue;
            listener = new UDPListener(port, receiveCallback);
        }

        void AddMessageToQueue(OscPacket packet)
        {
            lock (messageQueue)
            {
                if (packet as OscBundle != null)
                {
                    OscBundle bundle = (OscBundle)packet;

                    foreach (object msg in bundle.Messages)
                    {
                        if (msg as OscMessage != null)
                        {
                            messageQueue.Enqueue((OscMessage)msg);
                        }
                        else
                        {
                            Debug.LogError("Bundle contains invalid OSC message.");
                        }
                    }
                }
                else if (packet as OscMessage != null)
                {
                    messageQueue.Enqueue((OscMessage)packet);
                }
                else
                {
                    Debug.LogError("Packet is not a valid OSC message.");
                }
            }
        }

        private void OnDisable()
        {
            listener.Close();
            receiveCallback = null;
        }

        public void ChangePort(int newPort)
        {
            port = newPort;
            listener.Close();
            messageQueue.Clear();
            listener = new UDPListener(port, receiveCallback);
        }

        public bool HasMessagesWaiting()
        {
            lock (messageQueue)
            {
                return 0 < messageQueue.Count;
            }
        }

        public OscMessage GetNextMessage()
        {
            lock (messageQueue)
            {
                return messageQueue.Dequeue();
            }
        }
    }
}