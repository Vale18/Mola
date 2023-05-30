using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
    [RequireComponent(typeof(OVRSkeleton)), DefaultExecutionOrder(1)]
    public class OVRAutoHandTracker : MonoBehaviour{
    
        OVRSkeleton skeleton;
        float indexBend;
        float middleBend;
        float ringBend;
        float pinkyBend;
        float thumbBend;
        
        Quaternion lastIndexFingerRot1;
        Quaternion lastIndexFingerRot2;
        Quaternion lastMiddleFingerRot1;
        Quaternion lastMiddleFingerRot2;
        Quaternion lastRingFingerRot1;
        Quaternion lastRingFingerRot2;
        Quaternion lastPinkyFingerRot1;
        Quaternion lastPinkyFingerRot2;
        Quaternion lastThumbFingerRot1;
        Quaternion lastThumbFingerRot2;
        Quaternion lastThumbFingerRot3;
        
        public float GetFingerCurl(OVRFingerEnum finger) {
            switch(finger){
                case OVRFingerEnum.index:
                    return indexBend*0.7f;
                case OVRFingerEnum.middle:
                    return middleBend*0.7f;
                case OVRFingerEnum.ring:
                    return ringBend*0.7f;
                case OVRFingerEnum.pinky:
                    return pinkyBend*0.7f;
                case OVRFingerEnum.thumb:
                    return thumbBend*0.7f;
            };

            return 1;
        }

        //YOU CAN INCREASE THE FINGER BEND SPEED BY INCREASING THE fingerSmoothSpeed VALUE ON EACH FINGER
        //YOU CAN DISABLE FINGER SWAY BY TURNING SWAY STRENGTH ON HAND TO 0 OR DISABLEIK ENABLED

        public Hand hand;
        
        [Header("Bend Fingers")]
        public Finger thumb;
        public Finger index;
        public Finger middle;
        public Finger ring;
        public Finger pinky;
        [Tooltip("Allows fingers to move while holding an object"), Space]
        public bool freeFingers = true;

        
        [Header("Grab Action"), Space]
        [Tooltip("The required fingers to be bent to the required finger bend to call the grab event, all of these fingers needs to be past the required bend value [Represents AND]")]
        public FingerBend[] grabFingersRequired;

        [Header("Squeeze Action"), Space]
        [Tooltip("The required fingers to be bent to the required finger bend to call the squeeze event, all of these fingers needs to be past the required bend value [Represents AND]")]
        public FingerBend[] squeezeFingersRequired;
        
        bool grabbing;
        bool squeezing;
        Rigidbody body;
        CollisionDetectionMode mode;

        void Start(){
            skeleton = GetComponent<OVRSkeleton>();
            body = GetComponent<Rigidbody>();
            mode = body.collisionDetectionMode;
        }

        private void OnEnable() {
            hand.disableIK = true; 
        }

        private void OnDisable() {
            body.isKinematic = false;
            body.collisionDetectionMode = mode;
            hand.disableIK = false;
            thumb.secondaryOffset = 0;
            index.secondaryOffset = 0;
            middle.secondaryOffset = 0;
            ring.secondaryOffset = 0;
            pinky.secondaryOffset = 0;
        }

        void FixedUpdate(){
            foreach(OVRBone bone in skeleton.Bones) {
                if (bone.Id == OVRSkeleton.BoneId.Hand_Index1) 
                    indexBend = - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Index2) 
                    indexBend +=  - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Middle1) 
                    middleBend = - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Middle2) 
                    middleBend += - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Ring1) 
                    ringBend = - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Ring2) 
                    ringBend += - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Pinky1) 
                    pinkyBend = - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Pinky2) 
                    pinkyBend += - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Thumb1) 
                    thumbBend = - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Thumb2) 
                    thumbBend += - bone.Transform.localRotation.z;
                if (bone.Id == OVRSkeleton.BoneId.Hand_Thumb3) 
                    thumbBend += - bone.Transform.localRotation.z;
            }


            if(hand.IsGrabbing())
                return;

            if(!skeleton.IsDataHighConfidence) {
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.isKinematic = true;
                return;
            }
            else {
                body.collisionDetectionMode = mode;
                body.isKinematic = false;
            }

            if(!OVRInput.IsControllerConnected(OVRInput.Controller.Hands)) {
                return;
            }

            
            bool grab = IsGrabbing();
            if(!grabbing && grab) {
                grabbing = true;
                hand.Grab();
            }

            if(grabbing && !grab) {
                grabbing = false;
                hand.Release();
                foreach(var finger in hand.fingers) {
                    finger.SetFingerBend(0);
                }
            }


            bool squeeze = IsSqueezing();
            if(!squeezing && squeeze) {
                squeezing = true;
                hand.Squeeze();
            }

            if(squeezing && !squeeze) {
                squeezing = false;
                hand.Unsqueeze();
            }
            
            if(!hand.holdingObj && !hand.IsPosing()) {
                thumb.secondaryOffset = GetFingerCurl(OVRFingerEnum.thumb);
                index.secondaryOffset = GetFingerCurl(OVRFingerEnum.index);
                middle.secondaryOffset = GetFingerCurl(OVRFingerEnum.middle);
                ring.secondaryOffset = GetFingerCurl(OVRFingerEnum.ring);
                pinky.secondaryOffset = GetFingerCurl(OVRFingerEnum.pinky);

                hand.SetGrip((thumb.secondaryOffset + index.secondaryOffset + middle.secondaryOffset + ring.secondaryOffset + pinky.secondaryOffset) / 5f);

                thumb.UpdateFinger();
                index.UpdateFinger();
                middle.UpdateFinger();
                ring.UpdateFinger();
                pinky.UpdateFinger();
            }
            else if(freeFingers && hand.holdingObj && !hand.IsPosing()) {
                foreach(var finger in hand.fingers)
                    finger.SetFingerBend(0);

                thumb.secondaryOffset = thumb.GetLastHitBend();
                index.secondaryOffset = index.GetLastHitBend();
                middle.secondaryOffset = middle.GetLastHitBend();
                ring.secondaryOffset = ring.GetLastHitBend();
                pinky.secondaryOffset = pinky.GetLastHitBend();

                if(GetFingerCurl(OVRFingerEnum.thumb) < thumb.GetLastHitBend())
                    thumb.secondaryOffset = GetFingerCurl(OVRFingerEnum.thumb);

                if(GetFingerCurl(OVRFingerEnum.index) < index.GetLastHitBend())
                    index.secondaryOffset = GetFingerCurl(OVRFingerEnum.index);

                if(GetFingerCurl(OVRFingerEnum.middle) < middle.GetLastHitBend())
                    middle.secondaryOffset = GetFingerCurl(OVRFingerEnum.middle);

                if(GetFingerCurl(OVRFingerEnum.ring) < ring.GetLastHitBend())
                    ring.secondaryOffset = GetFingerCurl(OVRFingerEnum.ring);

                if(GetFingerCurl(OVRFingerEnum.pinky) < pinky.GetLastHitBend())
                    pinky.secondaryOffset = GetFingerCurl(OVRFingerEnum.pinky);

                thumb.UpdateFinger();
                index.UpdateFinger();
                middle.UpdateFinger();
                ring.UpdateFinger();
                pinky.UpdateFinger();
            }
            
        }
        



        public bool IsGrabbing(){
            bool requiredFingers = true;
            
            if(grabFingersRequired.Length == 0)
                requiredFingers = false;
            else
                for (int i = 0; i < grabFingersRequired.Length; i++){
                    if(GetFingerCurl(grabFingersRequired[i].finger) < grabFingersRequired[i].amount){
                        requiredFingers = false;
                    }
                }

            return requiredFingers;
        }


        public bool IsSqueezing(){
            bool requiredFingers = true;
            
            if(squeezeFingersRequired.Length == 0)
                requiredFingers = false;
            else
                for (int i = 0; i < squeezeFingersRequired.Length; i++){
                    if (GetFingerCurl(squeezeFingersRequired[i].finger) < squeezeFingersRequired[i].amount){
                        requiredFingers = false;
                    }
                }

            return requiredFingers;
        }
    }

    public enum OVRFingerEnum {
        index,
        middle,
        ring,
        pinky,
        thumb
    }

    [System.Serializable]
    public struct FingerBend {
        public float amount;
        public OVRFingerEnum finger;
    }
}
