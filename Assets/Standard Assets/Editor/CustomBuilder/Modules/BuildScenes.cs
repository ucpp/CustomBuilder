using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEditor;
using System.ComponentModel;

namespace CustomBuilderModules
{
	[Description("Build Scenes")]
	public class BuildScenes : CustomBuilderModule
	{
		public override void OnBeforeBuild(CustomBuildConfiguration config)
		{
			var scenes = EditorBuildSettings.scenes;
			if (scenes == null)
			{
				return;
			}
			foreach (var s in scenes)
			{
				if (s.enabled && !config.scenes.Contains(s.path))
				{
					config.scenes.Add(s.path);
				}
			}
		}
	}
}

