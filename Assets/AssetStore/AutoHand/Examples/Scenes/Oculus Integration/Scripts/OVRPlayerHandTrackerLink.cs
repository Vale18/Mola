using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand.Demo {
    
    public class OVRPlayerHandTrackerLink : MonoBehaviour{
         public OVRAutoHandTracker handTracker;
        public AutoHandPlayer player;
        [Tooltip("Every Finger must be bent past the bendValue to trigger the event")]
        public FingerBendData[] fingerBendPast;
        [Tooltip("Every Finger must be bent before the bendValue to trigger the event")]
        public FingerBendData[] fingerBendBelow;


        bool pressed;

        // Update is called once per frame
        void Update(){
            if(!pressed && IsPressed()) {
                pressed = true;
                player.Move(new Vector2(0, 1));

            }
            else if(pressed && !IsPressed()) {
                pressed = false;
                player.Move(new Vector2(0, 0));
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