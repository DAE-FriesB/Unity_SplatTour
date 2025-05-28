using System.ComponentModel;
using UnityEngine;

public class PresenterBase<TModel> : MonoBehaviour where TModel: ModelBase
{
	private TModel _model;

	public TModel Model
	{
		get => _model;
		set
		{
			if(_model == value) return;
			if(_model != null)
			{
				_model.PropertyChanged -= _model_PropertyChanged;
			}
			_model = value;
			if(_model != null)
			{
				_model.PropertyChanged += _model_PropertyChanged;
			}
		}
	}

	private void _model_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		HandlePropertyChanged(e.PropertyName);
	}

	protected virtual void HandleModelChanged()
	{

	}

	protected virtual void HandlePropertyChanged(string propertyName)
	{

	}
}
