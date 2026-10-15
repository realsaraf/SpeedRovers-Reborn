using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Store artifacts are separate from mobile exports and can live on a spacious drive.
public static class DesktopStoreBuild
{
    public static void Windows() => Build(BuildTarget.StandaloneWindows64);
    public static void Mac() => Build(BuildTarget.StandaloneOSX);

    static void Build(BuildTarget target)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            throw new InvalidOperationException("Missing Unity module: " + target);
        var macBuildNumber = target == BuildTarget.StandaloneOSX
            ? DesktopStoreMacMetadata.ResolveBuildNumber(Environment.GetEnvironmentVariable("SPEED_ROVERS_MAC_BUILD_NUMBER"))
            : null;
        TrafficRideBuild.Prepare();
        PlayerSettings.companyName = "Mossberry";
        PlayerSettings.productName = "Speed Rovers 2";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.philino.speedrovers.reborn.desktop");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.allowFullscreenSwitch = false; // Runtime handles shortcuts once and saves the choice.
        PlayerSettings.runInBackground = false;
        PlayerSettings.usePlayerLog = true;
        PlayerSettings.useMacAppStoreValidation = target == BuildTarget.StandaloneOSX;
        if (target == BuildTarget.StandaloneOSX)
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.x64ARM64;
        var root = Environment.GetEnvironmentVariable("SPEED_ROVERS_DESKTOP_OUTPUT");
        if (string.IsNullOrWhiteSpace(root)) root = Path.GetFullPath("Builds/DesktopStore");
        var output = Path.Combine(root, target == BuildTarget.StandaloneOSX ? "macOS/Speed Rovers 2.app" : "Windows/Speed Rovers 2.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        AssetDatabase.SaveAssets();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Prototype3D/TrafficRide.unity" },
            locationPathName = output, target = target, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Desktop Store build failed: " + report.summary.result);
        if (target == BuildTarget.StandaloneOSX)
            DesktopStoreMacMetadata.StampBuildNumber(Path.Combine(output, "Contents/Info.plist"), macBuildNumber);
        Debug.Log("DESKTOP STORE BUILD SUCCEEDED: " + output);
    }
}

// This metadata step uses the emitted XML plist because this installed Unity
// version has no verified public macOS buildNumber API. Distribution signing
// follows this change and seals the final plist contents.
public static class DesktopStoreMacMetadata
{
    public static string ResolveBuildNumber(string configured)
    {
        var value = string.IsNullOrWhiteSpace(configured) ? "1" : configured.Trim();
        if (!Regex.IsMatch(value, @"\A[1-9][0-9]{0,3}(?:\.[0-9]{1,2}){0,2}\z"))
            throw new ArgumentException("SPEED_ROVERS_MAC_BUILD_NUMBER must be a positive Mac build number, such as 1 or 124.");
        return value;
    }

    public static void StampBuildNumber(string plistPath, string buildNumber)
    {
        var value = ResolveBuildNumber(buildNumber);
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
        using (var reader = XmlReader.Create(plistPath, readerSettings))
            document.Load(reader);
        var keys = document.SelectNodes("/plist/dict/key[text()='CFBundleVersion']");
        if (keys == null || keys.Count != 1)
            throw new InvalidDataException("Expected exactly one CFBundleVersion in the generated Mac Info.plist.");
        var node = keys[0].NextSibling;
        while (node != null && node.NodeType != XmlNodeType.Element)
            node = node.NextSibling;
        if (node == null || node.Name != "string")
            throw new InvalidDataException("Expected a string value for CFBundleVersion in the generated Mac Info.plist.");
        node.InnerText = value;
        var temporary = plistPath + ".desktop-build.tmp";
        var writerSettings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), NewLineHandling = NewLineHandling.None };
        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
        using (var writer = XmlWriter.Create(stream, writerSettings))
            document.Save(writer);
        File.Replace(temporary, plistPath, null);
    }
}
