using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KoiPond.JuvenileAppearancePrototype
{
    public static class JuvenileKohakuAppearanceAuthoring
    {
        public const string Root = "Assets/Models/Koi/ProofOfConcept/JuvenileKohakuAppearancePrototype";
        public const string SourcePrefab = "Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Prefabs/Juvenile_RiggingPrototype.prefab";
        public const string SourceAlbedo = "Assets/Models/Koi/New Juvenile/new_juvenile_texture.png";
        private static Texture2D _pending;
        private static string _audit;
        // Rest anatomy in the unchanged prefab-local frame, measured from source surface slices.
        private static readonly float[] Z = { -.19f, -.15f, -.12f, -.09f, -.06f, -.03f, 0f, .03f, .06f, .09f, .12f, .15f, .18f, .21f, .24f, .27f, .30f };
        private static readonly float[] Roof = { .035f, .041f, .051f, .063f, .079f, .091f, .099f, .103f, .106f, .107f, .103f, .099f, .093f, .085f, .072f, .051f, .009f };
        private static readonly float[] Belly = { -.045f, -.050f, -.058f, -.067f, -.077f, -.085f, -.088f, -.087f, -.085f, -.083f, -.082f, -.080f, -.090f, -.091f, -.086f, -.074f, -.048f };
        private static readonly Color Cream = new Color(.92f, .90f, .83f, 1f);
        private static readonly Color Pigment = new Color(.82f, .26f, .12f, 1f);

        private static float Profile(float z, float[] values)
        {
            for (int i = 1; i < Z.Length; i++)
                if (z <= Z[i]) return Mathf.Lerp(values[i-1], values[i], Mathf.InverseLerp(Z[i-1], Z[i], z));
            return values[values.Length-1];
        }
        private static float Smooth(float a, float b, float value)
        {
            float t = Mathf.InverseLerp(a, b, value);
            return t*t*(3f-2f*t);
        }
        private static float Eligibility(Vector3 p)
        {
            // Explicit distal fin and face envelopes; never use skin weights or an old vertex mask.
            if (p.z < -.19f || p.z > .267f) return 0f;
            float roof = Profile(p.z, Roof), belly = Profile(p.z, Belly);
            if (p.y > roof + .0015f || p.y < belly - .003f) return 0f;
            // Pectoral blades are outside the measured trunk below the shoulder.
            if (p.z > .035f && p.z < .165f && p.y < -.045f && Mathf.Abs(p.x) > .066f) return 0f;
            // Protect entire lateral face, both eye artwork footprints and all lips.
            if (p.z > .165f && p.y < .042f) return 0f;
            float tail = Smooth(-.19f, -.175f, p.z);
            float head = 1f - Smooth(.255f, .267f, p.z);
            float face = p.z > .155f ? Smooth(.042f, .055f, p.y) : 1f;
            float dorsal = 1f - Smooth(roof-.002f, roof+.0015f, p.y);
            float ventral = Smooth(belly-.003f, belly+.004f, p.y);
            return tail*head*face*dorsal*ventral;
        }
        private static float Patch(Vector3 p, float centerZ, float radiusZ, float radiusAngle)
        {
            float yCenter = (Profile(p.z, Roof) + Profile(p.z, Belly))*.5f;
            float angle = Mathf.Atan2(p.x, p.y-yCenter);
            float dz = (p.z-centerZ)/radiusZ, da = angle/radiusAngle;
            float distance = Mathf.Sqrt(dz*dz + da*da);
            return 1f-Smooth(.84f, 1f, distance);
        }
        private static Color Evaluate(Vector3 p, Color original)
        {
            float body = Eligibility(p);
            if (body <= 0f) return original;
            float marking = Mathf.Max(Patch(p,.211f,.030f,.78f), Mathf.Max(Patch(p,.099f,.054f,1.35f), Patch(p,-.079f,.044f,1.42f)));
            Color authored = Color.Lerp(Cream, Pigment, marking);
            authored.a = original.a;
            return Color.Lerp(original, authored, body);
        }
        private static float Cross(Vector2 a, Vector2 b) { return a.x*b.y-a.y*b.x; }

        public static string PrepareInMemory()
        {
            if (_pending != null) throw new InvalidOperationException("A candidate already exists; no refinement/retry authorized.");
            string output = Root + "/Textures/Juvenile_Kohaku_Appearance_v01.png";
            if (File.Exists(output)) throw new InvalidOperationException("Refusing candidate overwrite.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
            var renderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            var mesh = renderer.sharedMesh;
            var matrix = prefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            var positions = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
            var uv = mesh.uv;
            var indices = mesh.triangles;
            var source = new Texture2D(2,2,TextureFormat.RGBA32,false,false);
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(SourceAlbedo))) throw new InvalidOperationException("Original albedo cannot be decoded.");
                int width=source.width, height=source.height, count=width*height;
                var original=source.GetPixels32();
                var pixels=(Color32[])original.Clone();
                var owner=new int[count]; Array.Fill(owner,-1);
                var ownedPosition=new Vector3[count];
                var protectedPixel=new bool[count];
                int overlaps=0, conflicts=0, covered=0, editable=0;
                var conflictExamples=new StringBuilder();
                for(int tri=0;tri<indices.Length;tri+=3)
                {
                    int i0=indices[tri],i1=indices[tri+1],i2=indices[tri+2];
                    Vector2 a=uv[i0],b=uv[i1],c=uv[i2];
                    float determinant=Cross(b-a,c-a);
                    if(Mathf.Abs(determinant)<1e-14f) continue;
                    int xmin=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))*width),0,width-1);
                    int xmax=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))*width),0,width-1);
                    int ymin=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))*height),0,height-1);
                    int ymax=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))*height),0,height-1);
                    for(int y=ymin;y<=ymax;y++) for(int x=xmin;x<=xmax;x++)
                    {
                        var q=new Vector2((x+.5f)/width,(y+.5f)/height);
                        float v=Cross(q-a,c-a)/determinant, w=Cross(b-a,q-a)/determinant, u=1f-v-w;
                        if(u < -1e-6f || v < -1e-6f || w < -1e-6f) continue;
                        int pixel=y*width+x;
                        Vector3 p=positions[i0]*u+positions[i1]*v+positions[i2]*w;
                        Color value=Evaluate(p,original[pixel]);
                        bool protect=Eligibility(p)<=0f;
                        if(owner[pixel]>=0)
                        {
                            overlaps++;
                            Color old=pixels[pixel];
                            float difference=Mathf.Max(Mathf.Abs(old.r-value.r),Mathf.Max(Mathf.Abs(old.g-value.g),Mathf.Abs(old.b-value.b)));
                            if(difference>.035f)
                            {
                                conflicts++;
                                if(conflicts<=12) conflictExamples.AppendLine("UV pixel "+x+","+y+" first "+ownedPosition[pixel].ToString("F5")+" next "+p.ToString("F5")+" protected="+protectedPixel[pixel]+"/"+protect+" colorDifference="+difference.ToString("F5"));
                            }
                            continue;
                        }
                        owner[pixel]=tri/3;ownedPosition[pixel]=p;protectedPixel[pixel]=protect;
                        pixels[pixel]=value;covered++;
                        if(!protect)editable++;
                    }
                }
                _audit="Covered texels: "+covered+"; editable texels: "+editable+"; overlap samples: "+overlaps+"; incompatible color owners: "+conflicts+"\n"+conflictExamples;
                Directory.CreateDirectory(Root+"/Reports");
                File.WriteAllText(Root+"/Reports/Feasibility.txt",_audit);
                if(conflicts>0) throw new InvalidOperationException("STOP: incompatible UV owners prevent clean body/protected appearance. "+_audit);
                // Finite nearest ring padding only in unused UV space. Original occupied protected pixels never change.
                bool[] filled=owner.Select(o=>o>=0).ToArray();
                for(int ring=0;ring<12;ring++)
                {
                    var next=(Color32[])pixels.Clone();var nextFilled=(bool[])filled.Clone();
                    for(int y=1;y<height-1;y++) for(int x=1;x<width-1;x++)
                    {
                        int i=y*width+x;if(filled[i])continue;
                        int donor=-1;
                        foreach(int neighbor in new[]{i-1,i+1,i-width,i+width}) if(filled[neighbor]){donor=neighbor;break;}
                        if(donor>=0){next[i]=pixels[donor];nextFilled[i]=true;}
                    }
                    pixels=next;filled=nextFilled;
                }
                int protectedChanges=0;
                for(int i=0;i<count;i++) if(protectedPixel[i] && !pixels[i].Equals(original[i])) protectedChanges++;
                if(protectedChanges>0)throw new InvalidOperationException("Protected occupied texels were changed.");
                _pending=new Texture2D(width,height,TextureFormat.RGBA32,true,false);
                _pending.name="One pending Juvenile Kohaku surface-authored albedo";
                _pending.SetPixels32(pixels);_pending.Apply();
                return _audit+"Protected occupied texel changes: 0. One in-memory candidate; no appearance PNG saved.";
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        public static Texture2D Pending { get { return _pending; } }
        public static string Audit { get { return _audit; } }
    }
}
