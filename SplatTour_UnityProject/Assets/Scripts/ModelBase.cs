﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

public class ModelBase : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));	
	}
}
