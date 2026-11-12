using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using KoiPond.EarlyStageTransferPilot;

public static class KohakuFrySeamCleanup
{
    const string Folder = EarlyStageAlbedoTransferBuilder.Folder;
    const string Input = Folder + "/Textures/Kohaku_Fry_RedPattern_Candidate_v02.png";
    const string Output = Folder + "/Textures/Kohaku_Fry_RedPattern_Candidate_v03.png";
    public static string Bake()
    {
        if (File.Exists(Output)) throw new InvalidOperationException("v03 exists; refusing overwrite");
        var tex = new Texture2D(2,2,TextureFormat.RGBA32,false);
        tex.LoadImage(File.ReadAllBytes(Input)); int size = tex.width;
        var pixels = tex.GetPixels32(); var before = (Color32[])pixels.Clone();
        var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(EarlyStageAlbedoTransferBuilder.Fry).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
        var uv = mesh.uv; var triangles = mesh.triangles; var covered = new bool[pixels.Length];
        // Exact surface footprint. Padding never modifies an occupied face/fin/eye/marking texel.
        for (int f = 0; f < triangles.Length; f += 3)
        {
            Vector2 u=uv[triangles[f]]*size,v=uv[triangles[f+1]]*size,w=uv[triangles[f+2]]*size;
            float det=(v.x-u.x)*(w.y-u.y)-(v.y-u.y)*(w.x-u.x);
            if (Mathf.Abs(det)<1e-10f) continue;
            int minX=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.x,v.x,w.x)-.5f)),maxX=Mathf.Min(size-1,Mathf.FloorToInt(Mathf.Max(u.x,v.x,w.x)-.5f));
            int minY=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.y,v.y,w.y)-.5f)),maxY=Mathf.Min(size-1,Mathf.FloorToInt(Mathf.Max(u.y,v.y,w.y)-.5f));
            for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
            {
                float px=x+.5f-u.x,py=y+.5f-u.y;
                float b=(px*(w.y-u.y)-py*(w.x-u.x))/det,c=((v.x-u.x)*py-(v.y-u.y)*px)/det;
                if(b>=-1e-6f&&c>=-1e-6f&&1-b-c>=-1e-6f)covered[y*size+x]=true;
            }
        }
        var occupied=(bool[])covered.Clone(); int padded=0;
        // Ring-wise dilation avoids scan-order bias and extends the nearest valid island colour.
        for(int pass=0;pass<12;pass++)
        {
            var additions=new System.Collections.Generic.List<int>(); var values=new System.Collections.Generic.List<Color32>();
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int i=y*size+x;if(occupied[i])continue;
                int selected=-1;
                if(x>0&&occupied[i-1])selected=i-1;
                else if(x<size-1&&occupied[i+1])selected=i+1;
                else if(y>0&&occupied[i-size])selected=i-size;
                else if(y<size-1&&occupied[i+size])selected=i+size;
                if(selected>=0){additions.Add(i);values.Add(pixels[selected]);}
            }
            for(int j=0;j<additions.Count;j++){int i=additions[j];pixels[i]=values[j];occupied[i]=true;}padded+=additions.Count;
        }
        int surfaceChanges=0;
        for(int i=0;i<pixels.Length;i++)if(covered[i]&&!pixels[i].Equals(before[i]))surfaceChanges++;
        if(surfaceChanges!=0)throw new InvalidOperationException("Protected surface changed");
        tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(Output,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(Output,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Output);importer.maxTextureSize=size;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.mipMapsPreserveCoverage=false;importer.SaveAndReimport();
        string report="Fry v03 gutter cleanup; v02 surface colours and pattern unchanged\nSurface texels="+covered.Count(v=>v)+"\nSurface texels changed="+surfaceChanges+"\nGutter texels extended="+padded+"\n12-pixel ring-wise UV padding; no recolouring or pattern remapping. Pending visual review.";
        File.WriteAllText(Folder+"/Reports/Kohaku_Fry_RedPattern_v03_Validation.txt",report);return report;
    }
    public static string Capture()
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var cg=new GameObject("Temporary seam review camera");var cam=cg.AddComponent<Camera>();cam.orthographic=true;cam.cullingMask=1<<31;cam.nearClipPlane=.01f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.18f,.18f,.18f);
        var lg=new GameObject("Temporary seam review light");var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.cullingMask=1<<31;lg.transform.rotation=Quaternion.Euler(40,-25,0);
        var rt=new RenderTexture(768,768,24);cam.targetTexture=rt;var atlas=new Texture2D(2304,1536,TextureFormat.RGB24,false);
        Vector3[] views={new Vector3(1,.1f,0),Vector3.up,new Vector3(1,.6f,1)};
        try
        {
            for(int row=0;row<2;row++)
            {
                var go=KohakuCandidateBake.CopyForReview(AssetDatabase.LoadAssetAtPath<GameObject>(EarlyStageAlbedoTransferBuilder.Fry));foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();var mat=new Material(skin.sharedMaterial);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(row==0?Input:Output));skin.sharedMaterial=mat;
                var bounds=skin.bounds;cam.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.z)*.65f;
                for(int col=0;col<3;col++){cg.transform.position=bounds.center+views[col].normalized*2;cg.transform.LookAt(bounds.center,col==1?Vector3.forward:Vector3.up);cam.Render();RenderTexture.active=rt;atlas.ReadPixels(new Rect(0,0,768,768),col*768,(1-row)*768);}
                UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mat);
            }
            atlas.Apply();string path=Folder+"/Reports/Kohaku_Fry_RedPattern_v03_Comparisons.png";File.WriteAllBytes(path,atlas.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);return AssetDatabase.LoadAssetAtPath<Texture2D>(path).GetEntityId().ToString();
        }
        finally{RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(lg);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(atlas);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
