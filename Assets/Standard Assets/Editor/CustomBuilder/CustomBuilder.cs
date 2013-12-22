/*
	Add your name and your e-mail
    Programming: Maryan Yaroma, yaroma.maryan@gmail.com 
    Define Editor base on this script (need re-write):
    https://github.com/prime31/P31UnityAddOns/blob/master/Editor/GlobalDefinesWizard.cs
*/
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


#region using
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#endregion
// TODO : re-write defines editor realisation
[System.Serializable]
public class GlobalDefine : ISerializable
{
	public string define;
	public bool enabled;
	
	public GlobalDefine() { }
	
	protected GlobalDefine( SerializationInfo info, StreamingContext context )
	{
		define = info.GetString( "define" );
		enabled = info.GetBoolean( "enabled" );
	}
	
	public void GetObjectData( SerializationInfo info, StreamingContext context )
	{
		info.AddValue( "define", define );
		info.AddValue( "enabled", enabled );
	}
}

public class CustomBuilder : EditorWindow
{
	private const string BuildConfigurationsDir = "BuildConfigurations/";
	public List<GlobalDefine> _globalDefines = new List<GlobalDefine>();
	private const string saveKey = "userDefines";
	private Vector2 pos = Vector2.zero;

	[SerializeField]
	private string _currentConfigurationName;
	[SerializeField]
	private string _currentConfigurationSerialized;
	[SerializeField]
	private bool _currentConfigurationDirty;

	private CustomBuilderConfiguration _currentConfiguration;
	
	[MenuItem("Window/Builder %#1")]
	private static void OpenWindow()
	{
		var window = EditorWindow.GetWindow<CustomBuilder>(true, "Builder");
		window.minSize = new Vector2( 300, 400 );
		window.maxSize = new Vector2( 300, 400 );

		if( EditorPrefs.HasKey( saveKey ) )
		{
			var data = EditorPrefs.GetString( saveKey );
			var bytes = System.Convert.FromBase64String( data );
			var stream = new MemoryStream( bytes );
			
			var formatter = new BinaryFormatter();
			window._globalDefines = (List<GlobalDefine>)formatter.Deserialize( stream );
		}
		window.Show();
	}

	private void OnEnable()
	{
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
			this._currentConfigurationSerialized = this._currentConfiguration.ToJson().ToString();
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

		EditorGUILayout.LabelField("Custom Project Builder v.0.0.3");
	   	GUILayout.Space(10);

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

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("New"))
		{
			this.CreateNewConfiguration();
		}
		if (GUILayout.Button(this._currentConfiguration != null && this._currentConfigurationDirty ? "Save*" : "Save"))
		{
			this.SaveCurrentConfiguration();
		}
		EditorGUILayout.EndHorizontal();

		if (this._currentConfiguration == null && this._currentConfigurationName != null)
		{
			this._currentConfiguration = this.LoadConfiguration(this._currentConfigurationName);
			this._currentConfigurationDirty = false;
		}

		if (this._currentConfiguration != null)
		{
			this._currentConfigurationDirty |= this._currentConfiguration.OnGUI();
		}
	       
	        		
		var toRemove = new List<GlobalDefine>();
		if( GUILayout.Button( "Add Define" ) )
		{
			var d = new GlobalDefine();
			d.define = "NEW_DEFINE";
			d.enabled = false;
			_globalDefines.Add( d );
			pos.y += 20;
		}
		GUILayout.Space (10);
		pos = EditorGUILayout.BeginScrollView(pos);
		foreach( var define in _globalDefines )
		{
			if( DefineEditor( define ) )
					toRemove.Add( define );
		}
		foreach( var define in toRemove )
			_globalDefines.Remove( define );
		GUILayout.Space( 10 );
		EditorGUILayout.HelpBox ("Это тестовый пример сборщика проектов!", MessageType.Info);
		EditorGUILayout.EndScrollView();

		GUI.backgroundColor = Color.green;
		if (this._currentConfiguration != null)
		{
			if (GUILayout.Button("Build " + this._currentConfiguration.name))
			{
				SaveDefines();
				this.Build(this._currentConfiguration);
				this.Close();
			}
		}
	}

	public void Build(CustomBuilderConfiguration config)
	{
		BuildPipeline.BuildPlayer(
			config.scenesInBuild.ToArray(),
			config.buildPath,
			config.buildTarget,
			config.buildOptions
		);
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
		File.WriteAllText(path, config.ToJson().ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);

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
		File.WriteAllText(newPath, this._currentConfiguration.ToJson().ToString(Formatting.Indented), Encoding.UTF8);
		this._currentConfigurationDirty = false;
	}

	/// <summary>
	/// Get all scenes paths.
	/// </summary>
	/// <returns>The scenes paths.</returns>
	private string[] GetPaths()
    	{
	        string[] all_paths = AssetDatabase.GetAllAssetPaths();
	        List< string > paths = new List< string >();
	        for (int i = 0; i < all_paths.Length; i++)
	        {
	            if(all_paths[i].EndsWith(".unity"))
	                paths.Add(all_paths[i]);
	        }
	        string[] scenes_paths = paths.ToArray();
	        return scenes_paths;
        }



	private void SaveDefines()
	{
		if( _globalDefines.Count == 0 )
		{
			DeleteFiles();
			EditorPrefs.DeleteKey( saveKey );
			Close();
			return;
		}
		var formatter = new BinaryFormatter();
		using( var stream = new MemoryStream() )
		{
			formatter.Serialize( stream, _globalDefines );
			var data = System.Convert.ToBase64String( stream.ToArray() );
			stream.Close();
			
			EditorPrefs.SetString( saveKey, data );
		}
		var toDisk = _globalDefines.Where( d => d.enabled ).Select( d => d.define ).ToArray();
		if( toDisk.Length > 0 )
		{
			var builder = new System.Text.StringBuilder( "-define:" );
			for( var i = 0; i < toDisk.Length; i++ )
			{
				if( i < toDisk.Length - 1 )
					builder.AppendFormat( "{0};", toDisk[i] );
				else
					builder.Append( toDisk[i] );
			}
			WriteFiles( builder.ToString() );
			AssetDatabase.Refresh();
			ReimportSomethingToForceRecompile();
		}
		else
			DeleteFiles();
	}

	private void ReimportSomethingToForceRecompile()
	{
		var dataPathDir = new DirectoryInfo( Application.dataPath );
		var dataPathUri = new System.Uri( Application.dataPath );
		foreach( var file in dataPathDir.GetFiles( "SimpleBuilder.cs", SearchOption.AllDirectories ) )
		{
			var relativeUri = dataPathUri.MakeRelativeUri( new System.Uri( file.FullName ) );
			var relativePath = System.Uri.UnescapeDataString( relativeUri.ToString() );
			AssetDatabase.ImportAsset( relativePath, ImportAssetOptions.ForceUpdate );
		}
	}

	private void DeleteFiles()
	{
		var smcsFile = Path.Combine( Application.dataPath, "smcs.rsp" );
		var gmcsFile = Path.Combine( Application.dataPath, "gmcs.rsp" );
		
		if( File.Exists( smcsFile ) )
			File.Delete( smcsFile );
		
		if( File.Exists( gmcsFile ) )
			File.Delete( gmcsFile );
	}

	private void WriteFiles( string data )
	{
		var smcsFile = Path.Combine( Application.dataPath, "smcs.rsp" );
		var gmcsFile = Path.Combine( Application.dataPath, "gmcs.rsp" );
	
		File.WriteAllText( smcsFile, data );
		File.WriteAllText( gmcsFile, data );
	}

	private bool DefineEditor( GlobalDefine define )
	{
		EditorGUILayout.BeginHorizontal();
		define.define = EditorGUILayout.TextField( define.define );
		define.enabled = EditorGUILayout.Toggle( define.enabled );
		var remove = false;
		if( GUILayout.Button( "Remove" ) )
			remove = true;
		EditorGUILayout.EndHorizontal();
		return remove;
	}	
}
