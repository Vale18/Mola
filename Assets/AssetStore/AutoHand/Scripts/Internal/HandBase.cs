using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Autohand {

    /// <summary>
    /// 
    /// </summary>
    public enum HandMovementType {
        /// <summary>Movement method for Auto Hand V2 and below</summary>
        Legacy,
        /// <summary>Uses physics forces</summary>
        Forces
    }

    public enum HandType {
        both,
        right,
        left,
        none
    }

    public enum GrabType {
        /// <summary>On grab, hand will move to the grabbable, create grab connection, then return to follow position</summary>
        HandToGrabbable,
        /// <summary>On grab, grabbable will move to the hand, then create grab connection</summary>
        GrabbableToHand,
        /// <summary>On grab, grabbable instantly travel to the hand</summary>
        InstantGrab
    }

    [Serializable]
    public struct VelocityTimePair {
        public float time;
        public Vector3 velocity;
    }

    public delegate void HandGrabEvent(Hand hand, Grabbable grabbable);
    public delegate void HandGameObjectEvent(Hand hand, GameObject other);
    [Serializable]
    public class UnityHandGrabEvent : UnityEvent<Hand, Grabbable> { }
    [Serializable]
    public class UnityHandEvent : UnityEvent<Hand> { }
    [Serializable]
    [RequireComponent(typeof(Rigidbody)), DefaultExecutionOrder(-10)]
    /// <summary>This is the base of the Auto Hand hand class, used for organizational purposes</summary>
    public class HandBase : MonoBehaviour {


        [AutoHeader("AUTO HAND")]
        public bool ignoreMe;

        public Finger[] fingers;

        [Tooltip("An empty GameObject that should be placed on the surface of the center of the palm")]
        public Transform palmTransform;

        [FormerlySerializedAs("isLeft")]
        [Tooltip("Whether this is the left (on) or right (off) hand")]
        public bool left = false;


        [Space]


        [Tooltip("Maximum distance for pickup"), Min(0.01f)]
        public float reachDistance = 0.3f;


        [AutoToggleHeader("Enable Movement", 0, 0, tooltip = "Whether or not to enable the hand's Rigidbody Physics movement")]
        public bool enableMovement = true;

        [EnableIf("enableMovement"), Tooltip("Follow target, the hand will always try to match this transforms position with rigidbody movements")]
        public Transform follow;

        [EnableIf("enableMovement"), Tooltip("Returns hand to the target after this distance [helps just in case it gets stuck]"), Min(0)]
        public float maxFollowDistance = 0.5f;


        [EnableIf("enableMovement"), Tooltip("Amplifier for applied velocity on released object"), Min(0)]
        public float throwPower = 1f;

        [EnableIf("enableMovement"), Tooltip("Applies position interpolation to the hands to make the hands look smoother when the FPS is higher than the fixed timestep")]
        public bool enableInterpolation = true;

        [HideInInspector]
        public bool advancedFollowSettings = true;

        [AutoToggleHeader("Enable Auto Posing", 0, 0, tooltip = "Auto Posing will override Unity Animations -- This will disable all the Auto Hand IK, including animations from: finger sway, pose areas, finger bender scripts (runtime Auto Posing will still work)")]
        [Tooltip("Turn this on when you want to animate the hand or use other IK Drivers")]
        public bool enableIK = true;

        [EnableIf("enableIK"), Tooltip("How much the fingers sway from the velocity")]
        public float swayStrength = 0.7f;

        [EnableIf("enableIK"), Tooltip("This will offset each fingers bend (0 is no bend, 1 is full bend)")]
        public float gripOffset = 0.1f;






        //HIDDEN ADVANCED SETTINGS
        [NonSerialized, Tooltip("The maximum allowed velocity of the hand"), Min(0)]
        public float maxVelocity = 10f;

        [NonSerialized, Tooltip("Follow target speed (Can cause jittering if turned too high - recommend increasing drag with speed)"), Min(0)]
        public float followPositionStrength = 80;

        [HideInInspector, NonSerialized, Tooltip("Follow target rotation speed (Can cause jittering if turned too high - recommend increasing angular drag with speed)"), Min(0)]
        public float followRotationStrength = 100;

        [HideInInspector, NonSerialized, Tooltip("After this many seconds velocity data within a 'throw window' will be tossed out. (This allows you to get only use acceeleration data from the last 'x' seconds of the throw.)")]
        public float throwVelocityExpireTime = 0.125f;
        [HideInInspector, NonSerialized, Tooltip("After this many seconds velocity data within a 'throw window' will be tossed out. (This allows you to get only use acceeleration data from the last 'x' seconds of the throw.)")]
        public float throwAngularVelocityExpireTime = 0.25f;

        [HideInInspector, NonSerialized, Tooltip("Increase for closer finger tip results / Decrease for less physics checks - The number of steps the fingers take when bending to grab something")]
        public int fingerBendSteps = 100;

        [HideInInspector, NonSerialized]
        public float sphereCastRadius = 0.04f;

        [HideInInspector]
        public bool usingPoseAreas = true;

        [HideInInspector]
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;


        Grabbable HoldingObj = null;
        public Grabbable holdingObj {
            get { return HoldingObj; }
            internal set { HoldingObj = value; }
        }

        Grabbable _lookingAtObj = null;
        public Grabbable lookingAtObj {
            get { return _lookingAtObj; }
            protected set { _lookingAtObj = value; }
        }

        Transform _moveTo = null;
        public Transform moveTo {
            get {
                if(!gameObject.activeInHierarchy)
                    return null;

                if(_moveTo == null) {
                    _moveTo = new GameObject().transform;
                    _moveTo.parent = transform.parent;
                    _moveTo.name = "HAND FOLLOW POINT";
                }

                return _moveTo;
            }
        }

        public Rigidbody body { get; internal set; }

        Vector3 _grabPositionOffset = Vector3.zero;
        public Vector3 grabPositionOffset {
            get { return _grabPositionOffset; }
            protected set { _grabPositionOffset = value; }
        }

        Quaternion _grabRotationOffset = Quaternion.identity;
        public Quaternion grabRotationOffset {
            get { return _grabRotationOffset; }
            protected set { _grabRotationOffset = value; }
        }

        public bool disableIK {
            get { return !enableIK; }
            set { enableIK = !value; }
        }

        private CollisionTracker _collisionTracker;
        public CollisionTracker collisionTracker {
            get {
                if(_collisionTracker == null)
                    _collisionTracker = gameObject.AddComponent<CollisionTracker>();
                return _collisionTracker;
            }
            protected set {
                if(_collisionTracker != null)
                    Destroy(_collisionTracker);

                _collisionTracker = value;
            }
        }

        protected GrabbablePose _grabPose;
        protected GrabbablePose grabPose {
            get {
                return _grabPose;
            }
    
            set {
                if(value == null && _grabPose != null)
                    _grabPose.CancelHandPose(this as Hand);

                _grabPose = value;
            }
        }


        protected Joint heldJoint;

        protected bool grabbing = false;
        protected bool squeezing = false;
        protected bool grabbed = false;

        protected float triggerPoint;
        protected Coroutine handAnimateRoutine;
        protected HandPoseArea handPoseArea;
        protected HandPoseData preHandPoseAreaPose;

        protected List<Collider> handColliders = new List<Collider>();


        Transform _grabPoint;
        internal Transform grabPoint {
            get {
                if(_grabPoint == null && gameObject.scene.isLoaded) {
                    _grabPoint = new GameObject().transform;
                    _grabPoint.name = "grabPoint";
                }
                return _grabPoint;
            }
        }


        Transform _grabPosition;
        internal Transform grabPosition {
            get {
                if(!gameObject.activeInHierarchy)
                    _grabPosition = null;
                else if(gameObject.activeInHierarchy && _grabPosition == null) {
                    _grabPosition = new GameObject().transform;
                    _grabPosition.name = "grabPosition";
                    _grabPosition.parent = transform;
                }


                return _grabPosition;
            }
        }

        internal int handLayers;

        protected Collider palmCollider;
        protected RaycastHit highlightHit;

        protected HandVelocityTracker velocityTracker;
        protected Transform palmChild;
        protected Vector3 lastFrameFollowPos;
        protected Quaternion lastFrameFollowRot;

        protected Vector3 followVel;
        protected Vector3 followAngularVel;

        internal bool allowUpdateMovement = true;
        Vector3[] handRays = new Vector3[0];
        RaycastHit[] rayHits = new RaycastHit[0];

        Vector3[] updatePositionTracked = new Vector3[3];

        List<RaycastHit> closestHits = new List<RaycastHit>();
        List<Grabbable> closestGrabs = new List<Grabbable>();
        int tryMaxDistanceCount;

        Vector3 lastFollowPosition;
        Vector3 lastFollowRotation;

        bool prerendered = false;
        Vector3 preRenderPos;
        Quaternion preRenderRot;
        float currGrip = 1f;

        protected virtual void Awake() {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.None;
            body.useGravity = false;
            body.maxDepenetrationVelocity = 2f;
            body.solverIterations = 200;
            body.solverVelocityIterations = 200;

            if(palmCollider == null) {
                palmCollider = palmTransform.gameObject.AddComponent<BoxCollider>();
                (palmCollider as BoxCollider).size = new Vector3(0.2f, 0.15f, 0.05f);
                (palmCollider as BoxCollider).center = new Vector3(0f, 0f, -0.025f);
                palmCollider.enabled = false;
            }

            if(palmChild == null) {
                palmChild = new GameObject().transform;
                palmChild.parent = palmTransform;
                SetPalmRays();
            }

#if (UNITY_2020_3_OR_NEWER)
            var cams = FindObjectsOfType<Camera>(true);
#else
            var cams = FindObjectsOfType<Camera>();
#endif
            foreach (var cam in cams)
                cam.gameObject.AddComponent<HandStabilizer>().hand = this;
            
            if(velocityTracker == null)
                velocityTracker = new HandVelocityTracker(this);
        }

        protected virtual void OnEnable() {
            SetHandCollidersRecursive(transform);
        }

        protected virtual void OnDisable() {
            handColliders.Clear();
        }

        protected virtual void OnDestroy() {
            if(grabPoint != null)
                Destroy(grabPoint.gameObject);
            if(grabPosition != null)
                Destroy(grabPosition.gameObject);
            if(moveTo != null)
                Destroy(moveTo.gameObject);
        }

        protected virtual void FixedUpdate() {
            if(!IsGrabbing() && enableMovement && follow != null && !body.isKinematic) {
                MoveTo();
                TorqueTo();
            }

            velocityTracker.UpdateThrowing();


            if(ignoreMoveFrame) {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if(follow != null) {
                followVel = follow.position - lastFollowPosition;
                followAngularVel = follow.rotation.eulerAngles - lastFollowPosition;
                lastFollowPosition = follow.position;
                lastFollowRotation = follow.rotation.eulerAngles;
            }



            lastUpdateTime = Time.fixedTime;
            ignoreMoveFrame = false;
        }


        float lastUpdateTime;
        protected virtual void Update()
        {
            bool shouldInterpolate =
                CollisionCount() == 0
                && enableMovement
                && follow != null
                && !body.isKinematic
                && !IsGrabbing();

            if (shouldInterpolate && enableInterpolation)
            {
                var deltaTime = (Time.time - lastUpdateTime);
                var startHandPos = transform.position;

                if (holdingObj == null)
                {
                    MoveTo();
                   
                    body.velocity *= (1 - Mathf.Clamp01(body.drag * deltaTime));
                    transform.position = Vector3.MoveTowards(transform.position, moveTo.position, body.velocity.magnitude * deltaTime);
                    body.position = transform.position;

                    TorqueTo();
                    body.angularVelocity *= (1 - Mathf.Clamp01(body.angularDrag * deltaTime));
                    transform.rotation = Quaternion.Euler(Vector3.MoveTowards(transform.rotation.eulerAngles, moveTo.rotation.eulerAngles, body.angularVelocity.magnitude * deltaTime));
                    body.rotation = transform.rotation;

                    var deltaHandPos = transform.position - startHandPos;
                    if (transform.position != startHandPos)
                    {
                        if (body.SweepTest(deltaHandPos, out var hit, deltaHandPos.magnitude))
                        {
                            transform.position -= deltaHandPos;
                            body.position = transform.position;
                        }
                    }
                }
                else if (holdingObj.ShouldInterpolate())
                {
                    var startGrabPosition = grabPosition.position;
                    var startGrabRotation = grabPosition.rotation;

                    MoveTo();
                    body.velocity *= (1 - body.drag * deltaTime);
                    transform.position = Vector3.MoveTowards(transform.position, moveTo.position, body.velocity.magnitude * deltaTime);
                    body.position = transform.position;

                    TorqueTo();                    
                    body.angularVelocity *= (1 - body.angularDrag * deltaTime);
                    transform.rotation = Quaternion.Euler(Vector3.MoveTowards(transform.rotation.eulerAngles, moveTo.rotation.eulerAngles, body.angularVelocity.magnitude * deltaTime));
                    body.rotation = transform.rotation;

                    var deltaHandPos = transform.position - startHandPos;
                    var deltaHeldPos = grabPosition.position - startGrabPosition;
                    if (transform.position != startHandPos)
                    {
                        bool didHit = false;
                        var sweepTest = holdingObj.body.SweepTestAll(deltaHeldPos, deltaHeldPos.magnitude);
                        for (int i = 0; i < sweepTest.Length; i++)
                        {
                            if (sweepTest[i].rigidbody != holdingObj.body && !holdingObj.IsHolding(sweepTest[i].rigidbody))
                            {
                                didHit = true;
                            }
                        }
                        if (didHit)
                        {
                            transform.position -= deltaHandPos;
                            body.position = transform.position;
                        }
                        else
                        {
                            sweepTest = holdingObj.body.SweepTestAll(deltaHeldPos, deltaHeldPos.magnitude);
                            for (int i = 0; i < sweepTest.Length; i++)
                            {
                            if (sweepTest[i].rigidbody != holdingObj.body && !holdingObj.IsHolding(sweepTest[i].rigidbody))
                                {
                                    didHit = true;
                                }
                            }

                        }
                        
                        if (didHit)
                        {
                            transform.position -= transform.position - startHandPos;
                            body.position = transform.position;
                        }
                        else
                        {
                            holdingObj.body.transform.position += grabPosition.position - startGrabPosition;
                            holdingObj.body.transform.rotation *= (grabPosition.rotation * Quaternion.Inverse(startGrabRotation));
                            holdingObj.body.position = holdingObj.body.transform.position;
                            holdingObj.body.rotation = holdingObj.body.transform.rotation;
                        }
                    }
                }
            }

            if (!IsGrabbing())
                UpdateFingers(Time.deltaTime);


            for (int i = 1; i < updatePositionTracked.Length; i++)
                updatePositionTracked[i] = updatePositionTracked[i-1];
            updatePositionTracked[0] = transform.localPosition;

            lastUpdateTime = Time.time;
        }


        //This is used to force the hand to always look like its where it should be even when physics is being weird
        public virtual void OnPreRender() {
            preRenderPos = transform.position;
            preRenderRot = transform.rotation;

            //Hides fixed joint jitterings
            if(holdingObj != null && holdingObj.customGrabJoint == null && !IsGrabbing()) {
                transform.position = grabPoint.position;
                transform.rotation = grabPoint.rotation;
                prerendered = true;
            }
        }

        //This puts everything where it should be for the physics update
        public virtual void OnPostRender() {
            //Returns position after hiding for camera
            if(holdingObj != null && holdingObj.customGrabJoint == null && !IsGrabbing() && prerendered) {
                transform.position = preRenderPos;
                transform.rotation = preRenderRot;
            }
            prerendered = false;
        }


        /// <summary>Creates Joints between hand and grabbable, does not call grab events</summary>
        protected virtual void CreateJoint(Grabbable grab, float breakForce, float breakTorque) {
            if(grab.customGrabJoint == null) {
                var newJoint = gameObject.AddComponent<FixedJoint>().GetCopyOf(Resources.Load<FixedJoint>("DefaultJoint"));
                newJoint.anchor = Vector3.zero;
                newJoint.breakForce = breakForce;
                if(grab.HeldCount() == 1)
                    newJoint.breakForce += 250;
                newJoint.breakTorque = breakTorque;
                newJoint.connectedBody = grab.body;
                heldJoint = newJoint;
            }
            else {
                var newJoint = grab.body.gameObject.AddComponent<ConfigurableJoint>().GetCopyOf(grab.customGrabJoint);
                newJoint.anchor = Vector3.zero;
                if(grab.HeldCount() == 1)
                    newJoint.breakForce += 250;
                newJoint.breakForce = breakForce;
                newJoint.breakTorque = breakTorque;
                newJoint.connectedBody = body;
                heldJoint = newJoint;
            }
        }


        //====================== MOVEMENT  =======================
        //========================================================
        //========================================================

        protected bool ignoreMoveFrame;
        /// <summary>Moves the hand to the controller position using physics movement</summary>
        protected virtual void MoveTo() {
            SetMoveTo();

            if(followPositionStrength <= 0)
                return;


            var movePos = moveTo.position;
            var distance = Vector3.Distance(movePos, transform.position);

            //Returns if out of distance, if you aren't holding anything
            if(distance > maxFollowDistance) {
                if(holdingObj != null) {
                    if(holdingObj.parentOnGrab && tryMaxDistanceCount < 1) {
                        SetHandLocation(movePos, transform.rotation);
                        tryMaxDistanceCount += 2;
                    }
                    else if(!holdingObj.parentOnGrab || tryMaxDistanceCount >= 1) {
                        holdingObj.ForceHandRelease(this as Hand);
                        SetHandLocation(movePos, transform.rotation);
                    }
                }
                else {
                    SetHandLocation(movePos, transform.rotation);
                }
            }

            if(tryMaxDistanceCount > 0)
                tryMaxDistanceCount--;

            if(holdingObj && holdingObj.HeldCount() > 1)
                distance = Mathf.Clamp(distance, 0, 0.04f);

            if(CollisionCount() > 0) {
                Vector3 vel;
                var velocityClamp = maxVelocity;
                vel = (movePos - transform.position).normalized * followPositionStrength * distance;
                vel.x = Mathf.Clamp(vel.x, -velocityClamp, velocityClamp);
                vel.y = Mathf.Clamp(vel.y, -velocityClamp, velocityClamp);
                vel.z = Mathf.Clamp(vel.z, -velocityClamp, velocityClamp);

                //If not holding an object with more than one hand
                if(!(holdingObj && holdingObj.HeldCount() > 1))
                    vel = Vector3.MoveTowards(body.velocity, vel, 0.6f + body.velocity.magnitude / (velocityClamp) * (60f/Time.fixedDeltaTime));


                body.velocity = vel;
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
            var delta = (moveTo.rotation * Quaternion.Inverse(body.rotation));
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (float.IsInfinity(axis.x))
            {
                return;
            }

            if(angle > 180f)
                angle -= 360f;

            Vector3 angular = (Mathf.Deg2Rad * angle * followRotationStrength) * axis.normalized;

            if(CollisionCount() > 0)
                body.angularVelocity = Vector3.Lerp(body.angularVelocity, angular, 0.65f);
            else
                body.angularVelocity = Vector3.Lerp(body.angularVelocity, angular, 0.8f);

        }

        ///<summary>Moves the hand and whatever it might be holding (if teleport allowed) to given pos/rot</summary>
        public virtual void SetHandLocation(Vector3 pos, Quaternion rot) {
            if(holdingObj && holdingObj.parentOnGrab) {
                holdingObj.IgnoreInterpolationForOneFixedUpdate();
                ignoreMoveFrame = true;
                var deltaPos = pos - transform.position;
                var deltaRot = rot * Quaternion.Inverse(transform.rotation);

                transform.position = pos;
                transform.rotation = rot;
                body.position = transform.position;
                body.rotation = transform.rotation;

                var fromHoldingRot = holdingObj.body.transform.rotation;
                holdingObj.body.transform.position += deltaPos;
                holdingObj.body.transform.RotateAround(transform, deltaRot);
                holdingObj.body.position = holdingObj.body.transform.position;
                holdingObj.body.rotation = holdingObj.body.transform.rotation;

                grabPositionOffset = deltaRot * grabPositionOffset;

                var deltaHeldRot = Quaternion.Inverse(holdingObj.body.rotation) * fromHoldingRot;

                foreach(var jointed in holdingObj.jointedBodies)
                    if(!(jointed.CanGetComponent(out Grabbable grab) && grab.HeldCount() > 0)) {
                        jointed.position += deltaPos;
                        jointed.transform.RotateAround(holdingObj.body.transform, deltaHeldRot);
                    }

                velocityTracker.ClearThrow();
            }
            else {
                ignoreMoveFrame = true;
                //holdingObj?.ForceHandRelease(this as Hand);
                transform.position = pos;
                transform.rotation = rot;
                body.position = pos;
                body.rotation = rot;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }



        ///<summary>Moves the hand and keeps the local rotation</summary>
        public virtual void SetHandLocation(Vector3 pos) {
            SetMoveTo();
            SetHandLocation(pos, transform.rotation);
        }

        /// <summary>Resets the hand location to the follow</summary>
        public void ResetHandLocation() {
            SetHandLocation(moveTo.position, moveTo.rotation);
        }

        protected void SetMoveTo() {
            if(follow == null)
                return;

            //Sets [Move To] Object
            moveTo.position = follow.position;
            moveTo.rotation = follow.rotation;

            //Adjust the [Move To] based on offsets 
            if(holdingObj != null) {
                moveTo.position = follow.position + grabPositionOffset;
                moveTo.rotation = follow.rotation * grabRotationOffset;

                if(left) {
                    var leftRot = -holdingObj.heldRotationOffset;
                    leftRot.x *= -1;
                    moveTo.localRotation *= Quaternion.Euler(leftRot);
                    var moveLeft = holdingObj.heldPositionOffset;
                    moveLeft.x *= -1;
                    moveTo.position += transform.rotation * moveLeft;
                }
                else {
                    moveTo.position += transform.rotation * holdingObj.heldPositionOffset;
                    moveTo.localRotation *= Quaternion.Euler(holdingObj.heldRotationOffset);
                }
            }
        }

        /// <summary>Whether or not this hand can grab the grabbbale based on hand and grabbable settings</summary>
        public bool CanGrab(Grabbable grab) {
            var cantHandSwap = (grab.IsHeld() && grab.singleHandOnly && !grab.allowHeldSwapping);
            return (grab.CanGrab(this) && !IsGrabbing() && !cantHandSwap);
        }

        public float GetTriggerAxis() {
            return triggerPoint;
        }


        /// <summary>Finds the closest raycast from a cone of rays -> Returns average direction of all hits</summary>
        protected virtual Vector3 HandClosestHit(out RaycastHit closestHit, out Grabbable grabbable, float dist, int layerMask, Grabbable target = null) {
            Grabbable grab;
            Vector3 palmForward = palmTransform.forward;
            Vector3 palmPosition = palmTransform.position;
            Quaternion palmRotation = palmTransform.rotation;
            GameObject rayHitObject;
            Grabbable lastRayHitGrabbable = null;

            closestGrabs.Clear();
            closestHits.Clear();

            for(int i = 0; i < handRays.Length; i++) {
                //Debug.DrawRay(palmPosition - palmForward * sphereCastRadius, palmRotation * handRays[i] * dist, Color.red, Time.fixedUnscaledDeltaTime * 2f);
                if(Physics.SphereCast(palmPosition - palmForward * sphereCastRadius, sphereCastRadius, palmRotation * handRays[i], out rayHits[i], dist, layerMask, queryTriggerInteraction)) {
                    if((queryTriggerInteraction == QueryTriggerInteraction.Collide || !rayHits[i].collider.isTrigger) && palmTransform.InverseTransformPoint(rayHits[i].point).z > 0) {

                        rayHitObject = rayHits[i].collider.gameObject;
                        if(closestGrabs.Count > 0)
                            lastRayHitGrabbable = closestGrabs[closestGrabs.Count - 1];

                        if(closestGrabs.Count > 0 && rayHitObject == lastRayHitGrabbable.gameObject) {
                            if(target == null) {
                                closestGrabs.Add(lastRayHitGrabbable);
                                closestHits.Add(rayHits[i]);
                            }
                        }
                        else if(rayHitObject.HasGrabbable(out grab) && CanGrab(grab)) {
                            if(target == null || target == grab) {
                                closestGrabs.Add(grab);
                                closestHits.Add(rayHits[i]);
                            }
                        }
                    }
                }
            }

            int closestHitCount = closestHits.Count;

            if(closestHitCount > 0) {
                closestHit = closestHits[0];
                grabbable = closestGrabs[0];
                Vector3 dir = Vector3.zero;
                for(int i = 0; i < closestHitCount; i++) {
                    if(closestHits[i].distance / closestGrabs[i].grabPriorityWeight < closestHit.distance / grabbable.grabPriorityWeight) {
                        closestHit = closestHits[i];
                        grabbable = closestGrabs[i];
                    }

                    dir += closestHits[i].point - palmTransform.position;
                }

                if(holdingObj == null && !IsGrabbing()) {
                    if(grabPoint.parent != closestHit.transform)
                        grabPoint.parent = closestHit.transform;
                    grabPoint.position = closestHit.point;
                    grabPoint.up = closestHit.normal;
                }

                return dir / closestHitCount;
            }

            closestHit = new RaycastHit();
            grabbable = null;
            return Vector3.zero;
        }


        public bool IsPosing() {
            return handPoseArea != null || (holdingObj != null && holdingObj.HasCustomPose()) || handAnimateRoutine != null;
        }

        float fingerSwayVel;
        /// <summary>Determines how the hand should look/move based on its flags</summary>
        protected virtual void UpdateFingers(float deltaTime) {
            var averageVel = Vector3.zero;
            for (int i = 1; i < updatePositionTracked.Length; i++)
                averageVel += updatePositionTracked[i] - updatePositionTracked[i - 1];
            averageVel /= updatePositionTracked.Length;
            averageVel = (Quaternion.Inverse(palmTransform.rotation)*transform.parent.rotation)*averageVel;


            //Responsable for movement finger sway
            if (!grabbing && !disableIK && !IsPosing() && !holdingObj)
            {


                float vel = (averageVel*60).z;
                //if (left)
                //    vel *= -1;

                if (CollisionCount() > 0) vel = 0;
                fingerSwayVel = Mathf.MoveTowards(fingerSwayVel, vel, deltaTime * (Mathf.Abs((fingerSwayVel-vel) * 30f)));



                float grip = gripOffset + swayStrength * fingerSwayVel;
                currGrip = grip;

                foreach (var finger in fingers)
                {
                    finger.UpdateFinger(grip);
                }
            }
        }



        public int CollisionCount() {
            if(holdingObj != null)
                return collisionTracker.collisionObjects.Count + holdingObj.collisionTracker.collisionObjects.Count;
            return collisionTracker.collisionObjects.Count;
        }


        public void HandIgnoreCollider(Collider collider, bool ignore) {
            for(int i = 0; i < handColliders.Count; i++)
                Physics.IgnoreCollision(handColliders[i], collider, ignore);
        }

        public void SetLayer() {
            SetLayerRecursive(transform, LayerMask.NameToLayer(left ? Hand.leftHandLayerName : Hand.rightHandLayerName));
        }

        internal void SetLayerRecursive(Transform obj, int newLayer) {
            obj.gameObject.layer = newLayer;
            for(int i = 0; i < obj.childCount; i++) {
                SetLayerRecursive(obj.GetChild(i), newLayer);
            }
        }


        protected void SetHandCollidersRecursive(Transform obj) {
            handColliders.Clear();
            AddHandCol(obj);

            void AddHandCol(Transform obj1) {
                foreach(var col in obj1.GetComponents<Collider>())
                    handColliders.Add(col);

                for(int i = 0; i < obj1.childCount; i++) {
                    AddHandCol(obj1.GetChild(i));
                }
            }
        }


        public Vector3[] GetPalmRays() {
            SetPalmRays();
            return handRays;
        }

        protected virtual void SetPalmRays() {

            //This precalculates the rays so it has to do less math in realtime
            List<Vector3> rays = new List<Vector3>();
            for(int i = 0; i < 100; i++) {
                float ampI = Mathf.Sqrt(Mathf.Clamp(i * ((1.05f) * 500), 0.0001f, float.MaxValue)) / (Mathf.PI);
                rays.Add(Quaternion.Euler(0, Mathf.Cos(i * (-Mathf.PI / 200f + 1)) * ampI + 90, Mathf.Sin(i * (-Mathf.PI / 200f + 1)) * ampI) * -Vector3.right);
            }
            rayHits = new RaycastHit[100];
            handRays = rays.ToArray();
        }


        /// <summary>Returns the current throw velocity</summary>
        public Vector3 ThrowVelocity() { return velocityTracker.ThrowVelocity(); }

        /// <summary>Returns the current throw angular velocity</summary>
        public Vector3 ThrowAngularVelocity() { return velocityTracker.ThrowAngularVelocity(); }






        /// <summary>Returns true during the grabbing frames</summary>
        public bool IsGrabbing() {
            return grabbing;
        }

        public static int GetHandsLayerMask() {
            return LayerMask.GetMask(Hand.rightHandLayerName, Hand.leftHandLayerName);
        }
    }
}