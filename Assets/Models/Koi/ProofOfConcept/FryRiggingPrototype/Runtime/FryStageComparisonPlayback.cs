using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class FryStageComparisonPlayback : MonoBehaviour
{
    public enum PlaybackMode { ComparisonSequence, ContinuousSwim, ContinuousIdle, EatToIdle, EatToSwim, RepeatEat }
    [Header("Explicit references: adult, juvenile, fry")]
    [SerializeField] private GameObject[] _targets;
    [SerializeField] private SkinnedMeshRenderer[] _skins;
    [SerializeField] private AnimationClip[] _clips;
    [Header("Diagnostics")]
    [SerializeField] private PlaybackMode _mode;
    [SerializeField] private bool _paused;
    [SerializeField] private bool _adaptStages = true;
    [SerializeField] private bool _repeatSequence = true;
    [SerializeField, Range(0,1)] private float _fryBodyAmplitude = 1;
    [SerializeField, Range(0,1)] private float _fryTailAmplitude = 1;
    [SerializeField, Range(0,1)] private float _fryFinAmplitude = 1;
    [SerializeField, Range(0,1)] private float _fryEatAmplitude = 1;
    private Animator[] _animators;
    private Transform[][] _transforms;
    private Vector3[][] _restPositions, _restScales;
    private Quaternion[][] _restRotations;
    private Quaternion[][] _boneRotations;
    private Vector3[] _rootPositions;
    private int[] _mouthIndices;
    private PlayableGraph _graph;
    private AnimationClipPlayable[] _playables;
    private int _clipIndex=-1, _stage;
    private double _elapsed;
    private PlaybackMode _runningMode;
    public GameObject[] Targets => _targets;
    public SkinnedMeshRenderer[] Skins => _skins;
    public AnimationClip[] Clips => _clips;
    public int Stage => _stage;
    public int ClipIndex => _clipIndex;
    public float Elapsed => (float)_elapsed;
    public int CompletedStages { get; private set; }
    public int CompletedLoops { get; private set; }
    public bool Paused { get => _paused; set => _paused=value; }
    public bool AdaptStages { get => _adaptStages; set => _adaptStages=value; }
    public bool Finished { get; private set; }
    public void Initialize()
    {
        if(_transforms!=null)return;
        if(_targets==null||_targets.Length!=3||_skins==null||_skins.Length!=3||_clips==null||_clips.Length!=3)
            throw new System.InvalidOperationException("Three explicit stages and Swim/Idle/Eat required.");
        _animators=new Animator[3];_transforms=new Transform[3][];_restPositions=new Vector3[3][];_restScales=new Vector3[3][];_restRotations=new Quaternion[3][];_boneRotations=new Quaternion[3][];_rootPositions=new Vector3[3];_mouthIndices=new int[3];
        for(int s=0;s<3;s++)
        {
            _animators[s]=_targets[s].GetComponent<Animator>();
            if(_animators[s]==null||_skins[s]==null||_skins[s].bones.Length!=10)throw new System.InvalidOperationException("Explicit Animator and ten-bone skin required.");
            _animators[s].applyRootMotion=false;_animators[s].cullingMode=AnimatorCullingMode.AlwaysAnimate;
            _transforms[s]=_targets[s].GetComponentsInChildren<Transform>(true);
            int n=_transforms[s].Length;_restPositions[s]=new Vector3[n];_restScales[s]=new Vector3[n];_restRotations[s]=new Quaternion[n];
            for(int i=0;i<n;i++){var t=_transforms[s][i];_restPositions[s][i]=t.localPosition;_restScales[s][i]=t.localScale;_restRotations[s][i]=t.localRotation;}
            _boneRotations[s]=new Quaternion[10];for(int b=0;b<10;b++)_boneRotations[s][b]=_skins[s].bones[b].localRotation;
            _rootPositions[s]=_skins[s].bones[0].localPosition;
            _mouthIndices[s]=_skins[s].sharedMesh.GetBlendShapeIndex("MouthOpen");
            if(_mouthIndices[s]<0)throw new System.InvalidOperationException("MouthOpen missing");
        }
    }
    public void RestoreRest()
    {
        Initialize();RestoreCachedRest();
    }
    private void RestoreCachedRest()
    {
        if(_transforms==null)return;
        for(int s=0;s<_transforms.Length;s++)
        {
            if(_transforms[s]!=null)
                for(int i=0;i<_transforms[s].Length;i++)
                {
                    var t=_transforms[s][i];if(t==null)continue;
                    t.localPosition=_restPositions[s][i];t.localRotation=_restRotations[s][i];t.localScale=_restScales[s][i];
                }
            var skin=_skins[s];if(skin==null||skin.sharedMesh==null)continue;
            for(int b=0;b<skin.sharedMesh.blendShapeCount;b++)skin.SetBlendShapeWeight(b,0);
        }
    }
    private void DestroyPlayback()
    {
        if(_graph.IsValid())_graph.Destroy();
        _graph=default;_playables=null;_clipIndex=-1;
    }
    private bool HasLivePlaybackReferences()
    {
        for(int s=0;s<3;s++)
        {
            if(_targets[s]==null||_skins[s]==null||_skins[s].sharedMesh==null||_animators[s]==null)return false;
            var bones=_skins[s].bones;if(bones.Length!=10)return false;
            for(int b=0;b<bones.Length;b++)if(bones[b]==null)return false;
        }
        return true;
    }
    public void ApplyAdaptation(int clip)
    {
        if(!_adaptStages)return;
        for(int s=1;s<3;s++)
        {
            for(int b=0;b<10;b++)
            {
                float amp=b==3?1:clip==2?(s==1?.85f:_fryEatAmplitude):b>=6?(s==1?.85f:_fryTailAmplitude):b==4||b==5?(s==1?.70f:_fryFinAmplitude):(s==1?.70f:_fryBodyAmplitude);
                _skins[s].bones[b].localRotation=Quaternion.Slerp(_boneRotations[s][b],_skins[s].bones[b].localRotation,amp);
            }
            float translation=clip==2?(s==1?.85f:_fryEatAmplitude):(s==1?.70f:_fryBodyAmplitude);
            _skins[s].bones[0].localPosition=Vector3.Lerp(_rootPositions[s],_skins[s].bones[0].localPosition,translation);
        }
    }
    private void Awake(){Initialize();}
    private void OnEnable(){if(Application.isPlaying)ResetComparison();}
    [ContextMenu("Reset Comparison")]
    public void ResetComparison()
    {
        Initialize();_runningMode=_mode;_stage=0;_elapsed=0;CompletedStages=0;CompletedLoops=0;Finished=false;
        Begin(_mode==PlaybackMode.ContinuousIdle?1:_mode==PlaybackMode.EatToIdle||_mode==PlaybackMode.EatToSwim||_mode==PlaybackMode.RepeatEat?2:0);
    }
    public void SelectMode(PlaybackMode mode){_mode=mode;ResetComparison();}
    public void ConfigureTest(bool repeat){_mode=PlaybackMode.ComparisonSequence;_repeatSequence=repeat;_paused=false;ResetComparison();}
    private void Begin(int clip)
    {
        DestroyPlayback();RestoreRest();_clipIndex=clip;_elapsed=0;
        _graph=PlayableGraph.Create("Adult Juvenile Fry Shared Clock");_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);_playables=new AnimationClipPlayable[3];
        for(int s=0;s<3;s++){var p=AnimationClipPlayable.Create(_graph,_clips[clip]);p.SetApplyFootIK(false);p.SetApplyPlayableIK(false);p.SetSpeed(0);var o=AnimationPlayableOutput.Create(_graph,_targets[s].name,_animators[s]);o.SetSourcePlayable(p);_playables[s]=p;}
        _graph.Play();EvaluatePose(0);
    }
    private void EvaluatePose(double time)
    {
        RestoreRest();double t=_clips[_clipIndex].isLooping?time%_clips[_clipIndex].length:System.Math.Min(time,_clips[_clipIndex].length);
        foreach(var p in _playables)p.SetTime(t);_graph.Evaluate(0);ApplyAdaptation(_clipIndex);
    }
    private void Update()
    {
        if(_transforms==null||!HasLivePlaybackReferences()){enabled=false;return;}
        if(_mode!=_runningMode)ResetComparison();if(_paused||Finished)return;
        double before=_elapsed;_elapsed+=Time.deltaTime;
        if(_mode==PlaybackMode.ContinuousSwim||_mode==PlaybackMode.ContinuousIdle){CompletedLoops+=(int)(_elapsed/_clips[_clipIndex].length)-(int)(before/_clips[_clipIndex].length);EvaluatePose(_elapsed);return;}
        if(_mode==PlaybackMode.RepeatEat){if(_elapsed>=_clips[2].length){EvaluatePose(_clips[2].length);CompletedLoops++;Begin(2);}else EvaluatePose(_elapsed);return;}
        if(_mode==PlaybackMode.EatToIdle||_mode==PlaybackMode.EatToSwim){if(_clipIndex==2&&_elapsed>=_clips[2].length){EvaluatePose(_clips[2].length);CompletedStages++;Begin(_mode==PlaybackMode.EatToIdle?1:0);}else EvaluatePose(_elapsed);return;}
        double duration=_stage==0?_clips[0].length*3:_stage==1?_clips[1].length*3:_stage==2?_clips[2].length:_stage==3?.5:_clips[0].length;
        EvaluatePose(System.Math.Min(_elapsed,duration));
        if(_elapsed<duration)return;CompletedStages++;if(_stage<2)CompletedLoops+=3;
        if(_stage<4){_stage++;Begin(_stage==1?1:_stage==2?2:_stage==3?1:0);}
        else if(_repeatSequence){_stage=0;Begin(0);}else{Finished=true;_graph.Stop();}
    }
    private void CleanupPlayback()
    {
        DestroyPlayback();
        try {RestoreCachedRest();}
        finally
        {
            _animators=null;_transforms=null;_restPositions=null;_restScales=null;
            _restRotations=null;_boneRotations=null;_rootPositions=null;_mouthIndices=null;
        }
    }
    private void OnDisable(){CleanupPlayback();}
    private void OnDestroy(){CleanupPlayback();}
}
