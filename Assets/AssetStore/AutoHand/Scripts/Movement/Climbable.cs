using UnityEngine;

namespace Autohand {
    [RequireComponent(typeof(Grabbable))]
    public class Climbable : MonoBehaviour{
        public Vector3 axis = Vector3.one;
        public Stabber stabber;

        private void Start() {
            if(stabber != null) {
                stabber.StartStabEvent += (hand, grab) => {
                    enabled = true;
                };
                stabber.EndStabEvent += (hand, grab) => {
                    enabled = false;
                };
            }
        }
    }
}
