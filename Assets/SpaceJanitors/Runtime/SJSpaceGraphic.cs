using UnityEngine;
using UnityEngine.UI;

// Editable native Canvas geometry: no external images or flattened text.
[RequireComponent(typeof(CanvasRenderer))]
public class SJSpaceGraphic : MaskableGraphic {
    protected override void OnEnable(){
        // Older saved scenes may predate the required-component declaration.
        // Ensure it exists before Graphic registers itself for input/rendering.
        if(!GetComponent<CanvasRenderer>())gameObject.AddComponent<CanvasRenderer>();
        base.OnEnable();
    }
    public enum Shape { Stars,Planet,Ring,RoundPanel }
    public Shape shape;
    public float radius=18;
    public int seed=71;
    protected override void OnPopulateMesh(VertexHelper vh){
        vh.Clear();var r=rectTransform.rect;
        if(shape==Shape.Stars){var random=new System.Random(seed);for(int i=0;i<160;i++){float x=r.xMin+(float)random.NextDouble()*r.width,y=r.yMin+(float)random.NextDouble()*r.height,s=.7f+(float)random.NextDouble()*1.7f;Quad(vh,new Rect(x,y,s,s),new Color(color.r,color.g,color.b,.2f+(float)random.NextDouble()*.6f));}return;}
        if(shape==Shape.Planet || shape==Shape.Ring){
            int count=160;float outer=Mathf.Min(r.width,r.height)*.5f;var center=r.center;
            for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                if(shape==Shape.Ring){int n=vh.currentVertCount;Add(vh,center+u*outer,color);Add(vh,center+v*outer,color);Add(vh,center+v*(outer-2),color);Add(vh,center+u*(outer-2),color);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
                else{int n=vh.currentVertCount;Add(vh,center,new Color(.025f,.09f,.14f,1));Add(vh,center+u*outer,Color.Lerp(new Color(.025f,.06f,.11f,1),color,(u.x+u.y+2)/4));Add(vh,center+v*outer,Color.Lerp(new Color(.025f,.06f,.11f,1),color,(v.x+v.y+2)/4));vh.AddTriangle(n,n+1,n+2);}
            }return;
        }
        float rad=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);int segments=12;
        Add(vh,r.center,color);
        for(int corner=0;corner<4;corner++)for(int j=0;j<=segments;j++){
            float angle=(corner*90+j*90f/segments)*Mathf.Deg2Rad;
            Vector2 c=new Vector2(corner==0||corner==3?r.xMax-rad:r.xMin+rad,corner<2?r.yMax-rad:r.yMin+rad);
            Add(vh,c+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*rad,color);
        }
        for(int i=1;i<vh.currentVertCount;i++)vh.AddTriangle(0,i,i==vh.currentVertCount-1?1:i+1);
    }
    static void Add(VertexHelper vh,Vector2 p,Color c){var v=UIVertex.simpleVert;v.position=p;v.color=c;vh.AddVert(v);}
    static void Quad(VertexHelper vh,Rect r,Color c){int n=vh.currentVertCount;Add(vh,new Vector2(r.xMin,r.yMin),c);Add(vh,new Vector2(r.xMin,r.yMax),c);Add(vh,new Vector2(r.xMax,r.yMax),c);Add(vh,new Vector2(r.xMax,r.yMin),c);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
}
