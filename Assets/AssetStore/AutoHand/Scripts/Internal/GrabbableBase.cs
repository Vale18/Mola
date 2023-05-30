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
    public struct SaveRigidbodyData
    {
        GameObject origin;
        float mass;
        float angularDrag;
        float drag;
        bool useGravity;
        bool isKinematic;
        RigidbodyInterpolation interpolation;
        CollisionDetectionMode collisionDetectionMode;
        RigidbodyConstraints constraints;
        
        public SaveRigidbodyData(Rigidbody from, bool removeBody = true)
        {
            origin = from.gameObject;
            mass = from.mass;
            drag = from.drag;
            angularDrag = from.angularDrag;
            useGravity = from.useGravity;
            isKinematic = from.isKinematic;
            interpolation = from.interpolation;
            collisionDetectionMode = from.collisionDetectionMode;
            constraints = from.constraints;
            if(removeBody)
                GameObject.Destroy(from);
        }

        public Rigidbody ReloadRigidbody(){
            if(origin != null){
                if (origin.CanGetComponent<Rigidbody>(out var currBody))
                    return currBody;
                var from = origin.AddComponent<Rigidbody>();
                from.mass = mass;
                from.drag = drag;
                from.angularDrag = angularDrag;
                from.useGravity = useGravity;
                from.isKinematic = isKinematic;
                from.interpolation = interpolation;
                from.collisionDetectionMode = collisionDetectionMode;
                from.constraints = constraints;
                origin = null;
                return from;
            }
            return null;
        }
    }

    [DefaultExecutionOrder(-5)]
    public class GrabbableBase : MonoBehaviour{

        [AutoHeader("Grabbable")]
        public bool ignoreMe;

        [Tooltip("The physics body to connect this colliders grab to - if left empty will default to local body")]
        public Rigidbody body;

        [Tooltip("A copy of the mesh will be created and slighly scaled and this material will be applied to create a highlight effect with options")]
        public Material hightlightMaterial;

        [HideInInspector]
        public bool isGrabbable = true;

        private PlacePoint _placePoint = null;
        public PlacePoint placePoint { get { return _placePoint; } protected set { _placePoint = value; } }

        internal bool ignoreParent = false;

        protected List<Hand> heldBy = new List<Hand>();
        protected bool hightlighting;
        protected GameObject highlightObj;
        protected PlacePoint lastPlacePoint = null;

        protected Transform originalParent;
        protected Vector3 lastCenterOfMassPos;
        protected Quaternion lastCenterOfMassRot;
        protected CollisionDetectionMode detectionMode;
        protected RigidbodyInterpolation startInterpolation;

        protected internal bool beingGrabbed = false;
        protected bool heldBodyJointed = false;
        protected bool wasIsGrabbable = false;
        protected bool beingDestroyed = false;
        protected int originalLayer;
        protected Coroutine resetLayerRoutine = null;
        protected List<GrabbableChild> grabChildren = new List<GrabbableChild>();
        protected List<Transform> jointedParents = new List<Transform>();
        protected GrabbablePoseCombiner poseCombiner;
        protected List<Grabbable> jointedGrabbables = new List<Grabbable>();
        protected float lastUpdateTime;

        protected bool rigidbodyDeactivated = false;
        protected SaveRigidbodyData rigidbodyData;

        private CollisionTracker _collisionTracker;
        public CollisionTracker collisionTracker {
            get {
                if(_collisionTracker == null) {
                    if(!(_collisionTracker = GetComponent<CollisionTracker>())) {
                        _collisionTracker = gameObject.AddComponent<CollisionTracker>();
                        _collisionTracker.disableTriggersTracking = true;
                    }
                }
                return _collisionTracker;
            }
            protected set {
                if(_collisionTracker != null)
                    Destroy(_collisionTracker);

                _collisionTracker = value;
            }
        }

#if UNITY_EDITOR
        bool editorSelected = false;
    #endif

        protected virtual void Awake() {
            //Delete these layer setters if you want to use your own custom layer set
            if(gameObject.layer == LayerMask.NameToLayer("Default") || LayerMask.LayerToName(gameObject.layer) == "")
                gameObject.layer = LayerMask.NameToLayer(Hand.grabbableLayerNameDefault);
            
            if(heldBy == null)
                heldBy = new List<Hand>();

            if(body == null){
                if(GetComponent<Rigidbody>())
                    body = GetComponent<Rigidbody>();
                else
                    Debug.LogError("RIGIDBODY MISSING FROM GRABBABLE: " + transform.name + " \nPlease add/attach a rigidbody", this);
            }


    #if UNITY_EDITOR
            if (Selection.activeGameObject == gameObject){
                Selection.activeGameObject = null;
                Debug.Log("Auto Hand: Selecting the grabbable can cause lag and quality reduction at runtime. (Automatically deselecting at runtime) Remove this code at any time.", this);
                editorSelected = true;
            }

            Application.quitting += () => { if (editorSelected) Selection.activeGameObject = gameObject; };
    #endif

            originalLayer = gameObject.layer;
            originalParent = body.transform.parent;
            detectionMode = body.collisionDetectionMode;
            startInterpolation = body.interpolation;
            SetCollidersRecursive(body.transform);
        }

        protected virtual void Start() {
            if(!gameObject.CanGetComponent(out poseCombiner))
                poseCombiner = gameObject.AddComponent<GrabbablePoseCombiner>();

            GetPoseSaves(transform);

            void GetPoseSaves(Transform obj) {
                //Stop if you get to another grabbable
                if(obj.CanGetComponent(out Grabbable grab) && grab != this)
                    return;

                var poses = obj.GetComponents<GrabbablePose>();
                for(int i = 0; i < poses.Length; i++)
                    poseCombiner.AddPose(poses[i]);

                for(int i = 0; i < obj.childCount; i++)
                    GetPoseSaves(obj.GetChild(i));
            }
        }

        protected virtual void FixedUpdate() {
            if(heldBy.Count > 0 && body != null) {
                lastCenterOfMassRot = body.transform.rotation;
                lastCenterOfMassPos = body.transform.position;
            }
        }


        protected virtual void OnDisable(){
            if (resetLayerRoutine != null){
                StopCoroutine(resetLayerRoutine);
                resetLayerRoutine = null;
            }
        }
        

        
        internal void SetPlacePoint(PlacePoint point) {
            this.placePoint = point;
        }

        internal void SetGrabbableChild(GrabbableChild child) {
            if(!grabChildren.Contains(child))
                grabChildren.Add(child);
        }
        

        public void DeactivateRigidbody()
        {
            if (body != null){
                if(body != null)
                    rigidbodyData = new SaveRigidbodyData(body);
                body = null;
                rigidbodyDeactivated = true;
            }
        }

        public void ActivateRigidbody()
        {
            if (rigidbodyDeactivated){
                rigidbodyDeactivated = false;
                body = rigidbodyData.ReloadRigidbody();
            }
        }


        protected int GetOriginalLayer(){
            return originalLayer;
        }

        
        internal void SetLayerRecursive(Transform obj, int oldLayer, int newLayer) {
            for(int i = 0; i < grabChildren.Count; i++) {
                if(grabChildren[i].gameObject.layer == oldLayer)
                    grabChildren[i].gameObject.layer = newLayer;
            }
            SetChildrenLayers(obj);

            void SetChildrenLayers(Transform obj1){
                if(obj1.gameObject.layer == oldLayer)
                    obj1.gameObject.layer = newLayer;
                for(int i = 0; i < obj1.childCount; i++) {
                    SetChildrenLayers(obj1.GetChild(i));
                }
            }
        }

        internal void SetLayerRecursive(Transform obj, int newLayer) {
            SetLayerRecursive(obj, obj.gameObject.layer, newLayer);
        }

        protected Hand ignoringHand = null;
        //Invoked a quatersecond after releasing
        protected IEnumerator IgnoreHandCollision(float time, Hand hand) {
            if(ignoringHand != null)
                IgnoreHand(ignoringHand, false);
            ignoringHand = hand;
            if (time > 0){
                IgnoreHand(hand, true);
                yield return new WaitForSeconds(time);
            }

            IgnoreHand(hand, false);
            ignoringHand = null;
            resetLayerRoutine = null;
        }

        public bool GetSavedPose(out GrabbablePoseCombiner pose) {
            if(poseCombiner != null && poseCombiner.PoseCount() > 0) {
                pose = poseCombiner;
                return true;
            }
            else {
                pose = null;
                return false;
            }
        }

        public bool HasCustomPose() {
            return poseCombiner.PoseCount() > 0;
        }


        public void IgnoreHand(Hand hand, bool ignore)
        {
            foreach (var col in grabColliders)
                hand.HandIgnoreCollider(col, ignore);
        }

        protected List<Collider> grabColliders = new List<Collider>();
        void SetCollidersRecursive(Transform obj){
            foreach (var col in obj.GetComponents<Collider>())
                grabColliders.Add(col);

            for (int i = 0; i < obj.childCount; i++)
                SetCollidersRecursive(obj.GetChild(i));
        }
        
        //Resets to original collision dection
        protected void ResetRigidbody() {
            if (body != null)
            {
                body.collisionDetectionMode = detectionMode;
                body.interpolation = startInterpolation;
            }
        }

        public bool BeingDestroyed() {
            return beingDestroyed;
        }

        public void DebugBreak() {
#if UNITY_EDITOR
            Debug.Break();
#endif
        }


    }
}