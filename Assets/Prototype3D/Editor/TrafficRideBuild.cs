using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using System;
public static class TrafficRideBuild
{
    public static void Prepare()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Speed Rovers 2").AddComponent<TrafficRidePrototype>();
        root.carPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototype3D/Models/CarConcept.glb");
        root.surfaceShader=Shader.Find("Standard");root.waterShader=Shader.Find("SpeedRovers/RiverSurface");
        foreach(var path in new[]{"Assets/Prototype3D/Art/Cockpit.png","Assets/Prototype3D/Art/Facades.png"}){
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
            importer.alphaIsTransparency=path.Contains("Cockpit");importer.mipmapEnabled=!path.Contains("Cockpit");
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
        foreach(var asset in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/GrandTour/Audio"})){
            var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(asset));var settings=importer.defaultSampleSettings;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.75f;
            settings.loadType=importer.assetPath.Contains("Score")?AudioClipLoadType.Streaming:AudioClipLoadType.CompressedInMemory;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
        var pine=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/GrandTour/Pine.png");pine.alphaIsTransparency=true;pine.mipmapEnabled=true;pine.maxTextureSize=1024;pine.SaveAndReimport();
        root.cockpitArt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Prototype3D/Art/Cockpit.png");
        root.facadeArt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Prototype3D/Art/Facades.png");
        root.roadArt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Painted/CityAsphalt.png");
        root.treeArt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Painted/DayTree.png");
        root.foliageShader=Shader.Find("CockpitLab/StreetFoliage");
        const string skyPath="Assets/Prototype3D/Art/DaySky.mat";
        var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if(sky==null){sky=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(sky,skyPath);}
        sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Prototype3D/Art/DaySky.hdr"));
        sky.SetFloat("_Exposure",.75f);sky.SetFloat("_Rotation",50);EditorUtility.SetDirty(sky);root.skyMaterial=sky;
        AssetDatabase.SaveAssets();
        if(root.carPrefab==null)throw new Exception("glTF import failed");
        EditorSceneManager.SaveScene(scene,"Assets/Prototype3D/TrafficRide.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Prototype3D/TrafficRide.unity",true)};
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
        PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        PlayerSettings.productName="Speed Rovers 2";PlayerSettings.bundleVersion="2.1.0";
        AssetDatabase.SaveAssets();
    }
    public static void Build()
    {
        Prepare();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Prototype3D/TrafficRide.unity"},
            locationPathName="Builds/Landscape/Speed Rovers 2.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Cockpit build failed: "+report.summary.result);
        Debug.Log("LANDSCAPE GAME BUILD SUCCEEDED");
    }
}
