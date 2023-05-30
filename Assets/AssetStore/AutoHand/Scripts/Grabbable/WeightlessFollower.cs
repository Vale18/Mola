using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    [DefaultExecutionOrder(1)]
    public class WeightlessFollower : MonoBehaviour {
        [HideInInspector]
        public Transform follow;
        [HideInInspector]
        public Transform follow1;

        public Dictionary<Hand, Transform> heldMoveTo = new Dictionary<Hand, Transform>();

        [HideInInspector]
        public float followPositionStrength = 30;
        [HideInInspector]
        public float followRotationStrength = 30;

        [HideInInspector]
        public float maxVelocity = 5;

        [HideInInspector]
        public Grabbable grab;


        internal Rigidbody body;
        Transform moveTo;

        float startMass;
        float startDrag;
        float startAngleDrag;

        public void Start() {
            if(body == null)
                body = GetComponent<Rigidbody>();

            if(startAngleDrag == 0) {
                startMass = body.mass;
                startDrag = body.drag;
                startAngleDrag = body.angularDrag;
            }
        }

        public virtual void Set(Hand hand, Grabbable grab) {
            if(!heldMoveTo.ContainsKey(hand)) {
                heldMoveTo.Add(hand, new GameObject().transform);
                heldMoveTo[hand].name = "HELD FOLLOW POINT";
            }

            var tempTransform = AutoHandExtensions.transformRuler;
            tempTransform.position = hand.transform.position;
            tempTransform.rotation = hand.transform.rotation;

            var tempTransformChild = AutoHandExtensions.transformRulerChild;
            tempTransformChild.position = grab.transform.position;
            tempTransformChild.rotation = grab.transform.rotation;

            if(grab.maintainGrabOffset) {
                tempTransform.position = hand.follow.position + hand.grabPositionOffset;
                tempTransform.rotation = hand.follow.rotation * hand.grabRotationOffset;
            }
            else {
                tempTransform.position = hand.follow.position;
                tempTransform.rotation = hand.follow.rotation;
            }

            heldMoveTo[hand].parent = hand.moveTo;
            heldMoveTo[hand].position = tempTransformChild.position;
            heldMoveTo[hand].rotation = tempTransformChild.rotation;


            if(body == null)
                body = GetComponent<Rigidbody>();

            if(startAngleDrag == 0) {
                startMass = body.mass;
                startDrag = body.drag;
                startAngleDrag = body.angularDrag;
            }

            body.drag = hand.body.drag;
            body.angularDrag = hand.body.angularDrag;

            if(follow == null)
                follow = heldMoveTo[hand];
            else if(follow1 == null)
                follow1 = heldMoveTo[hand];

            followPositionStrength = hand.followPositionStrength;
            followRotationStrength = hand.followRotationStrength;
            maxVelocity = hand.maxVelocity;

            this.grab = grab;

            if(moveTo == null) {
                moveTo = new GameObject().transform;
                moveTo.name = gameObject.name + " FOLLOW POINT";
                moveTo.parent = follow.parent;
            }

            hand.OnReleased += (Hand hand1, Grabbable grab1) => { RemoveFollow(hand1, heldMoveTo[hand1]); };
        }



        public virtual void FixedUpdate() {
            if(follow == null)
                return;

            //Calls physics movements
            MoveTo();
            TorqueTo();
        }


        protected void SetMoveTo() {
            if(follow == null)
                return;

            //Sets [Move To] Object
            if(follow1) {
                moveTo.position = Vector3.Lerp(follow.position, follow1.position, 0.5f);
                moveTo.rotation = Quaternion.Lerp(follow.rotation, follow1.rotation, 0.5f);
            }
            else {
                moveTo.position = follow.position;
                moveTo.rotation = follow.rotation;
            }
        }


        /// <summary>Moves the hand to the controller position using physics movement</summary>
        protected virtual void MoveTo() {
            if(followPositionStrength <= 0 || body == null)
                return;

            SetMoveTo();

            var movePos = moveTo.position;
            var distance = Vector3.Distance(movePos, transform.position);

            if(grab.collisionTracker.collisionCount > 0) {
                var velocityClamp = maxVelocity;
                Vector3 vel = (movePos - transform.position).normalized * followPositionStrength * distance;
                vel.x = Mathf.Clamp(vel.x, -velocityClamp, velocityClamp);
                vel.y = Mathf.Clamp(vel.y, -velocityClamp, velocityClamp);
                vel.z = Mathf.Clamp(vel.z, -velocityClamp, velocityClamp);

                body.velocity = Vector3.MoveTowards(body.velocity, vel, 0.5f + body.velocity.magnitude / (velocityClamp));
            }
            else {
                var velocityClamp = maxVelocity;
                Vector3 vel = (movePos - transform.position).normalized * followPositionStrength * distance;
                vel.x = Mathf.Clamp(vel.x, -velocityClamp, velocityClamp);
                vel.y = Mathf.Clamp(vel.y, -velocityClamp, velocityClamp);
                vel.z = Mathf.Clamp(vel.z, -velocityClamp, velocityClamp);
                body.velocity = vel;
            }
        }

        /// <summary>Rotates the hand to the controller rotation using physics movement</summary>
        protected virtual void TorqueTo() {
            if(body == null)
                return;

            var delta = (moveTo.rotation * Quaternion.Inverse(body.rotation));
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if(float.IsInfinity(axis.x))
                return;

            if(angle > 180f)
                angle -= 360f;

            Vector3 angular = (Mathf.Deg2Rad * angle * followRotationStrength) * axis.normalized;

            if(CollisionCount() > 0)
                body.angularVelocity = Vector3.Lerp(body.angularVelocity, angular, 0.5f);
            else
                body.angularVelocity = Vector3.Lerp(body.angularVelocity, angular, 0.95f);
        }


        int CollisionCount() {
            return grab.CollisionCount();
        }

        public void RemoveFollow(Hand hand, Transform follow) {
            hand.OnReleased -= (Hand hand1, Grabbable grab1) => { RemoveFollow(hand1, heldMoveTo[hand1]); };

            if(this.follow == follow)
                this.follow = null;
            if(follow1 == follow)
                follow1 = null;

            if(this.follow == null && follow1 != null) {
                this.follow = follow1;
                follow1 = null;
            }

            if(this.follow == null && follow1 == null) {
                if(body != null) {
                    body.mass = startMass;
                    body.drag = startDrag;
                    body.angularDrag = startAngleDrag;
                }
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            Destroy(moveTo.gameObject);
            foreach(var transform in heldMoveTo)
                Destroy(transform.Value.gameObject);

            if (body != null)
            {
                body.mass = startMass;
                body.drag = startDrag;
                body.angularDrag = startAngleDrag;
            }
        }
    }


}
