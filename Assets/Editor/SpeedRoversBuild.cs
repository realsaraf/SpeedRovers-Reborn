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
        TrafficRideBuild.Prepare();
        PlayerSettings.companyName="Speed Rovers";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS,"com.philino.speedrovers.reborn");
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.philino.speedrovers.reborn");
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
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
            PlayerSettings.bundleVersion=Environment.GetEnvironmentVariable("SPEED_ROVERS_IOS_VERSION") ?? "2.1.1";
            PlayerSettings.iOS.buildNumber="100";
            PlayerSettings.iOS.targetOSVersionString="15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning=false;
            AssetDatabase.SaveAssets();
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Prototype3D/TrafficRide.unity" },
            locationPathName = output,
            target = target,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception(target + " build failed: " + report.summary.result);
        Debug.Log("Speed Rovers " + target + " build: " + Path.GetFullPath(output));
    }
}



