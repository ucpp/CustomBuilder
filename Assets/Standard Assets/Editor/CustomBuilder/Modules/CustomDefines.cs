using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace CustomBuilderModules
{
	[Description("Custom Defines")]
	public class CustomDefines : CustomBuilderModule
	{
		private List<string> _defines;

		public bool append { get; set; }
		public List<string> defines
		{ 
			get
			{
				return this._defines ?? (this._defines = new List<string>());
			}
			set
			{
				this._defines = value;
			}
		}

		public CustomDefines()
		{
			this.append = true;
		}

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.FromJson(data);
			if (data["defines"] != null)
			{
				this.defines = data["defines"].ToObject<List<string>>();
			}
			if (data["append"] != null)
			{
				this.append = (bool)data["append"];
			}
		}

		public override void ToJson(JObject data)
		{
			base.ToJson(data);
			data["defines"] = JToken.FromObject(this.defines);
			data["append"] = this.append;
		}

		public override void OnBuild(CustomBuildConfiguration config)
		{
			var targetGroup = CustomBuilder.GetBuildTargetGroup(config.buildTarget);
			var scriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
			config.parameters[this] = new State 
			{
				targetGroup = targetGroup,
				originalDefines = scriptingDefines
			};

			var newDefines = new List<string>();
			if (this.append)
			{
				foreach (var d in scriptingDefines.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
				{
					if (!newDefines.Contains(d))
					{
						newDefines.Add(d);
					}
				}
			}

			foreach (var s in this.defines)
			{
				if (!newDefines.Contains(s))
				{
					newDefines.Add(s);
				}
			}

			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", newDefines.ToArray()));
		}

		public override void OnCleanupBuild(CustomBuildConfiguration config)
		{
			var state = (State)config.parameters[this];
			PlayerSettings.SetScriptingDefineSymbolsForGroup(state.targetGroup, state.originalDefines);
		}

		public override void OnGUI()
		{
			Rotorz.ReorderableList.ReorderableListGUI.ListField(
				this.defines,
				(pos, value) => EditorGUI.TextField(pos, value)
			);
			this.append = EditorGUILayout.Toggle("Append Defines", this.append);
		}

		private class State
		{
			public BuildTargetGroup targetGroup;
			public string originalDefines;
		}
	}
}

