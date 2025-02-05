using Dependencies;
using System.Collections;
using Timing;
using UnityEngine;

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

	private ITimeService _timeService;
	public void Awake()
	{
		_timeService = DependencyService.GetService<ITimeService>();
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
			t += _timeService.DeltaTime * 2f;
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
