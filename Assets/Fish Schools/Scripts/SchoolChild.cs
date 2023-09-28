/**************************************									
	Copyright 2015 Unluck Software	
 	www.chemicalbliss.com								
***************************************/

using UnityEngine;


public class SchoolChild:MonoBehaviour{
	[HideInInspector]
	public SchoolController _spawner;
	Vector3 _wayPoint;
	[HideInInspector]
	public float _speed= 10.0f;				//Fish Speed
	float _stuckCounter;			//prevents looping around a waypoint
	float _damping;					//Turn speed
	Transform _model;				//Model with animations
	float _targetSpeed;				//Fish target speed
	float tParam = 0.0f;				//
	float _rotateCounterR;			//Used to increase avoidance speed over time
	float _rotateCounterL;			
	public Transform _scanner;				//Scanner object used for push, this rotates to check for collisions
	bool _scan = true;			
	bool _instantiated;			//Has this been instantiated
	static int _updateNextSeed = 0;	//When using frameskip seed will prevent calculations for all fish to be on the same frame
	int _updateSeed = -1;
	[HideInInspector]
	public Transform _cacheTransform;
	
	#if UNITY_EDITOR
	public static bool _sWarning;
	#endif
	
	public void Start(){
		//Check if there is a controller attached
		if(_cacheTransform == null) _cacheTransform = transform;
		if(_spawner != null){	
			SetRandomScale();			
			LocateRequiredChildren();
		    RandomizeStartAnimationFrame();
		    SkewModelForLessUniformedMovement();
			_speed = Random.Range(_spawner._minSpeed, _spawner._maxSpeed);
			Wander(0.0f);
			SetRandomWaypoint();
			CheckForBubblesThenInvoke();	
			_instantiated = true;
			GetStartPos();
			FrameSkipSeedInit();
			_spawner._activeChildren++;
			return;
		}
		
		this.enabled = false;
		Debug.Log(gameObject + " found no school to swim in: " + this + " disabled... Standalone fish not supported, please use the SchoolController"); 
	}
	
	public void Update() {
		//Skip frames
		if (_spawner._updateDivisor <=1 || _spawner._updateCounter == _updateSeed){
			CheckForDistanceToWaypoint();
		   	RotationBasedOnWaypointOrAvoidance();
		    ForwardMovement();
			RayCastToPushAwayFromObstacles();
			SetAnimationSpeed();
		}
	}
	
	public void FrameSkipSeedInit(){
		if(_spawner._updateDivisor > 1){
			int _updateSeedCap = _spawner._updateDivisor -1;
			_updateNextSeed++;
		    this._updateSeed = _updateNextSeed;
		    _updateNextSeed = _updateNextSeed % _updateSeedCap;
		}
	}
	
	public void CheckForBubblesThenInvoke() {
		if(_spawner._bubbles != null)
			InvokeRepeating("EmitBubbles", (_spawner._bubbles._emitEverySecond*Random.value)+1 , _spawner._bubbles._emitEverySecond);	
	}
	
	public void EmitBubbles(){
		_spawner._bubbles.EmitBubbles(_cacheTransform.position, _speed);
	}
	
	public void OnDisable() {
		CancelInvoke();
		_spawner._activeChildren--;
	}
	
	public void OnEnable() {
		if(_instantiated){
			CheckForBubblesThenInvoke();
			_spawner._activeChildren++;
		}
	}
	
	public void LocateRequiredChildren(){
		if(_model == null) _model = _cacheTransform.Find("Model");
		if(_scanner == null){
			_scanner = new GameObject().transform;
			_scanner.parent = this.transform;
			_scanner.localRotation = Quaternion.identity;
			_scanner.localPosition = Vector3.zero;
			#if UNITY_EDITOR
			if(!_sWarning){
				Debug.Log("No scanner assigned: creating... (Increase instantiate performance by manually creating a scanner object)");
				_sWarning = true;
			}
			#endif
		}
	}
	
	public void SkewModelForLessUniformedMovement() {
		// Adds a slight rotation to the model so that the fish get a little less uniformed movement	
		Quaternion rx = Quaternion.identity;
		rx.eulerAngles = new Vector3(0.0f, 0.0f , (float)Random.Range(-25, 25));
		_model.	rotation =	rx;
	}
	
	public void SetRandomScale(){
		float sc = Random.Range(_spawner._minScale, _spawner._maxScale);
		_cacheTransform.localScale=Vector3.one*sc;
	}
	
	public void RandomizeStartAnimationFrame(){
		foreach(AnimationState state in _model.GetComponent<Animation>()) {
		 	state.time = Random.value * state.length;
		}
	}
	
	public void GetStartPos(){
		//-Vector is to avoid zero rotation warning
		_cacheTransform.position = _wayPoint - new Vector3(.1f,.1f,.1f);
	}
	
	public Vector3 findWaypoint(){
		Vector3 t = Vector3.zero;
		t.x = Random.Range(-_spawner._spawnSphere, _spawner._spawnSphere) + _spawner._posBuffer.x;
		t.z = Random.Range(-_spawner._spawnSphereDepth, _spawner._spawnSphereDepth) + _spawner._posBuffer.z;
		t.y = Random.Range(-_spawner._spawnSphereHeight, _spawner._spawnSphereHeight) + _spawner._posBuffer.y;
		return t;
	}
	
	//Uses scanner to push away from obstacles
	public void RayCastToPushAwayFromObstacles() {
		if(_spawner._push){
			RotateScanner();
			RayCastToPushAwayFromObstaclesCheckForCollision();
		}
	}
	
	public void RayCastToPushAwayFromObstaclesCheckForCollision() {
		RaycastHit hit = new RaycastHit();
		float d = 0.0f;
		Vector3 cacheForward = _scanner.forward;
		if (Physics.Raycast(_cacheTransform.position, cacheForward, out hit, _spawner._pushDistance, _spawner._avoidanceMask)){		
			SchoolChild s = null;
			s = hit.transform.GetComponent<SchoolChild>();	
			d = (_spawner._pushDistance - hit.distance)/_spawner._pushDistance;	// Equals zero to one. One is close, zero is far	
			if(s != null){
				_cacheTransform.position -= cacheForward*_spawner._newDelta*d*_spawner._pushForce;	
			}
			else{
				_speed -= .01f*_spawner._newDelta;
				if(_speed < .1f)
				_speed = .1f;
				_cacheTransform.position -= cacheForward*_spawner._newDelta*d*_spawner._pushForce*2;
				//Tell scanner to rotate slowly
				_scan = false;
			}					
		}else{
			//Tell scanner to rotate randomly
			_scan = true;
		}
	}
	
	public void RotateScanner() {
		//Scan random if not pushing
		if(_scan){
			_scanner.rotation = Random.rotation;
			return;
		}
		//Scan slow if pushing
		_scanner.Rotate(new Vector3(150*_spawner._newDelta,0.0f,0.0f));
	}
	
	public bool Avoidance() {
		//Avoidance () - Returns true if there is an obstacle in the way
		if(!_spawner._avoidance)
			return false;		
		RaycastHit hit = new RaycastHit();
		float d = 0.0f;
		Quaternion rx = _cacheTransform.rotation;
		Vector3 ex = _cacheTransform.rotation.eulerAngles;
		Vector3 cacheForward = _cacheTransform.forward;
		Vector3 cacheRight = _cacheTransform.right;
		//Up / Down avoidance
		if (Physics.Raycast(_cacheTransform.position, -Vector3.up+(cacheForward*.1f), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){			
			//Debug.DrawLine(_cacheTransform.position,hit.point);
			d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;
			ex.x -= _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);
			rx.eulerAngles = ex;
			_cacheTransform.rotation = rx;
		}
		if (Physics.Raycast(_cacheTransform.position, Vector3.up+(cacheForward*.1f), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){
			//Debug.DrawLine(_cacheTransform.position,hit.point);
			d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;			
			ex.x += _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);	
			rx.eulerAngles = ex;
			_cacheTransform.rotation = rx;	
		}
		
		//Crash avoidance //Checks for obstacles forward
		if (Physics.Raycast(_cacheTransform.position, cacheForward+(cacheRight*Random.Range(-.1f, .1f)), out hit, _spawner._stopDistance, _spawner._avoidanceMask)){		
	//					Debug.DrawLine(_cacheTransform.position,hit.point);
					d = (_spawner._stopDistance - hit.distance)/_spawner._stopDistance;				
					ex.y -= _spawner._avoidSpeed*d*_spawner._newDelta*(_targetSpeed +3);
					rx.eulerAngles = ex;
					_cacheTransform.rotation = rx;
					_speed -= d*_spawner._newDelta*_spawner._stopSpeedMultiplier*_speed;				
					if(_speed < 0.01f){
						_speed = 0.01f;	
					}
					return true;
		}else if (Physics.Raycast(_cacheTransform.position, cacheForward+(cacheRight*(_spawner._avoidAngle+_rotateCounterL)), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){
	//				Debug.DrawLine(_cacheTransform.position,hit.point);
					d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;				
					_rotateCounterL+=.1f;
					ex.y -= _spawner._avoidSpeed*d*_spawner._newDelta*_rotateCounterL*(_speed +1);
					rx.eulerAngles = ex;
					_cacheTransform.rotation = rx;				
					if(_rotateCounterL > 1.5f)
						_rotateCounterL = 1.5f;				
					_rotateCounterR = 0.0f;
					return true;		
		}else if (Physics.Raycast(_cacheTransform.position, cacheForward+(cacheRight*-(_spawner._avoidAngle+_rotateCounterR)), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){
	//			Debug.DrawLine(_cacheTransform.position,hit.point);
					d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;
					if(hit.point.y < _cacheTransform.position.y){
						ex.y -= _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);
					}
					else{
						ex.x += _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);
					}
					_rotateCounterR +=.1f;
					ex.y += _spawner._avoidSpeed*d*_spawner._newDelta*_rotateCounterR*(_speed +1);
					rx.eulerAngles = ex;
					_cacheTransform.rotation = rx;	
					if(_rotateCounterR > 1.5f)
						_rotateCounterR = 1.5f;	
					_rotateCounterL = 0.0f;
					return true;
		}else{
			_rotateCounterL = 0.0f;
			_rotateCounterR = 0.0f;
		}
		return false;																	    																																				    																				
	}
	
	public void ForwardMovement(){
		_cacheTransform.position += _cacheTransform.TransformDirection(Vector3.forward)*_speed*_spawner._newDelta;
		if (tParam < 1) {
			if(_speed > _targetSpeed){
				tParam += _spawner._newDelta * _spawner._acceleration;
			}else{
				tParam += _spawner._newDelta * _spawner._brake;		
			}
			_speed = Mathf.Lerp(_speed, _targetSpeed,tParam);	
		}
	}
	
	public void RotationBasedOnWaypointOrAvoidance(){
		Quaternion rotation = Quaternion.identity;
	    rotation = Quaternion.LookRotation(_wayPoint - _cacheTransform.position);
	    if(!Avoidance()){
			_cacheTransform.rotation = Quaternion.Slerp(_cacheTransform.rotation, rotation, _spawner._newDelta * _damping);
		}
		//Limit rotation up and down to avoid freaky behavior
		float angle = _cacheTransform.localEulerAngles.x;
	    angle = (angle > 180) ? angle - 360 : angle;
		Quaternion rx = _cacheTransform.rotation;
	    Vector3 rxea = rx.eulerAngles;
	    rxea.x = ClampAngle(angle, -50.0f , 50.0f);
	    rx.eulerAngles = rxea;
		_cacheTransform.rotation = rx;
	}
	
	public void CheckForDistanceToWaypoint(){
		if((_cacheTransform.position - _wayPoint).magnitude < _spawner._waypointDistance+_stuckCounter){
	      	Wander(0.0f);	//create a new waypoint
	        _stuckCounter=0.0f;
	        CheckIfThisShouldTriggerNewFlockWaypoint();
	        return;
	    }
	    _stuckCounter+=_spawner._newDelta*(_spawner._waypointDistance*.25f);
	}
	
	public void CheckIfThisShouldTriggerNewFlockWaypoint(){
		if(_spawner._childTriggerPos){
			_spawner.SetRandomWaypointPosition();
		}
	}
	
	public static float ClampAngle(float angle,float min,float max) {
		if (angle < -360)angle += 360.0f;
		if (angle > 360)angle -= 360.0f;
		return Mathf.Clamp (angle, min, max);
	}
	
	public void SetAnimationSpeed(){
		foreach(AnimationState state in _model.GetComponent<Animation>()) {
	    	state.speed = (Random.Range(_spawner._minAnimationSpeed, _spawner._maxAnimationSpeed)*_spawner._schoolSpeed*this._speed)+.1f;   		   
		}
	}
	
	public void Wander(float delay){
		_damping = Random.Range(_spawner._minDamping, _spawner._maxDamping);
	    _targetSpeed = Random.Range(_spawner._minSpeed, _spawner._maxSpeed)*_spawner._speedCurveMultiplier.Evaluate(Random.value)*_spawner._schoolSpeed;
		Invoke("SetRandomWaypoint", delay);
	}
	
	public void SetRandomWaypoint(){
		tParam = 0.0f;
		_wayPoint = findWaypoint();
	}
}
