using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine.Serialization;


namespace Autohand {
    public enum HandGrabType {
        Default,
        HandToGrabbable,
        GrabbableToHand
    }

    [HelpURL("https://earnestrobot.notion.site/Grabbables-9308c564e60848a882eb23e9778ee2b6"), DefaultExecutionOrder(-5)]
    public class Grabbable : GrabbableBase {


        [Tooltip("This will copy the given grabbables settings to this grabbable when applied"), OnValueChanged("EditorCopyGrabbable")]
        public Grabbable CopySettings;

        [Header("Grab Settings")]
        [Tooltip("Which hand this can be held by")]
        public HandGrabType grabType = HandGrabType.Default;

        [Tooltip("Which hand this can be held by")]
        public HandType handType = HandType.both;


        [Tooltip("Whether or not this can be grabbed with more than one hand")]
        public bool singleHandOnly = false;

        [ShowIf("singleHandOnly")]
        [Tooltip("if false single handed items cannot be passes back and forth on grab")]
        public bool allowHeldSwapping = true;

        [Tooltip("Will the item automatically return the hand on grab - good for saved poses, bad for heavy things")]
        public bool instantGrab = false;

        [DisableIf("instantGrab"), Tooltip("If true (and using HandToGrabbable) the hand will only return to the follow while moving. Good for picking up objects without disrupting the things around them - you can change the speed of the hand return on the hand through the gentleGrabSpeed value")]
        public bool useGentleGrab = false;


        [Tooltip("Creates an offset an grab so the hand will not return to the hand on grab - Good for statically jointed grabbable objects")]
        public bool maintainGrabOffset = false;

        [Tooltip("Experimental - ignores weight of held object while held")]
        public bool ignoreWeight = false;

        [Tooltip("This will NOT parent the object under the hands on grab. This will parent the object to the parents of the hand, which allow you to move the hand parent object smoothly while holding an item, but will also allow you to move items that are very heavy - recommended for all objects that aren't very heavy or jointed to other rigidbodies")]
        public bool parentOnGrab = true;


        [Header("Release Settings")]
        [Tooltip("How much to multiply throw by for this grabbable when releasing - 0-1 for no or reduced throw strength")]
        [FormerlySerializedAs("throwMultiplyer")]
        public float throwPower = 1;

        [Tooltip("The required force to break the fixedJoint\n " +
                 "Turn this to \"infinity\" to disable (Might cause jitter)\n" +
                "Ideal value depends on hand mass and velocity settings")]
        public float jointBreakForce = 3500;



        [AutoSmallHeader("Advanced Settings")]
        public bool showAdvancedSettings = true;


        [Tooltip("Adds and links a GrabbableChild to each child with a collider on start - So the hand can grab them")]
        public bool makeChildrenGrabbable = true;

        [Min(0), Tooltip("I.E. Grab Prioirty - BIGGER IS BETTER - divides highlight distance by this when calculating which object to grab. Hands always grab closest object to palm")]
        public float grabPriorityWeight = 1;

        [Tooltip("The number of seconds that the hand collision should ignore the released object\n (Good for increased placement precision and resolves clipping errors)"), Min(0)]
        public float ignoreReleaseTime = 0.5f;

        [Space]

        [Tooltip("Offsets the grabbable by this much when being held")]
        public Vector3 heldPositionOffset;

        [Tooltip("Offsets the grabbable by this many degrees when being held")]
        public Vector3 heldRotationOffset;

        [Space]

        [Min(0), Tooltip("The joint that connects the hand and the grabbable. Defaults to the joint in AutoHand/Resources/DefaultJoint.prefab if empty")]
        public ConfigurableJoint customGrabJoint;

        [Space]

        [Tooltip("For the special use case of having grabbable objects with physics jointed peices move properly while being held")]
        public List<Rigidbody> jointedBodies = new List<Rigidbody>();

        [Tooltip("For the special use case of having things connected to the grabbable that the hand should ignore while being held (good for doors and drawers) -> for always active use the [GrabbableIgnoreHands] Component")]
        public List<Collider> heldIgnoreColliders = new List<Collider>();

        [Space]

        [Tooltip("Whether or not the break call made only when holding with multiple hands - if this is false the break event can be called by forcing an object into a static collider")]
        public bool pullApartBreakOnly = true;

        [AutoToggleHeader("Show Events")]
        public bool showEvents = true;
        [Space]
        [ShowIf("showEvents")]
        public UnityHandGrabEvent onGrab = new UnityHandGrabEvent();
        [ShowIf("showEvents")]
        public UnityHandGrabEvent onRelease = new UnityHandGrabEvent();

        [ShowIf("showEvents")]
        [Space, Space]
        public UnityHandGrabEvent onSqueeze = new UnityHandGrabEvent();
        [ShowIf("showEvents")]
        public UnityHandGrabEvent onUnsqueeze = new UnityHandGrabEvent();

        [Space, Space]
        [ShowIf("showEvents")]
        public UnityHandGrabEvent onHighlight = new UnityHandGrabEvent();
        [ShowIf("showEvents")]
        public UnityHandGrabEvent onUnhighlight = new UnityHandGrabEvent();
        [Space, Space]

        [ShowIf("showEvents")]
        public UnityHandGrabEvent OnJointBreak = new UnityHandGrabEvent();


        //Advanced Hidden Settings
        [HideInInspector, Tooltip("Lock hand in place on grab (This is a legacy setting, set hand kinematic on grab/release instead)")]
        public bool lockHandOnGrab = false;



        //For programmers <3
        public HandGrabEvent OnBeforeGrabEvent;
        public HandGrabEvent OnGrabEvent;

        public HandGrabEvent OnReleaseEvent;
        public HandGrabEvent OnJointBreakEvent;

        public HandGrabEvent OnSqueezeEvent;
        public HandGrabEvent OnUnsqueezeEvent;

        public HandGrabEvent OnHighlightEvent;
        public HandGrabEvent OnUnhighlightEvent;

        public PlacePointEvent OnPlacePointHighlightEvent;
        public PlacePointEvent OnPlacePointUnhighlightEvent;
        public PlacePointEvent OnPlacePointAddEvent;
        public PlacePointEvent OnPlacePointRemoveEvent;


        /// <summary>Whether or not this object was force released (dropped) when last released (as opposed to being intentionally released)</summary>
        public bool wasForceReleased { get; internal set; } = false;
        public Hand lastHeldBy { get; protected set; } = null;


        /// <summary>Legacy value for Throw Power</summary>
        public float throwMultiplyer {
            get { return throwPower; }
            set { throwPower = value; }
        }


#if UNITY_EDITOR
        bool editorSelected = false;
        void EditorCopyGrabbable() {
            if(CopySettings != null)
                EditorUtility.CopySerialized(CopySettings, this);
        }
#endif


        protected override void Start()
        {
            base.Start();
#if UNITY_EDITOR
            if (Selection.activeGameObject == gameObject)
            {
                Selection.activeGameObject = null;
                Debug.Log("Auto Hand: highlighting grabbables and rigidbodies in the inspector can cause lag and quality reduction at runtime in VR. (Automatically deselecting at runtime) Remove this code at any time.", this);
                editorSelected = true;
            }
            Application.quitting += () => { if (editorSelected && Selection.activeGameObject == null) Selection.activeGameObject = gameObject; };
#endif
        }

        protected new virtual void Awake() {
            if(makeChildrenGrabbable)
                MakeChildrenGrabbable();

            base.Awake();

            for(int i = 0; i < jointedBodies.Count; i++) {
                jointedParents.Add(jointedBodies[i].transform.parent ?? null);
                if(jointedBodies[i].gameObject.HasGrabbable(out var grabbable) && jointedGrabbables.Contains(grabbable))
                    jointedGrabbables.Add(grabbable);
            }


        }

        void Update()
        {
            UpdateHeldInterpolation();
        }

        protected override void FixedUpdate() {
            base.FixedUpdate();
            if(wasIsGrabbable && !(isGrabbable || enabled))
                ForceHandsRelease();
            wasIsGrabbable = isGrabbable || enabled;
            ignoreInterpolation = false;
            lastUpdateTime = Time.fixedTime;
        }


        protected virtual void OnDestroy()
        {
            beingDestroyed = true;
            if (resetLayerRoutine != null)
            {
                if (ignoringHand != null)
                {
                    IgnoreHand(ignoringHand, false);
                }
                StopCoroutine(resetLayerRoutine);
                resetLayerRoutine = null;
            }


            if (heldBy.Count != 0)
                ForceHandsRelease();

            MakeChildrenUngrabbable();
            if (placePoint != null && !placePoint.disablePlacePointOnPlace)
                placePoint.Remove(this);

            Destroy(poseCombiner);
        }


        internal bool ShouldInterpolate()
        {
            bool isGrabbing = false;
            for(int i = 0; i < heldBy.Count; i++)
                isGrabbing = isGrabbing || heldBy[i].IsGrabbing();

            return !rigidbodyDeactivated
                && !isGrabbing
                && !ignoreInterpolation
                && heldBy.Count > 0
                && jointedBodies.Count == 0
                && parentOnGrab
                && CollisionCount() == 0
                && !body.isKinematic
                && body.constraints == RigidbodyConstraints.None
                && HeldCount() == heldBy.Count
                && body.mass / 4f < heldBy[0].body.mass;
        }

        internal void UpdateHeldInterpolation() {

            if (false && ShouldInterpolate()) {
                var deltaTime = (Time.time - lastUpdateTime) / heldBy.Count;
                for (int i = 0; i < heldBy.Count; i++) {
                    var startPos = body.transform.position;
                    var startRot = body.transform.rotation;

                    body.transform.position = Vector3.MoveTowards(body.transform.position, heldBy[i].grabPosition.position, body.velocity.magnitude * deltaTime);
                    body.velocity *= (1 - body.drag * deltaTime); 
                    body.position = body.transform.position;

                    body.transform.rotation = Quaternion.Euler(Vector3.MoveTowards(body.transform.rotation.eulerAngles, heldBy[i].grabPosition.rotation.eulerAngles, body.angularVelocity.magnitude * deltaTime));
                    body.angularVelocity *= (1 - body.angularDrag * deltaTime);
                    body.rotation = body.transform.rotation;


                    var deltaPos = body.transform.position - startPos;
                    if (body.transform.position != startPos)
                    {
                        if (body.SweepTest(deltaPos, out var hit, deltaPos.magnitude))
                        {
                            if (hit.rigidbody != heldBy[0].body && (heldBy.Count == 1 || heldBy[1].body != hit.rigidbody))
                            {
                                body.transform.position -= deltaPos;
                                body.position = body.transform.position;
                            }
                            else
                            {
                                heldBy[i].transform.position += body.transform.position - startPos;
                                heldBy[i].body.position = body.transform.position;
                                heldBy[i].transform.rotation *= (body.transform.rotation * Quaternion.Inverse(startRot));
                                heldBy[i].body.rotation = body.transform.rotation;
                            }
                        }
                        else
                        {
                            heldBy[i].transform.position += body.transform.position - startPos;
                            heldBy[i].body.position = body.transform.position;
                            heldBy[i].transform.rotation *= (body.transform.rotation * Quaternion.Inverse(startRot));
                            heldBy[i].body.rotation = body.transform.rotation;
                        }
                    }
                }
            }

            lastUpdateTime = Time.time;
        }

        bool ignoreInterpolation = false;
        internal void IgnoreInterpolationForOneFixedUpdate() {
            ignoreInterpolation = true;
        }


        internal void IgnoreColliders(Collider bodyCapsule, bool ignore = true) {
            foreach(var col in grabColliders)
                Physics.IgnoreCollision(bodyCapsule, col, ignore);
        }


        Dictionary<Material, List<GameObject>> highlightObjs = new Dictionary<Material, List<GameObject>>();
        void TryCreateHighlight(Material customMat, Hand hand)
        {

            var highlightMat = customMat != null ? customMat : hightlightMaterial;
            highlightMat = highlightMat != null ? highlightMat : hand.defaultHighlight;
            if (highlightMat != null && !highlightObjs.ContainsKey(highlightMat))
            {
                highlightObjs.Add(highlightMat, new List<GameObject>());
                AddHighlightObject(transform);


                bool AddHighlightObject(Transform obj)
                {

                    //This will stop the highlighting subsearch if there is another grabbable so that grabbable can create its own highlight settings/section
                    if (obj.CanGetComponent<Grabbable>(out var grab) && grab != this)
                        return false;
                    if((highlightObjs[highlightMat].Contains(obj.gameObject)))
                        return true;

                    for (int i = 0; i < obj.childCount; i++)
                    {
                        if (!AddHighlightObject(obj.GetChild(i)))
                            break;
                    }

                    MeshRenderer meshRenderer;
                    if (obj.CanGetComponent(out meshRenderer))
                    {
                        //Creates a slightly larger copy of the mesh and sets its material to highlight material
                        var highlightObj = new GameObject();
                        highlightObj.transform.parent = obj;
                        highlightObj.transform.localPosition = Vector3.zero;
                        highlightObj.transform.localRotation = Quaternion.identity;
                        highlightObj.transform.localScale = Vector3.one * 1.001f;
                        highlightObj.AddComponent<MeshFilter>().sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                        var highlightRenderer = highlightObj.AddComponent<MeshRenderer>();
                        var mats = new Material[meshRenderer.materials.Length];
                        for (int i = 0; i < mats.Length; i++)
                            mats[i] = highlightMat;
                        highlightRenderer.materials = mats;
                        highlightObjs[highlightMat].Add(highlightObj);
                    }

                    return true;
                }
            }

        }

        void DestroyHighlightCopy()
        {

        }

        void ToggleHighlight(Hand hand, Material customMat, bool enableHighlight)
        {
            var highlightMat = customMat != null ? customMat : hightlightMaterial;
            highlightMat = highlightMat != null ? highlightMat : hand.defaultHighlight;
            if (highlightMat != null && highlightObjs.ContainsKey(highlightMat))
                for (int i = 0; i < highlightObjs[highlightMat].Count; i++)
                    highlightObjs[highlightMat][i].SetActive(enableHighlight);
        }

        /// <summary>Called when the hand starts aiming at this item for pickup</summary>
        internal virtual void Highlight(Hand hand, Material customMat = null) {
            if(!hightlighting) {
                hightlighting = true;
                onHighlight?.Invoke(hand, this);
                OnHighlightEvent?.Invoke(hand, this);
                TryCreateHighlight(customMat, hand);
                ToggleHighlight(hand, customMat, true);


                //if(highlightMat != null) {
                //    MeshRenderer meshRenderer;
                //    if(gameObject.CanGetComponent(out meshRenderer)) {
                //        //Creates a slightly larger copy of the mesh and sets its material to highlight material
                //        highlightObj = new GameObject();
                //        highlightObj.transform.parent = transform;
                //        highlightObj.transform.localPosition = Vector3.zero;
                //        highlightObj.transform.localRotation = Quaternion.identity;
                //        highlightObj.transform.localScale = Vector3.one * 1.001f;
                //        highlightObj.AddComponent<MeshFilter>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
                //        var highlightRenderer = highlightObj.AddComponent<MeshRenderer>();
                //        var mats = new Material[meshRenderer.materials.Length];
                //        for(int i = 0; i < mats.Length; i++)
                //            mats[i] = highlightMat;
                //        highlightRenderer.materials = mats;
                //    }
                //}
            }
        }


        /// <summary>Called when the hand stops aiming at this item</summary>
        internal virtual void Unhighlight(Hand hand, Material customMat = null) {
            if(hightlighting) {
                onUnhighlight?.Invoke(hand, this);
                OnUnhighlightEvent?.Invoke(hand, this);
                hightlighting = false;
                ToggleHighlight(hand, customMat, false);
            }
        }





        /// <summary>Called by the hands Squeeze() function is called and this item is being held</summary>
        internal virtual void OnSqueeze(Hand hand) {
            OnSqueezeEvent?.Invoke(hand, this);
            onSqueeze?.Invoke(hand, this);
        }

        /// <summary>Called by the hands Unsqueeze() function is called and this item is being held</summary>
        internal virtual void OnUnsqueeze(Hand hand) {
            OnUnsqueezeEvent?.Invoke(hand, this);
            onUnsqueeze?.Invoke(hand, this);
        }

        /// <summary>Called by the hand when this item is started being grabbed</summary>
        internal virtual void OnBeforeGrab(Hand hand) {

            OnBeforeGrabEvent?.Invoke(hand, this);
            Unhighlight(hand, null);
            beingGrabbed = true;
            if(resetLayerRoutine != null){
                if (ignoringHand != null)
                    IgnoreHand(ignoringHand, false);
                StopCoroutine(resetLayerRoutine);
                resetLayerRoutine = null;
            }
            resetLayerRoutine = StartCoroutine(IgnoreHandCollision(hand.maxGrabTime, hand));
        }

        /// <summary>Called by the hand whenever this item is grabbed</summary>
        internal virtual void OnGrab(Hand hand) {

            if (rigidbodyDeactivated)
                ActivateRigidbody();

            if (lockHandOnGrab)
                hand.body.isKinematic = true;

            body.collisionDetectionMode = body.isKinematic ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.None;
            body.solverIterations = 200;
            body.solverVelocityIterations = 200;

            if(parentOnGrab) {
                body.transform.parent = hand.transform.parent;
                foreach(var jointedBody in jointedBodies) {
                    jointedBody.transform.parent = hand.transform.parent;
                    if(jointedBody.gameObject.HasGrabbable(out var grab))
                        grab.heldBodyJointed = true;
                }
            }

            if(ignoreWeight) {
                if(!gameObject.CanGetComponent(out WeightlessFollower heldFollower) || singleHandOnly)
                    heldFollower = gameObject.AddComponent<WeightlessFollower>();
                heldFollower?.Set(hand, this);
            }

            collisionTracker.enabled = true;

            placePoint?.Remove(this);
            heldBy?.Add(hand);
            onGrab?.Invoke(hand, this);
            OnGrabEvent?.Invoke(hand, this);

            wasForceReleased = false;
            beingGrabbed = false;
        }

        /// <summary>Whether or not the hand can grab this grabbable</summary>
        public virtual bool CanGrab(HandBase hand) {
            return enabled && isGrabbable && (handType == HandType.both || (handType == HandType.left && hand.left) || (handType == HandType.right && !hand.left));
        }

        /// <summary>Called by the hand whenever this item is release</summary>
        internal virtual void OnRelease(Hand hand)
        {
            if (heldBy.Contains(hand)) {

                bool canPlace = placePoint != null && placePoint.CanPlace(this);

                BreakHandConnection(hand);

                if(body != null && heldBy.Count == 0) {
                    body.velocity = hand.ThrowVelocity() * throwMultiplyer;
                    var throwAngularVel = hand.ThrowAngularVelocity();
                    if(!float.IsNaN(throwAngularVel.x) && !float.IsNaN(throwAngularVel.y) && !float.IsNaN(throwAngularVel.z))
                        body.angularVelocity = throwAngularVel;
                }

                OnReleaseEvent?.Invoke(hand, this);
                onRelease?.Invoke(hand, this);

                Unhighlight(hand, null);

                if(placePoint != null && canPlace)
                    placePoint.Place(this);
            }
        }

        internal virtual void BreakHandConnection(Hand hand)
        {
            if (!heldBy.Remove(hand))
                return;

            if (lockHandOnGrab)
                hand.body.isKinematic = false;

            if(gameObject.activeInHierarchy && !beingDestroyed) {
                if(resetLayerRoutine != null) {
                    if(ignoringHand != null)
                        IgnoreHand(ignoringHand, false);
                    StopCoroutine(resetLayerRoutine);
                    resetLayerRoutine = null;
                }
                resetLayerRoutine = StartCoroutine(IgnoreHandCollision(ignoreReleaseTime, hand));
            }

            foreach(var collider in heldIgnoreColliders)
                hand.HandIgnoreCollider(collider, false);

            if(HeldCount() == 0) {
                beingGrabbed = false;
                ResetGrabbableAfterRlease();
            }

            if (body != null){
                body.solverIterations = Physics.defaultSolverIterations;
                body.solverVelocityIterations = Physics.defaultSolverVelocityIterations;
            }
            collisionTracker.enabled = false;
            lastHeldBy = hand;
        }

        /// <summary>Tells each hand holding this object to release</summary>
        public virtual void HandsRelease() {
            for(int i = heldBy.Count - 1; i >= 0; i--)
                heldBy[i].Release();
        }

        /// <summary>Tells each hand holding this object to release</summary>
        public virtual void HandRelease(Hand hand) {
            if(heldBy.Contains(hand)) {
                hand.Release();
            }
        }

        /// <summary>Forces all the hands on this object to relese without applying throw force or calling OnRelease event</summary>
        public virtual void ForceHandsRelease() {
            for(int i = heldBy.Count - 1; i >= 0; i--) {
                wasForceReleased = true;
                ForceHandRelease(heldBy[i]);
            }
        }

        /// <summary>Forces all the hands on this object to relese without applying throw force</summary>
        public virtual void ForceHandRelease(Hand hand) {
            if(heldBy.Contains(hand)) {
                var throwMult = throwPower;
                throwPower = 0;
                wasForceReleased = true;
                hand.Release();
                throwPower = throwMult;
            }
        }


        /// <summary>Called when the joint between the hand and this item is broken\n - Works to simulate pulling item apart event</summary>
        public virtual void OnHandJointBreak(Hand hand) {
            if(heldBy.Contains(hand)) {
                if (body != null){
                    body.WakeUp();
                    body.velocity *= 0;
                    body.angularVelocity *= 0;
                }

                if(!pullApartBreakOnly) {
                    OnJointBreakEvent?.Invoke(hand, this);
                    OnJointBreak?.Invoke(hand, this);
                }
                if(pullApartBreakOnly && HeldCount() > 1) {
                    OnJointBreakEvent?.Invoke(hand, this);
                    OnJointBreak?.Invoke(hand, this);
                }

                ForceHandRelease(hand);

                if(heldBy.Count > 0)
                    heldBy[0].SetHandLocation(heldBy[0].moveTo.position, heldBy[0].transform.rotation);
            }
        }

        //============================ GETTERS ============================
        //=================================================================
        //=================================================================


        /// <summary>Returns the list of hands that are currently holding this grabbables</summary>
        public List<Hand> GetHeldBy() {
            return heldBy;
        }
        
        /// <summary>Returns the number of hands currently holding this object [Call GetHeldBy() to get a list of the hand references]</summary>
        /// <param name="includedJointedCount">Whether or not to return the held count of only this grabbable, or the total of this grabbable and any jointed bodies with a grabbable attached</param>
        public int HeldCount(bool includedJointedCount = true) {
            var count = heldBy.Count;
            if(includedJointedCount)
                for(int i = 0; i < jointedGrabbables.Count; i++)
                    count += jointedGrabbables[i].heldBy.Count;
            return count;
        }



        /// <summary>Returns true if this grabbable is currently being held</summary>
        public bool IsHeld() {
            return heldBy.Count > 0;
        }

        /// <summary>Returns true during hand grabbing coroutine</summary>
        public bool BeingGrabbed() {
            return beingGrabbed;
        }

        /// <summary>Plays haptic on each hand holding this grabbable</summary>
        public void PlayHapticVibration() {
            foreach(var hand in heldBy) {
                hand.PlayHapticVibration();
            }
        }

        /// <summary>Plays haptic on each hand holding this grabbable</summary>
        public void PlayHapticVibration(float duration = 0.025f) {
            foreach(var hand in heldBy) {
                hand.PlayHapticVibration(duration);
            }
        }

        /// <summary>Plays haptic on each hand holding this grabbable</summary>
        public void PlayHapticVibration(float duration, float amp = 0.5f) {
            foreach(var hand in heldBy) {
                hand.PlayHapticVibration(duration, amp);
            }
        }



        public Vector3 GetVelocity() {
            if (body == null)
                return Vector3.zero;
            return lastCenterOfMassPos - body.position;
        }

        public Vector3 GetAngularVelocity() {
            Quaternion deltaRotation = body.rotation * Quaternion.Inverse(lastCenterOfMassRot);
            deltaRotation.ToAngleAxis(out var angle, out var axis);
            angle *= Mathf.Deg2Rad;
            return (1.0f / Time.fixedDeltaTime) * angle / 1.2f * axis;
        }



        public void SetParentOnGrab(bool parentOnGrab) {
            this.parentOnGrab = parentOnGrab;
        }

        /// <summary>Add a jointed rigidbody to this grabbable, important for continuity between a held object and it's jointed bodies</summary>
        public void AddJointedBody(Rigidbody body) {
            Grabbable grab;
            jointedBodies.Add(body);

            if(body.gameObject.HasGrabbable(out grab))
                jointedParents.Add(grab.originalParent);
            else
                jointedParents.Add(body.transform.parent);

            if(transform.parent != originalParent) {
                if(grab != null) {
                    if(grab.HeldCount() == 0)
                        grab.transform.parent = transform.parent;
                    grab.heldBodyJointed = true;
                }
                else
                    grab.transform.parent = transform.parent;
            }
        }

        /// <summary>Remove a jointed rigidbody in the jointedBodies list</summary>
        public void RemoveJointedBody(Rigidbody body) {
            var i = jointedBodies.IndexOf(body);
            if(jointedBodies[i].gameObject.HasGrabbable(out var grab)) {
                if(grab.HeldCount() == 0)
                    grab.transform.parent = grab.originalParent;
                grab.heldBodyJointed = false;
            }
            else
                jointedBodies[i].transform.parent = jointedParents[i];

            jointedBodies.RemoveAt(i);
            jointedParents.RemoveAt(i);
        }

        public void DoDestroy() {
            Destroy(gameObject);
        }

        /// <summary>Returns the total collision count of all this grabbable</summary>
        public int CollisionCount() {
            return collisionTracker.collisionObjects.Count;
        }

        /// <summary>Returns the total collision count of all the "jointed grabbables"</summary>
        public int JointedCollisionCount() {
            int count = 0;
            for(int i = 0; i < jointedGrabbables.Count; i++)
                count += jointedGrabbables[i].HeldCount();

            return count;
        }

        //Adds a reference script to child colliders so they can be grabbed
        void MakeChildrenGrabbable() {
            for(int i = 0; i < transform.childCount; i++) {
                AddChildGrabbableRecursive(transform.GetChild(i));
            }

            void AddChildGrabbableRecursive(Transform obj) {
                if(obj.CanGetComponent(out Collider col) && col.isTrigger == false && !obj.CanGetComponent<Grabbable>(out _) && !obj.CanGetComponent<GrabbableChild>(out _) && !obj.CanGetComponent<PlacePoint>(out _)) {
                    var child = obj.gameObject.AddComponent<GrabbableChild>();
                    child.gameObject.layer = originalLayer;
                    child.grabParent = this;
                }
                for(int i = 0; i < obj.childCount; i++) {
                    if(!obj.CanGetComponent<Grabbable>(out _))
                        AddChildGrabbableRecursive(obj.GetChild(i));
                }
            }
        }


        //Adds a reference script to child colliders so they can be grabbed
        void MakeChildrenUngrabbable() {
            for(int i = 0; i < transform.childCount; i++) {
                RemoveChildGrabbableRecursive(transform.GetChild(i));
            }

            void RemoveChildGrabbableRecursive(Transform obj) {
                if(obj.GetComponent<GrabbableChild>() && obj.GetComponent<GrabbableChild>().grabParent == this) {
                    Destroy(obj.gameObject.GetComponent<GrabbableChild>());
                }
                for(int i = 0; i < obj.childCount; i++) {
                    RemoveChildGrabbableRecursive(obj.GetChild(i));
                }
            }
        }

        /// <summary>INTERNAL - Sets the grabbables original layers</summary>
        internal void ResetGrabbableAfterRlease() {
            if(!beingDestroyed) {
                ResetRigidbody();

                if (body != null && !heldBodyJointed && (placePoint == null || !(placePoint.placedObject == this && placePoint.parentOnPlace)))
                {
                    body.transform.parent = originalParent;
                }

                for(int i = 0; i < jointedBodies.Count; i++) {
                    if(jointedBodies[i].gameObject.HasGrabbable(out var grab)) {
                        if(grab.HeldCount() == 0)
                            grab.transform.parent = grab.originalParent;
                        grab.heldBodyJointed = false;
                    }
                    else if(!heldBodyJointed)
                        jointedBodies[i].transform.parent = jointedParents[i];
                }
            }
        }

        public bool IsHolding(Rigidbody body)
        {
            foreach (var holding in heldBy)
            {
                if (holding.body == body)
                    return true;
            }

            return false;
        }

        public bool IsHolding(Hand hand)
        {
            foreach (var held in heldBy)
            {
                if (held == hand)
                    return true;
            }

            return false;
        }
    }
}
