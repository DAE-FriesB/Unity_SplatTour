using UnityEngine;

public class SplatTourPresenter : MonoBehaviour
{
	[SerializeField]
	private TextAsset _transformsJsonAsset;
	[SerializeField]
	private TextAsset _dataParser_transformsAsset;

	[SerializeField]
	private float _scale = 0.1f;

	private TransformParser _transformParser = new TransformParser();

	private TransformData[] _transforms = null;

	private void OnValidate()
	{
		if (_transformsJsonAsset != null)
		{
			if (_transformsJsonAsset.name.EndsWith(".bin"))
			{
				_transforms = _transformParser.ParseBinary(_transformsJsonAsset);
			}
			else if(_dataParser_transformsAsset != null)
			{
				Matrix4x4 transformMatrix = _transformParser.ParseDataParser_Transforms(_dataParser_transformsAsset);
				_transforms = _transformParser.ParseTransforms(_transformsJsonAsset, transformMatrix);

				
			}
			foreach (var t in _transforms)
			{
				t.ParentTransform = this.transform;
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (_transforms == null) return;

		for (int idx = 0; idx < _transforms.Length; ++idx)
		{

			var currTransform = _transforms[idx];
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(currTransform.Position, _scale);
			Gizmos.color = Color.red;
			Gizmos.DrawLine(currTransform.Position, currTransform.Position + currTransform.Ray.direction * _scale);
		}

	}
}
