using UnityEngine;
using UnityEngine.AI;

public class VirtualTourRaycaster
{
	private readonly Camera _camera;
	private readonly IndicatorModel _indicator;
	private readonly LayerMask _wallsAndFloorMask;

	public VirtualTourRaycaster(Camera camera, IndicatorModel indicator, LayerMask wallsAndFloorMask)
	{
		_camera = camera;
		_indicator = indicator;
		_wallsAndFloorMask = wallsAndFloorMask;
		_indicator.Mode = IndicatorModel.DecalMode.Hidden;

	}
	public void Update()
	{
		Vector3 mousePos = Input.mousePosition;
		mousePos.z = 10f;
		Ray r = _camera.ScreenPointToRay(mousePos);
		if (Physics.Raycast(r, out RaycastHit hit, _camera.farClipPlane, _wallsAndFloorMask))
		{
			if (hit.collider.CompareTag("Floor"))
			{
				if (NavMesh.SamplePosition(hit.point, out NavMeshHit navhit, 0.3f, NavMesh.AllAreas))
				{
					_indicator.Position = navhit.position;
					_indicator.Mode = IndicatorModel.DecalMode.Available;

				}
				else
				{
					_indicator.Position = hit.point;
					_indicator.Mode = IndicatorModel.DecalMode.Unavailable;

				}
			}
			else
			{
				//Debug.Log(hit.collider.gameObject.name);
				_indicator.Mode = IndicatorModel.DecalMode.Hidden;
			}

		}
		else
		{
			_indicator.Mode = IndicatorModel.DecalMode.Hidden;
		}
	}
}
