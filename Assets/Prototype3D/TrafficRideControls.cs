using UnityEngine;

public partial class TrafficRidePrototype
{
    static readonly Rect BrakeControl = new Rect(28, 500, 194, 194);
    static readonly Rect ThrottleControl = new Rect(1058, 500, 194, 194);
    static readonly Rect JumpControl = new Rect(252, 558, 132, 132);
    static readonly Rect NitroControl = new Rect(904, 526, 132, 164);
    static readonly Rect PauseControl = new Rect(1192,18,66,84);
    readonly System.Collections.Generic.Dictionary<int,GUIStyle> controlStyles = new System.Collections.Generic.Dictionary<int,GUIStyle>();
    GUIStyle ControlStyle(int size){if(!controlStyles.TryGetValue(size,out var style)){style=new GUIStyle(bodyStyle){fontSize=size,alignment=TextAnchor.MiddleCenter};controlStyles[size]=style;}return style;}
    Texture2D controlDisc, controlRim, controlGlow;
    readonly Color brakeAccent = new Color(1f, .48f, .25f);

    void PrepareRideControlArt()
    {
        if (controlDisc != null) return;
        controlDisc = MakeControlTexture(0); controlRim = MakeControlTexture(1); controlGlow = MakeControlTexture(2);
    }
    Texture2D MakeControlTexture(int kind)
    {
        const int size=256; var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float nx=(x+.5f-size*.5f)/(size*.5f), ny=(y+.5f-size*.5f)/(size*.5f), r=Mathf.Sqrt(nx*nx+ny*ny);
            float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.97f,1,r));
            if(kind==0){float light=.085f+(ny+1)*.025f;pixels[y*size+x]=new Color(light,light*1.35f,light*1.65f,edge*.9f);}
            else if(kind==1)pixels[y*size+x]=new Color(1,1,1,edge*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,.94f,r)));
            else pixels[y*size+x]=new Color(1,1,1,Mathf.Exp(-r*r*3.8f)*edge*.34f);
        }
        tex.SetPixels(pixels);tex.Apply(false,true);tex.wrapMode=TextureWrapMode.Clamp;return tex;
    }
    void ControlTexture(Texture2D texture,Rect rect,Color color)
    { var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,texture);GUI.color=old; }
    void ControlLine(Vector2 a,Vector2 b,float thickness,Color color)
    {
        var old=GUI.matrix;var d=b-a;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);
        Fill(new Rect(0,-thickness/2,d.magnitude,thickness),color);GUI.matrix=old;
    }
    void ControlArc(Vector2 center,float radius,float amount,Color color)
    {
        const int segments=32;
        for(int i=0;i<segments;i++){
            float angle=(135+i*270f/segments)*Mathf.Deg2Rad;
            Vector2 a=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
            Vector2 b=center+new Vector2(Mathf.Cos(angle+.085f),Mathf.Sin(angle+.085f))*radius;
            ControlLine(a,b,3.5f,i/(float)segments<amount?color:new Color(.45f,.6f,.67f,.2f));
        }
    }
    void ControlDial(Rect rect,Color color,bool pressed,float charge)
    {
        var glow=new Rect(rect.x-10,rect.y-10,rect.width+20,rect.height+20);
        ControlTexture(controlGlow,glow,new Color(color.r,color.g,color.b,pressed?1:.32f));
        ControlTexture(controlDisc,rect,Color.white);
        ControlTexture(controlRim,rect,new Color(color.r,color.g,color.b,pressed?1:.68f));
        ControlArc(rect.center,rect.width*.42f,charge,pressed?Color.white:color);
        if(pressed)ControlTexture(controlRim,new Rect(rect.x+8,rect.y+8,rect.width-16,rect.height-16),new Color(color.r,color.g,color.b,.45f));
    }
    static readonly Vector2[] GripOutline={new Vector2(-65,9),new Vector2(-57,-9),new Vector2(12,5),new Vector2(19,21),new Vector2(9,32),new Vector2(-65,16),new Vector2(-65,9)};
    static readonly Vector2[] GripHousing={new Vector2(17,-16),new Vector2(44,-9),new Vector2(40,32),new Vector2(20,28),new Vector2(17,-16)};
    static readonly Vector2[][] GripParts={GripOutline,GripHousing};
    Vector2 GripPoint(Vector2 origin,Vector2 point,bool right)=>origin+new Vector2(point.x*(right?-1:1),point.y);
    void GripStroke(Vector2 a,Vector2 b,Color tint,bool pressed)
    {
        ControlLine(a,b,pressed?12:8,new Color(tint.r,tint.g,tint.b,pressed?.12f:.055f));
        ControlLine(a,b,2.3f,tint);
    }
    void DrawGripControl(Rect rect,Color color,bool pressed,bool right)
    {
        Vector2 origin=new Vector2(rect.center.x,rect.y+82);
        Color stroke=pressed?Color.Lerp(color,Color.white,.55f):new Color(.91f,.97f,1,.72f);
        ControlTexture(controlGlow,new Rect(rect.x-20,rect.y-8,rect.width+40,rect.height),new Color(color.r,color.g,color.b,pressed?1:.32f));
        foreach(var points in GripParts)for(int i=1;i<points.Length;i++)
            GripStroke(GripPoint(origin,points[i-1],right),GripPoint(origin,points[i],right),stroke,pressed);
        for(int i=0;i<6;i++){
            float x=-51+i*10;var a=new Vector2(x,-4+(x+51)*.2f);var b=a+new Vector2(-5,18);
            ControlLine(GripPoint(origin,a,right),GripPoint(origin,b,right),1.8f,new Color(stroke.r,stroke.g,stroke.b,stroke.a*.65f));
        }
        // The curved lever follows the grip when pressed, like squeezing a real control.
        Vector2 p0=new Vector2(30,-13),p1=new Vector2(13,-39),p2=new Vector2(-42,-27),p3=new Vector2(-82,-51+(pressed?13:0));
        Vector2 previous=GripPoint(origin,p0,right);
        for(int i=1;i<=22;i++){
            float t=i/22f,u=1-t;Vector2 p=u*u*u*p0+3*u*u*t*p1+3*u*t*t*p2+t*t*t*p3;
            Vector2 next=GripPoint(origin,p,right);GripStroke(previous,next,stroke,pressed);previous=next;
        }
        if(right){
            // A short twist cue, not an enclosing button ring.
            Vector2 pivot=origin+new Vector2(16,6);Vector2 prev=pivot+new Vector2(0,-32);
            for(int i=1;i<=12;i++){float a=(-90+i*8)*Mathf.Deg2Rad;Vector2 next=pivot+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*32;ControlLine(prev,next,2.3f,pressed?Color.white:color);prev=next;}
            ControlLine(prev,prev+new Vector2(-8,-3),2.3f,pressed?Color.white:color);
        }
        Text(right?"THROTTLE":"BRAKE",rect.x-5,rect.y+135,rect.width+10,26,ControlStyle(22),new Color(1,1,1,.84f));
        string hint=pressed?(right?"ACCELERATING":"BRAKING"):right?(tiltControl?"HOLD + TILT":"HOLD + DRAG"):"HOLD";
        Text(hint,rect.x-12,rect.y+163,rect.width+24,22,ControlStyle(18),new Color(color.r,color.g,color.b,pressed?1:.68f));
    }
    void DrawRideControls()
    {
        PrepareRideControlArt();
        DrawGripControl(BrakeControl,brakeAccent,brake,false);
        DrawGripControl(ThrottleControl,accent,throttle,true);

        bool jumpReady=jumpCooldown<=0&&jump<=.01f&&speed>=15;
        var j=JumpControl.center;
        Color jc=jumpReady?Color.white:muted;
        ControlTexture(controlGlow,JumpControl,new Color(accent.r,accent.g,accent.b,jump>.01f?.65f:.12f));
        if(!jumpReady)ControlArc(j,JumpControl.width*.38f,1-Mathf.Clamp01(jumpCooldown/1.2f),new Color(.85f,.94f,1,.3f));
        ControlLine(new Vector2(j.x,j.y-2),new Vector2(j.x,j.y-31),4,jc);
        ControlLine(new Vector2(j.x-12,j.y-19),new Vector2(j.x,j.y-31),4,jc);
        ControlLine(new Vector2(j.x+12,j.y-19),new Vector2(j.x,j.y-31),4,jc);
        Text("JUMP",j.x-50,j.y+6,100,30,ControlStyle(22),jc);

        bool ready=nitro>=.5f&&!boosting&&fuel>0;
        Rect nr=new Rect(NitroControl.x,NitroControl.y+16,NitroControl.width,NitroControl.width);
        Color nc=ready||boosting?accent:muted;
        ControlTexture(controlGlow,nr,new Color(nc.r,nc.g,nc.b,boosting?.9f:ready?.35f:.12f));
        var n=nr.center;
        // A shaded cylinder with a valve, shoulders, charge window and 50% threshold.
        Fill(new Rect(n.x-12,n.y-48,24,5),new Color(.8f,.87f,.9f));
        Fill(new Rect(n.x-7,n.y-43,14,8),metal.color);
        for(int x=-20;x<=20;x++){float shade=.25f+.6f*Mathf.Pow(1-Mathf.Abs(x/21f),.6f);Fill(new Rect(n.x+x,n.y-33,1,54),new Color(shade*.8f,shade*.9f,shade));}
        Fill(new Rect(n.x-14,n.y-29,28,44),new Color(.025f,.08f,.1f));
        Fill(new Rect(n.x-12,n.y+13-40*nitro,24,40*nitro),boosting?Color.white:accent);
        Fill(new Rect(n.x-16,n.y-7,32,2),new Color(1,1,1,.65f));
        Text(boosting?"BOOST":"NITRO",n.x-56,n.y+24,112,27,ControlStyle(20),nc);
        Text(ready?"READY":boosting?"ACTIVE":Mathf.RoundToInt(nitro*100)+"% / 50%",NitroControl.x-10,NitroControl.y+147,152,18,ControlStyle(18),nc);
        if(!Application.isMobilePlatform){
            if(GUI.Button(NitroControl,GUIContent.none,GUIStyle.none))ActivateNitro();
            if(GUI.Button(JumpControl,GUIContent.none,GUIStyle.none))JumpBike();
        }
    }
    void ProcessRideTouch(int fingerId,Vector2 position,TouchPhase phase)
    {
        if(phase==TouchPhase.Ended||phase==TouchPhase.Canceled){touchStarts.Remove(fingerId);return;}
        if(phase==TouchPhase.Began){touchStarts[fingerId]=position;int action=ControlAt(position);if(action==2)ActivateNitro();else if(action==3)JumpBike();else if(action==4){SetPage(Page.Pause);return;}}
        if(!touchStarts.TryGetValue(fingerId,out var start))return;
        int control=ControlAt(start);
        if(control==1){throttle=true;if(!tiltControl)steerInput=Mathf.Clamp((position.x-start.x)/(Screen.width*.1f),-1,1);}
        else if(control==-1)brake=true;
    }
}
