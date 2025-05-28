using Dependencies;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Timing;
using UnityEngine;

public class AutoPlaySystem : MonoBehaviour
{
		
	public event EventHandler FinishedPlaying;

	[SerializeField]
	private VirtualTourInput _tourInput;

	[SerializeField]
	private float _maxMoveSpeed = 10f;
	[SerializeField]
	private float _maxRotationSpeed = 180f;

	[SerializeField]
	private float _startMoveDelay = 3f;

	[SerializeField]
	private SplatLoader _splatLoader;

	private bool _playing = false;
	private float _timer = 0f;

	Queue<AutoPlayCheckpoint> _checkpointsQueue = new Queue<AutoPlayCheckpoint>();

	private AutoPlayRaycaster _raycaster;
	private AutoPlayRotationInput _rotationInput;
	
	private ITimeService _timeService;

	private bool _animating = false;

	public bool ShouldAutoPlay { get; set; }


	private void Start()
	{
		_timeService = DependencyService.GetService<ITimeService>();
		if (ShouldAutoPlay)
		{
			StartAutoPlay();
		}
		else
		{
			gameObject.SetActive(false);
		}
	}	

	private void StartAutoPlay()
	{

		//fetch checkpoints
		foreach (AutoPlayCheckpoint checkpoint in GetComponentsInChildren<AutoPlayCheckpoint>())
		{
			_checkpointsQueue.Enqueue(checkpoint);

		}


		//inject input		
		_tourInput._rayCaster = this._raycaster = new AutoPlayRaycaster(_tourInput.Camera, _tourInput.IndicatorModel, _tourInput.LayerMask);
		_tourInput._cameraRotationInput = this._rotationInput = new AutoPlayRotationInput(_tourInput.Camera);
		_playing = true;
		this.gameObject.SetActive(true);

		_timer = 0f;
	}
	private void OnDrawGizmos()
	{
		if (_raycaster != null)
			Gizmos.DrawWireSphere(_raycaster.MouseTarget, 1f);
	}

	private void OnDrawGizmosSelected()
	{
		var checkpoints = GetComponentsInChildren<AutoPlayCheckpoint>();
		for (int idx = 1; idx < checkpoints.Length; ++idx)
		{
			Gizmos.DrawLine(checkpoints[idx - 1].transform.position, checkpoints[idx].transform.position);
		}
	}
	private void Update()
	{
		if (!_playing) return;
		if (!_checkpointsQueue.Any())
		{
			return;
		}
		if (!_splatLoader.IsLoaded) return;
		if (!_animating)
		{
			Vector3 targetPos = _checkpointsQueue.Dequeue().transform.position;
			StartCoroutine(AnimateTowardsCheckpoint(targetPos));
			Vector3 toTarget = targetPos - _cameraPos;
			toTarget.y *= 0.5f;
			_rotationInput.LookatTarget = (toTarget) * 10 + _cameraPos;
			_timer = 0f;
		}

		//Kill switch
		if (_animating && Input.GetKeyDown(KeyCode.E))
		{
			StopAllCoroutines();
			_animating = false;
			_checkpointsQueue.Clear();
			OnFinishedPlaying();
		}
		_rotationInput.MaxRotSpeed = _maxRotationSpeed;

	}

	IEnumerator AnimateTowardsCheckpoint(Vector3 checkpoint)
	{

		_animating = true;

		float distSq = (checkpoint - _raycaster.MouseTarget).sqrMagnitude;
		while (distSq > 0.01f)
		{
			_raycaster.MouseTarget = Vector3.MoveTowards(_raycaster.MouseTarget, checkpoint, _timeService.DeltaTime * _maxMoveSpeed);
			distSq = (checkpoint - _raycaster.MouseTarget).sqrMagnitude;
#if UNITY_EDITOR
			Debug.DrawLine(_cameraPos, _raycaster.MouseTarget, Color.blue);
#endif
			yield return null;
		}
		_raycaster.MouseTarget = checkpoint;

		while (!_tourInput.StartMove())
		{
			yield return null;
		}

		while (_tourInput.IsMoving)
		{
			yield return null;
		}


		_animating = false;
		if (_checkpointsQueue.Count == 0)
		{
			yield return new WaitForSeconds(3f);
			OnFinishedPlaying();
		}
	}



	protected virtual void OnFinishedPlaying()
	{
		FinishedPlaying?.Invoke(this, EventArgs.Empty);
	}

	private Vector3 _cameraPos => _tourInput.Camera.transform.position;
}
