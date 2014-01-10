/*
	Add your name and your e-mail
    Programming: Maryan Yaroma, yaroma.maryan@gmail.com 
				 Dmitriy Pyalov, dipyalov@gmail.com
*/
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;


public class CustomBuilder : EditorWindow
{
	private const string BuildConfigurationsDir = "BuildConfigurations/";
	private const string CurrentConfigPref = "CustomBuilder.CurrentConfig";

	[SerializeField]
	private bool _initialized;
	[SerializeField]
	private string _currentConfigurationName;
	[SerializeField]
	private string _currentConfigurationSerialized;
	[SerializeField]
	private bool _currentConfigurationDirty;

	private CustomBuilderConfiguration _currentConfiguration;
	private Vector2 _scrollPos;

	public static BuildTargetGroup GetBuildTargetGroup(BuildTarget buildTarget)
	{
		if (buildTarget == BuildTarget.WebPlayer 
		 	|| buildTarget == BuildTarget.WebPlayerStreamed)
		{
			return BuildTargetGroup.WebPlayer;
		}

		if (buildTarget == BuildTarget.StandaloneOSXIntel 
			|| buildTarget == BuildTarget.StandaloneOSXIntel64
			|| buildTarget == BuildTarget.StandaloneOSXUniversal
			|| buildTarget == BuildTarget.StandaloneWindows
			|| buildTarget == BuildTarget.StandaloneWindows64
			|| buildTarget == BuildTarget.StandaloneLinux
			|| buildTarget == BuildTarget.StandaloneLinux64
			|| buildTarget == BuildTarget.StandaloneLinuxUniversal)
		{
			return BuildTargetGroup.Standalone;
		}

		if (buildTarget == BuildTarget.StandaloneGLESEmu)
		{
			return BuildTargetGroup.GLESEmu;
		}

		if (buildTarget == BuildTarget.iPhone)
		{
			return BuildTargetGroup.iPhone;
		}

		if (buildTarget == BuildTarget.PS3)
		{
			return BuildTargetGroup.PS3;
		}

		if (buildTarget == BuildTarget.XBOX360)
		{
			return BuildTargetGroup.XBOX360;
		}

		if (buildTarget == BuildTarget.Android)
		{
			return BuildTargetGroup.Android;
		}

		if (buildTarget == BuildTarget.Wii)
		{
			return BuildTargetGroup.Wii;
		}

		if (buildTarget == BuildTarget.NaCl)
		{
			return BuildTargetGroup.NaCl;
		}

		if (buildTarget == BuildTarget.FlashPlayer)
		{
			return BuildTargetGroup.FlashPlayer;
		}

		if (buildTarget == BuildTarget.MetroPlayer)
		{
			return BuildTargetGroup.Metro;
		}

		if (buildTarget == BuildTarget.WP8Player)
		{
			return BuildTargetGroup.WP8;
		}

		if (buildTarget == BuildTarget.BB10)
		{
			return BuildTargetGroup.BB10;
		}

		return BuildTargetGroup.Unknown;
	}
	
	[MenuItem("Window/Builder... %#1")]
	private static void OpenWindow()
	{
		var window = EditorWindow.GetWindow<CustomBuilder>(true, "Custom Builder");
		window.Show();
	}
	[MenuItem("Window/Build Current %#&1")]
	private static void BuildCurrent()
	{
		string name = EditorPrefs.GetString(CurrentConfigPref, null);
		if (string.IsNullOrEmpty(name))
		{
			return;
		}
		string path = BuildConfigurationsDir + name + ".json";
		if (!File.Exists(path))
		{
			return;
		}

		var config = new CustomBuilderConfiguration();
		config.name = name;
		config.FromJson(JObject.Parse(File.ReadAllText(path)));
		config.Build();
	}

	[MenuItem("Window/Build Current %#&1", true)]
	private static bool Validate()
	{
		string name = EditorPrefs.GetString(CurrentConfigPref, null);
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		string path = BuildConfigurationsDir + name + ".json";
		if (!File.Exists(path))
		{
			return false;
		}

		return true;
	}

	private void OnEnable()
	{
		if (!this._initialized)
		{
			this._currentConfigurationName = EditorPrefs.GetString(CurrentConfigPref, null);
			this._initialized = true;
			return;
		}

		if (!string.IsNullOrEmpty(this._currentConfigurationSerialized))
		{
			this._currentConfiguration = new CustomBuilderConfiguration();
			this._currentConfiguration.FromJson(JObject.Parse(this._currentConfigurationSerialized));
		}
		else
		{
			this._currentConfiguration = null;
		}

		this._currentConfigurationSerialized = null;
	}

	private void OnDisable()
	{
		if (this._currentConfiguration != null)
		{					
			var obj = new JObject();
			this._currentConfiguration.ToJson(obj);
			this._currentConfigurationSerialized = obj.ToString();
		}
		else
		{
			this._currentConfigurationSerialized = null;
		}

		this._currentConfigurationSerialized = null;
	}

	private void OnGUI()
	{
		var configs = this.GetConfigurations();

		int oldConfigIndex = this._currentConfigurationName != null ? System.Array.IndexOf(configs, this._currentConfigurationName) : -1;
		int newConfigIndex = EditorGUILayout.Popup(
			oldConfigIndex,
			configs
		);
		if (newConfigIndex != oldConfigIndex)
		{
			this._currentConfigurationName = newConfigIndex != -1 ? configs[newConfigIndex] : null;
			this._currentConfiguration = null;
		}

		if (this._currentConfigurationName != null)
		{
			EditorPrefs.SetString(CurrentConfigPref, this._currentConfigurationName);
		}
		else
		{
			EditorPrefs.DeleteKey(CurrentConfigPref);
		}

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("New"))
		{
			this.CreateNewConfiguration();
		}

		var bg = GUI.backgroundColor;
		if (this._currentConfiguration != null && this._currentConfigurationDirty)
		{
			GUI.backgroundColor = Color.yellow;
		}
		if (GUILayout.Button(this._currentConfiguration != null && this._currentConfigurationDirty ? "Save*" : "Save"))
		{
			this.SaveCurrentConfiguration();
		}
		GUI.backgroundColor = bg;

		EditorGUILayout.EndHorizontal();

		if (this._currentConfiguration == null && this._currentConfigurationName != null)
		{
			this._currentConfiguration = this.LoadConfiguration(this._currentConfigurationName);
			this._currentConfigurationDirty = false;
		}
			
		if (this._currentConfiguration != null)
		{
			bg = GUI.backgroundColor;
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("Build " + this._currentConfiguration.name))
			{
				this._currentConfiguration.Build();
			}
			GUI.backgroundColor = bg;
		}

		this._scrollPos = EditorGUILayout.BeginScrollView(this._scrollPos);
		if (this._currentConfiguration != null)
		{
			this._currentConfigurationDirty |= this._currentConfiguration.OnGUI();
		}
		EditorGUILayout.EndScrollView();
	       
		if (this._currentConfiguration != null)
		{
			var modules = CustomBuilderModule.GetModules();
			int moduleIndex = EditorGUILayout.Popup("Add Module", -1, modules.ConvertAll(x => x.description).ToArray());
			var module = moduleIndex >= 0 ? modules[moduleIndex] : null;

			if (module != null)
			{
				this._currentConfiguration.AddModule(module);
				this._currentConfigurationDirty = true;
			}
		}
	}

	private CustomBuilderConfiguration LoadConfiguration(string name)
	{
		var config = new CustomBuilderConfiguration();

		string path = BuildConfigurationsDir + name + ".json";
		if (File.Exists(path))
		{
			config.name = name;
			config.FromJson(JObject.Parse(File.ReadAllText(path)));
		}
		else
		{
			config.InitializeNew(name);
		}

		return config;
	}

	private string[] GetConfigurations()
	{
		if (!Directory.Exists(BuildConfigurationsDir))
		{
			return new string[0];
		}

		return Directory.GetFiles(BuildConfigurationsDir, "*.json").Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();
	}

	private void CreateNewConfiguration()
	{
		string nameBase = "Build";
		string name = nameBase;
		string path = null;
		int i = 0;
		while (true)
		{
			path = BuildConfigurationsDir + name + ".json";
			if (!File.Exists(path))
			{
				break;
			}

			name = nameBase + (++i).ToString();
		}

		var config = new CustomBuilderConfiguration();
		config.InitializeNew(name);
		var obj = new JObject();
		config.ToJson(obj);
		File.WriteAllText(path, obj.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);

		this._currentConfigurationName = name;
		this._currentConfiguration = config;
		this._currentConfigurationDirty = false;
	}

	private void SaveCurrentConfiguration()
	{
		if (this._currentConfiguration == null || string.IsNullOrEmpty(this._currentConfiguration.name))
		{
			return;
		}

		if (this._currentConfigurationName != this._currentConfiguration.name)
		{
			string oldPath = BuildConfigurationsDir + this._currentConfigurationName + ".json";
			if (File.Exists(oldPath))
			{
				File.Delete(oldPath);
			}
		}

		this._currentConfigurationName = this._currentConfiguration.name;
		string newPath = BuildConfigurationsDir + this._currentConfigurationName + ".json";
		var obj = new JObject();
		this._currentConfiguration.ToJson(obj);
		File.WriteAllText(newPath, obj.ToString(Formatting.Indented), Encoding.UTF8);
		this._currentConfigurationDirty = false;
	}
}
