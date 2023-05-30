using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand.Demo {
    
    [System.Serializable]
    public struct OVRControllerEventData {
        public OVRInput.Controller controller;
        public OVRInput.Button button;
        public UnityEvent OnPress;
        public UnityEvent OnRelease;
        internal bool pressed;
    }
    
    public class OVRControllerEvent : MonoBehaviour{
        public OVRControllerEventData[] eventList;

        void Update(){
            for (int i = 0; i < eventList.Length; i++){
                if (!eventList[i].pressed && OVRInput.GetDown(eventList[i].button, eventList[i].controller)) {
                    eventList[i].OnPress?.Invoke();
                    eventList[i].pressed = true;
                }
                if (eventList[i].pressed && OVRInput.GetUp(eventList[i].button, eventList[i].controller)) { 
                    eventList[i].OnRelease?.Invoke();
                    eventList[i].pressed = false;
                }
            }
        }
    }
}