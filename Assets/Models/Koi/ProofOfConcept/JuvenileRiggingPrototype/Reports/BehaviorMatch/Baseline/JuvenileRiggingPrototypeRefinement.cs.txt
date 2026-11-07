using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class JuvenileRiggingPrototypeRefinement
{
    public static void Refine()
    {
        var mesh=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(JuvenileRiggingPrototypeBuilder.MeshPath);
        var v=mesh.vertices;var tris=mesh.triangles;var original=mesh.boneWeights;
        var keys=new Dictionary<Vector3,int>();var reps=new int[v.Length];var positions=new List<Vector3>();
        for(int i=0;i<v.Length;i++){if(!keys.TryGetValue(v[i],out int id)){id=keys.Count;keys.Add(v[i],id);positions.Add(v[i]);}reps[i]=id;}
        var neighbors=Enumerable.Range(0,keys.Count).Select(_=>new HashSet<int>()).ToArray();
        for(int t=0;t<tris.Length;t+=3)for(int c=0;c<3;c++){int a=reps[tris[t+c]],b=reps[tris[t+(c+1)%3]];if(a!=b){neighbors[a].Add(b);neighbors[b].Add(a);}}
        var weights=new float[keys.Count,10];for(int i=0;i<v.Length;i++)for(int b=0;b<10;b++)weights[reps[i],b]=JuvenileRiggingPrototypeBuilder.Influence(original[i],b);
        var sourceDeltas=new Vector3[v.Length];mesh.GetBlendShapeFrameVertices(0,0,sourceDeltas,null,null);
        var deltas=new Vector3[keys.Count];for(int i=0;i<v.Length;i++)deltas[reps[i]]=sourceDeltas[i];
        // Final refinement: 96 additional passes after the initial 24-pass review.
        for(int pass=0;pass<96;pass++)
        {
            var next=new float[keys.Count,10];var nextDeltas=new Vector3[keys.Count];
            for(int i=0;i<keys.Count;i++)
            {
                float total=0;Vector3 deltaSum=Vector3.zero;
                foreach(int j in neighbors[i]){float factor=1f/Mathf.Max((positions[j]-positions[i]).magnitude,.000003f);total+=factor;deltaSum+=deltas[j]*factor;}
                nextDeltas[i]=total>0?Vector3.Lerp(deltas[i],deltaSum/total,.55f):deltas[i];
                for(int b=0;b<10;b++)
                {
                    float sum=0;foreach(int j in neighbors[i])sum+=weights[j,b]/Mathf.Max((positions[j]-positions[i]).magnitude,.000003f);
                    next[i,b]=total>0?Mathf.Lerp(weights[i,b],sum/total,.55f):weights[i,b];
                }
            }
            weights=next;deltas=nextDeltas;
        }
        var output=new BoneWeight[v.Length];
        for(int i=0;i<v.Length;i++)
        {
            int id=reps[i];var order=Enumerable.Range(0,10).OrderByDescending(b=>weights[id,b]).Take(4).ToArray();float sum=order.Sum(b=>weights[id,b]);
            output[i]=new BoneWeight{boneIndex0=order[0],boneIndex1=order[1],boneIndex2=order[2],boneIndex3=order[3],weight0=weights[id,order[0]]/sum,weight1=weights[id,order[1]]/sum,weight2=weights[id,order[2]]/sum,weight3=weights[id,order[3]]/sum};
        }
        mesh.boneWeights=output;
        var shape=v.Select((p,i)=>deltas[reps[i]]).ToArray();mesh.ClearBlendShapes();mesh.AddBlendShapeFrame("MouthOpen",100,shape,new Vector3[v.Length],new Vector3[v.Length]);
        EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();
        File.WriteAllText(JuvenileRiggingPrototypeBuilder.Folder+"/Reports/Refinement.txt","Initial 24 weight passes followed by 96 final weight/mouth-delta topology-adjacency passes. 0.55 neighbor blend, inverse-edge-length weighting; identical-position seam vertices share weights/deltas; no rest vertices, UVs or indices changed. Transferred adult weights retained as initialization. No mouth topology changes. Remaining distortions must be reported, not treated as production-ready.");
    }
}
