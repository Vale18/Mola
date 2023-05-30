using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    public class IgnoreHandPlayerCollision : MonoBehaviour {
        public List<Collider> colliders;

        IEnumerable Start() {
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            ActivateIgnoreCollision();
        }

        public void ActivateIgnoreCollision() {
            foreach(var col in colliders)
                AutoHandPlayer.Instance.IgnoreCollider(col, true);
        }
        public void DeactivateIgnoreCollision() {
            foreach(var col in colliders)
                AutoHandPlayer.Instance.IgnoreCollider(col, false);
        }
    }
}