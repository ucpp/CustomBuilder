using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEditor;
using System.ComponentModel;

namespace CustomBuilderModules
{
	[Description("All Scenes")]
	public class AllScenes : CustomBuilderModule
	{
		public override void OnBeforeBuild(CustomBuildConfiguration config)
		{
			var allPaths = AssetDatabase.GetAllAssetPaths();
			for (int i = 0; i < allPaths.Length; i++)
			{
				if (allPaths[i].EndsWith(".unity") && !config.scenes.Contains(allPaths[i]))
				{
					config.scenes.Add(allPaths[i]);
				}
			}
		}
	}
}

