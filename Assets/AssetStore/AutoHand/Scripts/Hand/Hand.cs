using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Autohand {
    [HelpURL("https://earnestrobot.notion.site/Hand-967e36c2ab2945b2b0f75cea84624b2f"), DefaultExecutionOrder(-10)]
    public class Hand : HandBase {


        [AutoToggleHeader("Enable Highlight", 0, 0, tooltip = "Raycasting for grabbables to highlight is expensive, you can disable it here if you aren't using it")]
        public bool usingHighlight = true;

        [EnableIf("usingHighlight")]
        [Tooltip("The layers to highlight and use look assist on --- Nothing will default on start")]
        public LayerMask highlightLayers;

        [EnableIf("usingHighlight")]
        [Tooltip("Leave empty for none - used as a default option for all grabbables with empty highlight material")]
        public Material defaultHighlight;


        [AutoToggleHeader("Show Advanced")]
        public bool showAdvanced = false;


        [ShowIf("showAdvanced")]
        [Tooltip("Whether the hand should go to the object and come back on grab, or the object to float to the hand on grab. Will default to HandToGrabbable for objects that have \"parentOnGrab\" disabled")]
        public GrabType grabType = GrabType.HandToGrabbable;

        [ShowIf("showAdvanced")]
        [Tooltip("Makes grab smoother; also based on range and reach distance - a very near grab is minGrabTime and a max distance grab is maxGrabTime"), Min(0)]
        public float minGrabTime = 0.1f;
        [ShowIf("showAdvanced")]
        [Tooltip("Makes grab smoother; also based on range and reach distance - a very near grab is minGrabTime and a max distance grab is maxGrabTime"), Min(0)]
        public float maxGrabTime = 0.25f;

        [ShowIf("showAdvanced")]
        [Tooltip("The animation curve based on the grab time 0-1"), Min(0)]
        public AnimationCurve grabCurve;

        [ShowIf("showAdvanced")]
        [Tooltip("Speed at which the gentle grab returns the grabbable"), Min(0)][FormerlySerializedAs("smoothReturnSpeed")]
        public float gentleGrabSpeed = 1;

        [ShowIf("showAdvanced")]
        [Tooltip("This is used in conjunction with custom poses. For a custom pose to work it must has the same PoseIndex as the hand. Used for when your game has multiple hands")]
        public int poseIndex = 0;

        [AutoLine]
        public bool ignoreMe1;





#if UNITY_EDITOR
        bool editorSelected = false;
#endif


        public static string[] grabbableLayers = { "Grabbable", "Grabbing" };

        //The layer is used and applied to all grabbables in if the hands layer is set to default
        public static string grabbableLayerNameDefault = "Grabbable";
        //This helps the auto grab distinguish between what item is being grabbaed and the items around it
        public static string grabbingLayerName = "Grabbing";

        //This was added by request just in case you want to add different layers for left/right hand
        public static string rightHandLayerName = "Hand";
        public static string leftHandLayerName = "Hand";



        ///Events for all my programmers out there :)/// 
        /// <summary>Called when the grab event is triggered, event if nothing is being held</summary>
        public event HandGrabEvent OnTriggerGrab;
        /// <summary>Called at the very start of a grab before anything else</summary>
        public event HandGrabEvent OnBeforeGrabbed;
        /// <summary>Called when the hand grab connection is made (the frame the hand touches the grabbable)</summary>
	    public event HandGrabEvent OnGrabbed;

        /// <summary>Called when the release event is triggered, event if nothing is being held</summary>
        public event HandGrabEvent OnTriggerRelease;
        public event HandGrabEvent OnBeforeReleased;
        /// <summary>Called at the end the release</summary>
        public event HandGrabEvent OnReleased;

        /// <summary>Called when the squeeze button is pressed, regardless of whether an object is held or not (grab returns null)</summary>
        public event HandGrabEvent OnSqueezed;
        /// <summary>Called when the squeeze button is released, regardless of whether an object is held or not (grab returns null)</summary>
        public event HandGrabEvent OnUnsqueezed;

        /// <summary>Called when highlighting starts</summary>
        public event HandGrabEvent OnHighlight;
        /// <summary>Called when highlighting ends</summary>
        public event HandGrabEvent OnStopHighlight;

        /// <summary>Called whenever joint breaks or force release event is called</summary>
        public event HandGrabEvent OnForcedRelease;
        /// <summary>Called when the physics joint between the hand and the grabbable is broken by force</summary>
        public event HandGrabEvent OnGrabJointBreak;

        /// <summary>Legacy Event - same as OnRelease</summary>
        public event HandGrabEvent OnHeldConnectionBreak;

        public event HandGameObjectEvent OnHandCollisionStart;
        public event HandGameObjectEvent OnHandCollisionStop;
        public event HandGameObjectEvent OnHandTriggerStart;
        public event HandGameObjectEvent OnHandTriggerStop;

        List<HandTriggerAreaEvents> triggerEventAreas = new List<HandTriggerAreaEvents>();

        Coroutine tryGrab;
        Coroutine highlightRoutine;
        float startGrabDist;
        HandPoseData openHandPose;
        Grabbable lastHoldingObj;

        Coroutine _grabRoutine;
        Coroutine grabRoutine {
            get { return _grabRoutine; }
            set {
                if(value != null && _grabRoutine != null) {
                    StopCoroutine(_grabRoutine);
                    if(holdingObj != null) {
                        holdingObj.body.velocity = Vector3.zero;
                        holdingObj.body.angularVelocity = Vector3.zero;
                        holdingObj.beingGrabbed = false;
                    }
                    BreakGrabConnection();
                    grabbing = false;
                }
                _grabRoutine = value;
            }
        }


        protected override void Awake() {
            if(highlightLayers == 0) {
                highlightLayers = LayerMask.GetMask(grabbableLayerNameDefault);
            }

            handLayers = LayerMask.GetMask(rightHandLayerName, leftHandLayerName);
            
            base.Awake();

            if(enableMovement) {
                body.drag = 10;
                body.angularDrag = 38;
                body.useGravity = false;
            }



            SetLayer();
            base.Awake();
        }


        private void Start()
        {

#if UNITY_EDITOR
            if (Selection.activeGameObject == gameObject)
            {
                Selection.activeGameObject = null;
                Debug.Log("Auto Hand: highlighting hand component in the inspector can cause lag and quality reduction at runtime in VR. (Automatically deselecting at runtime) Remove this code at any time.", this);
                editorSelected = true;
            }

            Application.quitting += () => { if (editorSelected && Selection.activeGameObject == null) Selection.activeGameObject = gameObject; };
#endif
        }


        protected override void OnEnable() {
            base.OnEnable();
            highlightRoutine = StartCoroutine(HighlightUpdate(Time.fixedUnscaledDeltaTime*4));
            collisionTracker.OnCollisionFirstEnter += OnCollisionFirstEnter;
            collisionTracker.OnCollisionLastExit += OnCollisionLastExit;
            collisionTracker.OnTriggerFirstEnter += OnTriggerFirstEnter;
            collisionTracker.OnTriggeLastExit += OnTriggerLastExit;

            collisionTracker.OnCollisionFirstEnter += (collision) => { OnHandCollisionStart?.Invoke(this, collision); };
            collisionTracker.OnCollisionLastExit += (collision) => { OnHandCollisionStop?.Invoke(this, collision); };
            collisionTracker.OnTriggerFirstEnter += (collision) => { OnHandTriggerStart?.Invoke(this, collision); };
            collisionTracker.OnTriggeLastExit += (collision) => { OnHandTriggerStop?.Invoke(this, collision); };

        }

        protected override void OnDisable() {
            foreach(var trigger in triggerEventAreas)
                trigger.Exit(this);

            if(tryGrab != null)
                StopCoroutine(tryGrab);
            if(highlightRoutine != null)
                StopCoroutine(highlightRoutine);

            base.OnDisable();
            collisionTracker.OnCollisionFirstEnter -= OnCollisionFirstEnter;
            collisionTracker.OnCollisionLastExit -= OnCollisionLastExit;
            collisionTracker.OnTriggerFirstEnter -= OnTriggerFirstEnter;
            collisionTracker.OnTriggeLastExit -= OnTriggerLastExit;

            collisionTracker.OnCollisionFirstEnter -= (collision) => { OnHandCollisionStart?.Invoke(this, collision); };
            collisionTracker.OnCollisionLastExit -= (collision) => { OnHandCollisionStop?.Invoke(this, collision); };
            collisionTracker.OnTriggerFirstEnter -= (collision) => { OnHandTriggerStart?.Invoke(this, collision); };
            collisionTracker.OnTriggeLastExit -= (collision) => { OnHandTriggerStop?.Invoke(this, collision); };
        }


        protected override void Update() {
            if(enableMovement) {
                if(holdingObj && !holdingObj.maintainGrabOffset && !IsGrabbing()) {
                    var deltaDist = Vector3.Distance(follow.position, lastFrameFollowPos);
                    var deltaRot = Quaternion.Angle(follow.rotation, lastFrameFollowRot);
                    grabPositionOffset = Vector3.MoveTowards(grabPositionOffset, Vector3.zero, (deltaDist) * gentleGrabSpeed * Time.deltaTime * 60f);
                    grabRotationOffset = Quaternion.RotateTowards(grabRotationOffset, Quaternion.identity, (deltaRot) * gentleGrabSpeed * Time.deltaTime * 60f);

                    if(!holdingObj.useGentleGrab) {
                        grabPositionOffset = Vector3.MoveTowards(grabPositionOffset, Vector3.zero, Time.deltaTime / GetGrabTime());
                        grabRotationOffset = Quaternion.RotateTowards(grabRotationOffset, Quaternion.identity, (90f * Time.deltaTime) / GetGrabTime());
                    }
                }

                lastFrameFollowPos = follow.position;
                lastFrameFollowRot = follow.rotation;
            }
            base.Update();
        }


        float GetGrabTime() {
            var distanceDivider = Mathf.Clamp01(startGrabDist / reachDistance);
            var grabVelocityOffset = holdingObj.body != null ? holdingObj.body.velocity.magnitude * Time.fixedDeltaTime * distanceDivider * 2 : 0f;
            var handVelocityOffset = body.velocity.magnitude * Time.fixedDeltaTime * distanceDivider * 2;
            return Mathf.Clamp(minGrabTime + ((maxGrabTime - minGrabTime) * distanceDivider) - grabVelocityOffset - handVelocityOffset, 0, maxGrabTime);
        }

        //================== CORE INTERACTION FUNCTIONS ===================
        //================================================================
        //================================================================


        /// <summary>Function for controller trigger fully pressed -> Grabs whatever is directly in front of and closest to the hands palm</summary>
        public virtual void Grab() {
            var grabType = this.grabType;
            Grab(grabType);
        }

        /// <summary>Function for controller trigger fully pressed -> Grabs whatever is directly in front of and closest to the hands palm</summary>
        public virtual void Grab(GrabType grabType) {
            OnTriggerGrab?.Invoke(this, null);
            foreach(var triggerArea in triggerEventAreas) {
                triggerArea.Grab(this);
            }
            if(usingHighlight && !grabbing && holdingObj == null && lookingAtObj != null) {
                var newGrabType = this.grabType;
                if(lookingAtObj.grabType != HandGrabType.Default)
                    newGrabType = lookingAtObj.grabType == HandGrabType.GrabbableToHand ? GrabType.GrabbableToHand : GrabType.HandToGrabbable;
                
                grabRoutine = StartCoroutine(GrabObject(GetHighlightHit(), lookingAtObj, newGrabType));
            }

            else if(!grabbing && holdingObj == null) {
                if(HandClosestHit(out RaycastHit closestHit, out Grabbable grabbable, reachDistance, ~handLayers) != Vector3.zero && grabbable != null) {
                    var newGrabType = this.grabType;
                    if(grabbable.grabType != HandGrabType.Default)
                        newGrabType = grabbable.grabType == HandGrabType.GrabbableToHand ? GrabType.GrabbableToHand : GrabType.HandToGrabbable;
                    if(grabbable != null)
                        grabRoutine = StartCoroutine(GrabObject(closestHit, grabbable, newGrabType));
                }
            }
            else if(holdingObj != null && holdingObj.CanGetComponent(out GrabLock grabLock)) {
                grabLock.OnGrabPressed?.Invoke();
            }
        }

        /// <summary>Grabs based on raycast and grab input data</summary>
        public virtual void Grab(RaycastHit hit, Grabbable grab, GrabType grabType = GrabType.InstantGrab) {
            bool objectFree = grab.body.isKinematic != true && grab.body.constraints == RigidbodyConstraints.None;
            if(!grabbing && holdingObj == null && this.CanGrab(grab) && objectFree) {
                grabRoutine = StartCoroutine(GrabObject(hit, grab, grabType));
            }
        }

        /// <summary>Attempts grab on given grabbable</summary>
        public virtual void TryGrab(Grabbable grab) {

            if(!grabbing && holdingObj == null && this.CanGrab(grab)) {

                grab.body.position = palmTransform.position + palmTransform.forward * reachDistance;
                grab.body.transform.position = grab.body.position;

                var grabLayer = grab.gameObject.layer;
                var grabbingLayer = LayerMask.NameToLayer(grabbingLayerName);
                var grabPoint = grab.body.worldCenterOfMass;
                SetLayerRecursive(grab.transform, grabbingLayer);
                RaycastHit lastHit = new RaycastHit();
                bool didHit = false;

                for(int i = 0; i < 3; i++) {
                    if(!grabbing && holdingObj == null) {
                        Ray handGrabRay = new Ray(palmTransform.position, (grabPoint - palmTransform.position));

                        if(Physics.SphereCast(handGrabRay.origin - handGrabRay.direction * sphereCastRadius * 10f, sphereCastRadius * 3, handGrabRay.direction.normalized, out var hit, Vector3.Distance(grabPoint, palmTransform.position) * 2 + sphereCastRadius * 10, 1 << grabbingLayer, queryTriggerInteraction)) {
                            var hitToCenterOfMassDistance = grabPoint - hit.point;
                            grab.body.position = palmTransform.position - handGrabRay.direction.normalized * reachDistance * 0.5f + hitToCenterOfMassDistance;
                            grab.body.transform.position = grab.body.position;
                            lastHit = hit;
                            didHit = true;

                            if(HandClosestHit(out RaycastHit closestHit, out Grabbable grabbable, reachDistance * 2, 1 << grabbingLayer) != Vector3.zero && grabbable != null) {
                                SetLayerRecursive(grab.transform, grabLayer);
                                grabbable.body.velocity = Vector3.zero;
                                grabbable.body.angularVelocity = Vector3.zero;
                                grabRoutine = StartCoroutine(GrabObject(closestHit, grabbable, GrabType.InstantGrab));
                            }
                        }

                    }
                }

                if(!grabbing && holdingObj == null && didHit) {
                    Grab(lastHit, grab, GrabType.InstantGrab);
                }


            }
        }


        /// <summary>Function for controller trigger unpressed</summary>
        public virtual void Release() {
            OnTriggerRelease?.Invoke(this, null);
            foreach(var triggerArea in triggerEventAreas) {
                triggerArea.Release(this);
            }

            if(holdingObj && !holdingObj.wasForceReleased && holdingObj.CanGetComponent<GrabLock>(out _))
                return;

            if(holdingObj != null) {
                OnBeforeReleased?.Invoke(this, holdingObj);
                //Do the holding object calls and sets
                holdingObj?.OnRelease(this);
                OnHeldConnectionBreak?.Invoke(this, holdingObj);
                OnReleased?.Invoke(this, holdingObj);
                ignoreMoveFrame = true;
            }

            BreakGrabConnection();
        }

        /// <summary>This will force release the hand without throwing or calling OnRelease\n like losing grip on something instead of throwing</summary>
        public virtual void ForceReleaseGrab() {
            if(holdingObj != null) {
                OnForcedRelease?.Invoke(this, holdingObj);
                holdingObj?.ForceHandRelease(this);
            }
        }

        /// <summary>Old function left for backward compatability -> Will release grablocks, recommend using ForceReleaseGrab() instead</summary>
        public virtual void ReleaseGrabLock() {
            ForceReleaseGrab();
        }


        /// <summary>Event for controller grip</summary>
        public virtual void Squeeze() {
            OnSqueezed?.Invoke(this, holdingObj);
            holdingObj?.OnSqueeze(this);

            foreach(var triggerArea in triggerEventAreas) {
                triggerArea.Squeeze(this);
            }
            squeezing = true;
        }

        /// <summary>Event for controller ungrip</summary>
        public virtual void Unsqueeze() {
            squeezing = false;
            OnUnsqueezed?.Invoke(this, holdingObj);
            holdingObj?.OnUnsqueeze(this);

            foreach(var triggerArea in triggerEventAreas) {
                triggerArea.Unsqueeze(this);
            }
        }

        /// <summary>Breaks the grab event</summary>
        public virtual void BreakGrabConnection(bool callEvent = true) {

            if(holdingObj != null) {
                if(squeezing)
                    holdingObj.OnUnsqueeze(this);

                if(grabbing) {
                    if (holdingObj.body != null){
                        holdingObj.body.velocity = Vector3.zero;
                        holdingObj.body.angularVelocity = Vector3.zero;
                    }
                }

                foreach(var finger in fingers) {
                    finger.SetCurrentFingerBend(finger.GetLastHitBend());
                }

                if(holdingObj.ignoreReleaseTime == 0) {
                    transform.position = holdingObj.body.transform.InverseTransformPoint(startHandGrabPosition);
                    body.position = transform.position;
                }

                holdingObj.BreakHandConnection(this);
                lastHoldingObj = holdingObj;
                holdingObj = null;
            }


            velocityTracker.Disable(throwVelocityExpireTime);
            grabbed = false;
            grabPose = null;
            lookingAtObj = null;
            grabPositionOffset = Vector3.zero;
            grabRotationOffset = Quaternion.identity;
            grabRoutine = null;

            if(heldJoint != null) {
                Destroy(heldJoint);
                heldJoint = null;
            }
        }

        /// <summary>Creates the grab connection</summary>
        public virtual void CreateGrabConnection(Grabbable grab, Vector3 handPos, Quaternion handRot, Vector3 grabPos, Quaternion grabRot, bool executeGrabEvents = false) {

            if(executeGrabEvents) {
                OnBeforeGrabbed?.Invoke(this, grab);
                grab.OnBeforeGrab(this);
            }


            transform.position = handPos;
            body.position = handPos;
            transform.rotation = handRot;
            body.rotation = handRot;

            grab.transform.position = grabPos;
            grab.body.position = grabPos;
            grab.transform.rotation = grabRot;
            grab.body.rotation = grabRot;

            grabPoint.parent = grab.transform;
            grabPoint.transform.position = handPos;
            grabPoint.transform.rotation = handRot;


            holdingObj = grab;

            grabPosition.transform.position = holdingObj.body.transform.position;
            grabPosition.transform.rotation = holdingObj.body.transform.rotation;

            if(!(holdingObj.grabType == HandGrabType.GrabbableToHand) && !(grabType == GrabType.GrabbableToHand)) {
                grabPositionOffset = transform.position - follow.transform.position;
                grabRotationOffset = Quaternion.Inverse(follow.transform.rotation) * transform.rotation;
            }

            //If it's a predetermined Pose
            if(holdingObj.GetSavedPose(out var poseCombiner)) {
                if(poseCombiner.CanSetPose(this)) {
                    grabPose = poseCombiner.GetClosestPose(this);
                    grabPose.SetHandPose(this);
                }
            }

            if(executeGrabEvents) {
                OnGrabbed?.Invoke(this, holdingObj);
                holdingObj.OnGrab(this);
            }

            CreateJoint(holdingObj, holdingObj.jointBreakForce * ((1f / Time.fixedUnscaledDeltaTime) / 60f), float.PositiveInfinity);
        }

        public virtual void OnJointBreak(float breakForce) {
            if(heldJoint != null) {
                Destroy(heldJoint);
                heldJoint = null;
            }
            if(holdingObj != null) {
                holdingObj.body.velocity /= 100f;
                holdingObj.body.angularVelocity /= 100f;
                OnGrabJointBreak?.Invoke(this, holdingObj);
                holdingObj?.OnHandJointBreak(this);
            }
        }


        //=============== HIGHLIGHT AND LOOK ASSIST ===================
        //=============================================================
        //=============================================================

        /// <summary>Manages the highlighting for grabbables</summary>
        public virtual void UpdateHighlight() {
            if(usingHighlight && highlightLayers != 0 && holdingObj == null && !IsGrabbing()) {
                Vector3 dir = HandClosestHit(out highlightHit, out Grabbable newLookingAtObj, reachDistance, highlightLayers);

                //Zero means it didn't hit
                if(dir != Vector3.zero && (newLookingAtObj != null && newLookingAtObj.CanGrab(this))) {
                    //Changes look target
                    if(newLookingAtObj != lookingAtObj) {
                        //Unhighlights current target if found
                        if(lookingAtObj != null) {
                            OnStopHighlight?.Invoke(this, lookingAtObj);
                            lookingAtObj.Unhighlight(this);
                        }

                        lookingAtObj = newLookingAtObj;

                        //Highlights new target if found
                        OnHighlight?.Invoke(this, lookingAtObj);
                        lookingAtObj.Highlight(this);
                    }
                }
                //If it was looking at something but now it's not there anymore
                else if(newLookingAtObj == null && lookingAtObj != null) {
                    //Just in case the object your hand is looking at is destroyed
                    OnStopHighlight?.Invoke(this, lookingAtObj);
                    lookingAtObj.Unhighlight(this);

                    lookingAtObj = null;
                }
            }
        }

        /// <summary>Returns the closest raycast hit from the hand's highlighting system, if no highlight, returns blank raycasthit</summary>
        public RaycastHit GetHighlightHit() {
            highlightHit.point = grabPoint.position;
            highlightHit.normal = grabPoint.up;
            return highlightHit; 
        }




        //======================== GETTERS AND SETTERS ====================
        //=================================================================
        //=================================================================

        /// <summary>Takes a raycasthit and grabbable and automatically poses the hand</summary>
        public void AutoPose(RaycastHit hit, Grabbable grabbable) {
            var grabbableLayer = grabbable.gameObject.layer;
            var grabbingLayer = LayerMask.NameToLayer(Hand.grabbingLayerName);
            grabbable.SetLayerRecursive(grabbable.transform, grabbingLayer);

            Vector3 palmLocalPos = palmTransform.localPosition;
            Quaternion palmLocalRot = palmTransform.localRotation;

            for(int i = 0; i < 10; i++)
                Calculate();

            void Calculate() {
                Align();

                var grabDir = hit.point - palmTransform.position;
                transform.position += grabDir;
                body.position = transform.position;

                palmCollider.enabled = true;
                if(Physics.ComputePenetration(hit.collider, hit.collider.transform.position, hit.collider.transform.rotation,
                    palmCollider, palmCollider.transform.position, palmCollider.transform.rotation, out var dir, out var dist)) {
                    transform.position -= dir * dist / 2f;
                    body.position = transform.position;
                }
                palmCollider.enabled = false;

                Align();

                transform.position -= palmTransform.forward * grabDir.magnitude / 3f;
                body.position = transform.position;
            }

            void Align() {
                palmChild.position = transform.position;
                palmChild.rotation = transform.rotation;

                palmTransform.LookAt(hit.point, palmTransform.up);

                transform.position = palmChild.position;
                transform.rotation = palmChild.rotation;

                palmTransform.localPosition = palmLocalPos;
                palmTransform.localRotation = palmLocalRot;
            }

            foreach(var finger in fingers)
                finger.BendFingerUntilHit(fingerBendSteps, LayerMask.GetMask(Hand.grabbingLayerName));

            grabbable.SetLayerRecursive(grabbable.transform, grabbableLayer);

        }


        /// <summary>Returns the current hand pose, ignoring what is being held - (IF SAVING A HELD POSE USE GetHeldPose())</summary>
        public HandPoseData GetHandPose() {
            return new HandPoseData(this);
        }

        /// <summary>Returns the hand pose relative to what it's holding</summary>
        public HandPoseData GetHeldPose() {
            if(holdingObj)
                return new HandPoseData(this, holdingObj);
            return new HandPoseData(this);
        }

        /// <summary>Sets the hand pose and connects the grabbable</summary>
        public virtual void SetHeldPose(HandPoseData pose, Grabbable grabbable, bool createJoint = true) {
            //Set Pose
            pose.SetPose(this, grabbable.transform);

            if(createJoint) {
                holdingObj = grabbable;
                OnBeforeGrabbed?.Invoke(this, holdingObj);
                holdingObj.body.transform.position = transform.position;

                CreateJoint(holdingObj, holdingObj.jointBreakForce * ((1f / Time.fixedUnscaledDeltaTime) / 60f), float.PositiveInfinity);

                grabPoint.parent = holdingObj.transform;
                grabPoint.transform.position = transform.position;
                grabPoint.transform.rotation = transform.rotation;

                OnGrabbed?.Invoke(this, holdingObj);
                holdingObj.OnGrab(this);

                SetHandLocation(moveTo.position, moveTo.rotation);

                grabbed = true;
            }

        }

        /// <summary>Sets the hand pose</summary>
        public void SetHandPose(HandPoseData pose) {
            pose.SetPose(this, null);
        }

        /// <summary>Sets the hand pose</summary>
        public void SetHandPose(GrabbablePose pose) {
            pose.GetHandPoseData(this).SetPose(this, null);
        }

        /// <summary>Takes a new pose and an amount of time and poses the hand</summary>
        public void UpdatePose(HandPoseData pose, float time) {
            if(handAnimateRoutine != null)
                StopCoroutine(handAnimateRoutine);
            if(gameObject.activeInHierarchy)
                handAnimateRoutine = StartCoroutine(LerpHandPose(GetHandPose(), pose, time));
        }

        /// <summary>If the grabbable has a GrabbablePose, this will return it. Null if none</summary>
        public GrabbablePose GetGrabPose(Transform from, Grabbable grabbable) {
            GrabbablePose grabPose = null;
            if(grabbable.GetSavedPose(out var poseCombiner) && poseCombiner.CanSetPose(this)) {
                grabPose = poseCombiner.GetClosestPose(this);
                return grabPose;
            }

            return grabPose;
        }

        /// <summary>If the held grabbable has a GrabbablePose, this will return it. Null if none</summary>
        public bool GetCurrentHeldGrabPose(Transform from, Grabbable grabbable, out GrabbablePose grabPose, out Transform relativeTo) {
            if(grabbable.GetSavedPose(out var poseCombiner) && poseCombiner.CanSetPose(this)) {
                grabPose = poseCombiner.GetClosestPose(this);
                relativeTo = grabbable.transform;
                return true;
            }
            if(grabbable.GetSavedPose(out var poseCombiner1) && poseCombiner1.CanSetPose(this)) {
                grabPose = poseCombiner1.GetClosestPose(this);
                relativeTo = from;
                return true;
            }

            grabPose = null;
            relativeTo = from;
            return false;
        }

        /// <summary>Returns the current held object - null if empty (Same as GetHeld())</summary>
        public Grabbable GetHeldGrabbable() {
            return holdingObj;
        }

        /// <summary>Returns the current held object - null if empty (Same as GetHeldGrabbable())</summary>
        public Grabbable GetHeld() {
            return holdingObj;
        }

        /// <summary>Returns true if squeezing has been triggered</summary>
        public bool IsSqueezing() {
            return squeezing;
        }



        //========================= HELPER FUNCTIONS ======================
        //=================================================================
        //=================================================================

        /// <summary>Resets the grab offset created on grab for a smoother hand return</summary>
        public void ResetGrabOffset() {

            grabPositionOffset = transform.position - follow.transform.position;
            grabRotationOffset = Quaternion.Inverse(follow.transform.rotation) * transform.rotation;
        }

        /// <summary>Sets the hands grip 0 is open 1 is closed</summary>
        public void SetGrip(float grip) {
            triggerPoint = grip;
        }

        [ContextMenu("Set Pose - Relax Hand")]
        public void RelaxHand() {
            foreach(var finger in fingers)
                finger.SetFingerBend(gripOffset);
        }

        [ContextMenu("Set Pose - Open Hand")]
        public void OpenHand() {
            foreach(var finger in fingers)
                finger.SetFingerBend(0);
        }

        [ContextMenu("Set Pose - Close Hand")]
        public void CloseHand() {
            foreach(var finger in fingers)
                finger.SetFingerBend(1);
        }

        [ContextMenu("Bend Fingers Until Hit")]
        /// <summary>Bends each finger until they hit</summary>
        public void ProceduralFingerBend() {
            ProceduralFingerBend(~LayerMask.GetMask(rightHandLayerName, leftHandLayerName));
        }

        /// <summary>Bends each finger until they hit</summary>
        public void ProceduralFingerBend(int layermask) {
            foreach(var finger in fingers) {
                finger.BendFingerUntilHit(fingerBendSteps, layermask);
            }
        }

        /// <summary>Bends each finger until they hit</summary>
        public void ProceduralFingerBend(RaycastHit hit) {
            foreach(var finger in fingers) {
                finger.BendFingerUntilHit(fingerBendSteps, hit.transform.gameObject.layer);
            }
        }

        /// <summary>Plays haptic vibration on the hand controller if supported by controller link</summary>
        public void PlayHapticVibration() {
            PlayHapticVibration(0.05f, 0.5f);
        }

        /// <summary>Plays haptic vibration on the hand controller if supported by controller link</summary>
        public void PlayHapticVibration(float duration) {
            PlayHapticVibration(duration, 0.5f);
        }

        /// <summary>Plays haptic vibration on the hand controller if supported by controller link</summary>
        public void PlayHapticVibration(float duration, float amp = 0.5f) {
            if(left)
                HandControllerLink.handLeft.TryHapticImpulse(duration, amp);
            else
                HandControllerLink.handRight.TryHapticImpulse(duration, amp);
        }


        //========================= SAVING FUNCTIONS ======================
        //=================================================================
        //=================================================================
        public Hand copyFromHand;
            
        [Button("Copy Pose"), ContextMenu("COPY POSE")]
        public void CopyPose()
        {
            if (copyFromHand != null)
            {
                if (copyFromHand.fingers.Length != fingers.Length)
                {
                    Debug.LogError("Cannot copy pose because hand reference does not have the same number of fingers attached as this hand");

                }
                else
                {
                    for (int i = 0; i < copyFromHand.fingers.Length; i++)
                    {
#if UNITY_EDITOR
                        EditorUtility.SetDirty(fingers[i]);
#endif
                        fingers[i].CopyPose(copyFromHand.fingers[i]);
                    }
                    Debug.Log("Auto Hand: Copied Hand Pose!");
                }

            }
            else
            {
                Debug.LogError("Cannot copy pose because hand reference to copy from is not set");
            }
        }


        [Button("Save Open Pose"), ContextMenu("SAVE OPEN")]
        public void SaveOpenPose() {
            foreach(var finger in fingers) {
#if UNITY_EDITOR
                EditorUtility.SetDirty(finger);
#endif
                finger.SetMinPose();
            }
            Debug.Log("Auto Hand: Saved Open Hand Pose!");
        }

        [Button("Save Closed Pose"), ContextMenu("SAVE CLOSED")]
        public void SaveClosedPose() {
            foreach(var finger in fingers) {
#if UNITY_EDITOR
                EditorUtility.SetDirty(finger);
#endif
                finger.SetMaxPose();
            }
            Debug.Log("Auto Hand: Saved Closed Hand Pose!");
        }



        #region INTERNAL FUNCTIONS

        //======================= INTERNAL FUNCTIONS ======================
        //=================================================================
        //=================================================================


        protected virtual void OnCollisionFirstEnter(GameObject collision) {
            if(collision.CanGetComponent(out HandTouchEvent touchEvent)) {
                touchEvent.Touch(this);
            }
        }

        protected virtual void OnCollisionLastExit(GameObject collision) {
            if(collision.CanGetComponent(out HandTouchEvent touchEvent))
                touchEvent.Untouch(this);
        }

        protected virtual void OnTriggerFirstEnter(GameObject other) {
            CheckEnterPoseArea(other);
            if(other.CanGetComponent(out HandTriggerAreaEvents area)) {
                triggerEventAreas.Add(area);
                area.Enter(this);
            }
        }

        protected virtual void OnTriggerLastExit(GameObject other) {
            CheckExitPoseArea(other);
            if(other.CanGetComponent(out HandTriggerAreaEvents area)) {
                triggerEventAreas.Remove(area);
                area.Exit(this);
            }
        }

        //Highlighting doesn't need to be called every update, it can be called every 4th update without causing any noticable differrences 
        IEnumerator HighlightUpdate(float timestep) {
            //This will smooth out the highlight calls to help prevent occasional lag spike
            if(left)
                yield return new WaitForSecondsRealtime(timestep / 2);

            while(true) {
                if(usingHighlight) {
                    UpdateHighlight();
                }
                yield return new WaitForSecondsRealtime(timestep);
            }
        }

        Vector3 startHandGrabPosition;
        /// <summary>Takes a hit from a grabbable object and moves the hand towards that point, then calculates ideal hand shape</summary>
        protected IEnumerator GrabObject(RaycastHit hit, Grabbable grab, GrabType grabType) {
            //Checks if the grabbable script is enabled
            if(!CanGrab(grab))
                yield break;

            //SETS GRAB POINT
            grabPoint.parent = hit.collider.transform;
            grabPoint.position = hit.point;
            grabPoint.up = hit.normal;

            while(grab.beingGrabbed)
                yield return new WaitForEndOfFrame();


            CancelPose();
            ClearPoseArea();


            grabPose = null;
            grabbing = true;
            holdingObj = grab;
            lookingAtObj = null;
            var instantGrab = holdingObj.instantGrab || grabType == GrabType.InstantGrab;
            var startHoldingObj = holdingObj;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            foreach(var collider in holdingObj.heldIgnoreColliders)
                HandIgnoreCollider(collider, true);

            OnBeforeGrabbed?.Invoke(this, holdingObj);
            holdingObj.OnBeforeGrab(this);

            startHandGrabPosition = holdingObj.transform.InverseTransformPoint(transform.position);

            if(holdingObj == null) {
                CancelGrab();
                yield break;
            }


            //Sets Pose
            HandPoseData startGrabPose;

            var startGrabbablePosition = holdingObj.transform.position;
            var startGrabbableRotation = holdingObj.transform.rotation;
            startGrabDist = Vector3.Distance(palmTransform.position, grabPoint.position);

            if(grabPose = GetGrabPose(hit.collider.transform, holdingObj)) {
                startGrabPose = new HandPoseData(this, grabPose.transform);
            }
            else {
                startGrabPose = new HandPoseData(this, grabPoint);
                transform.position -= palmTransform.forward * 0.08f;
                body.position = transform.position;
                hit.point = grabPoint.position;
                hit.normal = grabPoint.up;
                AutoPose(hit, holdingObj);
            }

            var adjustedGrabTime = GetGrabTime();
            instantGrab = instantGrab || adjustedGrabTime == 0;

            if(grabType == GrabType.GrabbableToHand) {
                //Hand Swap - One Handed Items
                if(holdingObj.singleHandOnly && holdingObj.HeldCount() > 0) {
                    holdingObj.ForceHandRelease(holdingObj.GetHeldBy()[0]);
                }
            }

                //Smooth Grabbing
            if(!instantGrab) {
                Transform grabTarget = grabPose != null ? grabPose.transform : grabPoint;
                HandPoseData postGrabPose = grabPose == null ? new HandPoseData(this, grabPoint) : grabPose.GetHandPoseData(this);
                var endGrabbablePosition = transform.InverseTransformPoint(holdingObj.transform.position);
                var endGrabbableRotation = Quaternion.Inverse(palmTransform.transform.rotation) * holdingObj.transform.rotation;

                foreach(var finger in fingers)
                    finger.SetFingerBend(gripOffset + Mathf.Clamp01(finger.GetCurrentBend()/4f));
                openHandPose = GetHandPose();

                if(grabType == GrabType.HandToGrabbable || (grabType == GrabType.GrabbableToHand && (holdingObj.HeldCount() > 0 || !holdingObj.parentOnGrab))) {
                    for(float i = 0; i < adjustedGrabTime; i += Time.deltaTime) {
                        if(holdingObj != null) {
                            i += holdingObj.GetVelocity().magnitude * Time.deltaTime * 5;
                            i += followVel.magnitude * Time.deltaTime * 7;
                            if(i < adjustedGrabTime) {
                                var point = Mathf.Clamp01(i / adjustedGrabTime);
                                var handOpenTime = 0.5f;
                                var handTargetTime = 1.5f;

                                if(point < handOpenTime)
                                    HandPoseData.LerpPose(startGrabPose, openHandPose, grabCurve.Evaluate(point * 1f / handOpenTime)).SetFingerPose(this, grabTarget);
                                else
                                    HandPoseData.LerpPose(openHandPose, postGrabPose, grabCurve.Evaluate((point - handOpenTime) * (1f / (1 - handOpenTime)))).SetFingerPose(this, grabTarget);

                                HandPoseData.LerpPose(startGrabPose, postGrabPose, point * handTargetTime).SetPosition(this, grabTarget);
                                body.position = transform.position;
                                body.rotation = transform.rotation;

                                if(holdingObj.body != null)
                                    holdingObj.body.angularVelocity *= 0.5f;
                                yield return new WaitForEndOfFrame();
                            }
                        }
                    }

                    //Hand Swap - One Handed Items
                    if (holdingObj != null && holdingObj.singleHandOnly && holdingObj.HeldCount() > 0)
                    {
                        holdingObj.ForceHandRelease(holdingObj.GetHeldBy()[0]);
                        if (holdingObj.body != null)
                        {
                            holdingObj.body.velocity = Vector3.zero;
                            holdingObj.body.angularVelocity = Vector3.zero;
                        }
                    }
                }
                else if(grabType == GrabType.GrabbableToHand)
                {
                    holdingObj.ActivateRigidbody();

                    bool useGravity = true;
                    if (holdingObj.body != null)
                    {
                        useGravity = holdingObj.body.useGravity;
                        holdingObj.body.useGravity = false;
                    }
                    for(float i = 0; i < adjustedGrabTime; i += Time.deltaTime) {
                        if(holdingObj != null) {
                            var point = Mathf.Clamp01(i / adjustedGrabTime);
                            var handOpenTime = 0.5f;

                            if(point < handOpenTime)
                                HandPoseData.LerpPose(startGrabPose, openHandPose, grabCurve.Evaluate(point * 1f / handOpenTime)).SetFingerPose(this, grabTarget);
                            
                            else
                                HandPoseData.LerpPose(openHandPose, postGrabPose, grabCurve.Evaluate((point - handOpenTime) * (1f / (1 - handOpenTime)))).SetFingerPose(this, grabTarget);

                            SetMoveTo();
                            SetHandLocation(moveTo.position, moveTo.rotation);

                            body.position = transform.position;
                            body.rotation = transform.rotation;
                            holdingObj.body.transform.position = Vector3.Lerp(startGrabbablePosition, transform.TransformPoint(endGrabbablePosition), grabCurve.Evaluate(point*2));
                            holdingObj.body.transform.rotation = Quaternion.Lerp(startGrabbableRotation, palmTransform.rotation*endGrabbableRotation, grabCurve.Evaluate(point*2));

                            if (holdingObj.body != null)
                            {
                                holdingObj.body.position = holdingObj.body.transform.position;
                                holdingObj.body.rotation = holdingObj.body.transform.rotation;
                                holdingObj.body.velocity = Vector3.zero;
                                holdingObj.body.angularVelocity = Vector3.zero;
                            }
                            i += followVel.magnitude * Time.deltaTime * 2f;

                            yield return new WaitForEndOfFrame();

                        }
                    }

                    if(holdingObj != null && holdingObj.body != null)
                        holdingObj.body.useGravity = useGravity;
                    else if(startHoldingObj.body != null)
                        startHoldingObj.body.useGravity = useGravity;
                }

                if(holdingObj != null) {
                    if(grabPose != null)
                        grabPose.SetHandPose(this, grabTarget);
                    else
                        postGrabPose.SetPose(this, grabTarget);
                    body.position = transform.position;
                    body.rotation = transform.rotation;
                    if (holdingObj.body != null)
                    {
                        holdingObj.body.position = holdingObj.body.transform.position;
                        holdingObj.body.rotation = holdingObj.body.transform.rotation;
                    }
                }

            }
            else {
                //Hand Swap - One Handed Items
                if(holdingObj.singleHandOnly && holdingObj.HeldCount() > 0) {
                    holdingObj.ForceHandRelease(holdingObj.GetHeldBy()[0]);
                    if (holdingObj.body != null)
                    {
                        holdingObj.body.velocity = Vector3.zero;
                        holdingObj.body.angularVelocity = Vector3.zero;
                    }
                }

                if(grabPose != null) {
                    grabPose.SetHandPose(this, grabPose.transform);
                }
            }

            if(holdingObj == null) {
                CancelGrab();
                yield break;
            }

            holdingObj.ActivateRigidbody();
            CreateJoint(holdingObj, holdingObj.jointBreakForce * ((1f / Time.fixedUnscaledDeltaTime) / 60f), float.PositiveInfinity);

            SetMoveTo();

            grabPoint.transform.position = transform.position;
            grabPoint.transform.rotation = transform.rotation;
            grabPosition.position = holdingObj.transform.position;
            grabPosition.rotation = holdingObj.transform.rotation;

            OnGrabbed?.Invoke(this, holdingObj);
            holdingObj.OnGrab(this);



            if(!instantGrab || !holdingObj.parentOnGrab) {
                grabPositionOffset = transform.position - follow.transform.position;
                grabRotationOffset = Quaternion.Inverse(follow.transform.rotation) * transform.rotation;
            }
            if(instantGrab && holdingObj.parentOnGrab) {
                SetMoveTo();
                SetHandLocation(moveTo.position, moveTo.rotation);
            }

            if(holdingObj == null) {
                CancelGrab();
                yield break;
            }

            void CancelGrab() {
                BreakGrabConnection();
                if(startHoldingObj)
                {
                    if (startHoldingObj.body != null)
                    {
                        startHoldingObj.body.velocity = Vector3.zero;
                        startHoldingObj.body.angularVelocity = Vector3.zero;
                    }
                    startHoldingObj.beingGrabbed = false;
                }
                grabbing = false;
                grabRoutine = null;
            }

            grabbed = true;
            grabbing = false;
            startHoldingObj.beingGrabbed = false;
            grabRoutine = null;
        }

        /// <summary>Ensures any pose being made is canceled</summary>
        protected void CancelPose() {
            if(handAnimateRoutine != null)
                StopCoroutine(handAnimateRoutine);
            handAnimateRoutine = null;
            grabPose = null;
        }

        /// <summary>Not exactly lerped, uses non-linear sqrt function because it looked better -- planning animation curves options soon</summary>
        protected virtual IEnumerator LerpHandPose(HandPoseData fromPose, HandPoseData toPose, float totalTime) {
            float timePassed = 0;
            while(timePassed < totalTime) {
                SetHandPose(HandPoseData.LerpPose(fromPose, toPose, Mathf.Pow(timePassed / totalTime, 0.5f)));
                yield return new WaitForEndOfFrame();
                timePassed += Time.deltaTime;
            }
            SetHandPose(HandPoseData.LerpPose(fromPose, toPose, 1));
            handAnimateRoutine = null;
        }

        /// <summary>Checks and manages if any of the hands colliders enter a pose area</summary>
        protected virtual void CheckEnterPoseArea(GameObject other) {
            if(holdingObj || !usingPoseAreas || !other.activeInHierarchy)
                return;

            if(other && other.CanGetComponent(out HandPoseArea tempPose)) {
                for(int i = 0; i < tempPose.poseAreas.Length; i++) {
                    if(tempPose.poseIndex == poseIndex) {
                        if(tempPose.HasPose(left) && (handPoseArea == null || handPoseArea != tempPose)) {
                            if(handPoseArea == null)
                                preHandPoseAreaPose = GetHandPose();

                            else if(handPoseArea != null)
                                TryRemoveHandPoseArea(handPoseArea);

                            handPoseArea = tempPose;
                            handPoseArea?.OnHandEnter?.Invoke(this);
                            if(holdingObj == null)
                                UpdatePose(handPoseArea.GetHandPoseData(left), handPoseArea.transitionTime);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>Checks if manages any of the hands colliders exit a pose area</summary>
        protected virtual void CheckExitPoseArea(GameObject other) {
            if(!usingPoseAreas || !other.gameObject.activeInHierarchy)
                return;

            if(other.CanGetComponent(out HandPoseArea poseArea))
                TryRemoveHandPoseArea(poseArea);
        }

        internal void TryRemoveHandPoseArea(HandPoseArea poseArea) {
            if(handPoseArea != null && handPoseArea.gameObject.Equals(poseArea.gameObject)) {
                try
                {
                    if (holdingObj == null)
                    {
                        if (handPoseArea != null)
                            UpdatePose(preHandPoseAreaPose, handPoseArea.transitionTime);
                        handPoseArea?.OnHandExit?.Invoke(this);
                        handPoseArea = null;
                    }
                    else if (holdingObj != null)
                    {
                        handPoseArea?.OnHandExit?.Invoke(this);
                        handPoseArea = null;
                    }
                }
                catch(MissingReferenceException e)
                {
                    handPoseArea = null;
                    SetHandPose(preHandPoseAreaPose);
                }
            }
        }

        private void ClearPoseArea() {
            if(handPoseArea != null)
                handPoseArea.OnHandExit?.Invoke(this);
            handPoseArea = null;
        }

        internal virtual void RemoveHandTriggerArea(HandTriggerAreaEvents handTrigger) {
            handTrigger.Exit(this);
            triggerEventAreas.Remove(handTrigger);
        }

        #endregion
    }
}