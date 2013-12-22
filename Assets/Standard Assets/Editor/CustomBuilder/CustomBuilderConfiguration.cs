using UnityEngine;
using UnityEditor;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;



public class CustomBuilderConfiguration  
{
	// Fucking workaround because original BuildOptions is full of crap!
	private enum CustomBuildOptions
	{
		None = 0,
		Development = 1,
		AutoRunPlayer = 4,
		ShowBuiltPlayer = 8,
		BuildAdditionalStreamedScenes = 16,
		AcceptExternalModificationsToPlayer = 32,
		InstallInBuildFolder = 64,
		WebPlayerOfflineDeployment = 128,
		ConnectWithProfiler = 256,
		AllowDebugging = 512,
		SymlinkLibraries = 1024,
		UncompressedAssetBundle = 2048,
		ConnectToHost = 4096,
		DeployOnline = 8192,
		EnableHeadlessMode = 16384
	}

	public string name;
	public readonly List<string> scenesInBuild = new List<string>();
	public string buildPath;
	public BuildTarget buildTarget;
	public BuildOptions buildOptions;

	public void InitializeNew(string name)
	{
		this.name = name;
		this.buildPath = "Builds/" + name + "/" + name + ".exe";
		this.buildTarget = BuildTarget.StandaloneWindows;
	}

	public JObject ToJson()
	{
		var obj = new JObject 
		{
			{ "path", this.buildPath ?? "" },
			{ "buildTarget", this.buildTarget.ToString() }
		};

		if (this.buildOptions != BuildOptions.None)
		{
			var options = new JObject();
			var co = (CustomBuildOptions)this.buildOptions;

			foreach (CustomBuildOptions o in System.Enum.GetValues(typeof(CustomBuildOptions)))
			{
				if ((co & o) != 0)
				{			
					var name = new StringBuilder(o.ToString());
					name[0] = char.ToLowerInvariant(name[0]);
					options[name.ToString()] = true;
				}
			}
			obj["buildOptions"] = options;
		}

		if (this.scenesInBuild.Count > 0)
		{
			var scenes = new JArray();
			foreach (var s in this.scenesInBuild)
			{
				scenes.Add(s);
			}
			obj["scenes"] = scenes;
		}

		return obj;
	}

	public void FromJson(JObject json)
	{
		this.buildPath = null;
		this.scenesInBuild.Clear();
		this.buildTarget = (BuildTarget)0;
		this.buildOptions = BuildOptions.None;

		if (json == null)
		{
			return;
		}

		JToken obj;
		if (json.TryGetValue("path", out obj))
		{
			this.buildPath = (string)obj;
		}

		if (json.TryGetValue("buildTarget", out obj))
		{
			this.buildTarget = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), (string)obj, true);
		}

		if (json.TryGetValue("buildOptions", out obj))
		{
			var options = (JObject)obj;
			var co = (CustomBuildOptions)this.buildOptions;
			foreach (var p in options)
			{
				if ((bool)p.Value)
				{
					try
					{
						co |= (CustomBuildOptions)System.Enum.Parse(typeof(CustomBuildOptions), p.Key, true);
					}
					catch
					{
					}
				}
			}
			this.buildOptions = (BuildOptions)co;
		}

		if (json.TryGetValue("scenes", out obj))
		{
			var scenes = (JArray)obj;
			foreach (var s in scenes)
			{
				this.scenesInBuild.Add((string)s);
			}
		}
	}

	public bool OnGUI()
	{
		EditorGUI.BeginChangeCheck();

		this.name = EditorGUILayout.TextField("Name", this.name);

		GUI.backgroundColor = Color.gray;
		this.buildPath = EditorGUILayout.TextField("Path", this.buildPath);
		this.buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Type", this.buildTarget);
		var co = (CustomBuildOptions)this.buildOptions;
		co = (CustomBuildOptions)EditorGUILayout.EnumMaskField("Options", co);
		this.buildOptions = (BuildOptions)co;

		return EditorGUI.EndChangeCheck();
	}
}
