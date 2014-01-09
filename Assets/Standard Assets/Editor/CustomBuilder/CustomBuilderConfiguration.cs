using UnityEngine;
using UnityEditor;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.IO;


[Browsable(false)]
public sealed class CustomBuilderMissingModule : CustomBuilderModule
{
	private JObject _data = new JObject();
	private string _name;

	public override string name
	{
		get
		{
			return this._name;
		}
	}

	public CustomBuilderMissingModule(string name)
	{
		this._name = name;
	}

	public override void FromJson(JObject data)
	{
		base.FromJson(data);
		foreach (var p in data)
		{
			this._data[p.Key] = p.Value;
		}
	}

	public override void ToJson(JObject data)
	{
		base.ToJson(data);
		foreach (var p in this._data)
		{
			data[p.Key] = p.Value;
		}
	}
}

// Fucking workaround because original BuildOptions is full of crap!
public enum CustomBuilderBuildOptions
{
	None = 0,
	Development = 1,
	Unknown = 2,
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

public class CustomBuilderConfiguration  
{
	public string name;
	public string buildPath;
	public BuildTarget buildTarget;
	public CustomBuilderBuildOptions buildOptions;

	private readonly List<CustomBuilderModule> _modules = new List<CustomBuilderModule>();

	public void InitializeNew(string name)
	{
		this.name = name;
		this.buildPath = "Builds/" + name + "/" + name + ".exe";
		this.buildTarget = BuildTarget.StandaloneWindows;
	}

	public void ToJson(JObject json)
	{
		json["path"] = this.buildPath ?? "";
		json["buildTarget"] = this.buildTarget.ToString();

		if (this.buildOptions != CustomBuilderBuildOptions.None)
		{
			var options = new JObject();

			foreach (CustomBuilderBuildOptions o in System.Enum.GetValues(typeof(CustomBuilderBuildOptions)))
			{
				if ((this.buildOptions & o) != 0)
				{			
					var name = new StringBuilder(o.ToString());
					name[0] = char.ToLowerInvariant(name[0]);
					options[name.ToString()] = true;
				}
			}
			json["buildOptions"] = options;
		}

		if (this._modules.Count > 0)
		{
			var modules = new JArray();
			foreach (var m in this._modules)
			{
				var obj = new JObject();
				if (m.name != null)
				{
					obj["_name"] = m.name;
				}
				m.ToJson(obj);
				modules.Add(obj);
			}
			json["modules"] = modules;
		}
	}

	public void FromJson(JObject json)
	{
		this.buildPath = null;
		this.buildTarget = (BuildTarget)0;
		this.buildOptions = CustomBuilderBuildOptions.None;

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

			foreach (var p in options)
			{
				if ((bool)p.Value)
				{
					try
					{
						this.buildOptions |= (CustomBuilderBuildOptions)System.Enum.Parse(typeof(CustomBuilderBuildOptions), p.Key, true);
					}
					catch
					{
					}
				}
			}
		}

		this._modules.Clear();
		if (json.TryGetValue("modules", out obj))
		{
			foreach (JObject o in (JArray)obj)
			{

				string name = (string)o["_name"];
				o.Remove("_name");
				var info = CustomBuilderModule.GetModule(name);
				var module = (info != null) ? info.Instantiate() : new CustomBuilderMissingModule(name);
				module.FromJson(o);
				this._modules.Add(module);
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
		this.buildOptions = (CustomBuilderBuildOptions)EditorGUILayout.EnumMaskField("Options", this.buildOptions);


		List<CustomBuilderModule> moduleToRemove = null;
		foreach (var m in this._modules)
		{
			var info = CustomBuilderModule.GetModule(m.name);
			if (info == null)
			{
				EditorGUILayout.LabelField(m.name != null ? "Missing module: " + m.name : "Missing module");
			}
			else
			{
				EditorGUILayout.BeginHorizontal();
				m.isCollapsed = !EditorGUILayout.Foldout(!m.isCollapsed, info.description);
				if (GUILayout.Button("Delete"))
				{
					if (moduleToRemove == null)
					{
						moduleToRemove = new List<CustomBuilderModule>(1);
					}
					moduleToRemove.Add(m);
				}
				EditorGUILayout.EndHorizontal();
				if (!m.isCollapsed)
				{
					EditorGUI.indentLevel++;
					m.OnGUI();
					EditorGUI.indentLevel--;
				}
			}
		}
			
		if (moduleToRemove != null)
		{
			foreach (var m in moduleToRemove)
			{
				this._modules.Remove(m);
			}
		}

		return EditorGUI.EndChangeCheck();
	}

	public void AddModule(CustomBuilderModuleInfo module)
	{
		if (module == null)
		{
			return;
		}
		this._modules.Add(module.Instantiate());
	}

	public void Build()
	{
		var config = new CustomBuildConfiguration();
		config.buildPath = this.buildPath;
		config.buildTarget = this.buildTarget;
		config.buildOptions = (BuildOptions)this.buildOptions;


		foreach (var m in this._modules)
		{
			m.OnBeforeBuild(config);
		}

		foreach (var m in this._modules)
		{
			m.OnBuild(config);
		}

		var dir = Path.GetDirectoryName(config.buildPath);
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}

		BuildPipeline.BuildPlayer(config.scenes.ToArray(), config.buildPath, config.buildTarget, config.buildOptions);

		foreach (var m in this._modules)
		{
			m.OnAfterBuild(config);
		}
	}
}
