using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using KoiPond.EarlyStageTransferPilot;
public static class KohakuJuvenileRedPatternTrial
{
    const string Folder=EarlyStageAlbedoTransferBuilder.Folder;
    const string Output=Folder+"/Textures/Kohaku_Juvenile_RedPattern_Candidate_v01.png";
    const int R=192,A=128,S=2048;
    sealed class Surface
    {
        public Vector3[] V; public Vector2[] UV; public int[] T,Regions;
        public float Peduncle,Shoulder,Nose,FaceStart,FaceHeight;
        public Surface(string path)
        {
            var type=typeof(KohakuFryPatternTrial).GetNestedType("Surface",BindingFlags.NonPublic);
            var data=Activator.CreateInstance(type,new object[]{path});
            V=(Vector3[])type.GetField("V").GetValue(data);UV=(Vector2[])type.GetField("UV").GetValue(data);T=(int[])type.GetField("T").GetValue(data);Regions=(int[])type.GetField("Regions").GetValue(data);
            Peduncle=(float)type.GetField("Peduncle").GetValue(data);Shoulder=(float)type.GetField("Shoulder").GetValue(data);Nose=(float)type.GetField("Nose").GetValue(data);
            // Fit the protected facial domain relative to measured shoulder/nose, not Fry coordinates.
            FaceStart=Shoulder+(Nose-Shoulder)*.25f;
            var face=V.Where(p=>p.z>FaceStart&&Mathf.Abs(p.x)>.035f).ToArray();
            FaceHeight=face.Max(p=>p.y)*.52f;
        }
        public float Z(float z)=>z<=Shoulder?.62f*Mathf.InverseLerp(Peduncle,Shoulder,z):.62f+.38f*Mathf.InverseLerp(Shoulder,Nose,z);
    }
    static Texture2D Decode(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!t.LoadImage(File.ReadAllBytes(path)))throw new Exception(path);return t;}
    static float Angle(Vector3 p)=>Mathf.Repeat(Mathf.Atan2(p.y,p.x)/(2*Mathf.PI)+.5f,1);
    static float Sample(float[,] chart,float z,float a)
    {
        float zz=Mathf.Clamp01(z)*(R-1),aa=Mathf.Repeat(a,1)*A;int z0=Mathf.FloorToInt(zz),a0=Mathf.FloorToInt(aa)%A;
        return Mathf.Lerp(Mathf.Lerp(chart[z0,a0],chart[z0,(a0+1)%A],aa-Mathf.Floor(aa)),Mathf.Lerp(chart[Mathf.Min(z0+1,R-1),a0],chart[Mathf.Min(z0+1,R-1),(a0+1)%A],aa-Mathf.Floor(aa)),zz-z0);
    }
    public static string Bake()
    {
        if(File.Exists(Output))throw new Exception("Refusing existing candidate overwrite");
        var adult=new Surface(EarlyStageAlbedoTransferBuilder.Adult);var dst=new Surface(EarlyStageAlbedoTransferBuilder.Juvenile);
        var original=Decode("Assets/Models/Koi/New Juvenile/new_juvenile_texture.png");var source=Decode("Assets/Models/Koi/ProofOfConcept/KohakuTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_BaseColor.png");
        try
        {
            var sum=new float[R,A];var weights=new float[R,A];var used=new bool[adult.V.Length];
            for(int f=0;f<adult.Regions.Length;f++)if(adult.Regions[f]==0)for(int j=0;j<3;j++)used[adult.T[f*3+j]]=true;
            for(int i=0;i<used.Length;i++)if(used[i])
            {
                var p=adult.V[i];if(Mathf.Abs(p.x)>.04f&&p.z>.183f&&p.z<.281f&&p.y>-.049f&&p.y<.048f)continue;
                float z=adult.Z(p.z)*(R-1),a=Angle(p)*A;int z0=Mathf.FloorToInt(z),a0=Mathf.FloorToInt(a)%A;
                var c=source.GetPixelBilinear(adult.UV[i].x,adult.UV[i].y);float value=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.45f,c.r-Mathf.Max(c.g,c.b)));
                for(int dz=0;dz<2;dz++)for(int da=0;da<2;da++){int zz=Mathf.Min(z0+dz,R-1),aa=(a0+da)%A;float w=(dz==0?1-z+z0:z-z0)*(da==0?1-a+Mathf.Floor(a):a-Mathf.Floor(a));sum[zz,aa]+=value*w;weights[zz,aa]+=w;}
            }
            var chart=new float[R,A];var known=new bool[R,A];int measured=0,interpolated=0;
            for(int z=0;z<R;z++)for(int a=0;a<A;a++)if(weights[z,a]>.0001f){known[z,a]=true;chart[z,a]=sum[z,a]/weights[z,a];measured++;}
            for(int pass=0;pass<R+A;pass++)
            {
                var next=(float[,])chart.Clone();var nk=(bool[,])known.Clone();int added=0;
                for(int z=0;z<R;z++)for(int a=0;a<A;a++)if(!known[z,a]){float v=0;int n=0;for(int j=-1;j<=1;j+=2){int zz=Mathf.Clamp(z+j,0,R-1),aa=(a+j+A)%A;if(known[zz,a]){v+=chart[zz,a];n++;}if(known[z,aa]){v+=chart[z,aa];n++;}}if(n>0){next[z,a]=v/n;nk[z,a]=true;added++;}}
                chart=next;known=nk;interpolated+=added;if(added==0)break;
            }
            for(int pass=0;pass<5;pass++){var next=(float[,])chart.Clone();for(int z=0;z<R;z++)for(int a=0;a<A;a++)next[z,a]=(chart[z,a]*4+chart[Mathf.Max(0,z-1),a]+chart[Mathf.Min(R-1,z+1),a]+chart[z,(a+1)%A]+chart[z,(a+A-1)%A])/8;chart=next;}
            if(measured<R*A/2||measured+interpolated!=R*A)throw new Exception("Insufficient chart coverage");
            var pixels=new Color32[S*S];for(int y=0;y<S;y++)for(int x=0;x<S;x++)pixels[y*S+x]=original.GetPixelBilinear((x+.5f)/S,(y+.5f)/S);
            var baseline=(Color32[])pixels.Clone();var occupied=new bool[pixels.Length];var protect=new bool[pixels.Length];var masks=new float[pixels.Length];int conflicts=0,changed=0;
            for(int f=0;f<dst.Regions.Length;f++)
            {
                int ia=dst.T[f*3],ib=dst.T[f*3+1],ic=dst.T[f*3+2];var u=dst.UV[ia]*S;var v=dst.UV[ib]*S;var w=dst.UV[ic]*S;float det=(v.x-u.x)*(w.y-u.y)-(v.y-u.y)*(w.x-u.x);if(Mathf.Abs(det)<1e-10f)continue;
                for(int y=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.y,v.y,w.y)-.5f));y<=Mathf.Min(S-1,Mathf.FloorToInt(Mathf.Max(u.y,v.y,w.y)-.5f));y++)for(int x=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.x,v.x,w.x)-.5f));x<=Mathf.Min(S-1,Mathf.FloorToInt(Mathf.Max(u.x,v.x,w.x)-.5f));x++)
                {
                    float px=x+.5f-u.x,py=y+.5f-u.y,b=(px*(w.y-u.y)-py*(w.x-u.x))/det,c=((v.x-u.x)*py-(v.y-u.y)*px)/det,a=1-b-c;if(a<-1e-6f||b<-1e-6f||c<-1e-6f)continue;
                    int i=y*S+x;var p=dst.V[ia]*a+dst.V[ib]*b+dst.V[ic]*c;bool pr=dst.Regions[f]!=0||(p.z>dst.FaceStart&&p.y<dst.FaceHeight);float mask=pr?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.7f,Sample(chart,dst.Z(p.z),Angle(p))));
                    if(occupied[i]&&!pr&&!protect[i]&&Mathf.Abs(mask-masks[i])>.1f)conflicts++;
                    protect[i]|=pr;if(!occupied[i])masks[i]=mask;occupied[i]=true;
                }
            }
            if(conflicts!=0)throw new Exception("UV marking conflicts="+conflicts);
            for(int i=0;i<pixels.Length;i++)if(occupied[i]&&!protect[i]&&masks[i]>.001f){Color c=pixels[i];float detail=Mathf.Clamp(c.r,.15f,1);pixels[i]=Color.Lerp(c,new Color(detail,detail*.075f,detail*.055f,c.a),masks[i]);changed++;}
            int protectedChanges=0;for(int i=0;i<pixels.Length;i++)if(protect[i]&&!pixels[i].Equals(baseline[i]))protectedChanges++;
            if(protectedChanges!=0)throw new Exception("Protected artwork changed");
            var covered=(bool[])occupied.Clone();var prepadding=(Color32[])pixels.Clone();int padded=0;
            for(int pass=0;pass<12;pass++)
            {
                var additions=new List<int>();var values=new List<Color32>();for(int y=0;y<S;y++)for(int x=0;x<S;x++){int i=y*S+x;if(occupied[i])continue;int j=-1;if(x>0&&occupied[i-1])j=i-1;else if(x<S-1&&occupied[i+1])j=i+1;else if(y>0&&occupied[i-S])j=i-S;else if(y<S-1&&occupied[i+S])j=i+S;if(j>=0){additions.Add(i);values.Add(pixels[j]);}}for(int j=0;j<additions.Count;j++){int i=additions[j];occupied[i]=true;pixels[i]=values[j];}padded+=additions.Count;
            }
            int paddingChanges=0;for(int i=0;i<pixels.Length;i++)if(covered[i]&&!pixels[i].Equals(prepadding[i]))paddingChanges++;
            if(paddingChanges!=0)throw new Exception("Padding modified surface");
            var tex=new Texture2D(S,S,TextureFormat.RGBA32,false);tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(Output,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(Output,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(Output);importer.maxTextureSize=S;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            string report="Juvenile candidate NOT approved\nLandmarks peduncle="+dst.Peduncle+" shoulder="+dst.Shoulder+" nose="+dst.Nose+" faceStart="+dst.FaceStart+" faceHeight="+dst.FaceHeight+"\nChart measured="+measured+" interpolated="+interpolated+" total="+R*A+"\nUV conflicts="+conflicts+" protectedChanges="+protectedChanges+" coveredPixelsChangedByPadding="+paddingChanges+"\nRecolored="+changed+" protected="+protect.Count(v=>v)+" padded="+padded+"\nChart interpolation remains an approximation; original Juvenile artwork retained outside red chroma mask. No continuous runtime verification.";File.WriteAllText(Folder+"/Reports/Kohaku_Juvenile_RedPattern_v01_Validation.txt",report);return report;
        }
        finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(original);}
    }
    public static string Capture(float swimTime=-1)
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var cg=new GameObject("Juvenile review camera");var cam=cg.AddComponent<Camera>();cam.orthographic=true;cam.cullingMask=1<<31;cam.nearClipPlane=.01f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.18f,.18f,.18f);
        var lg=new GameObject("Juvenile review light");var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.cullingMask=1<<31;lg.transform.rotation=Quaternion.Euler(40,-25,0);var rt=new RenderTexture(768,768,24);cam.targetTexture=rt;var atlas=new Texture2D(3072,2304,TextureFormat.RGB24,false);
        Vector3[] views={new Vector3(1,.1f,0),Vector3.up,new Vector3(1,.6f,1),new Vector3(-1,.1f,0)};
        try
        {
            for(int row=0;row<3;row++)
            {
                var go=KohakuCandidateBake.CopyForReview(AssetDatabase.LoadAssetAtPath<GameObject>(row==0?EarlyStageAlbedoTransferBuilder.Adult:EarlyStageAlbedoTransferBuilder.Juvenile));foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();var mat=new Material(row==0?AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Koi/ProofOfConcept/KohakuPbrTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_PBR_Test.mat"):skin.sharedMaterial);if(row==2)mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output));skin.sharedMaterial=mat;
                if(swimTime>=0){var positions=skin.bones.Select(b=>b.localPosition).ToArray();AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/Koi_Swim.anim").SampleAnimation(go,swimTime);for(int b=0;b<positions.Length;b++)skin.bones[b].localPosition=positions[b];}
                var bounds=skin.bounds;cam.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.z)*.65f;for(int col=0;col<4;col++){cg.transform.position=bounds.center+views[col].normalized*2;cg.transform.LookAt(bounds.center,col==1?Vector3.forward:Vector3.up);cam.Render();RenderTexture.active=rt;atlas.ReadPixels(new Rect(0,0,768,768),col*768,(2-row)*768);}UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mat);
            }
            atlas.Apply();string path=Folder+"/Reports/Kohaku_Juvenile_RedPattern_v01_"+(swimTime<0?"Rest":"SwimSample")+".png";File.WriteAllBytes(path,atlas.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);return AssetDatabase.LoadAssetAtPath<Texture2D>(path).GetEntityId().ToString();
        }
        finally{RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(lg);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(atlas);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
