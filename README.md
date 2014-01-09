Custom Builder
=============

**Unity Pro required**

A tool for easy automating and customizing unity build process.

It uses standard BuildPipeline.Build, stores configuration in readable and editable JSON files, provides GUI for managing configurations and modularity for customizing build process.


Writing Custom Build Modules
----------------

To make a custom build module you need to create a class, derived from CustomBuildModule. The GUI automatically scans all available modules and provides means to use them in your configuration.

The module class can be decorated with *Description* and *Browsable* attributes to set custom title or hide module from GUI.

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;

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
```

* **OnBeforeBuild** - is fired first after the main configuration is set but before the build.
* **OnBuild** - is fired before the build but after all *OnBeforeBuild* are called. It can be used to check the options set by other modules in *OnBeforeBuild*.
* **OnAfterBuild** - is fired after the build is done.


* **FromJson** - is used to load module settings from stored data.
* **ToJson** - is used to store module settings to JSON file.


* **OnGUI** - is used to draw GUI for module settings in the builder window.
