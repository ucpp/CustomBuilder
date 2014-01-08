using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEditor;
using System.ComponentModel;

namespace CustomBuilderModules
{
	[Description("Scenes")]
	public class BuildScenes : CustomBuilderModule
	{
		public List<string> scenes = new List<string>();

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.FromJson(data);
			if (data["scenes"] != null)
			{
				this.scenes = data["scenes"].ToObject<List<string>>();
			}
		}

		public override void ToJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.ToJson(data);
			data["scenes"] = JToken.FromObject(this.scenes);
		}

		public override void OnBeforeBuild(CustomBuildConfiguration config)
		{
			if (this.scenes != null)
			{
				foreach (var s in this.scenes)
				{
					if (!config.scenes.Contains(s))
					{
						config.scenes.Add(s);
					}
				}
			}
		}

		public override void OnGUI()
		{
			Rotorz.ReorderableList.ReorderableListGUI.ListField(
				this.scenes,
				(pos, value) => EditorGUI.TextField(pos, value)
			);
			//ReorderableList
		}
	}
}

