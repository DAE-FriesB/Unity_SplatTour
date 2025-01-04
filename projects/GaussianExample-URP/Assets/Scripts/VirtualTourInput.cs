using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class VirtualTourInput : MonoBehaviour
{
	[SerializeField]
	private float _cameraHeight = 1.6f;

	[SerializeField]
	private float _maxMoveSpeed = 1f;

	[SerializeField]
	private float _moveAcceleration = 10f;

	[SerializeField]
	private Indicator _indicator;

	[SerializeField]
	private LayerMask _wallsAndFloorLayers;

	private IndicatorModel _indicatorModel;
	private Camera _camera;
	private Coroutine _moveCoroutine = null;
	private float _speed = 0f;

	VirtualTourRaycaster _rayCaster;
	CameraRotationInput _cameraRotationInput;

	private void Awake()
	{
		_camera = GetComponent<Camera>();
		_cameraRotationInput = new CameraRotationInput(_camera);
	}
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		_indicatorModel = _indicator.Model;
		transform.position = new Vector3(transform.position.x, _cameraHeight, transform.position.z);
		_rayCaster = new VirtualTourRaycaster(_camera, _indicatorModel, _wallsAndFloorLayers);
	}

	// Update is called once per frame
	void Update()
	{
		_cameraRotationInput.Update();
		if (_moveCoroutine != null)
		{
			_indicatorModel.Mode = IndicatorModel.DecalMode.Hidden;
			return;
		}
		
		_rayCaster.Update();

		if (_indicatorModel.Mode == IndicatorModel.DecalMode.Available && Input.GetMouseButtonDown(0))
		{
			_moveCoroutine = StartCoroutine(AnimateMove(_indicatorModel.Position));
		}
	}

	IEnumerator AnimateMove(Vector3 targetPosition)
	{
		Vector3 targetPos = targetPosition + Vector3.up * _cameraHeight;
		
		_speed = 0f;

		while (!ReachedTarget(targetPos, out float distSq))
		{
			float stopTime = _speed / _moveAcceleration;
			float stopDist = _speed * stopTime - 0.5f * _moveAcceleration * stopTime * stopTime;

			if (stopDist * stopDist < distSq)
			{
				_speed += Time.deltaTime * _moveAcceleration;
			}
			else
			{
				_speed -= Time.deltaTime * _moveAcceleration;
				
			}
			_speed = Mathf.Clamp(_speed, 0f, _maxMoveSpeed);

			transform.position = Vector3.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);


			yield return null;
		}
		transform.position = targetPos;
		_moveCoroutine = null;
	}
	private bool ReachedTarget(Vector3 targetPos, out float distSq)
	{
		distSq = (transform.position - targetPos).sqrMagnitude;
		return distSq <= 0.0001f;
	}
}
