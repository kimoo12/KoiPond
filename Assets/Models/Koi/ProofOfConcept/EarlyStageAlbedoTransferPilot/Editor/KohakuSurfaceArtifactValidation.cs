using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using KoiPond.EarlyStageTransferPilot;
public static class KohakuSurfaceArtifactValidation
{
 public const string F=EarlyStageAlbedoTransferBuilder.Folder;
 public const string R=F+"/Reports/SurfaceArtifactRefinement";
 public static string Prefab(bool j)=>j?EarlyStageAlbedoTransferBuilder.Juvenile:EarlyStageAlbedoTransferBuilder.Fry;
 public static string Albedo(bool j,bool candidate)=>F+"/Textures/Kohaku_"+(j?"Juvenile":"Fry")+"_RedPattern_Candidate_"+(j?(candidate?"v03":"v02"):(candidate?"v05":"v04"))+".png";
 static string Hash(string p){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p)));}
 public static void Protect(){if(Directory.Exists(R))throw new Exception("Output folder exists");var files=Directory.GetFiles("Assets","*",SearchOption.AllDirectories);Directory.CreateDirectory(R);File.WriteAllLines(R+"/Protection.tsv",files.Select(p=>p+"\t"+Hash(p)));}
 public static string Verify(){var lines=File.ReadAllLines(R+"/Protection.tsv");int changed=0;foreach(var line in lines){var a=line.Split('\t');if(!File.Exists(a[0])||Hash(a[0])!=a[1])changed++;}if(changed!=0)throw new Exception("Protected files changed="+changed);return "Unchanged protected files="+lines.Length;}
 public static string Capture(bool j,bool candidate=false,bool full=false)
 {
  string stage=j?"Juvenile":"Fry";var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  var cg=new GameObject("Transient diagnostic camera");var cam=cg.AddComponent<Camera>();cam.orthographic=true;cam.cullingMask=1<<31;cam.nearClipPlane=.001f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.18f,.18f,.18f);
  var lg=new GameObject("Transient diagnostic light");var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.cullingMask=1<<31;
  var rt=new RenderTexture(1024,1024,24);cam.targetTexture=rt;var atlas=new Texture2D(3072,4096,TextureFormat.RGB24,false);GameObject go=null;Material mat=null;
  try{for(int row=0;row<4;row++){go=KohakuCandidateBake.CopyForReview(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(j)));foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();mat=new Material(skin.sharedMaterial);if(candidate){var saved=AssetDatabase.LoadAssetAtPath<Material>(F+"/Materials/Kohaku_"+stage+"_ArtifactCandidate.mat");if(saved!=null){UnityEngine.Object.DestroyImmediate(mat);mat=new Material(saved);}}var map=AssetDatabase.LoadAssetAtPath<Texture2D>(Albedo(j,candidate));if(row==0){mat.shader=Shader.Find("Universal Render Pipeline/Unlit");mat.SetColor("_BaseColor",Color.white);}mat.SetTexture("_BaseMap",map);if(mat.HasProperty("_MainTex"))mat.SetTexture("_MainTex",map);if(row==1){mat.DisableKeyword("_NORMALMAP");mat.SetTexture("_BumpMap",null);if(mat.HasProperty("_BumpScale"))mat.SetFloat("_BumpScale",0);}if(row<3&&row>0){if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",0);if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",0);mat.DisableKeyword("_METALLICSPECGLOSSMAP");}skin.sharedMaterial=mat;
  var bounds=skin.bounds;var center=full?bounds.center:go.transform.TransformPoint(new Vector3(0,.075f,-.015f));cam.orthographicSize=full?Mathf.Max(bounds.size.x,bounds.size.z)*.65f:go.transform.lossyScale.x*.125f;
  for(int col=0;col<3;col++){var view=col==1?Vector3.up:col==2?new Vector3(-1,.25f,0):new Vector3(1,.25f,0);lg.transform.rotation=col==2?Quaternion.Euler(35,145,0):Quaternion.Euler(40,-25,0);cg.transform.position=center+view.normalized*2;cg.transform.LookAt(center,col==1?Vector3.forward:Vector3.up);cam.Render();RenderTexture.active=rt;atlas.ReadPixels(new Rect(0,0,1024,1024),col*1024,(3-row)*1024);}UnityEngine.Object.DestroyImmediate(go);go=null;UnityEngine.Object.DestroyImmediate(mat);mat=null;}
  atlas.Apply();string path=R+"/"+stage+(candidate?"_Candidate":"_Current")+(full?"_Full":"_Close")+".png";File.WriteAllBytes(path,atlas.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var parts=AssetDatabase.LoadAssetAtPath<Texture2D>(path).GetEntityId().ToString().Split(':');return path+" preview="+(long.Parse(parts[0])+(long.Parse(parts[1])<<32));}
  finally{if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(mat!=null)UnityEngine.Object.DestroyImmediate(mat);RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(lg);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(atlas);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
 }
}
