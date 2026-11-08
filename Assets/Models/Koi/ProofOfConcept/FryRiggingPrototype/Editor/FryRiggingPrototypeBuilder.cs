using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FryRiggingPrototypeBuilder
{
    public const string Folder = "Assets/Models/Koi/ProofOfConcept/FryRiggingPrototype";
    public const string Source = "Assets/Models/Koi/Materials/fry texture/Meshy_AI_stylized_cute_fish_20261024_image-to-3d-texture";
    public const string Adult = "Assets/Prefabs/Koi_Stage3_Adult.prefab";
    public const string Juvenile = "Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Prefabs/Juvenile_RiggingPrototype.prefab";
    public const string MeshPath = Folder + "/Meshes/Fry_RiggedMesh.asset";
    public const string PrefabPath = Folder + "/Prefabs/Fry_RiggingPrototype.prefab";
    public const float InternalScale = .3157f;
    // Independent full-import fry landmarks, not copied juvenile constants.
    public static readonly Vector3[] Joints = {
        new Vector3(0,0,.18f), new Vector3(0,.01f,.40f), new Vector3(0,-.015f,.65f),
        new Vector3(0,-.115f,.925f), new Vector3(-.345f,-.36f,.47f), new Vector3(.345f,-.36f,.47f),
        new Vector3(0,0,-.05f), new Vector3(0,0,-.34f), new Vector3(0,0,-.57f), new Vector3(0,0,-.74f)
    };
    private static readonly float[] FryZ = { -.952f,-.74f,-.57f,-.34f,-.05f,.18f,.40f,.65f,.86f,.951f };
    private static readonly float[] AdultZ = { -.3006f,-.25f,-.22f,-.15f,-.08f,0,.08f,.135f,.225f,.3004f };
    private static readonly float[] FryWidth = { .055f,.075f,.14f,.27f,.38f,.46f,.46f,.42f,.27f,.045f };
    private static readonly float[] AdultWidth = { .024f,.024f,.024f,.031f,.055f,.081f,.087f,.085f,.078f,.025f };

    public static GameObject Prepare()
    {
        Directory.CreateDirectory(Folder + "/Meshes"); Directory.CreateDirectory(Folder + "/Prefabs");
        if (File.Exists(MeshPath) || File.Exists(PrefabPath)) throw new InvalidOperationException("Prepare is one-shot; preserve existing prototype.");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + ".fbx");
        var filter = source.GetComponentInChildren<MeshFilter>();
        var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh); mesh.name = "Fry_RiggedMesh";
        AssetDatabase.CreateAsset(mesh, MeshPath);
        var preview = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Fry_RiggingPrototype"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
        try
        {
            var adultSkin = AssetDatabase.LoadAssetAtPath<GameObject>(Adult).GetComponentInChildren<SkinnedMeshRenderer>();
            var armature = UnityEngine.Object.Instantiate(adultSkin.transform.parent.Find("Armature").gameObject, root.transform); armature.name = "Armature";
            var bones = new Transform[10];
            for (int i=0;i<10;i++) { bones[i] = root.transform.Find(AnimationUtility.CalculateTransformPath(adultSkin.bones[i], adultSkin.transform.parent)); bones[i].position = Joints[i]*InternalScale; }
            armature.transform.localPosition = bones[0].localPosition; bones[0].localPosition = Vector3.zero;
            var fish = new GameObject("FishMesh"); fish.transform.SetParent(root.transform,false);
            fish.transform.localRotation = filter.transform.localRotation; fish.transform.localScale = filter.transform.localScale * InternalScale;
            var skin = fish.AddComponent<SkinnedMeshRenderer>(); skin.sharedMesh=mesh; skin.bones=bones; skin.rootBone=bones[0]; skin.quality=SkinQuality.Bone4; skin.updateWhenOffscreen=true;
            mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*fish.transform.localToWorldMatrix).ToArray();
            // Rest-length measurement uses untouched source geometry and renderer import mapping.
            var points=mesh.vertices.Select(v=>fish.transform.TransformPoint(v)).ToArray();
            float length=points.Max(v=>v.z)-points.Min(v=>v.z);
            var juvenileSkin=AssetDatabase.LoadAssetAtPath<GameObject>(Juvenile).GetComponentInChildren<SkinnedMeshRenderer>();
            var jp=juvenileSkin.sharedMesh.vertices.Select(v=>juvenileSkin.transform.TransformPoint(v)).ToArray();
            float juvenileLength=jp.Max(v=>v.z)-jp.Min(v=>v.z);
            root.transform.localScale=Vector3.one*(juvenileLength*.5f/length);
            skin.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Fry_AlbedoOnly.mat");
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            File.WriteAllText(Folder+"/Reports/Scale.txt","JuvenileLength="+juvenileLength+"\nFryLength="+(length*root.transform.localScale.x)+"\nRatio=0.5\nInternalScale="+InternalScale+"\nOuterScale="+root.transform.localScale.x+"\nConvention=full fin-inclusive Z extent; body-only length not independently segmented");
            AssetDatabase.SaveAssets(); return prefab;
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
    public static float Influence(BoneWeight w,int i) => (w.boneIndex0==i?w.weight0:0)+(w.boneIndex1==i?w.weight1:0)+(w.boneIndex2==i?w.weight2:0)+(w.boneIndex3==i?w.weight3:0);
    private static BoneWeight Pack(float[] v)
    {
        var ids=Enumerable.Range(0,10).OrderByDescending(i=>v[i]).Take(4).ToArray(); float sum=ids.Sum(i=>v[i]);
        if(sum<1e-8f)throw new InvalidOperationException("Empty transfer weight");
        return new BoneWeight{boneIndex0=ids[0],boneIndex1=ids[1],boneIndex2=ids[2],boneIndex3=ids[3],weight0=v[ids[0]]/sum,weight1=v[ids[1]]/sum,weight2=v[ids[2]]/sum,weight3=v[ids[3]]/sum};
    }
    public static int Region(Vector3 p)
    {
        bool fin = p.z>.25f && p.z<.67f && p.y<-.30f && Mathf.Abs(p.x)>.345f;
        return fin?(p.x<0?1:2):p.z<-.24f?3:p.z>.86f?4:0;
    }
    private static float Along(float z,float[] target)
    {
        int k=0; while(k<FryZ.Length-2&&z>FryZ[k+1])k++;
        return Mathf.Lerp(target[k],target[k+1],Mathf.InverseLerp(FryZ[k],FryZ[k+1],z));
    }
    private static Vector3 Map(Vector3 p,int region)
    {
        if(region==1||region==2)return new Vector3(p.x*.32f,p.y*.30f,Mathf.Lerp(.07f,.16f,Mathf.InverseLerp(.25f,.67f,p.z)));
        return new Vector3(p.x*Along(p.z,AdultWidth)/Mathf.Max(.035f,Along(p.z,FryWidth)),p.y*.30f,Along(p.z,AdultZ));
    }
    private sealed class Surface
    {
        public Vector3[] v,n; public int[] indices; public BoneWeight[] weights;
        private sealed class Node { public Bounds bounds; public Node left,right; public int[] tris; }
        private Node[] roots=new Node[5];
        public Surface(Mesh mesh)
        {
            v=mesh.vertices;n=mesh.normals;indices=mesh.triangles;weights=mesh.boneWeights;
            var groups=Enumerable.Range(0,5).Select(_=>new List<int>()).ToArray();
            for(int t=0;t<indices.Length;t+=3){var p=(v[indices[t]]+v[indices[t+1]]+v[indices[t+2]])/3;float l=0,r=0;for(int c=0;c<3;c++){l+=Influence(weights[indices[t+c]],4)/3;r+=Influence(weights[indices[t+c]],5)/3;}
                int region=l>.3f?1:r>.3f?2:p.z<-.115f?3:p.z>.225f?4:0;groups[region].Add(t);
                if(region==4&&p.z<.245f)groups[0].Add(t);if(region==3&&p.z>-.15f)groups[0].Add(t);
            }
            for(int i=0;i<5;i++)roots[i]=Build(groups[i].ToArray());
        }
        private Vector3 Center(int t)=>(v[indices[t]]+v[indices[t+1]]+v[indices[t+2]])/3;
        private Node Build(int[] ts)
        {
            if(ts.Length==0)throw new InvalidOperationException("Empty source region");var b=new Bounds(v[indices[ts[0]]],Vector3.zero);foreach(int t in ts)for(int c=0;c<3;c++)b.Encapsulate(v[indices[t+c]]);
            var node=new Node{bounds=b};if(ts.Length<=24){node.tris=ts;return node;}
            int axis=b.size.x>b.size.y?(b.size.x>b.size.z?0:2):(b.size.y>b.size.z?1:2);Array.Sort(ts,(a,bv)=>Center(a)[axis].CompareTo(Center(bv)[axis]));int half=ts.Length/2;node.left=Build(ts.Take(half).ToArray());node.right=Build(ts.Skip(half).ToArray());return node;
        }
        public void Closest(int region,Vector3 p,Vector3 normal,out int tri,out Vector3 bary,out float distance)
        {
            int t=-1;Vector3 bc=Vector3.zero;float best=float.MaxValue;Search(roots[region],p,normal,ref t,ref bc,ref best);tri=t;bary=bc;distance=Mathf.Sqrt(best);
        }
        private void Search(Node node,Vector3 p,Vector3 normal,ref int tri,ref Vector3 bary,ref float best)
        {
            if(node.bounds.SqrDistance(p)>best)return;
            if(node.tris!=null){foreach(int t in node.tris){int a=indices[t],b=indices[t+1],c=indices[t+2];var bc=Bary(p,v[a],v[b],v[c]);var sn=(n[a]*bc.x+n[b]*bc.y+n[c]*bc.z).normalized;if(Vector3.Dot(sn,normal)<0)continue;var q=v[a]*bc.x+v[b]*bc.y+v[c]*bc.z;float d=(q-p).sqrMagnitude;if(d<best){best=d;tri=t;bary=bc;}}}
            else {var first=node.left.bounds.SqrDistance(p)<node.right.bounds.SqrDistance(p)?node.left:node.right;Search(first,p,normal,ref tri,ref bary,ref best);Search(first==node.left?node.right:node.left,p,normal,ref tri,ref bary,ref best);}
        }
    }
    private static Vector3 Bary(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
    {
        Vector3 ab=b-a,ac=c-a,ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0)return new Vector3(1,0,0);
        var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return new Vector3(0,1,0);
        float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0){float t=d1/(d1-d3);return new Vector3(1-t,t,0);}
        var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return new Vector3(0,0,1);
        float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0){float t=d2/(d2-d6);return new Vector3(1-t,0,t);}
        float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0){float t=(d4-d3)/((d4-d3)+(d5-d6));return new Vector3(0,1-t,t);}
        float den=va+vb+vc;return Mathf.Abs(den)<1e-20f?new Vector3(1,0,0):new Vector3(va,vb,vc)/den;
    }
    public static void Transfer()
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);var sourceFilter=AssetDatabase.LoadAssetAtPath<GameObject>(Source+".fbx").GetComponentInChildren<MeshFilter>();var full=sourceFilter.transform.localToWorldMatrix;
        var surface=new Surface(AssetDatabase.LoadAssetAtPath<GameObject>(Adult).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
        var v=mesh.vertices;var n=mesh.normals;var output=new BoneWeight[v.Length];var cache=new Dictionary<Vector3,BoneWeight>();var dist=new List<float>();int[] regions=new int[5];int headRefinements=0,finRefinements=0;
        for(int i=0;i<v.Length;i++)
        {
            var p=full.MultiplyPoint3x4(v[i]);int region=Region(p);regions[region]++;if(cache.TryGetValue(v[i],out var cw)){output[i]=cw;continue;}
            var mapped=Map(p,region);var wn=full.MultiplyVector(n[i]).normalized;
            // Approximate inverse-transpose local anatomical scaling for query-normal gating.
            float sx=region==1||region==2?.32f:Along(p.z,AdultWidth)/Mathf.Max(.035f,Along(p.z,FryWidth));
            float sz=region==1||region==2?.225f:.3157f;var queryNormal=new Vector3(wn.x/sx,wn.y/.30f,wn.z/sz).normalized;
            surface.Closest(region,mapped,queryNormal,out int tri,out Vector3 bc,out float d);
            if(tri<0)throw new InvalidOperationException("No orientation-compatible adult surface at fry vertex "+i+". No silent fallback.");dist.Add(d);
            var values=new float[10];for(int c=0;c<3;c++){int vi=surface.indices[tri+c];for(int b=0;b<10;b++)values[b]+=Influence(surface.weights[vi],b)*bc[c];}
            values[3]=0; // mouth assignment below is explicitly localized, not transported muzzle leakage.
            if(region==1||region==2)
            {
                int fin=region==1?4:5;values[region==1?5:4]=0;
                float bodySx=Along(p.z,AdultWidth)/Mathf.Max(.035f,Along(p.z,FryWidth));
                var bodyNormal=new Vector3(wn.x/bodySx,wn.y/.30f,wn.z/.3157f).normalized;
                surface.Closest(0,Map(p,0),bodyNormal,out int bodyTri,out Vector3 bodyBc,out float bodyDistance);
                if(bodyTri<0)throw new InvalidOperationException("No compatible body attachment surface at "+i);
                var body=new float[10];for(int c=0;c<3;c++)for(int b=0;b<10;b++)body[b]+=Influence(surface.weights[surface.indices[bodyTri+c]],b)*bodyBc[c];
                body[3]=body[4]=body[5]=0;
                if(p.z>.57f){float protect=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.57f,.76f,p.z));for(int b=0;b<10;b++)body[b]*=1-protect;body[2]+=protect;}
                var bodyPacked=Pack(body);for(int b=0;b<10;b++)body[b]=Influence(bodyPacked,b);
                var finPacked=Pack(values);for(int b=0;b<10;b++)values[b]=Influence(finPacked,b);
                float attachment=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.345f,.43f,Mathf.Abs(p.x)))
                    *Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.32f,p.z))
                    *(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.59f,.67f,p.z)))
                    *Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.30f,-.40f,p.y));
                for(int b=0;b<10;b++)values[b]=Mathf.Lerp(body[b],values[b],attachment);
                finRefinements++;
            }
            else {values[4]=0;values[5]=0;if(p.z>.57f){float protect=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.57f,.76f,p.z));for(int b=0;b<10;b++)values[b]*=1-protect;values[2]+=protect;headRefinements++;}}
            float lip=LipMask(p);if(p.y<-.115f&&lip>0){for(int b=0;b<10;b++)values[b]*=1-.18f*lip;values[3]=.18f*lip;}
            var weight=Pack(values);cache[v[i]]=weight;output[i]=weight;
        }
        mesh.boneWeights=output;EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();dist.Sort();
        File.WriteAllText(Folder+"/Reports/RigAudit.txt","Method=adult region/orientation-constrained triangle barycentric transfer; fry-specific protected head, sided fin attachment, localized jaw\nUniqueMatches="+dist.Count+"\nOpposingNormalMatches=0\nFallbackAssignments=0\nDistanceMeanAdultUnits="+dist.Average()+"\nDistanceP95="+dist[(int)(dist.Count*.95f)]+"\nDistanceMax="+dist.Last()+"\nDistanceOver.04="+dist.Count(x=>x>.04f)+"\nRegions="+string.Join(",",regions)+"\nHeadCorrections="+headRefinements+"\nFinCorrections="+finRefinements+"\nBoneCoverage="+string.Join(",",Enumerable.Range(0,10).Select(b=>output.Count(w=>Influence(w,b)>1e-5f))));
    }
    public static float LipMask(Vector3 p)
    {
        float x=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.065f,.145f,Mathf.Abs(p.x)));
        float z=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.895f,.932f,p.z));
        float y=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.10f,Mathf.Abs(p.y+.115f)));
        return x*y*z;
    }
    public static void RefineDiscontinuities()
    {
        // Deterministic: regenerate the selected adult transfer, then correct only detected local jumps.
        Transfer();
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);var vertices=mesh.vertices;var weights=mesh.boneWeights;
        var full=AssetDatabase.LoadAssetAtPath<GameObject>(Source+".fbx").transform.localToWorldMatrix;
        var unique=new Dictionary<Vector3,int>();var vertexGroup=new int[vertices.Length];var positions=new List<Vector3>();
        for(int i=0;i<vertices.Length;i++){if(!unique.TryGetValue(vertices[i],out int g)){g=positions.Count;unique.Add(vertices[i],g);positions.Add(full.MultiplyPoint3x4(vertices[i]));}vertexGroup[i]=g;}
        var links=positions.Select(_=>new HashSet<int>()).ToArray();var values=positions.Select(_=>new float[10]).ToArray();
        for(int i=0;i<vertices.Length;i++)for(int b=0;b<10;b++)values[vertexGroup[i]][b]=Influence(weights[i],b);
        var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3)for(int j=0;j<3;j++){int a=vertexGroup[indices[i+j]],b=vertexGroup[indices[i+(j+1)%3]];if(a!=b){links[a].Add(b);links[b].Add(a);}}
        var mask=new bool[positions.Count];
        for(int a=0;a<positions.Count;a++)if(positions[a].z<.57f)foreach(int b in links[a]){if(positions[b].z>=.57f)continue;float jump=0;for(int k=0;k<10;k++)jump+=Mathf.Abs(values[a][k]-values[b][k]);if(jump>.5f){mask[a]=true;mask[b]=true;}}
        int seeds=mask.Count(x=>x);for(int ring=0;ring<4;ring++){var next=(bool[])mask.Clone();for(int i=0;i<mask.Length;i++)if(mask[i])foreach(int j in links[i])if(positions[j].z<.57f)next[j]=true;mask=next;}
        for(int iteration=0;iteration<24;iteration++)
        {
            var next=values.Select(x=>(float[])x.Clone()).ToArray();
            for(int i=0;i<values.Length;i++)if(mask[i]&&links[i].Count>0)
            {
                var average=new float[10];int count=0;
                foreach(int j in links[i]){if(positions[i].x*positions[j].x<0&&(Mathf.Abs(positions[i].x)>.3f||Mathf.Abs(positions[j].x)>.3f))continue;for(int b=0;b<10;b++)average[b]+=values[j][b];count++;}
                if(count==0)continue;for(int b=0;b<10;b++)next[i][b]=Mathf.Lerp(values[i][b],average[b]/count,.4f);
                next[i][3]=0;if(positions[i].x<0)next[i][5]=0;else next[i][4]=0;
                var packed=Pack(next[i]);for(int b=0;b<10;b++)next[i][b]=Influence(packed,b);
            }
            values=next;
        }
        for(int i=0;i<weights.Length;i++)weights[i]=Pack(values[vertexGroup[i]]);
        mesh.boneWeights=weights;EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();
        File.WriteAllText(Folder+"/Reports/RegionalCorrection.txt","Detected discontinuity seeds="+seeds+"\nExpanded unique-vertex correction mask="+mask.Count(x=>x)+" / "+positions.Count+"\n24 bounded .4 neighbor relaxation passes, four adjacency rings; head z>=.57 and mouth protected; no geometry smoothing; no wholesale weight-method fallback. Input regenerated from original adult surface transfer, cannot accumulate on repeated calls.");
    }
    public static void Mouth()
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);var filter=AssetDatabase.LoadAssetAtPath<GameObject>(Source+".fbx").GetComponentInChildren<MeshFilter>();var full=filter.transform.localToWorldMatrix;
        var vs=mesh.vertices;var ds=new Vector3[vs.Length];int count=0;for(int i=0;i<vs.Length;i++){var p=full.MultiplyPoint3x4(vs[i]);float mask=LipMask(p);float jaw=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.09f,-.14f,p.y));ds[i]=full.inverse.MultiplyVector(new Vector3(0,Mathf.Lerp(.006f,-.012f,jaw)*mask,.0015f*jaw*mask));if(ds[i].sqrMagnitude>0)count++;}
        mesh.ClearBlendShapes();mesh.AddBlendShapeFrame("MouthOpen",100,ds,new Vector3[vs.Length],new Vector3[vs.Length]);mesh.bounds=new Bounds(mesh.bounds.center,mesh.bounds.size*2f);EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();
        File.WriteAllText(Folder+"/Reports/MouthDesign.txt","AffectedVertices="+count+"\nFullImportUpperY=.006; LowerY=-.012; lowerZ=.0015\nLocalized bounded shallow lip movement, source timing retained. No new interior/topology. Convincing aperture is a visual acceptance requirement and not assumed.\nProtected eyes/cheeks lie outside x<.145/z>.895 lip mask.");
    }
}
