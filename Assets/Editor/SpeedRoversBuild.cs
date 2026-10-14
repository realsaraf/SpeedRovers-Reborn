using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SpeedRovers.Reborn;

public static class SpeedRoversBuild
{
    public static void Prepare()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Speed Rovers");
        root.AddComponent<SpeedRoversGame>();
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SpeedRovers.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/SpeedRovers.unity", true) };
        PlayerSettings.companyName = "Speed Rovers";
        PlayerSettings.productName = "Speed Rovers Reborn";
        PlayerSettings.bundleVersion = "0.10.0";
        foreach (var target in new[] { UnityEditor.Build.NamedBuildTarget.Standalone,
                     UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.Build.NamedBuildTarget.iOS,
                     UnityEditor.Build.NamedBuildTarget.WebGL })
            PlayerSettings.SetApplicationIdentifier(target, "com.philino.speedrovers.reborn");
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Brand/AppIcon.png");
        if (icon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
        Debug.Log("Speed Rovers scene prepared successfully.");
    }

    public static void BuildWindows()
    {
        Build("Builds/Windows/Speed Rovers Reborn.exe", BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
    }

    public static void BuildWindowsPainted()
    {
        Build("Builds/WindowsPainted/Speed Rovers Reborn.exe", BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
    }

    public static void BuildWindowsRush()
    {
        Build("Builds/WindowsRush/Speed Rovers Reborn.exe", BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
    }

    public static void BuildDesktop()
    {
        BuildWindows();
        BuildMac();
        BuildLinux();
    }

    public static void BuildAllPlatforms()
    {
        BuildDesktop();
        BuildAndroid();
        BuildIOS();
        BuildWebGL();
    }

    public static void BuildMac()
    {
        UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64;
        Build("Builds/macOS/Speed Rovers Reborn.app", BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
    }

    public static void BuildLinux()
    {
        Build("Builds/Linux/Speed Rovers Reborn.x86_64", BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
    }

    public static void BuildAndroid()
    {
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.bundleVersionCode = 2;
        PlayerSettings.Android.useCustomKeystore = false;
        EditorUserBuildSettings.buildAppBundle = false;
        Build("Builds/Android/Speed Rovers Reborn.apk", BuildTargetGroup.Android, BuildTarget.Android);
    }

    public static void BuildIOS()
    {
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        Build("Builds/iOS", BuildTargetGroup.iOS, BuildTarget.iOS);
    }

    public static void BuildWebGL()
    {
        // Uncompressed files can be served by any ordinary static file server.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "PROJECT:SpeedRovers";
        Build("Builds/WebGL", BuildTargetGroup.WebGL, BuildTarget.WebGL);
    }

    public static void ReportTargets()
    {
        foreach (BuildTarget target in new[] { BuildTarget.StandaloneWindows64, BuildTarget.StandaloneOSX,
                     BuildTarget.StandaloneLinux64, BuildTarget.Android, BuildTarget.iOS, BuildTarget.WebGL })
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            Debug.Log("Speed Rovers target " + target + ": " +
                      (BuildPipeline.IsBuildTargetSupported(group, target) ? "installed" : "module missing"));
        }
    }

    static void Build(string output, BuildTargetGroup group, BuildTarget target)
    {
        if (!BuildPipeline.IsBuildTargetSupported(group, target))
            throw new Exception("Unity build module is not installed for " + target);
        Prepare();
        if(target==BuildTarget.iOS)
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS,"com.philino.speedrovers.reborn");
            PlayerSettings.productName="Speed Rovers 2";
            PlayerSettings.bundleVersion=Environment.GetEnvironmentVariable("SPEED_ROVERS_IOS_VERSION") ?? "1.0.6";
            PlayerSettings.iOS.buildNumber="100";
            PlayerSettings.iOS.targetOSVersionString="15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning=false;
            AssetDatabase.SaveAssets();
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/SpeedRovers.unity" },
            locationPathName = output,
            target = target,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception(target + " build failed: " + report.summary.result);
        Debug.Log("Speed Rovers " + target + " build: " + Path.GetFullPath(output));
    }
}



