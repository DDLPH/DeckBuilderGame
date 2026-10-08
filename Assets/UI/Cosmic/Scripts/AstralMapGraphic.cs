using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Resolution-independent ornaments and silhouettes for the cosmic map.
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AstralMapGraphic : MaskableGraphic
{
    public enum Motif { Frame, Divider, NodeRing, Swords, Skull, Flame, Pouch, Eclipse, Question, Heart, Coin, Crescent, Chevron }
    public Motif motif;
    public float emphasis;
    public Color detailColor = new Color(0.012f, 0.008f, 0.026f, 1f);
    private VertexHelper mesh;
    private float unit;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        mesh = vh;
        unit = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        switch (motif)
        {
            case Motif.Frame: Frame(); break;
            case Motif.Divider: Divider(); break;
            case Motif.NodeRing: NodeRing(); break;
            case Motif.Swords: Swords(); break;
            case Motif.Skull: Skull(); break;
            case Motif.Flame: Flame(); break;
            case Motif.Pouch: Pouch(); break;
            case Motif.Eclipse: Eclipse(); break;
            case Motif.Question: Question(); break;
            case Motif.Heart: Heart(); break;
            case Motif.Coin: Coin(); break;
            case Motif.Crescent: Crescent(Vector2.zero, unit * 0.67f); break;
            case Motif.Chevron: Line(P(.27f,.52f), P(-.27f,0), unit*.13f,color); Line(P(-.27f,0),P(.27f,-.52f),unit*.13f,color); break;
        }
    }

    private Vector2 P(float x, float y) => new Vector2(x, y) * unit;
    private Color Fade(Color c, float alpha) { c.a *= alpha; return c; }
    private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int n = mesh.currentVertCount;
        mesh.AddVert(a, tint, Vector2.zero); mesh.AddVert(b, tint, Vector2.zero);
        mesh.AddVert(c, tint, Vector2.zero); mesh.AddVert(d, tint, Vector2.zero);
        mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
    }
    private void Line(Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 normal = new Vector2(-(b-a).y, (b-a).x).normalized * width * .5f;
        Quad(a-normal,a+normal,b+normal,b-normal,tint);
    }
    private void Ring(Vector2 center, float rx, float ry, float width, Color tint, float from=0f, float to=360f)
    {
        int segments = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(to-from)/4f));
        Vector2 previous = center + new Vector2(Mathf.Cos(from*Mathf.Deg2Rad)*rx,Mathf.Sin(from*Mathf.Deg2Rad)*ry);
        for(int i=1;i<=segments;i++)
        {
            float a=Mathf.Lerp(from,to,(float)i/segments)*Mathf.Deg2Rad;
            Vector2 next=center+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry);
            Line(previous,next,width,tint); previous=next;
        }
    }
    private void Disc(Vector2 center, float radius, Color tint, int segments=80)
    {
        int n=mesh.currentVertCount; mesh.AddVert(center,tint,Vector2.zero);
        for(int i=0;i<=segments;i++)
        {
            float angle=i*Mathf.PI*2/segments;
            mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint,Vector2.zero);
            if(i>0) mesh.AddTriangle(n,n+i,n+i+1);
        }
    }
    private void Halo(float radius, float spread, Color tint)
    {
        int n=mesh.currentVertCount;
        for(int i=0;i<=96;i++)
        {
            float a=i*Mathf.PI*2/96;
            Vector2 dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            mesh.AddVert(dir*radius,tint,Vector2.zero);
            mesh.AddVert(dir*(radius+spread),Fade(tint,0),Vector2.zero);
            if(i>0) { int k=n+i*2; mesh.AddTriangle(k-2,k-1,k); mesh.AddTriangle(k-1,k+1,k); }
        }
    }
    private void Diamond(Vector2 center,float radius,Color tint,bool filled=true)
    {
        Vector2[] p={center+Vector2.up*radius,center+Vector2.right*radius*.65f,center+Vector2.down*radius,center+Vector2.left*radius*.65f};
        if(filled) Poly(p,tint); else for(int i=0;i<4;i++)Line(p[i],p[(i+1)%4],1.2f,tint);
    }
    private void Shape(Color tint, params Vector2[] normalized)
    {
        Vector2[] points=new Vector2[normalized.Length];
        for(int i=0;i<points.Length;i++)points[i]=normalized[i]*unit;
        Poly(points,tint);
    }
    private void Poly(Vector2[] points,Color tint)
    {
        // Ear clipping handles the concave silhouettes without filling cutouts.
        List<int> indexes=new List<int>();
        float area=0;
        for(int i=0;i<points.Length;i++)area+=Cross(points[i],points[(i+1)%points.Length]);
        for(int i=0;i<points.Length;i++)indexes.Add(area>0?i:points.Length-1-i);
        int baseIndex=mesh.currentVertCount;
        foreach(Vector2 p in points)mesh.AddVert(p,tint,Vector2.zero);
        int guard=points.Length*points.Length;
        while(indexes.Count>2 && guard-->0)
        {
            bool clipped=false;
            for(int i=0;i<indexes.Count;i++)
            {
                int a=indexes[(i+indexes.Count-1)%indexes.Count],b=indexes[i],c=indexes[(i+1)%indexes.Count];
                if(Cross(points[b]-points[a],points[c]-points[b])<=.001f)continue;
                bool contains=false;
                foreach(int j in indexes)
                {
                    if(j==a||j==b||j==c)continue;
                    Vector2 p=points[j];
                    if(Cross(points[b]-points[a],p-points[a])>=0 && Cross(points[c]-points[b],p-points[b])>=0 && Cross(points[a]-points[c],p-points[c])>=0){contains=true;break;}
                }
                if(contains)continue;
                mesh.AddTriangle(baseIndex+a,baseIndex+b,baseIndex+c);indexes.RemoveAt(i);clipped=true;break;
            }
            if(!clipped)break;
        }
    }
    private float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;

    private void NodeRing()
    {
        float r=unit*.78f;
        Halo(r,unit*.21f,Fade(color,.12f+emphasis*.55f));
        Disc(Vector2.zero,r,detailColor);
        Ring(Vector2.zero,r,r,1.8f,color);
        Ring(Vector2.zero,r-unit*.05f,r-unit*.05f,1f,Fade(color,.40f));
        Ring(Vector2.zero,r+unit*.035f,r+unit*.035f,.75f,Fade(color,.4f+emphasis*.3f));
        if(emphasis>.5f)
        {
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                Line(d*(r+unit*.03f),d*(r+unit*.23f),1f,Fade(color,emphasis));
            }
        }
    }
    private void Eclipse()
    {
        float r=unit*.55f;
        Color warm=new Color(1f,.83f,.76f,color.a);
        Halo(r,unit*.38f,Fade(color,.18f));
        Halo(r,unit*.18f,Fade(warm,.35f));
        Halo(r,unit*.085f,Fade(warm,.78f));
        for(int i=0;i<36;i++)
        {
            float a=i*Mathf.PI*2/36;
            Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),n=new Vector2(-d.y,d.x);
            float length=unit*(.11f+.09f*(.5f+.5f*Mathf.Sin(i*2.7f)));
            Poly(new[]{d*(r-.01f*unit)-n*unit*.021f,d*(r+length)+n*unit*.018f,d*(r-.01f*unit)+n*unit*.021f},Fade(warm,.44f));
        }
        Disc(Vector2.zero,r,detailColor);
        Ring(Vector2.zero,r,r,2.3f,warm);
        Ring(Vector2.zero,r-unit*.017f,r-unit*.017f,1f,Fade(color,.8f));
    }
    private void Swords()
    {
        for(int side=-1;side<=1;side+=2)
        {
            Vector2 tip=P(side*.57f,.67f),baseBlade=P(-side*.32f,-.37f);
            Vector2 d=(tip-baseBlade).normalized,n=new Vector2(-d.y,d.x);
            Poly(new[]{baseBlade-n*unit*.10f,tip-d*unit*.20f-n*unit*.07f,tip,tip-d*unit*.20f+n*unit*.07f,baseBlade+n*unit*.10f},color);
            Vector2 guard=baseBlade-d*unit*.025f;
            Line(guard-n*unit*.23f,guard+n*unit*.23f,unit*.065f,color);
            Line(guard-d*unit*.04f,guard-d*unit*.29f,unit*.07f,color);
            Disc(guard-d*unit*.30f,unit*.065f,color,16);
            Line(baseBlade,tip-d*unit*.17f,unit*.016f,Fade(detailColor,.40f));
        }
    }
    private void Skull()
    {
        Shape(color,new Vector2(-.45f,.36f),new Vector2(-.33f,.55f),new Vector2(0,.64f),new Vector2(.33f,.55f),new Vector2(.45f,.36f),new Vector2(.41f,-.04f),new Vector2(.25f,-.22f),new Vector2(.23f,-.53f),new Vector2(-.23f,-.53f),new Vector2(-.25f,-.22f),new Vector2(-.41f,-.04f));
        Shape(color,new Vector2(-.34f,.48f),new Vector2(-.61f,.59f),new Vector2(-.70f,.87f),new Vector2(-.66f,.41f),new Vector2(-.47f,.21f));
        Shape(color,new Vector2(.34f,.48f),new Vector2(.61f,.59f),new Vector2(.70f,.87f),new Vector2(.66f,.41f),new Vector2(.47f,.21f));
        Shape(detailColor,new Vector2(-.33f,.19f),new Vector2(-.08f,.10f),new Vector2(-.14f,-.04f),new Vector2(-.31f,-.04f));
        Shape(detailColor,new Vector2(.33f,.19f),new Vector2(.08f,.10f),new Vector2(.14f,-.04f),new Vector2(.31f,-.04f));
        Shape(detailColor,new Vector2(0,.04f),new Vector2(-.07f,-.16f),new Vector2(.07f,-.16f));
        for(int i=-1;i<=1;i++)Line(P(i*.105f,-.29f),P(i*.105f,-.54f),unit*.035f,detailColor);
    }
    private void Flame()
    {
        Shape(color,new Vector2(0,.89f),new Vector2(.24f,.53f),new Vector2(.22f,.19f),new Vector2(.42f,.36f),new Vector2(.62f,-.05f),new Vector2(.55f,-.43f),new Vector2(.24f,-.64f),new Vector2(-.21f,-.63f),new Vector2(-.53f,-.42f),new Vector2(-.60f,-.09f),new Vector2(-.34f,.42f),new Vector2(-.29f,.03f),new Vector2(-.08f,.35f));
        Shape(detailColor,new Vector2(.05f,.17f),new Vector2(.29f,-.17f),new Vector2(.21f,-.54f),new Vector2(-.13f,-.62f),new Vector2(-.25f,-.34f),new Vector2(-.08f,-.01f),new Vector2(-.08f,-.34f));
    }
    private void Pouch()
    {
        Shape(color,new Vector2(-.20f,.45f),new Vector2(.20f,.45f),new Vector2(.44f,.03f),new Vector2(.52f,-.40f),new Vector2(.39f,-.59f),new Vector2(0,-.67f),new Vector2(-.39f,-.59f),new Vector2(-.52f,-.40f),new Vector2(-.44f,.03f));
        Shape(color,new Vector2(-.22f,.52f),new Vector2(-.36f,.76f),new Vector2(-.13f,.72f),new Vector2(0,.83f),new Vector2(.13f,.72f),new Vector2(.36f,.76f),new Vector2(.22f,.52f));
        Line(P(-.29f,.46f),P(.29f,.46f),unit*.04f,detailColor);
        Line(P(-.20f,.48f),P(-.41f,.20f),unit*.025f,color);
        Line(P(.20f,.48f),P(.41f,.20f),unit*.025f,color);
        Ring(P(0,-.20f),unit*.42f,unit*.42f,unit*.019f,Fade(detailColor,.5f),195,345);
    }
    private void Question()
    {
        Ring(P(-.01f,.32f),unit*.29f,unit*.29f,unit*.105f,color,-20,185);
        Line(P(.26f,.23f),P(.02f,-.08f),unit*.105f,color);
        Line(P(.02f,-.08f),P(.02f,-.28f),unit*.105f,color);
        Disc(P(.02f,-.53f),unit*.073f,color,24);
    }
    private void Heart()
    {
        Shape(color,new Vector2(0,-.64f),new Vector2(-.62f,-.02f),new Vector2(-.68f,.31f),new Vector2(-.52f,.56f),new Vector2(-.26f,.60f),new Vector2(0,.37f),new Vector2(.26f,.60f),new Vector2(.52f,.56f),new Vector2(.68f,.31f),new Vector2(.62f,-.02f));
    }
    private void Coin()
    {
        Disc(Vector2.zero,unit*.76f,Fade(color,.48f));
        Ring(Vector2.zero,unit*.76f,unit*.76f,unit*.07f,color);
        Ring(Vector2.zero,unit*.57f,unit*.57f,unit*.03f,color);
        Shape(color,new Vector2(-.08f,.40f),new Vector2(.24f,.23f),new Vector2(.18f,-.31f),new Vector2(-.18f,-.40f),new Vector2(-.32f,.16f));
        Line(P(-.08f,.40f),P(-.18f,-.40f),unit*.027f,detailColor);
    }
    private void Crescent(Vector2 center,float r)
    {
        List<Vector2> p=new List<Vector2>();
        for(int i=0;i<=50;i++) { float a=(60+i*240f/50)*Mathf.Deg2Rad;p.Add(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r); }
        for(int i=0;i<=50;i++) { float a=(270-i*180f/50)*Mathf.Deg2Rad;p.Add(center+new Vector2(r*.50f,0)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*.87f); }
        Poly(p.ToArray(),color);
    }
    private void Divider()
    {
        Rect r=rectTransform.rect;float cy=r.center.y;
        Line(new Vector2(r.xMin,cy),new Vector2(-20,cy),1f,Fade(color,.65f));
        Line(new Vector2(20,cy),new Vector2(r.xMax,cy),1f,Fade(color,.65f));
        Diamond(new Vector2(0,cy),9,color,false);
        Diamond(new Vector2(-32,cy),3.5f,Fade(color,.8f));Diamond(new Vector2(32,cy),3.5f,Fade(color,.8f));
        Line(new Vector2(0,cy-15),new Vector2(0,cy+15),.8f,Fade(color,.6f));
    }
    private void Frame()
    {
        Rect r=rectTransform.rect;float l=r.xMin+4,rr=r.xMax-4,b=r.yMin+4,t=r.yMax-4;
        Vector2[] p={new Vector2(l+9,b),new Vector2(rr-9,b),new Vector2(rr,b+9),new Vector2(rr,t-9),new Vector2(rr-9,t),new Vector2(l+9,t),new Vector2(l,t-9),new Vector2(l,b+9)};
        for(int i=0;i<p.Length;i++)Line(p[i],p[(i+1)%p.Length],1.35f,color);
        Line(new Vector2(l+7,b+7),new Vector2(rr-7,b+7),.7f,Fade(color,.65f));
        Line(new Vector2(l+7,t-7),new Vector2(rr-7,t-7),.7f,Fade(color,.65f));
        Line(new Vector2(l+7,b+7),new Vector2(l+7,t-7),.7f,Fade(color,.65f));
        Line(new Vector2(rr-7,b+7),new Vector2(rr-7,t-7),.7f,Fade(color,.65f));
        foreach(Vector2 corner in new[]{new Vector2(l,b),new Vector2(rr,b),new Vector2(l,t),new Vector2(rr,t)})
        {
            float sx=corner.x<0?1:-1,sy=corner.y<0?1:-1;
            Line(corner,new Vector2(corner.x+sx*28,corner.y),.9f,color);
            Line(corner,new Vector2(corner.x,corner.y+sy*28),.9f,color);
            Line(corner,new Vector2(corner.x+sx*19,corner.y+sy*19),.9f,Fade(color,.8f));
            float a= sx>0?(sy>0?0:270):(sy>0?90:180);
            Ring(corner,24,24,.9f,Fade(color,.65f),a,a+90);
        }
        if(r.height>170)
        {
            Ring(Vector2.zero,r.width*.43f,r.height*.46f,.7f,Fade(color,.17f));
            Diamond(new Vector2(0,t),8,color);
            Line(new Vector2(0,t-36),new Vector2(0,t+12),.8f,Fade(color,.65f));
            Vector2 emblem=new Vector2(0,b+1);
            Disc(emblem,26,detailColor);Ring(emblem,28,28,1,color);
            Crescent(emblem,16);
            Line(emblem+Vector2.down*40,emblem+Vector2.down*21,.8f,color);
            Diamond(emblem+Vector2.down*34,4,color);
        }
    }
}
