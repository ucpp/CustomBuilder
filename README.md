Custom Builder for Unity 3D
===========================

**Unity Pro required**

A tool for easy automating and customizing Unity 3D build process.

It uses standard BuildPipeline.Build, stores configuration in readable and editable JSON files, provides GUI for managing configurations and modularity for customizing build process.

**Core features**
- Support for mutltiple configurations, stored in separate external JSON files - easy to read & modify.
- Easy modularity, because every project's build pipeline is unique.
- Can be launched programmatically (for the ones who want to make unattended builds).

How To Start
------------
Just copy the contents *Assets/Standard Assets* folder to your project. Then click *Windows -> Builder...* in the main menu.

Writing Custom Build Modules
----------------------------

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

Builder module events are fired in the same order the modules are arranged in configuration if not stated otherwise.

- **OnBeforeBuild** - is fired first after the main configuration is set but before the build.
- **OnBuild** - is fired before the build but after all *OnBeforeBuild* are called. It can be used to check the options set by other modules in *OnBeforeBuild*.
- **OnAfterBuild** - is fired after the build is done. Is not fired when the build fails.
- **OnCleanupBuild** - is fired after the build is done and after all *OnAfterBuild* calls. Is fired even when the build fails. Fired in reverse order.

The following methods are not build events but still can be overriden to suit your needs.

- **FromJson** - is used to load module settings from stored data.
- **ToJson** - is used to store module settings to JSON file.


- **OnGUI** - is used to draw GUI for module settings in the builder window.
