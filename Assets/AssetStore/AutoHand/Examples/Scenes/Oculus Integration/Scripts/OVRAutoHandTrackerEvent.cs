using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand.Demo{
    [System.Serializable]
    public struct FingerBendData {
        public float bendValue;
        public OVRFingerEnum finger;
    }

    public class OVRAutoHandTrackerEvent : MonoBehaviour{
        public OVRAutoHandTracker handTracker;
        [Tooltip("Every Finger must be bent past the bendValue to trigger the event")]
        public FingerBendData[] fingerBendPast;
        [Tooltip("Every Finger must be bent before the bendValue to trigger the event")]
        public FingerBendData[] fingerBendBelow;

        public UnityEvent Pressed;
        public UnityEvent Released;

        bool pressed;

        // Update is called once per frame
        void Update(){
            if(!pressed && IsPressed()) {
                pressed = true;
                Pressed?.Invoke();
            }
            else if(pressed && !IsPressed()) {
                pressed = false;
                Released?.Invoke();
            }
        }

        public bool IsPressed(){
            bool requiredFingers = true;
            
            for (int i = 0; i < fingerBendPast.Length; i++){
                if(handTracker.GetFingerCurl(fingerBendPast[i].finger) < fingerBendPast[i].bendValue){
                    requiredFingers = false;
                }
            }

            for (int i = 0; i < fingerBendBelow.Length; i++){
                if(handTracker.GetFingerCurl(fingerBendBelow[i].finger) > fingerBendBelow[i].bendValue){
                    requiredFingers = false;
                }
            }

            return requiredFingers;
        }
    }
}