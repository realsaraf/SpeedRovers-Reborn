using System;
using System.Collections.Generic;
using UnityEngine;

public partial class TrafficRidePrototype
{
    [Serializable] public class CourseBook { public Course[] routes; }
    [Serializable] public class Course { public string name; public float length; public CourseSection[] sections; public CoursePoint[] points; }
    [Serializable] public class CourseSection { public string name; public float start,light,rain,cloud; public int theme; }
    [Serializable] public class CoursePoint { public float z,x,y; }
    sealed class OriginalTransform { public Transform t; public Vector3 p,s; public Quaternion q; public Mesh mesh; }
    readonly Dictionary<Transform,List<OriginalTransform>> blockOriginals=new Dictionary<Transform,List<OriginalTransform>>();
    readonly Dictionary<Transform,Vector3> logicalPositions=new Dictionary<Transform,Vector3>();
    CourseBook courseBook;
    Course CourseNow=>courseBook.routes[Mathf.Clamp(route,0,courseBook.routes.Length-1)];
    float health=1,cleanTime,rainAmount,cloudAmount,healthPulse;
    bool crashResult;
    Material stoneMaterial,waterMaterial,towerGlass,nightGlass,terrainMaterial,pineMaterial;
    public Shader waterShader;
    ParticleSystem rainParticles;
    readonly string[] racerNames={"NOVA","BLAZE","GHOST"};
    void InitGrandTour()
    {
        courseBook=JsonUtility.FromJson<CourseBook>(Resources.Load<TextAsset>("GrandTour/courses").text);
        stoneMaterial=Mat("Cut stone",new Color(.37f,.39f,.4f),0,.17f);
        terrainMaterial=new Material(Resources.Load<Shader>("GrandTour/RockSurface"));terrainMaterial.color=new Color(.85f,.82f,.76f);
        var rock=Resources.Load<Texture2D>("GrandTour/RockDiffuse");if(rock!=null)terrainMaterial.mainTexture=rock;
        pineMaterial=new Material(foliageShader);pineMaterial.mainTexture=Resources.Load<Texture2D>("GrandTour/Pine");if(pineMaterial.mainTexture==null)pineMaterial.mainTexture=treeArt;
        stoneMaterial=new Material(terrainMaterial);stoneMaterial.color=new Color(.65f,.68f,.7f);stoneMaterial.mainTexture=rock;
        towerGlass=Mat("Reflective tower glazing",new Color(.57f,.68f,.73f),.3f,.5f);towerGlass.mainTexture=facadeArt;towerGlass.mainTextureScale=new Vector2(.25f,1);
        nightGlass=Mat("Warm office lighting",new Color(.7f,.55f,.27f),.2f,.65f);nightGlass.EnableKeyword("_EMISSION");nightGlass.SetColor("_EmissionColor",new Color(.6f,.34f,.12f)*.8f);
        waterMaterial=waterShader!=null?new Material(waterShader):riverMaterial;
        var rainGO=new GameObject("Weather rain");rainGO.transform.SetParent(view.transform,false);rainGO.transform.localPosition=new Vector3(0,12,20);
        rainParticles=rainGO.AddComponent<ParticleSystem>();var main=rainParticles.main;main.loop=true;main.startLifetime=1.2f;main.startSpeed=0;main.startSize=.025f;main.maxParticles=1000;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var shape=rainParticles.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(45,2,55);
        var velocity=rainParticles.velocityOverLifetime;velocity.enabled=true;velocity.y=-25;velocity.z=-12;
        var emission=rainParticles.emission;emission.rateOverTime=0;
        var render=rainGO.GetComponent<ParticleSystemRenderer>();render.renderMode=ParticleSystemRenderMode.Stretch;render.lengthScale=5;render.velocityScale=.06f;render.sharedMaterial=white;
        engine.Stop();engine.clip=Resources.Load<AudioClip>("GrandTour/Audio/Engine");engine.Play();
    }
    CourseSection SectionAt(float at)
    {
        var sections=CourseNow.sections;for(int i=sections.Length-1;i>=0;i--)if(at>=sections[i].start)return sections[i];return sections[0];
    }
    Vector2 CourseOffset(float at)
    {
        var points=CourseNow.points;at=Mathf.Clamp(at,0,CourseNow.length);
        for(int i=1;i<points.Length;i++)if(at<=points[i].z){var a=points[i-1];var b=points[i];float t=Mathf.InverseLerp(a.z,b.z,at);t=t*t*(3-2*t);return new Vector2(Mathf.Lerp(a.x,b.x,t),Mathf.Lerp(a.y,b.y,t));}
        return new Vector2(points[points.Length-1].x,points[points.Length-1].y);
    }
    float CourseYaw(float at)=>Mathf.Atan2(CourseOffset(at+2).x-CourseOffset(at-2).x,4)*Mathf.Rad2Deg;
    float CoursePitch(float at)=>-Mathf.Atan2(CourseOffset(at+2).y-CourseOffset(at-2).y,4)*Mathf.Rad2Deg;
    void RememberBlock(Transform root)
    {
        var list=new List<OriginalTransform>();foreach(Transform t in root){var f=t.GetComponent<MeshFilter>();list.Add(new OriginalTransform{t=t,p=t.localPosition,s=t.localScale,q=t.localRotation,mesh=f!=null?f.sharedMesh:null});}blockOriginals[root]=list;
    }
    void RestoreLogicalWorld()
    {
        foreach(var pair in logicalPositions)if(pair.Key!=null)pair.Key.position=pair.Value;logicalPositions.Clear();
        foreach(var block in blocks)block.position=new Vector3(0,0,block.position.z);
    }
    void ProjectActor(Transform t,bool oncoming=false)
    {
        logicalPositions[t]=t.position;var offset=CourseOffset(distance+t.position.z);var origin=CourseOffset(distance);
        t.position+=new Vector3(offset.x-origin.x,offset.y,0);t.rotation=Quaternion.Euler(CoursePitch(distance+logicalPositions[t].z),CourseYaw(distance+logicalPositions[t].z)+(oncoming?180:0),0);
    }
    void ProjectCourseWorld()
    {
        var origin=CourseOffset(distance);foreach(var block in blocks)block.position=new Vector3(-origin.x,0,block.position.z);
        for(int i=0;i<cars.Count;i++)ProjectActor(cars[i],i%3==0);
        foreach(var r in racers)ProjectActor(r.root);
        foreach(var p in pickups){ProjectActor(p.root);p.root.Rotate(0,p.kind==PickupKind.Coin?Time.time*120:Mathf.Sin(Time.time*2)*8,0,Space.Self);}
        foreach(var h in hazards)ProjectActor(h);
        view.transform.position=new Vector3(PlayerX,1.55f+origin.y+jump,0);
        view.transform.rotation=Quaternion.Euler(CoursePitch(distance)+impact*2+(brake?1:0),CourseYaw(distance)+steerInput,lean*.25f);
    }
    void BendSlab(OriginalTransform o,float at)
    {
        const int count=17;var vertices=new Vector3[count*4];var uv=new Vector2[vertices.Length];var indices=new List<int>();float start=o.p.z-o.s.z/2;
        for(int k=0;k<count;k++){
            float z=start+o.s.z*k/(count-1);var offset=CourseOffset(at+z);
            for(int c=0;c<4;c++){vertices[k*4+c]=new Vector3(o.p.x+(c%2==0?-.5f:.5f)*o.s.x+offset.x,o.p.y+(c<2?.5f:-.5f)*o.s.y+offset.y,z);uv[k*4+c]=new Vector2(c%2,z/80);}
            if(k==0)continue;int a=(k-1)*4,b=k*4;
            foreach(var q in new[]{new[]{a,b,b+1,a+1},new[]{a+2,a+3,b+3,b+2},new[]{a,a+2,b+2,b},new[]{a+1,b+1,b+3,a+3}})indices.AddRange(new[]{q[0],q[1],q[2],q[0],q[2],q[3]});
        }
        var filter=o.t.GetComponent<MeshFilter>();if(filter.sharedMesh!=o.mesh)Destroy(filter.sharedMesh);
        var mesh=new Mesh{name="Continuous course slab"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();filter.sharedMesh=mesh;
        o.t.localPosition=Vector3.zero;o.t.localRotation=Quaternion.identity;o.t.localScale=Vector3.one;
    }
    void GrandConfigure(Transform block,float at)
    {
        int theme=SectionAt(Mathf.Max(0,at)).theme;if(!blockOriginals.TryGetValue(block,out var originals))return;
        foreach(var o in originals){
            o.t.localPosition=o.p;o.t.localScale=o.s;o.t.localRotation=o.q;o.t.gameObject.SetActive(true);string name=o.t.name;
            if(name=="Building"||name=="Roof cornice"||name=="Masonry plinth")o.t.gameObject.SetActive(theme==0||theme==1);
            if(name=="Leafy street tree"||name=="Tree planter")o.t.gameObject.SetActive(theme==0||theme==6);
            if(name=="Road"||name=="Raised sidewalk"||name=="Curb"||name=="Road edge"||name=="Center line"){BendSlab(o,at);continue;}
            var offset=CourseOffset(at+o.p.z);o.t.localPosition+=new Vector3(offset.x,offset.y,0);o.t.localRotation=Quaternion.Euler(CoursePitch(at+o.p.z),CourseYaw(at+o.p.z),0)*o.q;
            if(name=="Building"&&theme==1){o.t.localScale=new Vector3(o.s.x,o.s.y*(1.3f+Mathf.Abs(Mathf.Sin((at+o.p.z)*.04f))*1.4f),o.s.z);o.t.localPosition+=Vector3.up*(o.t.localScale.y-o.s.y)/2;}
            if(name=="Roof cornice"&&theme==1)o.t.gameObject.SetActive(false);
            if(theme==5&&(name=="Street lamp"||name=="Inward lamp arm"||name=="Lamp head"))o.t.gameObject.SetActive(false);
        }
        var old=block.Find("District scenery");if(old!=null)Destroy(old.gameObject);
        var scenery=new GameObject("District scenery").transform;scenery.SetParent(block,false);
        if(theme==0||theme==1)CityDetail(scenery,at,theme);
        if(theme==2||theme==3)Waterfront(scenery,at,theme);
        if(theme==4||theme==6)Mountains(scenery,at,theme);
        if(theme==5)Tunnel(scenery,at);
        foreach(Transform t in scenery){
            if(t.name=="Mountain terrain"||t.name.StartsWith("World "))continue;
            if(t.name=="Tunnel ceiling"||t.name=="Tunnel wall"){BendSlab(new OriginalTransform{t=t,p=t.localPosition,s=t.localScale,q=t.localRotation,mesh=t.GetComponent<MeshFilter>().sharedMesh},at);t.gameObject.AddComponent<GrandTourMeshLifetime>();continue;}
            var p=t.localPosition;var offset=CourseOffset(at+p.z);t.localPosition+=new Vector3(offset.x,offset.y,0);t.localRotation=Quaternion.Euler(0,CourseYaw(at+p.z),0)*t.localRotation;
        }
    }
    void CityDetail(Transform root,float at,int theme)
    {
        for(int side=-1;side<=1;side+=2){for(int k=0;k<4;k++){
            float z=10+k*20,variant=Mathf.Abs(Mathf.Sin((at+z)*.05f));
            Box("Store canopy",new Vector3(side*10.3f,3.5f,z),new Vector3(2.8f,.22f,6),k%2==0?paint:rubber,root);
            Box("Store glass",new Vector3(side*10.38f,1.7f,z),new Vector3(.12f,2.7f,5.6f),towerGlass,root);
            for(int m=0;m<4;m++)Box("Store window frame",new Vector3(side*10.27f,1.7f,z-2.4f+m*1.6f),new Vector3(.15f,2.7f,.06f),metal,root);
            Box("Shop fascia",new Vector3(side*10.2f,3,z),new Vector3(.18f,.25f,5),amber,root);
            if(theme==1){float h=55+variant*45;Box("Skyline tower",new Vector3(side*(42+variant*35),h/2,z),new Vector3(14,h,14),towerGlass,root);
                for(int f=0;f<8;f++)Box("Office ribbon",new Vector3(side*(42+variant*35),6+f*6,z),new Vector3(14.2f,.22f,14.2f),nightGlass,root);}
        }Box("Bench",new Vector3(side*9,.6f,35),new Vector3(.8f,.15f,2.6f),stoneMaterial,root);}
    }
    Vector3 OnCourse(Vector3 p,float at){var o=CourseOffset(at+p.z);return p+new Vector3(o.x,o.y,0);}
    void CourseBar(string name,Vector3 a,Vector3 b,float radius,Transform root,float at){Bar("World "+name,OnCourse(a,at),OnCourse(b,at),radius,metal,root);}
    float CableHeight(float at)=>7+14*Mathf.Pow(Mathf.Cos(at*Mathf.PI/400),2);
    void Waterfront(Transform root,float at,int theme)
    {
        Box("World river",new Vector3(0,-5,40),new Vector3(1000,.16f,80),waterMaterial,root);
        for(int side=-1;side<=1;side+=2){
            for(int k=0;k<8;k++){float z=k*10;
                CourseBar("rail",new Vector3(side*10.2f,1.1f,z),new Vector3(side*10.2f,1.1f,z+10),.10f,root,at);
                CourseBar("support",new Vector3(side*10.2f,0,z),new Vector3(side*10.2f,1.1f,z),.08f,root,at);
                if(theme==2){CourseBar("cable",new Vector3(side*10,CableHeight(at+z),z),new Vector3(side*10,CableHeight(at+z+10),z+10),.16f,root,at);
                    CourseBar("hanger",new Vector3(side*10,.2f,z),new Vector3(side*10,CableHeight(at+z),z),.05f,root,at);}}
            if(theme==2&&Mathf.RoundToInt(at/80)%5==0){Box("Bridge tower",new Vector3(side*10,13,0),new Vector3(1.3f,26,2.8f),stoneMaterial,root);Box("Foundation",new Vector3(side*10,-3,0),new Vector3(3,6,6),stoneMaterial,root);}
            if(theme==3)for(int k=0;k<2;k++){float z=20+k*40,h=25+Mathf.Abs(Mathf.Sin((at+z)*.013f))*30;
                float x=side*(110+Mathf.Abs(Mathf.Sin((at+z)*.021f))*65);
                Box("Waterfront skyline",new Vector3(x,h/2,z),new Vector3(18,h,24),towerGlass,root);
                Box("Waterfront roof",new Vector3(x,h,z),new Vector3(19,.7f,25),stoneMaterial,root);
                Box("Shoreline foundation",new Vector3(x,-3,z),new Vector3(50,5,40),stoneMaterial,root);
                for(int f=1;f<h/4;f++)Box("Facade floor band",new Vector3(x,f*4,z),new Vector3(18.15f,.18f,24.15f),metal,root);
            }
        }
        if(theme==2&&Mathf.RoundToInt(at/80)%5==0)Box("Tower crossbeam",new Vector3(0,23,0),new Vector3(21,1.6f,2.8f),stoneMaterial,root);
    }
    void Mountains(Transform root,float at,int theme)
    {
        for(int side=-1;side<=1;side+=2){
            var mesh=new Mesh{name="Authored terrain patch"};const int nx=30,nz=21;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var tris=new List<int>();
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){float across=x*7,along=z*4;float alongRoute=at+along;float height=(.25f+Mathf.Pow(Mathf.Sin(alongRoute*.004f+across*.018f),2)*.5f+Mathf.Sin(alongRoute*.017f+across*.12f)*.08f)*across;
                height+=Mathf.SmoothStep(0,1,across/30)*(Mathf.PerlinNoise(alongRoute*.033f,across*.07f)*14+Mathf.PerlinNoise(alongRoute*.11f,across*.19f)*5);
                v[z*nx+x]=OnCourse(new Vector3(side*(11+across),height-.5f,along),at);uv[z*nx+x]=new Vector2(across/10,(at+along)/10);
                if(x>0&&z>0){int a=z*nx+x;tris.AddRange(side>0?new[]{a,a-nx,a-nx-1,a,a-nx-1,a-1}:new[]{a,a-nx-1,a-nx,a,a-1,a-nx-1});}}
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=tris.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();var g=new GameObject("Mountain terrain");g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=terrainMaterial;g.AddComponent<GrandTourMeshLifetime>();
            if(theme==6)for(int k=0;k<12;k++){
                float z=k*6.5f,x=side*(12+(k%4)*2.5f),height=9+Mathf.Abs(Mathf.Sin(at+k))*5;
                for(int q=0;q<2;q++){var tree=GameObject.CreatePrimitive(PrimitiveType.Quad);tree.name="Alpine spruce";tree.transform.SetParent(root,false);
                    tree.transform.localPosition=new Vector3(x,height/2,z);tree.transform.localScale=new Vector3(height*.5f,height,1);tree.transform.localRotation=Quaternion.Euler(0,q*90+k*23,0);
                    tree.GetComponent<Renderer>().sharedMaterial=pineMaterial;Destroy(tree.GetComponent<Collider>());}
            }
            for(int k=0;k<8;k++)CourseBar("mountain guardrail",new Vector3(side*8,.9f,k*10),new Vector3(side*8,.9f,k*10+10),.15f,root,at);
        }
    }
    void Tunnel(Transform root,float at)
    {
        Box("Tunnel ceiling",new Vector3(0,8,40),new Vector3(21,1,80),stoneMaterial,root);
        for(int side=-1;side<=1;side+=2){Box("Tunnel wall",new Vector3(side*10,4,40),new Vector3(1,8,80),stoneMaterial,root);
            for(int f=0;f<8;f++){float z=f*10;Box("Tunnel lining seam",new Vector3(side*9.45f,4,z),new Vector3(.1f,8,.08f),metal,root);Box("Tunnel safety stripe",new Vector3(side*9.43f,1.8f,z+5),new Vector3(.12f,.3f,9.8f),white,root);}
            
            for(int k=0;k<8;k++){Box("Tunnel light",new Vector3(side*7,7.4f,k*10),new Vector3(.5f,.1f,3),amber,root);Box("Wall reflector",new Vector3(side*9.4f,1,k*10),new Vector3(.08f,.15f,.6f),amber,root);}}
    }
    void DrawSpeedAir()
    {
        float strength=Mathf.Clamp01((speed-180)/330);if(strength<=0)return;
        for(int i=0;i<32;i++){
            float phase=Mathf.Repeat(animationTime*(.8f+speed/350)+i*.137f,1);
            Vector2 edge=new Vector2(i%2==0?-150:1430,130+(i*83)%540),center=new Vector2(640,310);
            Vector2 p=Vector2.Lerp(center,edge,.67f+phase*.33f),direction=(edge-center).normalized;
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg,p);
            Fill(new Rect(p.x,p.y,18+phase*45,1.2f),new Color(.85f,.95f,1,strength*phase*.3f));GUI.matrix=matrix;
        }
    }
    void DrawGrandTourHUD()
    {
        Fill(new Rect(26,112,220,49),ink);Meter("BIKE "+Mathf.CeilToInt(health*100)+"%"+(cleanTime>5&&health<1?" REPAIRING":""),health,36,115,200,health<.3f?new Color(1,.3f,.15f):accent);
        var safe=Screen.safeArea;float scale=Mathf.Min(safe.width/1280f,safe.height/720f),left=safe.x+(safe.width-1280*scale)*.5f,top=Screen.height-safe.yMax+(safe.height-720*scale)*.5f;
        int closest=-1;float best=220;for(int n=0;n<racers.Count;n++){float gap= racers[n].progress-distance;if(gap>0&&gap<best){best=gap;closest=n;}}
        for(int i=0;i<racers.Count;i++){
            var r=racers[i];float gap=r.progress-distance;var screen=view.WorldToScreenPoint(r.root.position+Vector3.up*2.1f);
            if(i==closest&&screen.z>0&&Mathf.Abs(gap)<220){
                float x=(screen.x-left)/scale,y=(Screen.height-screen.y-top)/scale;
                if(x>100&&x<1180&&y>140&&y<500)
                    Text(Mathf.Abs(gap).ToString("0")+" m",x-25,y-34,50,20,new GUIStyle(tinyStyle){fontSize=14,alignment=TextAnchor.MiddleCenter},new Color(.85f,.97f,1,.58f));
            }
            Text(racerNames[i]+" "+(gap>=0?"+":"-")+Mathf.Abs(gap).ToString("0")+" M",1045,115+i*24,220,24,tinyStyle,liveries[i]);
        }
        if(health<.3f)Text("CRITICAL DAMAGE / RIDE CLEAN TO REPAIR",430,180,600,30,bodyStyle,new Color(1,.4f,.2f));
    }
    void AdvanceGrandTour(float dt)
    {
        cleanTime+=dt;healthPulse=Mathf.MoveTowards(healthPulse,0,dt*2);if(cleanTime>5&&health>0)health=Mathf.Min(1,health+dt*.022f);
        var s=SectionAt(distance);var secs=CourseNow.sections;int index=Array.IndexOf(secs,s);var prev=secs[Mathf.Max(0,index-1)];float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(s.start,s.start+650,distance));
        float light=Mathf.Lerp(prev.light,s.light,blend);rainAmount=Mathf.Lerp(prev.rain,s.rain,blend);cloudAmount=Mathf.Lerp(prev.cloud,s.cloud,blend);
        RenderSettings.sun.intensity=light;RenderSettings.sun.color=Color.Lerp(new Color(1,.88f,.7f),new Color(.65f,.76f,.9f),cloudAmount);
        RenderSettings.sun.transform.rotation=Quaternion.Euler(Mathf.Lerp(38,13,distance/CourseNow.length),-38+distance/CourseNow.length*65,0);
        RenderSettings.ambientIntensity=Mathf.Lerp(1,.6f,cloudAmount);RenderSettings.fogColor=Color.Lerp(new Color(.63f,.73f,.82f),new Color(.32f,.39f,.46f),cloudAmount);
        RenderSettings.fogStartDistance=Mathf.Lerp(200,80,rainAmount);RenderSettings.fogEndDistance=Mathf.Lerp(650,300,rainAmount);
        if(RenderSettings.skybox!=null)RenderSettings.skybox.SetFloat("_Exposure",Mathf.Lerp(.75f,.3f,cloudAmount));
        asphalt.SetFloat("_Glossiness",Mathf.Lerp(.18f,.83f,rainAmount));asphalt.SetFloat("_Metallic",rainAmount*.32f);
        var emission=rainParticles.emission;emission.rateOverTime=rainAmount*650;
        engine.pitch=.65f+(speed%85)/85*.75f+gear*.09f;engine.volume=throttle?.26f:.15f;
    }
}
public sealed class GrandTourMeshLifetime:MonoBehaviour
{
    void OnDestroy(){var f=GetComponent<MeshFilter>();if(f!=null&&f.sharedMesh!=null)Destroy(f.sharedMesh);}
}
