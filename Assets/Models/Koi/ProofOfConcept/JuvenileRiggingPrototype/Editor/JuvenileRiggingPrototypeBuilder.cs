using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class JuvenileRiggingPrototypeBuilder
{
    public const string Folder = "Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype";
    public const string MeshPath = Folder + "/Meshes/Juvenile_RiggedMesh.asset";
    public const string PrefabPath = Folder + "/Prefabs/Juvenile_RiggingPrototype.prefab";
    public const string SourcePath = "Assets/Models/Koi/Meshes/Koi_Stage1_Juvenile.fbx";
    public const string AdultPath = "Assets/Prefabs/Koi_Stage3_Adult.prefab";
    public const float DisplayScale = 0.3157f;
    private static readonly float[] JuvenileZ = { -.952f, -.70f, -.52f, -.31f, -.06f, .20f, .47f, .68f, .85f, .951f };
    private static readonly float[] AdultZ = { -.3006f, -.25f, -.22f, -.15f, -.08f, 0f, .08f, .135f, .225f, .3004f };
    private static readonly float[] Width = { .067f, .065f, .09f, .16f, .245f, .295f, .285f, .292f, .185f, .05f };
    private static readonly float[] AdultWidth = { .024f, .024f, .024f, .031f, .055f, .081f, .087f, .085f, .078f, .025f };
    public static readonly Vector3[] JointPositions = {
        new Vector3(0,0,.20f), new Vector3(0,.025f,.47f), new Vector3(0,-.035f,.70f),
        new Vector3(0,-.14f,.86f), new Vector3(-.22f,-.29f,.46f), new Vector3(.22f,-.29f,.46f),
        new Vector3(0,0,-.06f), new Vector3(0,0,-.31f), new Vector3(0,0,-.52f), new Vector3(0,0,-.70f)
    };
    [Serializable] public class Audit
    {
        public int vertices, triangles, ambiguousMatches, poorMatches;
        public float averageDistance, p95Distance, maxDistance;
        public int[] regionCounts, boneCoverage;
        public Vector3[] jointPositions;
        public string method;
    }
    private sealed class Surface
    {
        public Vector3[] v, normals, deltas;
        public BoneWeight[] weights;
        public int[] indices;
        public sealed class Node { public Bounds bounds; public Node left, right; public int[] tris; }
        public Node[] roots = new Node[5];
        public Surface(UnityEngine.Mesh m)
        {
            v=m.vertices; normals=m.normals; weights=m.boneWeights; indices=m.triangles;
            deltas=new Vector3[v.Length]; m.GetBlendShapeFrameVertices(0,0,deltas,null,null);
            var groups=Enumerable.Range(0,5).Select(_=>new List<int>()).ToArray();
            for(int t=0;t<indices.Length;t+=3)
            {
                var center=(v[indices[t]]+v[indices[t+1]]+v[indices[t+2]])/3f;
                float left=0,right=0; for(int c=0;c<3;c++){ left+=Influence(weights[indices[t+c]],4)/3f; right+=Influence(weights[indices[t+c]],5)/3f; }
                int region=left>.3f?1:right>.3f?2:center.z<-.115f?3:center.z>.225f?4:0;
                groups[region].Add(t);
                // Mouth/head and tail boundary overlap supports smooth transition correspondence.
                if(region==4 && center.z<.24f)groups[0].Add(t);
                if(region==3 && center.z>-.14f)groups[0].Add(t);
            }
            for(int r=0;r<5;r++)roots[r]=Build(groups[r].ToArray());
        }
        private Node Build(int[] tris)
        {
            if(tris.Length==0)throw new InvalidOperationException("Empty anatomical source region");
            var b=new Bounds(v[indices[tris[0]]],Vector3.zero);
            foreach(int t in tris) for(int c=0;c<3;c++)b.Encapsulate(v[indices[t+c]]);
            var node=new Node{bounds=b};
            if(tris.Length<=16){node.tris=tris;return node;}
            int axis=b.size.x>b.size.y?(b.size.x>b.size.z?0:2):(b.size.y>b.size.z?1:2);
            Array.Sort(tris,(a,bv)=>Centroid(a)[axis].CompareTo(Centroid(bv)[axis]));
            int half=tris.Length/2;node.left=Build(tris.Take(half).ToArray());node.right=Build(tris.Skip(half).ToArray());return node;
        }
        private Vector3 Centroid(int t)=> (v[indices[t]]+v[indices[t+1]]+v[indices[t+2]])/3f;
        public void Closest(int region,Vector3 p,Vector3 normal,out int tri,out Vector3 bary,out float distance,out float alignment)
        {
            int bestTri=-1;Vector3 bestBary=Vector3.zero;float best=float.MaxValue, actual=float.MaxValue, align=0;
            Search(roots[region],p,normal,ref best,ref bestTri,ref bestBary,ref actual,ref align);
            tri=bestTri;bary=bestBary;distance=Mathf.Sqrt(actual);alignment=align;
        }
        private void Search(Node node,Vector3 p,Vector3 normal,ref float best,ref int tri,ref Vector3 bary,ref float distance,ref float alignment)
        {
            if(node.bounds.SqrDistance(p)>best)return;
            if(node.tris!=null)
            {
                foreach(int t in node.tris)
                {
                    int a=indices[t],b=indices[t+1],c=indices[t+2];Vector3 bc=TriangleBary(p,v[a],v[b],v[c]);
                    var q=v[a]*bc.x+v[b]*bc.y+v[c]*bc.z;float d=(q-p).sqrMagnitude;
                    var n=(normals[a]*bc.x+normals[b]*bc.y+normals[c]*bc.z).normalized;
                    float dot=Vector3.Dot(normal,n);float score=d+Mathf.Max(0,-dot)*.000225f;
                    if(score<best){best=score;tri=t;bary=bc;distance=d;alignment=dot;}
                }
            }
            else
            {
                var first=node.left.bounds.SqrDistance(p)<node.right.bounds.SqrDistance(p)?node.left:node.right;
                var second=first==node.left?node.right:node.left;
                Search(first,p,normal,ref best,ref tri,ref bary,ref distance,ref alignment);
                Search(second,p,normal,ref best,ref tri,ref bary,ref distance,ref alignment);
            }
        }
    }
    private static Vector3 TriangleBary(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
    {
        Vector3 ab=b-a,ac=c-a,ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
        if(d1<=0&&d2<=0)return new Vector3(1,0,0);
        var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return new Vector3(0,1,0);
        float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0){float t=d1/(d1-d3);return new Vector3(1-t,t,0);}
        var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return new Vector3(0,0,1);
        float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0){float t=d2/(d2-d6);return new Vector3(1-t,0,t);}
        float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0){float t=(d4-d3)/((d4-d3)+(d5-d6));return new Vector3(0,1-t,t);}
        float denom=va+vb+vc;if(Mathf.Abs(denom)<1e-20f)return new Vector3(1,0,0);
        return new Vector3(va/denom,vb/denom,vc/denom);
    }
    public static float Influence(BoneWeight w,int bone)
    {
        return (w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)+(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
    }
    private static float Along(float z,float[] target)
    {
        int k=0;while(k<JuvenileZ.Length-2&&z>JuvenileZ[k+1])k++;
        return Mathf.Lerp(target[k],target[k+1],Mathf.InverseLerp(JuvenileZ[k],JuvenileZ[k+1],z));
    }
    public static int Region(Vector3 p)
    {
        bool fin=p.z>.10f&&p.z<.64f&&p.y<-.18f&&(Mathf.Abs(p.x)>.285f||(p.y<-.34f&&Mathf.Abs(p.x)>.205f));
        return fin?(p.x<0?1:2):p.z<-.24f?3:p.z>.85f?4:0;
    }
    private static Vector3 Map(Vector3 p,int region)
    {
        if(region==1||region==2)return new Vector3(p.x*(.1526f/.3854f),p.y*(.164f/.5f),Mathf.Lerp(.07f,.16f,Mathf.InverseLerp(.10f,.64f,p.z)));
        return new Vector3(p.x*Along(p.z,AdultWidth)/Mathf.Max(.04f,Along(p.z,Width)),p.y*.36f,Along(p.z,AdultZ));
    }
    public static GameObject Transfer()
    {
        Directory.CreateDirectory(Folder+"/Prefabs");
        var mesh=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(MeshPath);
        var sourceGo=AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        var adult=AssetDatabase.LoadAssetAtPath<GameObject>(AdultPath);
        var adultSkin=adult.GetComponentInChildren<SkinnedMeshRenderer>();
        var surface=new Surface(adultSkin.sharedMesh);
        var full=sourceGo.transform.localToWorldMatrix;var local=mesh.vertices;var localNormals=mesh.normals;
        var weights=new BoneWeight[local.Length];var delta=new Vector3[local.Length];var distances=new float[local.Length];
        var audit=new Audit{vertices=local.Length,triangles=mesh.triangles.Length/3,regionCounts=new int[5],boneCoverage=new int[10],jointPositions=JointPositions.Select(p=>p*DisplayScale).ToArray(),method="Piecewise anatomical correspondence; region-constrained BVH nearest triangle, normal penalty, barycentric adult weights; deterministic seam consistency"};
        var cache=new Dictionary<Vector3,(BoneWeight,Vector3,float,int,float)>();
        for(int i=0;i<local.Length;i++)
        {
            if(cache.TryGetValue(local[i],out var cached)){weights[i]=cached.Item1;delta[i]=cached.Item2;distances[i]=cached.Item3;audit.regionCounts[cached.Item4]++;if(cached.Item5<0)audit.ambiguousMatches++;if(cached.Item3>.04f)audit.poorMatches++;continue;}
            var p=full.MultiplyPoint3x4(local[i]);int region=Region(p);audit.regionCounts[region]++;
            var mapped=Map(p,region);var normal=full.MultiplyVector(localNormals[i]).normalized;
            surface.Closest(region,mapped,normal,out int tri,out Vector3 bary,out float dist,out float alignment);
            if(tri<0)throw new InvalidOperationException("No surface match");distances[i]=dist;if(dist>.04f)audit.poorMatches++;if(alignment<0)audit.ambiguousMatches++;
            var values=new float[10];var d=Vector3.zero;
            for(int corner=0;corner<3;corner++)
            {
                int index=surface.indices[tri+corner];float factor=bary[corner];
                for(int bone=0;bone<10;bone++)values[bone]+=Influence(surface.weights[index],bone)*factor;
                d+=surface.deltas[index]*factor;
            }
            // Reject contralateral fin leakage and body matches that captured thin fins.
            if(region==1)values[5]=0;else if(region==2)values[4]=0;else{values[4]=0;values[5]=0;}
            if(p.z<.78f)values[3]=0;
            var order=Enumerable.Range(0,10).OrderByDescending(k=>values[k]).Take(4).ToArray();float sum=order.Sum(k=>values[k]);
            if(sum<1e-8f)throw new InvalidOperationException("Surface transfer produced empty weights");
            weights[i]=new BoneWeight{boneIndex0=order[0],boneIndex1=order[1],boneIndex2=order[2],boneIndex3=order[3],weight0=values[order[0]]/sum,weight1=values[order[1]]/sum,weight2=values[order[2]]/sum,weight3=values[order[3]]/sum};
            // Transport adult lip deltas through local anatomical scaling, then localize to juvenile lips.
            float mask=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,.91f,p.z))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.04f,-.15f,p.y));
            var worldDelta=Vector3.Scale(d,new Vector3(2.4f,2.78f,1.65f))*mask;
            delta[i]=full.inverse.MultiplyVector(worldDelta);
            cache[local[i]]=(weights[i],delta[i],dist,region,alignment);
        }
        var preview=EditorSceneManager.NewPreviewScene();var root=new GameObject("Juvenile_RiggingPrototype");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
        try
        {
            var armature=UnityEngine.Object.Instantiate(adult.transform.GetChild(0).gameObject,root.transform);armature.name="Armature";
            var bones=new Transform[10];
            for(int b=0;b<10;b++){string path=AnimationUtility.CalculateTransformPath(adultSkin.bones[b],adult.transform);bones[b]=root.transform.Find(path);bones[b].position=root.transform.TransformPoint(JointPositions[b]*DisplayScale);}
            var fish=new GameObject("FishMesh");fish.transform.SetParent(root.transform,false);fish.transform.localRotation=sourceGo.transform.localRotation;fish.transform.localScale=sourceGo.transform.localScale*DisplayScale;
            var skin=fish.AddComponent<SkinnedMeshRenderer>();skin.bones=bones;skin.rootBone=bones[0];skin.sharedMesh=mesh;skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=true;
            // Root's keyed position is zero-based. Offset the full copied armature, not Root itself.
            var rootOffset=bones[0].localPosition;armature.transform.localPosition=rootOffset;bones[0].localPosition=Vector3.zero;
            mesh.boneWeights=weights;mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*fish.transform.localToWorldMatrix).ToArray();
            mesh.ClearBlendShapes();mesh.AddBlendShapeFrame("MouthOpen",100,delta,new Vector3[local.Length],new Vector3[local.Length]);
            mesh.bounds=new Bounds(mesh.bounds.center,mesh.bounds.size*1.7f);skin.localBounds=mesh.bounds;
            for(int b=0;b<10;b++)audit.boneCoverage[b]=weights.Count(w=>Influence(w,b)>1e-5f);
            var sorted=distances.OrderBy(x=>x).ToArray();audit.averageDistance=distances.Average();audit.p95Distance=sorted[(int)(sorted.Length*.95f)];audit.maxDistance=sorted.Last();
            File.WriteAllText(Folder+"/Reports/RigAudit.json",JsonUtility.ToJson(audit,true));
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();AssetDatabase.Refresh();return prefab;
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
    }
}
