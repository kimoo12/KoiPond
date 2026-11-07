using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Animator))]
public sealed class JuvenilePrototypePlayback : MonoBehaviour
{
    [SerializeField] private AnimationClip[] _clips;
    [SerializeField] private SkinnedMeshRenderer _skin;
    private Animator _animator;
    private Transform[] _bones;
    private Vector3[] _positions;
    private Quaternion[] _rotations;
    private PlayableGraph _graph;
    private AnimationClipPlayable _playable;
    private int _stage;
    private float _elapsed;
    public bool Finished { get; private set; }
    public int CompletedStages { get; private set; }
    public int Stage => _stage;
    public float StageTime => _elapsed;
    public SkinnedMeshRenderer Skin => _skin;

    private void Awake()
    {
        _animator=GetComponent<Animator>();
        _animator.applyRootMotion=false;
        _animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        _bones=_skin.bones;_positions=new Vector3[_bones.Length];_rotations=new Quaternion[_bones.Length];
        for(int i=0;i<_bones.Length;i++){_positions[i]=_bones[i].localPosition;_rotations[i]=_bones[i].localRotation;}
    }
    private void Start(){Begin(0);}
    private void Begin(int stage)
    {
        if(_graph.IsValid())_graph.Destroy();
        for(int i=0;i<_bones.Length;i++){_bones[i].localPosition=_positions[i];_bones[i].localRotation=_rotations[i];}
        _skin.SetBlendShapeWeight(0,0);
        _stage=stage;_elapsed=0;
        int index=stage==3?1:stage;
        _graph=PlayableGraph.Create("Juvenile source clip test");_graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        _playable=AnimationClipPlayable.Create(_graph,_clips[index]);
        _playable.SetApplyFootIK(false);_playable.SetApplyPlayableIK(false);
        var output=AnimationPlayableOutput.Create(_graph,"Juvenile",_animator);output.SetSourcePlayable(_playable);_graph.Play();
    }
    private void Update()
    {
        if(Finished)return;
        _elapsed+=Time.deltaTime;
        float duration=_stage==0?_clips[0].length*3:_stage==1?_clips[1].length*3:_stage==2?_clips[2].length:.5f;
        if(_elapsed>=duration)
        {
            CompletedStages++;
            if(_stage<3)Begin(_stage+1);
            else{Finished=true;_graph.Stop();}
        }
    }
    private void OnDisable(){if(_graph.IsValid())_graph.Destroy();}
}
