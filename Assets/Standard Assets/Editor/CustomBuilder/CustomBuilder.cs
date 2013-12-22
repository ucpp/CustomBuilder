/*
	Add your name and your e-mail
    Programming: Maryan Yaroma, yaroma.maryan@gmail.com 
    Define Editor base on this script (need re-write):
    https://github.com/prime31/P31UnityAddOns/blob/master/Editor/GlobalDefinesWizard.cs
*/
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

public class SimpleBuilder : ScriptableWizard
{
	public List<GlobalDefine> _globalDefines = new List<GlobalDefine>();
	private BuildTarget build_type = BuildTarget.StandaloneWindows;
	private BuildOptions build_options = BuildOptions.None;
	private const string saveKey = "userDefines";
	private string build_name = "TestBuild";
	private Vector2 pos = Vector2.zero;
	
	[MenuItem( "Builder/Simple Builder %#1", false, 0 )]
	static void CreateBuilderWindow()
	{
		var window = ScriptableWizard.DisplayWizard<SimpleBuilder>( "Simple Builder", "Save", "Cancel" );
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
		window.ShowUtility();
	}

	private void OnGUI()
	{
		EditorGUILayout.LabelField("Simple Project Builder v.0.0.3");
	        GUILayout.Space (10);
	        build_name = EditorGUILayout.TextField (build_name);
	        GUI.backgroundColor = Color.green;
	        if (GUILayout.Button ("Building " + build_name)) 
	        {
				SaveDefines();
	            string build_path = "Build/PC/" + build_name + ".exe";
	            BuildPipeline.BuildPlayer (GetPaths (), build_path, build_type, build_options);
	            this.Close();
	        }
	        GUI.backgroundColor = Color.gray;
	        build_type = (BuildTarget)EditorGUILayout.EnumPopup ("Type:",build_type);
	        build_options = (BuildOptions)EditorGUILayout.EnumPopup ("Options:", build_options);
	        		
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
