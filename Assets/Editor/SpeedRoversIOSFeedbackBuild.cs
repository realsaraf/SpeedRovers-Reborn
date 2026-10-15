#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class SpeedRoversIOSFeedbackBuild
{
    [PostProcessBuild(100)]
    public static void Configure(BuildTarget target,string path)
    {
        if(target!=BuildTarget.iOS)return;
        string projectPath=PBXProject.GetPBXProjectPath(path);
        var project=new PBXProject();project.ReadFromFile(projectPath);
        string framework=project.GetUnityFrameworkTargetGuid();
        foreach(string name in new[]{"CoreHaptics.framework","UIKit.framework","AVFoundation.framework","AudioToolbox.framework"})
            project.AddFrameworkToProject(framework,name,false);
        project.WriteToFile(projectPath);
        string plistPath=Path.Combine(path,"Info.plist");
        var plist=new PlistDocument();plist.ReadFromFile(plistPath);
        plist.root.SetString("NSMotionUsageDescription","Tilt your phone to steer your motorcycle during a race.");
        plist.WriteToFile(plistPath);
    }
}
#endif
