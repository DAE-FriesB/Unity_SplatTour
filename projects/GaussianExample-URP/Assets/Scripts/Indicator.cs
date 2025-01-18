using System.Collections;
using UnityEngine;

public class IndicatorModel : ModelBase
{
	public enum DecalMode
	{
		Available,
		Unavailable,
		Hidden
	}


	private DecalMode _decalMode = DecalMode.Hidden;
	public DecalMode Mode
	{
		get => _decalMode;
		set
		{
			//if(_decalMode.Equals(value))
			if (_decalMode == value)
				return;
			_decalMode = value;
			OnPropertyChanged();
		}
	}


	private Vector3 _position;
	public Vector3 Position
	{
		get => _position;
		set
		{
			//if(_position.Equals(value))
			if (_position == value)
				return;
			_position = value;
			OnPropertyChanged();
		}
	}


}

public class Indicator : PresenterBase<IndicatorModel>
{
	[SerializeField]
	private Color _BlockedColor;
	private Color _defaultColor;
	private Color _hiddenColor = new Color(0f, 0f, 0f, 0f);

	[SerializeField]
	private Renderer _decalRenderer;

	private Material _decalMaterial;

	private Coroutine _animCoroutine;

	public void Awake()
	{
		Model = new IndicatorModel();
		Model.Position = transform.position;
		_decalMaterial = new Material(_decalRenderer.sharedMaterial);
		_decalRenderer.sharedMaterial = _decalMaterial;
		_defaultColor = _decalMaterial.color;

	}

	protected override void HandlePropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(IndicatorModel.Position):
				transform.position = Model.Position;
				break;
			case nameof(IndicatorModel.Mode):
				UpdateVisibility();
				break;
		}

	}

	private void UpdateVisibility()
	{
		if(_animCoroutine != null)
		{
			StopCoroutine(_animCoroutine);
		}
		Color animColor = Model.Mode switch
		{

			IndicatorModel.DecalMode.Hidden => _hiddenColor,
			IndicatorModel.DecalMode.Available => _defaultColor,
			IndicatorModel.DecalMode.Unavailable => _BlockedColor,
			_ => throw new System.NotImplementedException(),
		};
		_animCoroutine= StartCoroutine(AnimateDecalColor(animColor, Model.Mode == IndicatorModel.DecalMode.Hidden));
	}

	IEnumerator AnimateDecalColor(Color targetColor, bool HideOnEnd)
	{
		_decalRenderer.enabled = true;
		Color startColor = _decalMaterial.color;
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * 2f;
			Color c = Color.Lerp(startColor, targetColor, t);
			_decalMaterial.color = c;
			yield return null;
		}

		_decalMaterial.color = targetColor;
		if (HideOnEnd)
		{
			_decalRenderer.enabled = false;
		}
	}




}
