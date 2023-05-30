using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Autohand {
    public enum PlacePointNameType
    {
        name,
        tag
    }

    public delegate void PlacePointEvent(PlacePoint point, Grabbable grabbable);
    [Serializable]
    public class UnityPlacePointEvent : UnityEvent<PlacePoint, Grabbable> { }
    [HelpURL("https://earnestrobot.notion.site/Place-Points-e6361a414928450dbb53d76fd653cf9a")]
    //You can override this by turning the radius to zero, and using any other trigger collider
    public class PlacePoint : MonoBehaviour{
        [AutoHeader("Place Point")]
        public bool ignoreMe;


        [AutoSmallHeader("Place Settings")]
        public bool showPlaceSettings = true;
        [Tooltip("Snaps an object to the point at start, leave empty for no target")]
        public Grabbable startPlaced;
        [Tooltip("This will make the point place the object as soon as it enters the radius, instead of on release")]
        public Transform placedOffset;
        [Tooltip("The radius of the place point (relative to scale)")]
        public float placeRadius = 0.1f;
        [Space]
        [Tooltip("This will make the point place the object as soon as it enters the radius, instead of on release")]
        public bool parentOnPlace = true;
        [Tooltip("This will make the point place the object as soon as it enters the radius, instead of on release")]
        public bool forcePlace = false;
        [Tooltip("If true and will force hand to release on place when force place is called. If false the hand will attempt to keep the connection to the held object (but can still break due to max distances/break forces)")]
        public bool forceHandRelease = true;
        [Space]
        [Tooltip("Whether or not the placed object should be disabled on placement (this will hide the placed object and leave the place point active for a new object)")]
        public bool destroyObjectOnPlace = false;
        [Tooltip("Whether or not the placed object should have its rigidbody disabled on place, good for parenting placed objects under dynamic objects")]
        public bool disableRigidbodyOnPlace = false;
        [Tooltip("Whether or not the grabbable should be disabled on place")]
        public bool disableGrabOnPlace = false;
        [Tooltip("Whether or not this place point should be disabled on placement. It will maintain its connection and can no longer accept new items. Causes less overhead if true")]
        public bool disablePlacePointOnPlace = false;
        [Space]

        [Tooltip("If true and will force release on place")]
        [DisableIf("disableRigidbodyOnPlace")]
        public bool makePlacedKinematic = true;
        
        [DisableIf("disableRigidbodyOnPlace")]
        [Tooltip("The rigidbody to attach the placed grabbable to - leave empty means no joint")]
        public Rigidbody placedJointLink;
        [DisableIf("disableRigidbodyOnPlace")]
        public float jointBreakForce = 1000;


        [AutoSmallHeader("Place Requirements")]
        public bool showPlaceRequirements = true;


        [Tooltip("Whether the placeNames should compare names or tags")]
        public PlacePointNameType nameCompareType;
        [Tooltip("Will allow placement for any grabbable with a name containing this array of strings, leave blank for any grabbable allowed")]
        public string[] placeNames;
        [Tooltip("Will prevent placement for any name containing this array of strings")]
        public string[] blacklistNames;

        [Tooltip("(Unless empty) Will only allow placement any object contained here")]
        public List<Grabbable> onlyAllows;
        [Tooltip("Will NOT allow placement any object contained here")]
        public List<Grabbable> dontAllows;

        [Tooltip("The layer that this place point will check for placeable objects, if none will default to Grabbable")]
        public LayerMask placeLayers;

        [Tooltip("Whether or not to only allow placement of an object while it's being held (or released)")]
        public bool heldPlaceOnly = false;
        [Space]

        [AutoToggleHeader("Show Events")]
        public bool showEvents = true;
        [ShowIf("showEvents")]
        public UnityPlacePointEvent OnPlace;
        [ShowIf("showEvents")]
        public UnityPlacePointEvent OnRemove;
        [ShowIf("showEvents")]
        public UnityPlacePointEvent OnHighlight;
        [ShowIf("showEvents")]
        public UnityPlacePointEvent OnStopHighlight;
        
        //For the programmers
        public PlacePointEvent OnPlaceEvent;
        public PlacePointEvent OnRemoveEvent;
        public PlacePointEvent OnHighlightEvent;
        public PlacePointEvent OnStopHighlightEvent;

        public Grabbable highlightingObj { get; protected set; } = null;
        public Grabbable placedObject { get; protected set; } = null;
        public Grabbable lastPlacedObject { get; protected set; } = null;


        [HideInInspector] //Legacy Value
        public Vector3 radiusOffset;

        protected FixedJoint joint = null;

        //How far the placed object has to be moved to count to auto remove from point so something else can take its place
        protected float removalDistance = 0.05f;
        protected float lastPlacedTime;
        protected Vector3 placePosition;
        protected Transform originParent;
        protected bool placingFrame;
        protected CollisionDetectionMode placedObjDetectionMode;
        float tickRate = 0.02f;
        Collider[] collidersNonAlloc = new Collider[30];


        protected virtual void Start(){
            if (placedOffset == null)
                placedOffset = transform;

            if(placeLayers == 0)
                placeLayers = LayerMask.GetMask(Hand.grabbableLayerNameDefault);

            SetStartPlaced();
        }

        Coroutine checkRoutine;
        protected virtual void OnEnable() {
            checkRoutine = StartCoroutine(CheckPlaceObjectLoop());
        }

        protected virtual void OnDisable() {
            StopCoroutine(checkRoutine);   
        }

        public virtual bool CanPlace(Grabbable placeObj) {


            if(placedObject != null)
                return false;

            if (heldPlaceOnly && placeObj.HeldCount() == 0)
                return false;

            if (onlyAllows.Count > 0 && !onlyAllows.Contains(placeObj))
                return false;

            if (dontAllows.Count > 0 && dontAllows.Contains(placeObj))
                return false;

            if (placeNames.Length == 0 && blacklistNames.Length == 0)
                return true;

            if (blacklistNames.Length > 0)
                foreach(var badName in blacklistNames)
                {
                    if (nameCompareType == PlacePointNameType.name && placeObj.name.Contains(badName))
                        return false;
                    if (nameCompareType == PlacePointNameType.tag && placeObj.CompareTag(badName))
                        return false;
                }

            if (placeNames.Length > 0)
                foreach (var placeName in placeNames)
                {
                    if (placeObj.name.Contains(placeName))
                    {
                        if (nameCompareType == PlacePointNameType.name && placeObj.name.Contains(placeName))
                            return true;
                        if (nameCompareType == PlacePointNameType.tag && placeObj.CompareTag(placeName))
                            return true;
                    }
                }
            else
                return true;

            return false;
        }

        int lastOverlapCount = 0;
        Grabbable tempGrabbable;
        protected virtual IEnumerator CheckPlaceObjectLoop() {
            var scale = Mathf.Abs(transform.lossyScale.x < transform.lossyScale.y ? transform.lossyScale.x : transform.lossyScale.y);
            scale = Mathf.Abs(scale < transform.lossyScale.z ? scale : transform.lossyScale.z);

            CheckPlaceObject(placeRadius, scale);

            yield return new WaitForSeconds(UnityEngine.Random.Range(0f, tickRate));
            while(gameObject.activeInHierarchy) {
                    CheckPlaceObject(placeRadius, scale);

                yield return new WaitForSeconds(tickRate);
            }
        }

        void CheckPlaceObject(float radius, float scale) {
            if(!disablePlacePointOnPlace && !disableRigidbodyOnPlace && placedObject != null && !IsStillOverlapping(placedObject, scale))
                Remove(placedObject);
            
            if(placedObject == null && highlightingObj == null) {
                var overlaps = Physics.OverlapSphereNonAlloc(placedOffset.position + transform.rotation * radiusOffset, radius * scale, collidersNonAlloc, placeLayers);
                if(overlaps != lastOverlapCount) {
                    for(int i = 0; i < overlaps; i++) {
                        if(AutoHandExtensions.HasGrabbable(collidersNonAlloc[i].gameObject, out tempGrabbable) && CanPlace(tempGrabbable)) {
                            Highlight(tempGrabbable);
                        }
                    }
                }
                lastOverlapCount = overlaps;
            }
            else if(highlightingObj != null) {
                if(!IsStillOverlapping(highlightingObj, scale)) {
                    StopHighlight(highlightingObj);
                }
            }
        }

        public virtual void TryPlace(Grabbable placeObj) {
            if(CanPlace(placeObj))
                Place(placeObj);
        }

        public virtual void Place(Grabbable placeObj) {
            if (placedObject != null)
                return;

            if(placeObj.placePoint != null && placeObj.placePoint != this)
                placeObj.placePoint.Remove(placeObj);

            placedObject = placeObj;
            placedObject.SetPlacePoint(this);

            if ((forceHandRelease || disableRigidbodyOnPlace) && placeObj.HeldCount() > 0)
                placeObj.ForceHandsRelease();

            placingFrame = true;
            originParent = placeObj.transform.parent;

            placeObj.transform.position = placedOffset.position;
            placeObj.transform.rotation = placedOffset.rotation;

            if (placeObj.body != null)
            {
                placeObj.body.position = placeObj.transform.position;
                placeObj.body.rotation = placeObj.transform.rotation;
                placeObj.body.velocity = Vector3.zero;
                placeObj.body.angularVelocity = Vector3.zero;
                placedObjDetectionMode = placeObj.body.collisionDetectionMode;

                if (makePlacedKinematic)
                {
                    placeObj.body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    placeObj.body.isKinematic = makePlacedKinematic;
                }
            
                if (placedJointLink != null){
                    joint = placedJointLink.gameObject.AddComponent<FixedJoint>();
                    joint.connectedBody = placeObj.body;
                    joint.breakForce = jointBreakForce;
                    joint.breakTorque = jointBreakForce;
                
                    joint.connectedMassScale = 1;
                    joint.massScale = 1;
                    joint.enableCollision = false;
                    joint.enablePreprocessing = false;
                }
            }


            placeObj.OnGrabEvent += OnPlacedObjectGrabbed;
            placeObj.OnReleaseEvent += OnPlacedObjectReleased;

            StopHighlight(placeObj);

            placePosition = placedObject.transform.position;
            placeObj.OnPlacePointAddEvent?.Invoke(this, placeObj);
            OnPlaceEvent?.Invoke(this, placeObj);
            OnPlace?.Invoke(this, placeObj);
            lastPlacedTime = Time.time;

            if (parentOnPlace)
                placedObject.body.transform.parent = transform;

            if (disableRigidbodyOnPlace)
                placeObj.DeactivateRigidbody();


            if (disablePlacePointOnPlace)
                enabled = false;

            if (disableGrabOnPlace || disablePlacePointOnPlace)
                placeObj.isGrabbable = false;

            if (destroyObjectOnPlace)
                Destroy(placedObject);
        }

        public void Remove() {
            if(placedObject != null)
                Remove(placedObject);
        }

        public virtual void Remove(Grabbable placeObj) {
            if (placeObj == null || placeObj != placedObject || disablePlacePointOnPlace)
                return;

            if (placeObj.body != null){
                if (makePlacedKinematic)
                    placeObj.body.isKinematic = false;

                placeObj.body.collisionDetectionMode = placedObjDetectionMode;
            }

            placeObj.OnGrabEvent -= OnPlacedObjectGrabbed;
            placeObj.OnReleaseEvent -= OnPlacedObjectReleased;

            if (!(placeObj.parentOnGrab && (placeObj.HeldCount() > 0 || placeObj.beingGrabbed)) && parentOnPlace && !placeObj.BeingDestroyed())
                placeObj.transform.parent = originParent;

            placedObject.OnPlacePointRemoveEvent?.Invoke(this, highlightingObj);
            OnRemoveEvent?.Invoke(this, placeObj);
            OnRemove?.Invoke(this, placeObj);

            Highlight(placeObj);

            if (disableRigidbodyOnPlace)
                placeObj.ActivateRigidbody();

            
            lastPlacedObject = placedObject;
            placedObject = null;

            if(joint != null){
                Destroy(joint);
                joint = null;
            }
        }


        internal virtual void Highlight(Grabbable from) {
            if(highlightingObj == null){
                from.SetPlacePoint(this);

                highlightingObj = from;
                highlightingObj.OnPlacePointHighlightEvent?.Invoke(this, highlightingObj);
                OnHighlightEvent?.Invoke(this, from);
                OnHighlight?.Invoke(this, from);

                if(placedObject == null && forcePlace)
                    Place(from);
            }
        }

        internal virtual void StopHighlight(Grabbable from) {
            if(highlightingObj != null) {
                highlightingObj.OnPlacePointUnhighlightEvent?.Invoke(this, highlightingObj);
                highlightingObj = null;
                OnStopHighlightEvent?.Invoke(this, from);
                OnStopHighlight?.Invoke(this, from);
                if (placedObject == null)
                    from.SetPlacePoint(null);
            }
        }



        protected bool IsStillOverlapping(Grabbable from, float scale = 1){
            var sphereCheck = Physics.OverlapSphereNonAlloc(placedOffset.position + placedOffset.rotation * radiusOffset, placeRadius * scale, collidersNonAlloc, placeLayers);
            for (int i = 0; i < sphereCheck; i++){
                if (collidersNonAlloc[i].attachedRigidbody == from.body) {
                    return true;
                }
            }
            return false;
        }



        public virtual void SetStartPlaced() {
            if(startPlaced != null){
                startPlaced.SetPlacePoint(this);
                Highlight(startPlaced);
                Place(startPlaced);
            }
        }
        
        public Grabbable GetPlacedObject() {
            return placedObject;
        }

        protected virtual void OnPlacedObjectGrabbed(Hand pHand, Grabbable pGrabbable)
        {
            // Unset kinematic status when the placed object is grabbed.
            if (makePlacedKinematic)
                pGrabbable.body.isKinematic = false;
        }

        protected virtual void OnPlacedObjectReleased(Hand pHand, Grabbable pGrabbable)
        {
            // Re-Place() grabbable when placed object is released before this has been unsubscribed to. (Before the object has left the bounds of the place points.)
            if (makePlacedKinematic)
                Place(pGrabbable);
        }

        protected virtual void OnJointBreak(float breakForce) {
            if(placedObject != null)
                Remove(placedObject);
        }

        void OnDrawGizmos() {
            if(placedOffset == null)
                placedOffset = transform;
            Gizmos.color = Color.white; 
            var scale = Mathf.Abs(transform.lossyScale.x < transform.lossyScale.y ? transform.lossyScale.x : transform.lossyScale.y);
            scale = Mathf.Abs(scale < transform.lossyScale.z ? scale : transform.lossyScale.z);

            Gizmos.DrawWireSphere(transform.rotation * radiusOffset + placedOffset.position, placeRadius* scale);
        }

    }
}
