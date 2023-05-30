using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand
{
    [RequireComponent(typeof(Hand))]
    public class HandGizmos : MonoBehaviour
        {
    #if UNITY_EDITOR
        private float lastOffset;
        private Quaternion lastHandRot;
        private Vector3 lastHandPos;
        private float lastReachDistance;

        Hand hand;
        Vector3[] handRays;

        private void OnDrawGizmos() {
            if (hand == null)
                hand = GetComponent<Hand>();

            if(hand.palmTransform == null)
                return;


            if(handRays == null || handRays.Length == 0)
                handRays = hand.GetPalmRays();

            foreach(var ray in handRays) {
                Gizmos.DrawRay(hand.palmTransform.position, hand.palmTransform.rotation * ray * hand.reachDistance);
                Gizmos.DrawWireSphere(hand.palmTransform.position + hand.palmTransform.rotation * ray * hand.reachDistance, hand.sphereCastRadius);
            }

        }


        private void OnDrawGizmosSelected() {
            if(hand.palmTransform == null)
                return;
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(hand.palmTransform.position, hand.palmTransform.forward* hand.reachDistance);

            if (lastOffset == 0)
                lastOffset = hand.gripOffset;
            if (hand.gripOffset != lastOffset){
                lastOffset = hand.gripOffset;
                hand.RelaxHand();
            }

            if (lastReachDistance == 0)
                lastReachDistance = hand.reachDistance;

            if (hand.reachDistance != lastReachDistance){
                var percent = hand.reachDistance / lastReachDistance;
                lastReachDistance = hand.reachDistance;
            }
        }
    #endif

    }
}