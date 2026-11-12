using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using KoiPond.EarlyStageTransferPilot;

public static class KohakuLocalizedAlbedoRefinement
{
    const string F = EarlyStageAlbedoTransferBuilder.Folder;
    const string Reports = F + "/Reports/LocalizedKohakuRefinement";
    static readonly Dictionary<string,string> Protected = new Dictionary<string,string>();
    static string Hash(string p) { using(var s=SHA256.Create()) return BitConverter.ToString(s.ComputeHash(File.ReadAllBytes(p))); }
    public static void Protect()
    {
        if(Directory.Exists(Reports)) throw new Exception("Review output folder exists; refusing overwrite.");
        foreach(var p in Directory.GetFiles("Assets","*",SearchOption.AllDirectories)) Protected[p]=Hash(p);
        Directory.CreateDirectory(Reports);
        File.WriteAllLines(Reports+"/SourceProtection.tsv",Protected.Select(p=>p.Key+"\t"+p.Value));
    }
    static Texture2D Decode(string p) { var t=new Texture2D(2,2,TextureFormat.RGBA32,false); if(!t.LoadImage(File.ReadAllBytes(p))) throw new Exception(p); return t; }
    static float Red(Color32 c) { return (c.r-Mathf.Max(c.g,c.b))/255f; }
    public static string Bake(bool juvenile)
    {
        string stage=juvenile?"Juvenile":"Fry", old=juvenile?"v01":"v03", next=juvenile?"v02":"v04";
        string input=F+"/Textures/Kohaku_"+stage+"_RedPattern_Candidate_"+old+".png", output=F+"/Textures/Kohaku_"+stage+"_RedPattern_Candidate_"+next+".png";
        if(File.Exists(output)) throw new Exception("Candidate exists: "+output);
        var tex=Decode(input); int s=tex.width; var before=tex.GetPixels32();var pixels=(Color32[])before.Clone();
        var type=typeof(KohakuFryPatternTrial).GetNestedType("Surface",BindingFlags.NonPublic);
        var data=Activator.CreateInstance(type,new object[]{juvenile?EarlyStageAlbedoTransferBuilder.Juvenile:EarlyStageAlbedoTransferBuilder.Fry});
        var v=(Vector3[])type.GetField("V").GetValue(data);var uv=(Vector2[])type.GetField("UV").GetValue(data);var tri=(int[])type.GetField("T").GetValue(data);var reg=(int[])type.GetField("Regions").GetValue(data);
        float lo=(float)type.GetField("Peduncle").GetValue(data), hi=(float)type.GetField("Nose").GetValue(data), shoulder=(float)type.GetField("Shoulder").GetValue(data);
        float face=juvenile?shoulder+(hi-shoulder)*.25f:.16f;
        float faceHeight=juvenile?v.Where(p=>p.z>face&&Mathf.Abs(p.x)>.035f).Max(p=>p.y)*.52f:.052f;
        // Estimate body roof from non-central upper-body surfaces, excluding the thin dorsal blade.
        const int rings=256;
        var roof=new float[rings];var donors=new List<int>[rings];for(int j=0;j<rings;j++){roof[j]=float.NaN;donors[j]=new List<int>();}
        var body=new bool[v.Length];for(int f=0;f<reg.Length;f++)if(reg[f]==0)for(int j=0;j<3;j++)body[tri[f*3+j]]=true;
        for(int j=0;j<v.Length;j++) if(body[j]&&v[j].y>0&&Mathf.Abs(v[j].x)>.025f&&Mathf.Abs(v[j].x)<.045f)
        {int z=Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(lo,hi,v[j].z)*(rings-1)),0,rings-1);donors[z].Add(j);roof[z]=float.IsNaN(roof[z])?v[j].y:Mathf.Max(roof[z],v[j].y);}
        for(int z=0;z<rings;z++)if(float.IsNaN(roof[z])){int best=-1;for(int k=0;k<rings;k++)if(!float.IsNaN(roof[k])&&(best<0||Mathf.Abs(k-z)<Mathf.Abs(best-z)))best=k;roof[z]=best<0?0:roof[best];}
        var occupied=new bool[pixels.Length];var protect=new bool[pixels.Length];var dorsal=new bool[pixels.Length];var bodyPixel=new bool[pixels.Length];var target=new Color32[pixels.Length];
        int ambiguous=0;
        for(int f=0;f<reg.Length;f++)
        {
            int ia=tri[f*3],ib=tri[f*3+1],ic=tri[f*3+2];var u=uv[ia]*s;var w=uv[ib]*s;var q=uv[ic]*s;float det=(w.x-u.x)*(q.y-u.y)-(w.y-u.y)*(q.x-u.x);if(Mathf.Abs(det)<1e-10f)continue;
            for(int y=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.y,w.y,q.y)-.5f));y<=Mathf.Min(s-1,Mathf.FloorToInt(Mathf.Max(u.y,w.y,q.y)-.5f));y++)for(int x=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.x,w.x,q.x)-.5f));x<=Mathf.Min(s-1,Mathf.FloorToInt(Mathf.Max(u.x,w.x,q.x)-.5f));x++)
            {
                float px=x+.5f-u.x,py=y+.5f-u.y,b=(px*(q.y-u.y)-py*(q.x-u.x))/det,c=((w.x-u.x)*py-(w.y-u.y)*px)/det,a=1-b-c;if(a<-1e-6f||b<-1e-6f||c<-1e-6f)continue;
                int i=y*s+x;var p=v[ia]*a+v[ib]*b+v[ic]*c;int z=Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(lo,hi,p.z)*(rings-1)),0,rings-1);
                bool attachment=reg[f]==4&&Mathf.Abs(p.x)>.009f&&p.y<=roof[z]+.006f;
                bool pr=(reg[f]!=0&&!attachment)||(p.z>face&&p.y<faceHeight);
                protect[i]|=pr;bodyPixel[i]|=!pr;occupied[i]=true;
                if(pr||Mathf.Abs(p.x)>.025f||p.y<roof[z]-.012f||p.y>roof[z]+.006f||Red(before[i])>.35f)continue;
                // Only bridge a narrow central body strip when both adjacent sides belong to red.
                Color32 left=new Color32(),right=new Color32();float dl=float.MaxValue,dr=float.MaxValue;
                for(int k=Mathf.Max(0,z-3);k<=Mathf.Min(rings-1,z+3);k++)foreach(int j in donors[k])
                {float d=(v[j]-p).sqrMagnitude;var col=(Color32)tex.GetPixelBilinear(uv[j].x,uv[j].y);if(v[j].x<0&&d<dl){dl=d;left=col;}if(v[j].x>0&&d<dr){dr=d;right=col;}}
                if(dl==float.MaxValue||dr==float.MaxValue||Red(left)<.5f||Red(right)<.5f)continue;
                var value=(Color32)Color.Lerp(left,right,Mathf.InverseLerp(-.025f,.025f,p.x));value.a=before[i].a;
                if(dorsal[i]&&Mathf.Abs(target[i].r-value.r)>32)ambiguous++;
                dorsal[i]=true;target[i]=value;
            }
        }
        int dorsalChanges=0,spots=0;
        for(int i=0;i<pixels.Length;i++)if(dorsal[i]&&!protect[i]){pixels[i]=target[i];dorsalChanges++;}
        if(juvenile)
        {
            var original=Decode("Assets/Models/Koi/New Juvenile/new_juvenile_texture.png");var src=original.GetPixels32();
            if(original.width!=s||original.height!=s)throw new Exception("Unexpected source dimensions");
            // Original achromatic dark pigmentation, confined to existing fully red interiors.
            var seed=new bool[pixels.Length];
            for(int y=2;y<s-2;y++)for(int x=2;x<s-2;x++){int i=y*s+x;var c=src[i];if(protect[i]||!bodyPixel[i]||Red(before[i])<.22f||c.r>155||Mathf.Abs(c.r-c.g)>38||Mathf.Abs(c.r-c.b)>38)continue;bool interior=true;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)if(Red(before[i+dy*s+dx])<.18f)interior=false;seed[i]=interior;}
            var seen=new bool[pixels.Length];var mask=new bool[pixels.Length];
            for(int i=0;i<seed.Length;i++)if(seed[i]&&!seen[i]){var group=new List<int>();var queue=new Queue<int>();queue.Enqueue(i);seen[i]=true;while(queue.Count>0){int j=queue.Dequeue();group.Add(j);int x=j%s,y=j/s;foreach(int n in new[]{x>0?j-1:j,x<s-1?j+1:j,y>0?j-s:j,y<s-1?j+s:j})if(seed[n]&&!seen[n]){seen[n]=true;queue.Enqueue(n);}}if(group.Count>=48)foreach(int j in group)mask[j]=true;}
            // Small feather extends through the spot edge, still strictly within red interiors.
            var core=(bool[])mask.Clone();for(int y=3;y<s-3;y++)for(int x=3;x<s-3;x++){int i=y*s+x;if(protect[i]||!bodyPixel[i]||Red(before[i])<.22f||src[i].r>205)continue;for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)if(core[i+dy*s+dx])mask[i]=true;}
            for(int y=1;y<s-1;y++)for(int x=1;x<s-1;x++){int i=y*s+x;if(!mask[i])continue;float sum=0;int count=0;var values=new List<byte>();for(int dy=-48;dy<=48;dy+=8)for(int dx=-48;dx<=48;dx+=8){int xx=x+dx,yy=y+dy;if(xx<0||xx>=s||yy<0||yy>=s)continue;int j=yy*s+xx;if(!mask[j]&&bodyPixel[j]&&!protect[j]&&Red(before[j])>.5f&&src[j].r>205)values.Add(before[j].r);}if(values.Count<4)continue;values.Sort();float clean=values[values.Count/2];for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){sum+=src[i+dy*s+dx].r;count++;}float residual=Mathf.Clamp(src[i].r-sum/count,-16,16);float r=Mathf.Clamp(clean+residual,0,255);pixels[i]=new Color32((byte)r,(byte)(r*.075f),(byte)(r*.055f),before[i].a);spots++;}
            UnityEngine.Object.DestroyImmediate(original);
        }
        int protectedChanges=0,otherChanges=0;var changes=new Color32[pixels.Length];for(int i=0;i<pixels.Length;i++){bool changed=!pixels[i].Equals(before[i]);if(changed&&protect[i])protectedChanges++;if(changed&&!bodyPixel[i])otherChanges++;changes[i]=changed?new Color32(255,0,0,255):new Color32(0,0,0,255);}
        if(protectedChanges!=0||otherChanges!=0||ambiguous!=0)throw new Exception("Protection/conflict failure "+protectedChanges+" "+otherChanges+" "+ambiguous);
        // Propagate changed island borders only into unused atlas gutters.
        var filled=(bool[])occupied.Clone();for(int pass=0;pass<12;pass++){var ids=new List<int>();var cols=new List<Color32>();for(int y=1;y<s-1;y++)for(int x=1;x<s-1;x++){int i=y*s+x;if(filled[i])continue;int j=filled[i-1]?i-1:filled[i+1]?i+1:filled[i-s]?i-s:filled[i+s]?i+s:-1;if(j>=0){ids.Add(i);cols.Add(pixels[j]);}}for(int j=0;j<ids.Count;j++){filled[ids[j]]=true;pixels[ids[j]]=cols[j];}}
        tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(output,tex.EncodeToPNG());AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(output);imp.maxTextureSize=s;imp.sRGBTexture=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.filterMode=FilterMode.Bilinear;imp.SaveAndReimport();
        tex.SetPixels32(changes);tex.Apply();File.WriteAllBytes(Reports+"/"+stage+"_SurfaceChanges.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        string report=stage+" "+next+" pending visual approval\nDorsal body texels="+dorsalChanges+" spot texels="+spots+" protectedChanges="+protectedChanges+" nonbodyChanges="+otherChanges+" conflicts="+ambiguous+"\nBlade protected; attachment threshold: |x|>.009, y<=measured body roof+.006. Red supported on BOTH neighboring sides; intentional white gaps retained. Spot residual uses original 3x3 fine detail and median clean-red neighbors; uncertain samples retained. No existing assets assigned.\n";
        File.WriteAllText(Reports+"/"+stage+"_Validation.txt",report);AssetDatabase.Refresh();return report;
    }
    public static string Verify()
    {var bad=Protected.Where(p=>!File.Exists(p.Key)||Hash(p.Key)!=p.Value).Select(p=>p.Key).ToArray();File.WriteAllText(Reports+"/Preservation.txt","Protected files="+Protected.Count+" changed="+bad.Length+"\n"+string.Join("\n",bad));if(bad.Length>0)throw new Exception("Protected files changed");return "Protected files unchanged: "+Protected.Count;}
    public static string Capture(bool juvenile,bool unlit=false,float sample=-1)
    {
        string stage=juvenile?"Juvenile":"Fry", old=juvenile?"v01":"v03", next=juvenile?"v02":"v04";var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var cg=new GameObject("Temporary camera");var cam=cg.AddComponent<Camera>();cam.orthographic=true;cam.cullingMask=1<<31;cam.nearClipPlane=.01f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.18f,.18f,.18f);var lg=new GameObject("Temporary light");var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.cullingMask=1<<31;lg.transform.rotation=Quaternion.Euler(40,-25,0);
        var rt=new RenderTexture(768,768,24);cam.targetTexture=rt;var atlas=new Texture2D(3072,2304,TextureFormat.RGB24,false);var views=new[]{new Vector3(1,.1f,0),Vector3.up,new Vector3(-1,.1f,0),new Vector3(1,.8f,0)};GameObject go=null;Material mat=null;
        try{for(int row=0;row<3;row++){go=KohakuCandidateBake.CopyForReview(AssetDatabase.LoadAssetAtPath<GameObject>(row==0?EarlyStageAlbedoTransferBuilder.Adult:juvenile?EarlyStageAlbedoTransferBuilder.Juvenile:EarlyStageAlbedoTransferBuilder.Fry));foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();mat=new Material(row==0?AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Koi/ProofOfConcept/KohakuPbrTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_PBR_Test.mat"):skin.sharedMaterial);var map=row==0?mat.GetTexture("_BaseMap"):AssetDatabase.LoadAssetAtPath<Texture2D>(F+"/Textures/Kohaku_"+stage+"_RedPattern_Candidate_"+(row==1?old:next)+".png");if(unlit){mat.shader=Shader.Find("Universal Render Pipeline/Unlit");mat.SetColor("_BaseColor",Color.white);}mat.SetTexture("_BaseMap",map);skin.sharedMaterial=mat;if(sample>=0){var positions=skin.bones.Select(b=>b.localPosition).ToArray();AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/Koi_Swim.anim").SampleAnimation(go,sample);for(int b=0;b<positions.Length;b++)skin.bones[b].localPosition=positions[b];}var bounds=skin.bounds;cam.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.z)*.65f;for(int col=0;col<4;col++){cg.transform.position=bounds.center+views[col].normalized*2;cg.transform.LookAt(bounds.center,col==1?Vector3.forward:Vector3.up);cam.Render();RenderTexture.active=rt;atlas.ReadPixels(new Rect(0,0,768,768),col*768,(2-row)*768);}UnityEngine.Object.DestroyImmediate(go);go=null;UnityEngine.Object.DestroyImmediate(mat);mat=null;}atlas.Apply();string path=Reports+"/"+stage+"_"+(unlit?"Albedo":"Lit")+(sample>=0?"_Swim":"_Rest")+".png";File.WriteAllBytes(path,atlas.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var id=AssetDatabase.LoadAssetAtPath<Texture2D>(path).GetEntityId();return path+" entity="+id;}
        finally{if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(mat!=null)UnityEngine.Object.DestroyImmediate(mat);RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(lg);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(atlas);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
