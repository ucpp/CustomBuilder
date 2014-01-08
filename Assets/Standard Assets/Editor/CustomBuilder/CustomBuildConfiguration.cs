using System;
using System.Collections.Generic;
using UnityEditor;

public class CustomBuildConfiguration
{
	private Dictionary<string, object> _parameters;
	private List<string> _scenes = new List<string>();

	public Dictionary<string, object> parameters 
	{
		get
		{
			return this._parameters ?? (this._parameters = new Dictionary<string, object>());
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

