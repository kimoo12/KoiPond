using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class JuvenileRiggingPrototypeValidation
{
    public const string ScenePath=JuvenileRiggingPrototypeBuilder.Folder+"/Scenes/Juvenile_RiggingPrototype_Test.unity";
    [Serializable] public class ClipResult
    {
        public string name;
        public float length, maxDisplacement, maxEdgeRatio, maxMouthWeight, loopSeamError;
        public int frames, unresolvedBindings, nonfiniteVertices, stretchedEdges, collapsedEdges;
        public float[] regionDisplacement=new float[5];
        public string[] warnings;
    }
    [Serializable] public class Result
    {
        public bool geometryPreserved, allUVsPreserved, weightsValid, seamsConsistent;
        public float restMaxError, maxWeightSumError;
        public int vertices, triangles, mouthVertices;
        public ClipResult[] clips;
    }
    public static Scene BuildScene()
    {
        Directory.CreateDirectory(JuvenileRiggingPrototypeBuilder.Folder+"/Scenes");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene present: preserve it before opening isolated test.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var rig=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(JuvenileRiggingPrototypeBuilder.PrefabPath),scene);rig.name="Juvenile_Animated_Test";
        var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();rig.AddComponent<Animator>();var playback=rig.AddComponent<JuvenilePrototypePlayback>();
        var so=new SerializedObject(playback);so.FindProperty("_skin").objectReferenceValue=skin;
        var clips=so.FindProperty("_clips");clips.arraySize=3;
        for(int i=0;i<3;i++)clips.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/Koi_"+new[]{"Swim","Idle","Eat"}[i]+".anim");so.ApplyModifiedPropertiesWithoutUndo();
        var reference=new GameObject("Juvenile_Original_Rest_Reference");reference.transform.position=new Vector3(-.85f,0,0);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(JuvenileRiggingPrototypeBuilder.SourcePath);
        var fish=new GameObject("OriginalFishMesh");fish.transform.SetParent(reference.transform,false);fish.transform.localRotation=source.transform.localRotation;fish.transform.localScale=source.transform.localScale*JuvenileRiggingPrototypeBuilder.DisplayScale;
        fish.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAllAssetsAtPath(JuvenileRiggingPrototypeBuilder.SourcePath).OfType<UnityEngine.Mesh>().First();fish.AddComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;
        var adultSource=AssetDatabase.LoadAssetAtPath<GameObject>(JuvenileRiggingPrototypeBuilder.AdultPath);
        var adultSkin=adultSource.GetComponentInChildren<SkinnedMeshRenderer>();
        var adult=new GameObject("Adult_Rig_ReadOnly_Reference");adult.transform.position=new Vector3(.85f,0,0);
        var armature=UnityEngine.Object.Instantiate(adultSource.transform.GetChild(0).gameObject,adult.transform);armature.name="Armature";
        var adultFish=new GameObject("FishMesh");adultFish.transform.SetParent(adult.transform,false);var r=adultFish.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=adultSkin.sharedMesh;r.sharedMaterials=adultSkin.sharedMaterials;r.bones=adultSkin.bones.Select(b=>adult.transform.Find(AnimationUtility.CalculateTransformPath(b,adultSource.transform))).ToArray();r.rootBone=r.bones[0];
        var camera=new GameObject("Prototype Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(1.35f,.8f,2.1f);camera.transform.LookAt(Vector3.zero);camera.orthographic=true;camera.orthographicSize=.72f;camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.16f,.19f);camera.allowHDR=false;
        for(int i=0;i<2;i++){var light=new GameObject(i==0?"Neutral Key":"Neutral Fill").AddComponent<Light>();light.type=LightType.Directional;light.color=Color.white;light.intensity=i==0?1.5f:.9f;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(i==0?45:145,i==0?-40:120,0);}
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.4f,.4f);RenderSettings.fog=false;RenderSettings.skybox=null;
        EditorSceneManager.SaveScene(scene,ScenePath);return scene;
    }
    private static void Restore(Transform[] bones,Vector3[] positions,Quaternion[] rotations,SkinnedMeshRenderer skin)
    {
        for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}skin.SetBlendShapeWeight(0,0);
    }
    public static Result Validate()
    {
        var root=SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.GetComponent<JuvenilePrototypePlayback>()!=null);
        var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
        var source=AssetDatabase.LoadAllAssetsAtPath(JuvenileRiggingPrototypeBuilder.SourcePath).OfType<UnityEngine.Mesh>().First();
        var result=new Result{vertices=mesh.vertexCount,triangles=mesh.triangles.Length/3,geometryPreserved=mesh.vertices.SequenceEqual(source.vertices)&&mesh.triangles.SequenceEqual(source.triangles)&&mesh.normals.SequenceEqual(source.normals)&&mesh.tangents.SequenceEqual(source.tangents),allUVsPreserved=true,weightsValid=true,seamsConsistent=true};
        for(int channel=0;channel<8;channel++){var a=new List<Vector4>();var b=new List<Vector4>();mesh.GetUVs(channel,a);source.GetUVs(channel,b);result.allUVsPreserved&=a.SequenceEqual(b);}
        var vertices=mesh.vertices;var weights=mesh.boneWeights;var seamWeights=new Dictionary<Vector3,BoneWeight>();
        for(int i=0;i<weights.Length;i++){var w=weights[i];float error=Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1);result.maxWeightSumError=Mathf.Max(error,result.maxWeightSumError);if(error>1e-5f||w.weight0<0||w.weight1<0||w.weight2<0||w.weight3<0||float.IsNaN(error))result.weightsValid=false;for(int b=0;b<10;b++)if(float.IsNaN(JuvenileRiggingPrototypeBuilder.Influence(w,b)))result.weightsValid=false;if(seamWeights.TryGetValue(vertices[i],out var old)){for(int b=0;b<10;b++)if(Mathf.Abs(JuvenileRiggingPrototypeBuilder.Influence(w,b)-JuvenileRiggingPrototypeBuilder.Influence(old,b))>1e-5f)result.seamsConsistent=false;}else seamWeights[vertices[i]]=w;}
        var shape=new Vector3[vertices.Length];mesh.GetBlendShapeFrameVertices(0,0,shape,null,null);result.mouthVertices=shape.Count(v=>v.sqrMagnitude>1e-20f);
        var bones=skin.bones;var positions=bones.Select(b=>b.localPosition).ToArray();var rotations=bones.Select(b=>b.localRotation).ToArray();var baked=new UnityEngine.Mesh();
        // Verified in Unity 6000.6: useScale=true returns the original mesh-local units for this scaled renderer.
        Restore(bones,positions,rotations,skin);skin.BakeMesh(baked,true);var rest=baked.vertices;
        for(int i=0;i<rest.Length;i++)result.restMaxError=Mathf.Max(result.restMaxError,Vector3.Distance(skin.transform.TransformPoint(rest[i]),skin.transform.TransformPoint(vertices[i])));
        var tris=mesh.triangles;var localToWorld=skin.transform.localToWorldMatrix;var unscaled=Matrix4x4.TRS(Vector3.zero,skin.transform.localRotation,skin.transform.localScale/JuvenileRiggingPrototypeBuilder.DisplayScale);
        var regions=vertices.Select(v=>JuvenileRiggingPrototypeBuilder.Region(unscaled.MultiplyPoint3x4(v))).ToArray();
        var results=new List<ClipResult>();
        foreach(string name in new[]{"Swim","Idle","Eat"})
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/Koi_"+name+".anim");var cr=new ClipResult{name=name,length=clip.length};
            foreach(var binding in AnimationUtility.GetCurveBindings(clip)){var target=root.transform.Find(binding.path);if(target==null)cr.unresolvedBindings++;else if(binding.type==typeof(SkinnedMeshRenderer)&&(target.GetComponent<SkinnedMeshRenderer>()==null||mesh.GetBlendShapeIndex("MouthOpen")<0))cr.unresolvedBindings++;}
            Vector3[] first=null,last=null;
            var mouthBinding=AnimationUtility.GetCurveBindings(clip).First(b=>b.propertyName=="blendShape.MouthOpen");
            float mouthPeakTime=AnimationUtility.GetEditorCurve(clip,mouthBinding).keys.OrderByDescending(k=>k.value).First().time;
            for(int frame=0;frame<=33;frame++)
            {
                float sampleTime=frame==33?mouthPeakTime:clip.length*frame/32f;
                Restore(bones,positions,rotations,skin);clip.SampleAnimation(root,sampleTime);skin.BakeMesh(baked,true);var v=baked.vertices;cr.frames++;
                if(frame==0)first=v;if(frame==32)last=v;
                cr.maxMouthWeight=Mathf.Max(cr.maxMouthWeight,skin.GetBlendShapeWeight(0));
                for(int i=0;i<v.Length;i++){if(float.IsNaN(v[i].x)||float.IsInfinity(v[i].x)||float.IsNaN(v[i].y)||float.IsInfinity(v[i].y)||float.IsNaN(v[i].z)||float.IsInfinity(v[i].z))cr.nonfiniteVertices++;float d=localToWorld.MultiplyVector(v[i]-rest[i]).magnitude;cr.maxDisplacement=Mathf.Max(cr.maxDisplacement,d);cr.regionDisplacement[regions[i]]=Mathf.Max(cr.regionDisplacement[regions[i]],d);}
                for(int t=0;t<tris.Length;t+=3)for(int c=0;c<3;c++){int a=tris[t+c],b=tris[t+(c+1)%3];float before=localToWorld.MultiplyVector(rest[a]-rest[b]).magnitude;if(before<.00006f)continue;float ratio=localToWorld.MultiplyVector(v[a]-v[b]).magnitude/before;cr.maxEdgeRatio=Mathf.Max(cr.maxEdgeRatio,ratio);if(ratio>2)cr.stretchedEdges++;if(ratio<.2f)cr.collapsedEdges++;}
            }
            if(clip.isLooping)for(int i=0;i<first.Length;i++)cr.loopSeamError=Mathf.Max(cr.loopSeamError,localToWorld.MultiplyVector(first[i]-last[i]).magnitude);
            cr.warnings=new[]{"Surface correspondence is approximate across different anatomy; inspect fin-root and mouth stretch visually.","Mouth topology is unchanged: no new inner-mouth cavity or lip topology is supplied.","Edge distortion counts are accumulated triangle-edge samples, not unique vertices."};results.Add(cr);
        }
        Restore(bones,positions,rotations,skin);UnityEngine.Object.DestroyImmediate(baked);result.clips=results.ToArray();
        Directory.CreateDirectory(JuvenileRiggingPrototypeBuilder.Folder+"/Reports");File.WriteAllText(JuvenileRiggingPrototypeBuilder.Folder+"/Reports/ValidationResults.json",JsonUtility.ToJson(result,true));return result;
    }
    public static void CaptureContactSheets()
    {
        var scene=SceneManager.GetActiveScene();var root=scene.GetRootGameObjects().Single(g=>g.GetComponent<JuvenilePrototypePlayback>()!=null);var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
        var bones=skin.bones;var pos=bones.Select(b=>b.localPosition).ToArray();var rot=bones.Select(b=>b.localRotation).ToArray();
        var hidden=scene.GetRootGameObjects().Where(g=>g!=root&&g.GetComponentInChildren<Renderer>()!=null).ToArray();foreach(var g in hidden)g.SetActive(false);
        bool wasEnabled=skin.enabled;
        var snapshotMesh=new UnityEngine.Mesh();var snapshot=new GameObject("Temporary CPU Pose Snapshot");snapshot.transform.SetParent(root.transform,false);
        snapshot.transform.localPosition=skin.transform.localPosition;snapshot.transform.localRotation=skin.transform.localRotation;snapshot.transform.localScale=skin.transform.localScale;
        snapshot.AddComponent<MeshFilter>().sharedMesh=snapshotMesh;snapshot.AddComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;skin.enabled=false;
        var camera=scene.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).First(c=>c!=null);
        var cp=camera.transform.position;var cq=camera.transform.rotation;float co=camera.orthographicSize;
        string path=JuvenileRiggingPrototypeBuilder.Folder+"/Reports/Captures";Directory.CreateDirectory(path);
        var directions=new[]{Vector3.right,Vector3.up,Vector3.forward,new Vector3(1,.6f,1).normalized};
        try
        {
            foreach(string name in new[]{"Swim","Idle","Eat"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/Koi_"+name+".anim");
                var sheet=new Texture2D(2048,2048,TextureFormat.RGB24,false);var target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);target.Create();var tile=new Texture2D(512,512,TextureFormat.RGB24,false);
                for(int row=0;row<4;row++)
                {
                    Restore(bones,pos,rot,skin);clip.SampleAnimation(root,clip.length*row/4f);skin.BakeMesh(snapshotMesh,true);
                    for(int col=0;col<4;col++)
                    {
                        Vector3 center=root.transform.position+new Vector3(0,.015f,0);camera.transform.position=center+directions[col]*2;
                        camera.transform.rotation=Quaternion.LookRotation(center-camera.transform.position,col==1?Vector3.forward:Vector3.up);camera.orthographicSize=.375f;camera.targetTexture=target;camera.Render();
                        var old=RenderTexture.active;RenderTexture.active=target;tile.ReadPixels(new Rect(0,0,512,512),0,0);tile.Apply();RenderTexture.active=old;sheet.SetPixels(col*512,(3-row)*512,512,512,tile.GetPixels());
                    }
                }
                sheet.Apply();File.WriteAllBytes(path+"/"+name+"_SideTopFrontIso_0_25_50_75.png",sheet.EncodeToPNG());camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(sheet);
            }
        }
        finally{Restore(bones,pos,rot,skin);skin.enabled=wasEnabled;UnityEngine.Object.DestroyImmediate(snapshot);UnityEngine.Object.DestroyImmediate(snapshotMesh);foreach(var g in hidden)g.SetActive(true);camera.transform.SetPositionAndRotation(cp,cq);camera.orthographicSize=co;camera.targetTexture=null;}
        AssetDatabase.Refresh();
    }
}
