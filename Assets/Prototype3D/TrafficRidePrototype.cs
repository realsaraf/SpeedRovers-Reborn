using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Landscape race runtime. World geometry and imported vehicle assembly.
public partial class TrafficRidePrototype : MonoBehaviour
{
    public GameObject carPrefab;
    public Shader surfaceShader;
    public Shader foliageShader;
    public Texture2D cockpitArt, facadeArt, roadArt, treeArt;
    public Material skyMaterial;
    Camera view;
    Transform cockpit;
    readonly List<Transform> blocks = new List<Transform>();
    readonly List<Transform> cars = new List<Transform>();
    readonly List<Transform> wheels = new List<Transform>();
    float speed, distance, lateral, lean, impact, hitCooldown;
    bool throttle, brake, paused, capture;
    
    Material asphalt, concrete, metal, glass, paint, rubber, white, amber;
    AudioSource engine;
    float audioPhase;
    volatile float enginePitch = 55, engineVolume;

    string captureDir;
    TextMesh cockpitReadout;
    int captureStage;

    Material Mat(string name, Color color, float metallic=0, float smooth=.25f)
    {
        var m=new Material(surfaceShader); m.name=name; m.color=color;
        m.SetFloat("_Metallic",metallic); m.SetFloat("_Glossiness",smooth); return m;
    }
    GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent=null)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name;
        g.transform.SetParent(parent,false); g.transform.localPosition=position; g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=material; Destroy(g.GetComponent<Collider>()); return g;
    }
    GameObject Ball(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name=name; g.transform.SetParent(parent,false);
        g.transform.localPosition=position; g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=material; Destroy(g.GetComponent<Collider>()); return g;
    }
    void Bar(string name, Vector3 a, Vector3 b, float radius, Material material, Transform parent)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name=name; g.transform.SetParent(parent,false);
        g.transform.localPosition=(a+b)/2; g.transform.localScale=new Vector3(radius,Vector3.Distance(a,b)/2,radius);
        g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        g.GetComponent<Renderer>().sharedMaterial=material; Destroy(g.GetComponent<Collider>());
    }
    void Start()
    {
        foreach(var a in Environment.GetCommandLineArgs()) if(a.StartsWith("-ride-capture=")) {captureDir=a.Substring(14);capture=true;}
        Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
        QualitySettings.shadows=ShadowQuality.All; QualitySettings.shadowDistance=150; QualitySettings.shadowResolution=ShadowResolution.High;
        QualitySettings.antiAliasing=4;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.54f,.67f,.82f); RenderSettings.ambientEquatorColor=new Color(.44f,.47f,.50f);
        RenderSettings.ambientGroundColor=new Color(.2f,.23f,.26f);
        RenderSettings.fog=true; RenderSettings.fogColor=new Color(.66f,.75f,.82f); RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=150; RenderSettings.fogEndDistance=520;
        var sky=new Material(Shader.Find("Skybox/Procedural")); sky.SetColor("_SkyTint",new Color(.48f,.61f,.8f)); sky.SetFloat("_AtmosphereThickness",1.15f); RenderSettings.skybox=sky;
        if(skyMaterial!=null){RenderSettings.skybox=skyMaterial;DynamicGI.UpdateEnvironment();}
        var sun=new GameObject("Afternoon sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.35f;
        sun.color=new Color(1,.89f,.72f); sun.shadows=LightShadows.Soft; sun.shadowBias=.035f; sun.transform.rotation=Quaternion.Euler(38,-38,0); RenderSettings.sun=sun;
        asphalt=Mat("Fine asphalt",new Color(.22f,.25f,.28f),0,.12f);
        var tex=new Texture2D(128,128); var pixels=new Color[16384]; var random=new System.Random(9);
        for(int i=0;i<pixels.Length;i++){float c=.72f+(float)random.NextDouble()*.35f;pixels[i]=new Color(c,c,c);}
        tex.SetPixels(pixels);tex.Apply();tex.wrapMode=TextureWrapMode.Repeat;asphalt.mainTexture=roadArt!=null?roadArt:tex;asphalt.mainTextureScale=new Vector2(3,16);
        asphalt.color=new Color(.7f,.72f,.75f);
        concrete=Mat("Limestone",new Color(.65f,.63f,.57f)); metal=Mat("Brushed alloy",new Color(.25f,.28f,.31f),.75f,.65f);
        glass=Mat("Blue architectural glass",new Color(.17f,.32f,.43f),.5f,.75f); paint=Mat("Motorcycle petrol blue",new Color(.025f,.35f,.46f),.65f,.7f);
        rubber=Mat("Rubber and gloves",new Color(.035f,.043f,.055f)); white=Mat("Road paint",new Color(.9f,.88f,.77f));
        amber=Mat("Warm lamps",new Color(1,.7f,.25f)); amber.EnableKeyword("_EMISSION"); amber.SetColor("_EmissionColor",new Color(1,.65f,.2f)*2);
        view=new GameObject("Rider eye camera").AddComponent<Camera>();view.transform.position=new Vector3(1.85f,1.55f,0);
        view.fieldOfView=75;view.nearClipPlane=.04f;view.farClipPlane=650;view.backgroundColor=RenderSettings.fogColor;view.tag="MainCamera";
        view.gameObject.AddComponent<AudioListener>();
        for(int i=0;i<9;i++) CreateBlock(i*80-80,i);
        CreateCockpit();
        if(carPrefab==null) {Debug.LogError("Prototype car asset missing");enabled=false;return;}
        for(int i=0;i<3;i++) CreateCar(i);
        engine=gameObject.AddComponent<AudioSource>();engine.loop=true;engine.spatialBlend=0;
        InitializeRaceGame();
    }
    void EngineAudio(float[] data)
    {
        float hz=enginePitch,vol=engineVolume;
        for(int i=0;i<data.Length;i++){audioPhase+=hz/44100f;audioPhase-=Mathf.Floor(audioPhase);float p=audioPhase*6.283185f;
            data[i]=(Mathf.Sin(p)+.3f*Mathf.Sin(p*2)+.12f*Mathf.Sin(p*4))*vol;}
    }
    void CreateBlock(float z,int index)
    {
        var root=new GameObject("City block "+index).transform;root.position=new Vector3(0,0,z);blocks.Add(root);
        Box("Road",new Vector3(0,-.12f,40),new Vector3(15,.24f,80),asphalt,root);
        for(int s=-1;s<=1;s+=2)
        {
            Box("Raised sidewalk",new Vector3(s*9,-.02f,40),new Vector3(3,.3f,80),concrete,root);
            Box("Curb",new Vector3(s*7.6f,.12f,40),new Vector3(.2f,.26f,80),white,root);
            Box("Road edge",new Vector3(s*7.2f,.012f,40),new Vector3(.12f,.02f,80),white,root);
            for(int k=0;k<8;k++)
            {
                float bz=k*10+5; float h=32+(index+k)%5*5;
                var facade=Mat("Textured architecture",Color.white,0,.18f);
                facade.mainTexture=facadeArt;facade.mainTextureScale=new Vector2(.25f,1);facade.mainTextureOffset=new Vector2(((index+k+(s+1))%4)*.25f,0);
                Box("Building",new Vector3(s*15,h/2,bz),new Vector3(9,h,10),facade,root);
                Box("Roof cornice",new Vector3(s*15,h,bz),new Vector3(9.5f,.35f,10),concrete,root);
                Box("Masonry plinth",new Vector3(s*10.45f,.18f,bz),new Vector3(.15f,.35f,10),concrete,root);
                if(k%2!=0)continue;
                Bar("Street lamp",new Vector3(s*8.6f,.15f,bz),new Vector3(s*8.6f,6,bz),.13f,metal,root);
                Bar("Inward lamp arm",new Vector3(s*8.6f,6,bz),new Vector3(s*6.6f,6.2f,bz),.1f,metal,root);
                Box("Lamp head",new Vector3(s*6.6f,6.2f,bz),new Vector3(.55f,.14f,1),amber,root);
                var foliage=new Material(foliageShader);foliage.mainTexture=treeArt;
                for(int q=0;q<2;q++){
                    var tree=GameObject.CreatePrimitive(PrimitiveType.Quad);tree.name="Leafy street tree";tree.transform.SetParent(root,false);
                    tree.transform.localPosition=new Vector3(s*9.2f,4.2f,bz+8);tree.transform.localScale=new Vector3(5.3f,8,1);
                    tree.transform.localRotation=Quaternion.Euler(0,q*90,0);tree.GetComponent<Renderer>().sharedMaterial=foliage;Destroy(tree.GetComponent<Collider>());
                }
                Box("Tree planter",new Vector3(s*9.2f,.3f,bz+8),new Vector3(1.7f,.5f,1.7f),concrete,root);
            }
        }
        for(int k=0;k<10;k++) foreach(float x in new[]{-3.65f,3.65f})
            Box("Lane dash",new Vector3(x,.015f,k*8+2),new Vector3(.13f,.025f,3.5f),white,root);
        foreach(float x in new[]{-.12f,.12f}) Box("Center line",new Vector3(x,.016f,40),new Vector3(.09f,.025f,80),amber,root);
        RememberBlock(root);
    }
    void CreateCockpit()
    {
        cockpit=new GameObject("Cockpit").transform;cockpit.SetParent(view.transform,false);
        if(cockpitArt!=null)return;
        cockpit.localPosition=new Vector3(0,-.02f,.45f);
        // Cockpit geometry is temporary; can be replaced by layered art or an authored model.
        Ball("Fuel tank",new Vector3(0,-.70f,.38f),new Vector3(.62f,.6f,.9f),paint,cockpit);
        Box("Instrument cluster",new Vector3(0,-.32f,.66f),new Vector3(.36f,.2f,.08f),rubber,cockpit).transform.localRotation=Quaternion.Euler(16,0,0);
        Box("Instrument face",new Vector3(0,-.312f,.61f),new Vector3(.29f,.13f,.015f),glass,cockpit).transform.localRotation=Quaternion.Euler(16,0,0);
        var text=new GameObject("Digital speedometer");text.transform.SetParent(cockpit,false);text.transform.localPosition=new Vector3(0,-.31f,.591f);
        text.transform.localRotation=Quaternion.Euler(16,0,0);text.transform.localScale=Vector3.one*.008f;
        cockpitReadout=text.AddComponent<TextMesh>();cockpitReadout.fontSize=64;cockpitReadout.characterSize=1f;cockpitReadout.anchor=TextAnchor.MiddleCenter;cockpitReadout.color=new Color(.35f,1,1);
        for(int s=-1;s<=1;s+=2)
        {
            Bar("Handlebar",new Vector3(s*.08f,-.4f,.55f),new Vector3(s*.58f,-.39f,.38f),.045f,metal,cockpit);
            Bar("Grip",new Vector3(s*.46f,-.39f,.38f),new Vector3(s*.65f,-.41f,.34f),.075f,rubber,cockpit);
            Ball("Gloved hand",new Vector3(s*.54f,-.41f,.32f),new Vector3(.16f,.12f,.20f),rubber,cockpit);
            Bar("Jacket sleeve",new Vector3(s*.56f,-.45f,.27f),new Vector3(s*.78f,-.67f,-.04f),.18f,rubber,cockpit);
            Bar("Brake lever",new Vector3(s*.42f,-.36f,.43f),new Vector3(s*.66f,-.36f,.41f),.018f,metal,cockpit);
            Bar("Mirror stalk",new Vector3(s*.42f,-.35f,.49f),new Vector3(s*.64f,-.08f,.65f),.018f,metal,cockpit);
            Ball("Mirror housing",new Vector3(s*.65f,-.07f,.65f),new Vector3(.27f,.15f,.05f),rubber,cockpit);
            Ball("Mirror glass",new Vector3(s*.65f,-.07f,.622f),new Vector3(.245f,.125f,.012f),glass,cockpit);
        }
    }
    void CreateCar(int i)
    {
        var root=new GameObject("Traffic "+i).transform;
        var model=Instantiate(carPrefab,root);model.name="Concept car";
        Bounds b=new Bounds();bool first=true;
        foreach(var r in model.GetComponentsInChildren<Renderer>()){if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}
        float scale=4.5f/Mathf.Max(b.size.z,b.size.x);model.transform.localScale=Vector3.one*scale;
        model.transform.localPosition=new Vector3(-b.center.x*scale,-b.min.y*scale,-b.center.z*scale);
        // glTF car's longitudinal axis is Z; oncoming cars rotate the complete object.
        root.position=new Vector3(i%3==0?-1.85f:(i%2==0?5.5f:1.85f),0,26+i*38);
        if(i%3==0)root.rotation=Quaternion.Euler(0,180,0);
        cars.Add(root);
        foreach(var r in model.GetComponentsInChildren<Renderer>())
        {
            var mats=r.sharedMaterials;
            for(int m=0;m<mats.Length;m++)if(mats[m]!=null&&mats[m].name.Contains("Paint 1"))
            {var copy=new Material(mats[m]);var colors=new[]{new Color(.7f,.025f,.015f),new Color(.08f,.26f,.48f),new Color(.65f,.69f,.72f)};
                if(copy.HasProperty("baseColorFactor"))copy.SetColor("baseColorFactor",colors[i%3]);if(copy.HasProperty("_BaseColor"))copy.SetColor("_BaseColor",colors[i%3]);if(copy.HasProperty("_Color"))copy.color=colors[i%3];mats[m]=copy;}
            r.sharedMaterials=mats;
        }
        foreach(var t in model.GetComponentsInChildren<Transform>())
            if(t.name=="WheelFrontL"||t.name=="WheelFrontR"||t.name=="WheelRearL"||t.name=="WheelRearR")
            {
                var renderers=t.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                var wb=renderers[0].bounds;foreach(var r in renderers)wb.Encapsulate(r.bounds);
                var pivot=new GameObject("Wheel axle").transform;pivot.SetParent(root,false);pivot.position=wb.center;
                t.SetParent(pivot,true);wheels.Add(pivot);
            }
        Debug.Log("Traffic imported bounds "+b.size+" scale "+scale);
    }
}
