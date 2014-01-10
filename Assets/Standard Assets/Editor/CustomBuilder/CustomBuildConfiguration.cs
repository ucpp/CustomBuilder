using System;
using System.Collections.Generic;
using UnityEditor;

public class CustomBuildConfiguration
{
	private Dictionary<object, object> _parameters;
	private List<string> _scenes = new List<string>();

	public string error { get; set; }
	public Dictionary<object, object> parameters 
	{
		get
		{
			return this._parameters ?? (this._parameters = new Dictionary<object, object>());
		}
	}
	public List<string> scenes
	{
		get
		{
			return this._scenes;
		}
	}
	public string buildPath { get; set; }
	public BuildTarget buildTarget { get; set; }
	public BuildOptions buildOptions { get; set; }

}

