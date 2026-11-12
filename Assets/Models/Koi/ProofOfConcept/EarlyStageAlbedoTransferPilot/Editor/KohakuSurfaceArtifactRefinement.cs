using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KoiPond.EarlyStageTransferPilot;
public static class KohakuSurfaceArtifactRefinement
{
 const string F=EarlyStageAlbedoTransferBuilder.Folder;
 const string R=KohakuSurfaceArtifactValidation.R;
 sealed class Data
 {
  public int S;public Color32[] Current,Original,Normal;public bool[] Body,Fin,Protected,Occupied,Spot;public int Conflicts,FinFaces,Degenerate;
  public string NormalPath,OriginalPath;public Material Material;
  public Data(bool j)
  {
   var skin=AssetDatabase.LoadAssetAtPath<GameObject>(KohakuSurfaceArtifactValidation.Prefab(j)).GetComponentInChildren<SkinnedMeshRenderer>();Material=skin.sharedMaterial;
   OriginalPath=AssetDatabase.GetAssetPath(Material.GetTexture("_BaseMap"));NormalPath=AssetDatabase.GetAssetPath(Material.GetTexture("_BumpMap"));
   var current=Decode(KohakuSurfaceArtifactValidation.Albedo(j,false));S=current.width;Current=current.GetPixels32();UnityEngine.Object.DestroyImmediate(current);
   var original=Decode(OriginalPath);var normal=Decode(NormalPath);
   if(original.width!=S||original.height!=S||normal.width!=S||normal.height!=S)throw new Exception("Source resolution mismatch; no implicit resampling allowed");
   Original=original.GetPixels32();Normal=normal.GetPixels32();UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(normal);
   var type=typeof(KohakuFryPatternTrial).GetNestedType("Surface",BindingFlags.NonPublic);var data=Activator.CreateInstance(type,new object[]{KohakuSurfaceArtifactValidation.Prefab(j)});var v=(Vector3[])type.GetField("V").GetValue(data);var uv=(Vector2[])type.GetField("UV").GetValue(data);var tri=(int[])type.GetField("T").GetValue(data);var reg=(int[])type.GetField("Regions").GetValue(data);
   var root=AssetDatabase.LoadAssetAtPath<GameObject>(KohakuSurfaceArtifactValidation.Prefab(j));var matrix=root.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;var normals=skin.sharedMesh.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();
   float hi=(float)type.GetField("Nose").GetValue(data),shoulder=(float)type.GetField("Shoulder").GetValue(data);float face=j?shoulder+(hi-shoulder)*.25f:.16f;float fh=j?v.Where(p=>p.z>face&&Mathf.Abs(p.x)>.035f).Max(p=>p.y)*.52f:.052f;
   // Thin, vertical dorsal surfaces connected to a high dorsal tip seed; upward-facing body skin is excluded.
   var eligible=new bool[reg.Length];var blade=new bool[reg.Length];var faces=new List<int>[v.Length];for(int i=0;i<v.Length;i++)faces[i]=new List<int>();var queue=new Queue<int>();
   for(int f=0;f<reg.Length;f++){int a=tri[f*3],b=tri[f*3+1],c=tri[f*3+2];faces[a].Add(f);faces[b].Add(f);faces[c].Add(f);var p=(v[a]+v[b]+v[c])/3;var n=(normals[a]+normals[b]+normals[c]).normalized;eligible[f]=p.y>.052f&&p.z>-.18f&&p.z<.17f&&Mathf.Max(Mathf.Abs(v[a].x),Mathf.Abs(v[b].x),Mathf.Abs(v[c].x))<.025f&&Mathf.Abs(n.x)>.70f;if(eligible[f]&&p.y>.12f){blade[f]=true;queue.Enqueue(f);}}
   while(queue.Count>0){int f=queue.Dequeue();for(int k=0;k<3;k++)foreach(int n in faces[tri[f*3+k]])if(eligible[n]&&!blade[n]){blade[n]=true;queue.Enqueue(n);}}
   Body=new bool[S*S];Fin=new bool[S*S];Protected=new bool[S*S];Occupied=new bool[S*S];
   for(int f=0;f<reg.Length;f++)
   {
    int ia=tri[f*3],ib=tri[f*3+1],ic=tri[f*3+2];if(blade[f])FinFaces++;var u=uv[ia]*S;var w=uv[ib]*S;var q=uv[ic]*S;float det=(w.x-u.x)*(q.y-u.y)-(w.y-u.y)*(q.x-u.x);if(Mathf.Abs(det)<1e-10f){Degenerate++;continue;}
    for(int y=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.y,w.y,q.y)-.5f));y<=Mathf.Min(S-1,Mathf.FloorToInt(Mathf.Max(u.y,w.y,q.y)-.5f));y++)for(int x=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.x,w.x,q.x)-.5f));x<=Mathf.Min(S-1,Mathf.FloorToInt(Mathf.Max(u.x,w.x,q.x)-.5f));x++)
    {float px=x+.5f-u.x,py=y+.5f-u.y,b=(px*(q.y-u.y)-py*(q.x-u.x))/det,c=((w.x-u.x)*py-(w.y-u.y)*px)/det,a=1-b-c;if(a<-1e-6f||b<-1e-6f||c<-1e-6f)continue;int i=y*S+x;var p=v[ia]*a+v[ib]*b+v[ic]*c;bool fin=blade[f];bool pr=!fin&&((reg[f]!=0&&reg[f]!=4)||(p.z>face&&p.y<fh));bool body=!fin&&!pr;Occupied[i]=true;Fin[i]|=fin;Protected[i]|=pr;Body[i]|=body;}
   }
   for(int i=0;i<Body.Length;i++)if((Body[i]&&Fin[i])||(Body[i]&&Protected[i])){Conflicts++;Body[i]=false;Fin[i]=false;Protected[i]=true;}
   // Defect domains arise from original achromatic dark pigmentation, never global brightness.
   var seed=new bool[S*S];for(int i=0;i<seed.Length;i++){var c=Original[i];seed[i]=Body[i]&&Red(Current[i])&&c.r<238&&Mathf.Abs(c.r-c.g)<75&&Mathf.Abs(c.r-c.b)<75;}
   var seen=new bool[S*S];var core=new bool[S*S];for(int i=0;i<seed.Length;i++)if(seed[i]&&!seen[i]){var group=new List<int>();queue.Clear();queue.Enqueue(i);seen[i]=true;while(queue.Count>0){int k=queue.Dequeue();group.Add(k);foreach(int n in Neighbors(k,S))if(seed[n]&&!seen[n]){seen[n]=true;queue.Enqueue(n);}}if(group.Count>=20)foreach(int k in group)core[k]=true;}
   // Include anti-aliased residual outlines around the original spot, preserving pattern edges.
   Spot=(bool[])core.Clone();for(int pass=0;pass<12;pass++){var next=(bool[])Spot.Clone();for(int i=0;i<Spot.Length;i++)if(Spot[i])foreach(int n in Neighbors(i,S))if(Body[n]&&Red(Current[n]))next[n]=true;Spot=next;}
   for(int y=3;y<S-3;y++)for(int x=3;x<S-3;x++){int i=y*S+x;if(!Spot[i])continue;for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)if(!Red(Current[i+dy*S+dx]))Spot[i]=false;}
  }
 }
 static Texture2D Decode(string p){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!t.LoadImage(File.ReadAllBytes(p)))throw new Exception(p);return t;}
 static bool Red(Color32 c)=>c.r>30&&c.g<c.r*.22f&&c.b<c.r*.22f;
 static IEnumerable<int> Neighbors(int i,int s){if(i%s>0)yield return i-1;if(i%s<s-1)yield return i+1;if(i>=s)yield return i-s;if(i<s*(s-1))yield return i+s;}
 public static string Audit(bool j){var d=new Data(j);string text=(j?"Juvenile":"Fry")+": dorsal connected faces="+d.FinFaces+", shared protected texels="+d.Conflicts+", degenerate UV="+d.Degenerate+", spot footprint="+d.Spot.Count(v=>v)+"\nNormal source="+d.NormalPath+"\nDiagnosis: albedo pigmentation/spot rims visible without lighting; normal-enabled spot relief also visible. Geometry-driven broad facets retained.\n";File.WriteAllText(R+"/"+(j?"Juvenile":"Fry")+"_Audit.txt",text);return text;}
 static Color32[] Solve(Color32[] input,bool[] mask,int s,bool normal)
 {
  var ids=Enumerable.Range(0,mask.Length).Where(i=>mask[i]).ToArray();var values=input.Select(c=>new Vector3(c.r,c.g,c.b)).ToArray();var next=new Vector3[values.Length];
  var seen=new bool[mask.Length];var queue=new Queue<int>();
  foreach(int seed in ids)if(!seen[seed]){var group=new List<int>();var border=new HashSet<int>();queue.Enqueue(seed);seen[seed]=true;while(queue.Count>0){int i=queue.Dequeue();group.Add(i);foreach(int n in Neighbors(i,s)){if(!mask[n])border.Add(n);else if(!seen[n]){seen[n]=true;queue.Enqueue(n);}}}var mean=Vector3.zero;foreach(int i in border)mean+=values[i];if(border.Count>0)mean/=border.Count;foreach(int i in group)values[i]=mean;}
  // Dirichlet harmonic interpolation retains clean border variation; no original spot residual is reintroduced.
  for(int pass=0;pass<320;pass++){foreach(int i in ids){var sum=Vector3.zero;int count=0;foreach(int n in Neighbors(i,s)){sum+=values[n];count++;}next[i]=count>0?sum/count:values[i];}foreach(int i in ids)values[i]=next[i];}
  var output=(Color32[])input.Clone();foreach(int i in ids){var c=values[i];if(normal){var n=new Vector3(c.x/127.5f-1,c.y/127.5f-1,c.z/127.5f-1).normalized;if(n.sqrMagnitude<.5f)n=Vector3.forward;c=(n+Vector3.one)*127.5f;}output[i]=new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(c.x),0,255),(byte)Mathf.Clamp(Mathf.RoundToInt(c.y),0,255),(byte)Mathf.Clamp(Mathf.RoundToInt(c.z),0,255),input[i].a);}return output;
 }
 static void Pad(Color32[] pixels,Data d){var filled=(bool[])d.Occupied.Clone();for(int pass=0;pass<12;pass++){var ids=new List<int>();var cols=new List<Color32>();for(int y=1;y<d.S-1;y++)for(int x=1;x<d.S-1;x++){int i=y*d.S+x;if(filled[i])continue;int selected=-1;foreach(int n in Neighbors(i,d.S))if(filled[n]){selected=n;break;}if(selected>=0){ids.Add(i);cols.Add(pixels[selected]);}}for(int k=0;k<ids.Count;k++){filled[ids[k]]=true;pixels[ids[k]]=cols[k];}}}
 static Color32[] Process(Data d,bool normal){var result=Solve(normal?d.Normal:d.Current,d.Spot,d.S,normal);if(!normal)for(int i=0;i<result.Length;i++)if(d.Fin[i]&&!d.Protected[i]&&Red(d.Current[i])&&!Red(d.Original[i])){result[i]=d.Original[i];result[i].a=d.Current[i].a;}Pad(result,d);return result;}
 static void Save(string path,Color32[] pixels,int s,bool normal){if(File.Exists(path))throw new Exception("Refusing overwrite "+path);var t=new Texture2D(s,s,TextureFormat.RGBA32,false,true);t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.maxTextureSize=s;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.sRGBTexture=!normal;imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;imp.convertToNormalmap=false;imp.filterMode=FilterMode.Bilinear;imp.mipmapEnabled=true;imp.SaveAndReimport();}
 public static string RebuildReview(bool j)
 {
  var d=new Data(j);string stage=j?"Juvenile":"Fry",version=j?"v03":"v05";var a=Process(d,false);var n=Process(d,true);
  if(!a.SequenceEqual(Process(d,false))||!n.SequenceEqual(Process(d,true)))throw new Exception("Repeatability failed");
  int bad=0;for(int i=0;i<a.Length;i++)if(d.Occupied[i]){if(!a[i].Equals(d.Current[i])&&!d.Spot[i]&&!d.Fin[i])bad++;if(!n[i].Equals(d.Normal[i])&&!d.Spot[i])bad++;}if(bad!=0)throw new Exception("Outside-mask changes");
  string[] paths={KohakuSurfaceArtifactValidation.Albedo(j,true),F+"/Textures/Kohaku_"+stage+"_Normal_Candidate_"+version+".png"};var sets=new[]{a,n};
  for(int k=0;k<2;k++){string backup=R+"/"+Path.GetFileName(paths[k])+".firstpass.bytes";if(File.Exists(backup))throw new Exception("Revision already performed");File.Copy(paths[k],backup);var t=new Texture2D(d.S,d.S,TextureFormat.RGBA32,false,true);t.SetPixels32(sets[k]);t.Apply();File.WriteAllBytes(paths[k],t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(paths[k],ImportAssetOptions.ForceSynchronousImport);}
  File.AppendAllText(R+"/"+stage+"_Validation.txt","Revision: clean-border initialization, deterministic=true, outside-mask=0. Initial unapproved output bytes backed up.\n");return stage+" corrected review output; deterministic and masked checks passed";
 }
 public static string Bake(bool j)
 {
  var d=new Data(j);string stage=j?"Juvenile":"Fry",version=j?"v03":"v05";var albedo=Process(d,false);var repeat=Process(d,false);if(!albedo.SequenceEqual(repeat))throw new Exception("Albedo nondeterministic");var normals=Process(d,true);repeat=Process(d,true);if(!normals.SequenceEqual(repeat))throw new Exception("Normals nondeterministic");
  int changed=0,fin=0,bad=0,normalChanged=0;for(int i=0;i<albedo.Length;i++){if(d.Occupied[i]&&!albedo[i].Equals(d.Current[i])){changed++;if(d.Fin[i])fin++;if(!d.Spot[i]&&!d.Fin[i])bad++;}if(d.Occupied[i]&&!normals[i].Equals(d.Normal[i])){normalChanged++;if(!d.Spot[i])bad++;}}if(bad!=0)throw new Exception("Outside-mask changes "+bad);
  Save(KohakuSurfaceArtifactValidation.Albedo(j,true),albedo,d.S,false);string np=F+"/Textures/Kohaku_"+stage+"_Normal_Candidate_"+version+".png";Save(np,normals,d.S,true);
  var mat=new Material(d.Material);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(KohakuSurfaceArtifactValidation.Albedo(j,true)));if(mat.HasProperty("_MainTex"))mat.SetTexture("_MainTex",mat.GetTexture("_BaseMap"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(np));string mp=F+"/Materials/Kohaku_"+stage+"_ArtifactCandidate.mat";if(File.Exists(mp))throw new Exception("Material exists");AssetDatabase.CreateAsset(mat,mp);AssetDatabase.SaveAssets();
  var diag=new Color32[albedo.Length];for(int i=0;i<diag.Length;i++)diag[i]=d.Spot[i]?new Color32(255,0,0,255):d.Fin[i]?new Color32(0,255,0,255):new Color32(0,0,0,255);Save(R+"/"+stage+"_Masks.png",diag,d.S,false);
  string text=stage+" "+version+": changed surface albedo="+changed+", fin restoration="+fin+", normal="+normalChanged+", outside mask="+bad+", deterministic=true\n320 synchronous harmonic iterations; 12-pixel spot footprint dilation constrained to red interiors and protected owners; 12-pixel exterior padding. All original maps/assignments preserved.\n";File.WriteAllText(R+"/"+stage+"_Validation.txt",text);return text;
 }
}
