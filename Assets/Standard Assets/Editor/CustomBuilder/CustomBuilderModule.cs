using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

public sealed class CustomBuilderModuleInfo
{
	public string name { get; private set; }
	public string description { get; private set; }
	public Type type { get; private set; }

	public CustomBuilderModuleInfo(Type type)
	{
		this.type = type;
		this.name = type.FullName;
		this.description = this.name;

		var attrs = type.GetCustomAttributes(typeof(DescriptionAttribute), false);
		if (attrs.Length > 0)
		{
			this.description = ((DescriptionAttribute)attrs[0]).Description;
		}
	}

	public CustomBuilderModule Instantiate()
	{
		return (CustomBuilderModule)Activator.CreateInstance(this.type);
	}
}

public abstract class CustomBuilderModule
{
	private static readonly Dictionary<string, CustomBuilderModuleInfo> _modules = LoadModules();

	public virtual string name
	{
		get
		{
			return this.GetType().FullName;
		}
	}
	public virtual bool isCollapsed { get; set; }

	public static List<CustomBuilderModuleInfo> GetModules()
	{
		return new List<CustomBuilderModuleInfo>(_modules.Values);
	}
	public static CustomBuilderModuleInfo GetModule(string name)
	{
		if (name == null)
		{
			return null;
		}
		CustomBuilderModuleInfo module;
		if (!_modules.TryGetValue(name, out module))
		{
			return null;
		}
		return module;
	}



	public virtual void FromJson(JObject data)
	{
		this.isCollapsed = false;
		JToken obj;
		if (data.TryGetValue("_isCollapsed", out obj))
		{
			this.isCollapsed = (bool)obj;
			data.Remove("_isCollapsed");
		}
	}

	public virtual void ToJson(JObject data)
	{
		if (this.isCollapsed)
		{
			data["_isCollapsed"] = true;
		}
	}

	public virtual void OnGUI()
	{
	}

	public virtual void OnBeforeBuild(CustomBuildConfiguration config)
	{
	}

	public virtual void OnBuild(CustomBuildConfiguration config)
	{
	}

	public virtual void OnAfterBuild(CustomBuildConfiguration config)
	{
	}

	public virtual void OnCleanupBuild(CustomBuildConfiguration config)
	{
	}

	private static Dictionary<string, CustomBuilderModuleInfo> LoadModules()
	{
		var result = new Dictionary<string, CustomBuilderModuleInfo>();
		foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (a.GlobalAssemblyCache)
			{
				continue;
			}

			Type[] types;
			try
			{
				types = a.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				types = ex.Types;
			}

			if (types != null)
			{
				foreach (var t in types)
				{
					if (t == null)
					{
						continue;
					}
					if (typeof(CustomBuilderModule).IsAssignableFrom(t) && t.IsPublic && !t.IsAbstract)
					{
						var attrs = t.GetCustomAttributes(typeof(BrowsableAttribute), true);
						if (attrs.Length > 0 && !((BrowsableAttribute)attrs[0]).Browsable)
						{
							continue;
						}

						result[t.FullName] = new CustomBuilderModuleInfo(t);
					}
				}
			}				
		}
		return result;
	}
}