using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;
using NaughtyAttributes;

namespace Autohand.Demo{
    public class OVRHandPlayerControllerLink : MonoBehaviour{
        public AutoHandPlayer player;
        public OVRInput.Controller moveController;
        public OVRInput.Axis2D moveAxis;

        public OVRInput.Controller turnController;
        public OVRInput.Axis2D turnAxis;

        public void Update() {
            player.Move(OVRInput.Get(moveAxis, moveController));
            player.Turn(OVRInput.Get(turnAxis, turnController).x);
        }
    }
}
