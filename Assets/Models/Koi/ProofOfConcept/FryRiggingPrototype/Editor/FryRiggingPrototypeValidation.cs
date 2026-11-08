using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class FryRiggingPrototypeValidation
{
    [Serializable] public class SampleResult
    {
        public string stage, clip, mode;
        public int samples, unresolvedBindings, nonfiniteVertices, outsideBounds;
        public float maxDisplacement, normalizedDisplacement, maxEdgeRatio, mouthPeak;
    }
    [Serializable] public class Results
    {
        public bool verticesExact, normalsExact, tangentsExact, indicesExact, allUVsExact;
        public int vertices, triangles, invalidWeights, seamErrors, mouthLeakage, sourceChanges;
        public float maxWeightSumError, fryJuvenileRatio;
        public float[] lengths, restErrors;
        public SampleResult[] clips;
    }
    public static Results Run(FryStageComparisonPlayback play, bool captures)
    {
        play.Initialize(); play.RestoreRest();
        string f=FryRiggingPrototypeBuilder.Folder; Directory.CreateDirectory(f+"/Reports/Captures");
        var mesh=play.Skins[2].sharedMesh;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(FryRiggingPrototypeBuilder.Source+".fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
        var output=new Results{vertices=mesh.vertexCount,triangles=mesh.triangles.Length/3,verticesExact=mesh.vertices.SequenceEqual(source.vertices),normalsExact=mesh.normals.SequenceEqual(source.normals),tangentsExact=mesh.tangents.SequenceEqual(source.tangents),indicesExact=mesh.triangles.SequenceEqual(source.triangles),allUVsExact=true,lengths=new float[3],restErrors=new float[3]};
        for(int uv=0;uv<8;uv++){var a=new List<Vector4>();var b=new List<Vector4>();mesh.GetUVs(uv,a);source.GetUVs(uv,b);output.allUVsExact&=a.SequenceEqual(b);}
        var weights=mesh.boneWeights;var verts=mesh.vertices;var ds=new Vector3[verts.Length];mesh.GetBlendShapeFrameVertices(mesh.GetBlendShapeIndex("MouthOpen"),0,ds,null,null);
        var seams=new Dictionary<Vector3,int>();var full=AssetDatabase.LoadAssetAtPath<GameObject>(FryRiggingPrototypeBuilder.Source+".fbx").transform.localToWorldMatrix;
        for(int i=0;i<weights.Length;i++)
        {
            var w=weights[i];float sum=w.weight0+w.weight1+w.weight2+w.weight3;output.maxWeightSumError=Mathf.Max(output.maxWeightSumError,Mathf.Abs(sum-1));
            var values=new[]{w.weight0,w.weight1,w.weight2,w.weight3};var ids=new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};
            if(values.Any(v=>float.IsNaN(v)||float.IsInfinity(v)||v<0)||ids.Any(id=>id<0||id>=10)||Mathf.Abs(sum-1)>1e-5f)output.invalidWeights++;
            if(seams.TryGetValue(verts[i],out int first)){if(!weights[first].Equals(w)||ds[first]!=ds[i])output.seamErrors++;}else seams.Add(verts[i],i);
            var p=full.MultiplyPoint3x4(verts[i]);if((Mathf.Abs(p.x)>.145f||p.z<.895f)&&(ds[i].sqrMagnitude>0||FryRiggingPrototypeBuilder.Influence(w,3)>0))output.mouthLeakage++;
        }
        var baseline=new Vector3[3][];var bake=new UnityEngine.Mesh();
        try
        {
            for(int s=0;s<3;s++)
            {
                var skin=play.Skins[s];baseline[s]=skin.sharedMesh.vertices.Select(v=>skin.transform.TransformPoint(v)).ToArray();output.lengths[s]=baseline[s].Max(v=>v.z)-baseline[s].Min(v=>v.z);
                skin.BakeMesh(bake,true);output.restErrors[s]=bake.vertices.Select((v,i)=>Vector3.Distance(skin.transform.TransformPoint(v),baseline[s][i])).Max();
            }
            output.fryJuvenileRatio=output.lengths[2]/output.lengths[1];
            var records=new List<SampleResult>();
            for(int mode=0;mode<2;mode++)
            {
                play.AdaptStages=mode==1;
                for(int clip=0;clip<3;clip++)
                {
                    var c=play.Clips[clip];var mouthBinding=AnimationUtility.GetCurveBindings(c).First(b=>b.propertyName=="blendShape.MouthOpen");var curve=AnimationUtility.GetEditorCurve(c,mouthBinding);var peak=curve.keys.OrderByDescending(k=>k.value).First();
                    var times=Enumerable.Range(0,33).Select(i=>c.length*i/32f).Concat(new[]{peak.time}).Distinct().OrderBy(t=>t).ToArray();
                    var row=new SampleResult[3];for(int s=0;s<3;s++){row[s]=new SampleResult{stage=play.Targets[s].name,clip=c.name,mode=mode==0?"Source":"StageAdapted",samples=times.Length,mouthPeak=peak.value};foreach(var binding in AnimationUtility.GetCurveBindings(c)){var target=play.Targets[s].transform.Find(binding.path);if(target==null||target.GetComponent(binding.type)==null)row[s].unresolvedBindings++;}}
                    foreach(float t in times)
                    {
                        Sample(play,c,t,clip);
                        for(int s=0;s<3;s++)
                        {
                            var skin=play.Skins[s];skin.BakeMesh(bake,true);var bv=bake.vertices;var points=bv.Select(v=>skin.transform.TransformPoint(v)).ToArray();
                            for(int i=0;i<points.Length;i++)
                            {
                                var v=points[i];if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))row[s].nonfiniteVertices++;
                                row[s].maxDisplacement=Mathf.Max(row[s].maxDisplacement,Vector3.Distance(v,baseline[s][i]));if(!skin.localBounds.Contains(bv[i]))row[s].outsideBounds++;
                            }
                            var indices=skin.sharedMesh.triangles;float threshold=output.lengths[s]*1e-4f;
                            for(int i=0;i<indices.Length;i+=3)for(int edge=0;edge<3;edge++){int a=indices[i+edge],b=indices[i+(edge+1)%3];float rest=Vector3.Distance(baseline[s][a],baseline[s][b]);if(rest>threshold)row[s].maxEdgeRatio=Mathf.Max(row[s].maxEdgeRatio,Vector3.Distance(points[a],points[b])/rest);}
                        }
                    }
                    for(int s=0;s<3;s++){row[s].normalizedDisplacement=row[s].maxDisplacement/output.lengths[s];records.Add(row[s]);}
                    if(captures){Sample(play,c,peak.time,clip);CaptureSheet(play,"Clip_"+c.name+"_"+(mode==0?"Source":"Adapted"),false);File.WriteAllText(f+"/Reports/Captures/"+c.name+"_"+mode+"_Labels.txt","Columns: Adult | Juvenile | Fry; rows: side, top, front, three-quarter.\nTime="+peak.time+"; MouthOpen="+peak.value+"; same orthographic size .36 for all columns; ratio 1:.60:.30; Edit Mode sampled NOT runtime.");}
                }
            }
            play.RestoreRest();if(captures)CaptureSheet(play,"Rest_ThreeStages",false);
            if(captures){Sample(play,play.Clips[2],AnimationUtility.GetEditorCurve(play.Clips[2],AnimationUtility.GetCurveBindings(play.Clips[2]).First(b=>b.propertyName=="blendShape.MouthOpen")).keys.OrderByDescending(k=>k.value).First().time,2);CaptureSheet(play,"Eat_EyesMouth_Closeup",true);}
            output.clips=records.ToArray();
            File.WriteAllText(f+"/Reports/ValidationResults.json",JsonUtility.ToJson(output,true));
            File.WriteAllText(f+"/Reports/Captures/README.txt","All Clip_ and Rest images are diagnostic Edit Mode camera renders, not runtime footage. Stage columns adult/juvenile/fry; rows side/top/front/three-quarter. Closeup sheet uses independently magnified head region: adult .13, juvenile .09, fry .055 orthographic size; NOT size comparison. No capture is a replacement texture.");
            AssetDatabase.Refresh();return output;
        }
        finally {UnityEngine.Object.DestroyImmediate(bake);play.RestoreRest();play.AdaptStages=true;}
    }
    public static void Sample(FryStageComparisonPlayback play,AnimationClip clip,float time,int index)
    {
        play.RestoreRest();for(int s=0;s<3;s++)clip.SampleAnimation(play.Targets[s],time);play.ApplyAdaptation(index);
    }
    private static void CaptureSheet(FryStageComparisonPlayback play,string name,bool closeup)
    {
        const int width=640,height=400;
        var sheet=new Texture2D(width*3,height*4,TextureFormat.RGB24,false);
        var cg=new GameObject("TemporaryDiagnosticCamera"){hideFlags=HideFlags.HideAndDontSave};var cam=cg.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.065f,.08f,.11f);cam.nearClipPlane=.001f;cam.farClipPlane=10;
        var rt=new RenderTexture(width,height,24);rt.Create();cam.targetTexture=rt;var active=RenderTexture.active;
        var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);var states=renderers.Select(r=>r.enabled).ToArray();
        Vector3[] views={new Vector3(1,0,0),new Vector3(0,1,0),new Vector3(0,0,1),new Vector3(1,.65f,1)};
        try
        {
            for(int s=0;s<3;s++)
            {
                foreach(var r in renderers)r.enabled=r==play.Skins[s];
                for(int angle=0;angle<4;angle++)
                {
                    Vector3 center=play.Targets[s].transform.position;
                    if(closeup)center=play.Targets[s].transform.TransformPoint(play.Skins[s].bones[2].position-play.Targets[s].transform.position);
                    // Bone position is already world space; use it directly for closeups.
                    if(closeup)center=play.Skins[s].bones[2].position;
                    cam.orthographicSize=closeup?(s==0?.13f:s==1?.09f:.055f):.36f;
                    cg.transform.position=center+views[angle].normalized*2;cg.transform.LookAt(center,angle==1?Vector3.forward:Vector3.up);
                    cam.Render();RenderTexture.active=rt;var tile=new Texture2D(width,height,TextureFormat.RGB24,false);tile.ReadPixels(new Rect(0,0,width,height),0,0);tile.Apply();sheet.SetPixels(s*width,(3-angle)*height,width,height,tile.GetPixels());UnityEngine.Object.DestroyImmediate(tile);
                }
            }
            sheet.Apply();File.WriteAllBytes(FryRiggingPrototypeBuilder.Folder+"/Reports/Captures/"+name+".png",sheet.EncodeToPNG());
        }
        finally {for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=states[i];cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.Object.DestroyImmediate(cg);}
    }
}
