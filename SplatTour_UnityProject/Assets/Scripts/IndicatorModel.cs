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
