using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace KoiPond.JuvenileAppearancePrototype
{
    public static class JuvenileNormalCorrection
    {
        public const string Root = JuvenileKohakuAppearanceAuthoring.Root + "/NormalCorrection_v01";
        private static float Cross(Vector2 a, Vector2 b) { return a.x*b.y-a.y*b.x; }
        private static bool[] Expand(bool[] input, int width, int height, int radius, bool erode)
        {
            int stride=width+1;var sum=new int[stride*(height+1)];
            for(int y=0;y<height;y++){int row=0;for(int x=0;x<width;x++){if(input[y*width+x])row++;sum[(y+1)*stride+x+1]=sum[y*stride+x+1]+row;}}
            var output=new bool[input.Length];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                int x0=Math.Max(0,x-radius),x1=Math.Min(width-1,x+radius),y0=Math.Max(0,y-radius),y1=Math.Min(height-1,y+radius);
                int count=sum[(y1+1)*stride+x1+1]-sum[y0*stride+x1+1]-sum[(y1+1)*stride+x0]+sum[y0*stride+x0];
                output[y*width+x]=erode?count==(x1-x0+1)*(y1-y0+1):count>0;
            }
            return output;
        }
        public static string CorrectOnce()
        {
            string output=Root+"/Textures/Juvenile_Kohaku_Normal_ContourCorrection_v01.png";
            if(File.Exists(output))throw new InvalidOperationException("One correction already exists; refusing another pass.");
            var baseMaterial=AssetDatabase.LoadAssetAtPath<Material>(JuvenileKohakuAppearanceAuthoring.Root+"/Materials/Juvenile_Kohaku_Appearance_v01.mat");
            string normalPath=AssetDatabase.GetAssetPath(baseMaterial.GetTexture("_BumpMap"));
            var normal=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            var albedo=new Texture2D(2,2,TextureFormat.RGBA32,false,false);
            try
            {
                if(!ImageConversion.LoadImage(normal,File.ReadAllBytes(normalPath))||!ImageConversion.LoadImage(albedo,File.ReadAllBytes(JuvenileKohakuAppearanceAuthoring.SourceAlbedo)))throw new InvalidOperationException("Cannot decode source maps.");
                int width=normal.width,height=normal.height,n=width*height;
                if(width!=albedo.width||height!=albedo.height)throw new InvalidOperationException("Map dimensions differ.");
                var original=normal.GetPixels32();var colors=albedo.GetPixels32();var result=(Color32[])original.Clone();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(JuvenileKohakuAppearanceAuthoring.SourcePrefab);
                var renderer=prefab.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=renderer.sharedMesh;
                var matrix=prefab.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                var positions=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();var uv=mesh.uv;var triangles=mesh.triangles;
                // Read the already-reviewed body eligibility without changing or running the albedo writer.
                var eligibility=typeof(JuvenileKohakuAppearanceAuthoring).GetMethod("Eligibility",BindingFlags.Static|BindingFlags.NonPublic);
                var eligible=positions.Select(p=>(float)eligibility.Invoke(null,new object[]{p})>.98f).ToArray();
                var owner=new byte[n]; // 1 confirmed interior, 2 any protected owner (takes precedence).
                for(int tri=0;tri<triangles.Length;tri+=3){
                    int i0=triangles[tri],i1=triangles[tri+1],i2=triangles[tri+2];
                    byte domain=eligible[i0]&&eligible[i1]&&eligible[i2]?(byte)1:(byte)2;
                    var a=uv[i0];var b=uv[i1];var c=uv[i2];float det=Cross(b-a,c-a);if(Mathf.Abs(det)<1e-14f)continue;
                    int x0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))*width),0,width-1),x1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))*width),0,width-1);
                    int y0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))*height),0,height-1),y1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))*height),0,height-1);
                    for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){
                        var q=new Vector2((x+.5f)/width,(y+.5f)/height);float v=Cross(q-a,c-a)/det,w=Cross(b-a,q-a)/det,u=1-v-w;
                        if(u>=-1e-6f&&v>=-1e-6f&&w>=-1e-6f){int pixel=y*width+x;if(domain>owner[pixel])owner[pixel]=domain;}
                    }
                }
                var safe=Expand(owner.Select(o=>o==1).ToArray(),width,height,3,true);
                var pigment=new bool[n];
                for(int i=0;i<n;i++){
                    var c=colors[i];int max=Math.Max(c.r,Math.Max(c.g,c.b));
                    pigment[i]=(max<178)||(c.r-c.g>32&&c.r-c.b>35);
                }
                // Reject tiny color/scale flecks; actual old markings have substantial connected footprint.
                var seen=new bool[n];var queue=new int[n];int components=0;
                for(int seed=0;seed<n;seed++)if(pigment[seed]&&!seen[seed]){
                    int start=0,end=1;queue[0]=seed;seen[seed]=true;
                    while(start<end){int p=queue[start++],x=p%width,y=p/width;
                        if(x>0)Visit(p-1,pigment,seen,queue,ref end);if(x<width-1)Visit(p+1,pigment,seen,queue,ref end);
                        if(y>0)Visit(p-width,pigment,seen,queue,ref end);if(y<height-1)Visit(p+width,pigment,seen,queue,ref end);
                    }
                    if(end<120){for(int k=0;k<end;k++)pigment[queue[k]]=false;}else components++;
                }
                var outside=Expand(pigment,width,height,14,false);var inside=Expand(pigment,width,height,14,true);
                var mask=new bool[n];var selected=new System.Collections.Generic.List<int>();
                for(int i=0;i<n;i++)if(safe[i]&&outside[i]&&!inside[i]){mask[i]=true;selected.Add(i);}
                if(selected.Count==0)throw new InvalidOperationException("No reviewed body spot contours located.");
                // Harmonic reconstruction only in 28-pixel contour bands. Interiors and all other anatomy
                // remain original. Solver sweeps are a fixed numerical solve, not visual correction attempts.
                var normals=new Vector3[n];
                for(int i=0;i<n;i++){var c=original[i];normals[i]=new Vector3(c.r/127.5f-1,c.g/127.5f-1,c.b/127.5f-1).normalized;}
                foreach(int i in selected)normals[i]=Vector3.forward;
                for(int sweep=0;sweep<360;sweep++)foreach(int i in selected){normals[i]=(normals[i-1]+normals[i+1]+normals[i-width]+normals[i+width])*.25f;}
                int changed=0,protectedChanges=0;float maximumDeviation=0;
                foreach(int i in selected){var v=normals[i].normalized;if(float.IsNaN(v.x)||v.z<=0)throw new InvalidOperationException("Invalid reconstructed tangent normal.");
                    result[i]=new Color32((byte)Mathf.RoundToInt((v.x+1)*127.5f),(byte)Mathf.RoundToInt((v.y+1)*127.5f),(byte)Mathf.RoundToInt((v.z+1)*127.5f),original[i].a);
                    if(!result[i].Equals(original[i]))changed++;maximumDeviation=Mathf.Max(maximumDeviation,Mathf.Abs(1-v.magnitude));
                }
                for(int i=0;i<n;i++)if(!mask[i]&&!result[i].Equals(original[i]))protectedChanges++;
                if(protectedChanges!=0)throw new InvalidOperationException("Out-of-mask normal bytes changed.");
                normal.SetPixels32(result);normal.Apply();Directory.CreateDirectory(Root+"/Textures");File.WriteAllBytes(output,normal.EncodeToPNG());AssetDatabase.ImportAsset(output);
                var sourceImporter=(TextureImporter)AssetImporter.GetAtPath(normalPath);var importer=(TextureImporter)AssetImporter.GetAtPath(output);
                importer.textureType=TextureImporterType.NormalMap;importer.convertToNormalmap=false;importer.flipGreenChannel=sourceImporter.flipGreenChannel;importer.sRGBTexture=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=width;importer.mipmapEnabled=sourceImporter.mipmapEnabled;importer.filterMode=sourceImporter.filterMode;importer.wrapMode=sourceImporter.wrapMode;importer.SaveAndReimport();
                var material=new Material(baseMaterial);material.name="Juvenile Kohaku - single spot-contour normal correction";material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(output));Directory.CreateDirectory(Root+"/Materials");AssetDatabase.CreateAsset(material,Root+"/Materials/Juvenile_Kohaku_NormalCorrected_v01.mat");
                string audit="Retained pigment components: "+components+"; contour-band texels: "+selected.Count+"; changed normal texels: "+changed+"; out-of-mask/protected normal changes: "+protectedChanges+"; normalization error: "+maximumDeviation+".\nOnly the normal reference changes; albedo, shader, normal strength and other maps remain unchanged.\n";
                File.WriteAllText(Root+"/Reports/CorrectionAudit.txt",audit);AssetDatabase.SaveAssets();return audit;
            }
            finally{UnityEngine.Object.DestroyImmediate(normal);UnityEngine.Object.DestroyImmediate(albedo);}
        }
        private static void Visit(int i,bool[] pigment,bool[] seen,int[] queue,ref int end)
        {
            if(pigment[i]&&!seen[i]){seen[i]=true;queue[end++]=i;}
        }
    }
}
