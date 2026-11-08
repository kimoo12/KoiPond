using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Animator))]
public sealed class JuvenilePrototypePlayback : MonoBehaviour
{
    public enum PlaybackMode { ComparisonSequence, ContinuousSwim, ContinuousIdle, RepeatEat }
    [SerializeField] private AnimationClip[] _clips;
    [SerializeField] private SkinnedMeshRenderer _skin;
    [SerializeField] private SkinnedMeshRenderer _adultSkin;
    [SerializeField] private Animator _adultAnimator;
    [Header("Diagnostic Playback")]
    [SerializeField] private PlaybackMode _mode;
    [SerializeField] private bool _repeatSequence = true;
    [SerializeField] private bool _paused;
    [SerializeField] private bool _adaptJuvenile = true;
    [SerializeField] private bool _adultSecondaryMotion;
    [SerializeField, Range(0, 1)] private float _bodyAmplitude = .70f;
    [SerializeField, Range(0, 1)] private float _tailAmplitude = .85f;
    [SerializeField, Range(0, 1)] private float _finAmplitude = .70f;
    [SerializeField, Range(0, 1)] private float _eatAmplitude = .85f;
    private Animator _animator;
    private Transform[][] _bones;
    private Vector3[][] _positions;
    private Quaternion[][] _rotations;
    private Vector3[][] _scales;
    private int[] _mouthIndices;
    private PlayableGraph _graph;
    private AnimationClipPlayable[] _playables;
    private int _stage;
    private float _elapsed;
    private PlaybackMode _runningMode;
    public bool Finished { get; private set; }
    public int CompletedStages { get; private set; }
    public int Stage => _stage;
    public float StageTime => _elapsed;
    public SkinnedMeshRenderer Skin => _skin;
    public SkinnedMeshRenderer AdultSkin => _adultSkin;
    public bool AdaptJuvenile { get => _adaptJuvenile; set => _adaptJuvenile = value; }
    public bool Paused { get => _paused; set => _paused = value; }
    public int ClipIndex => _stage == 3 ? 1 : _stage == 4 ? 0 : _stage;

    public void Initialize()
    {
        if (_bones != null) return;
        _animator = GetComponent<Animator>();
        var skins = new[] { _skin, _adultSkin };
        var animators = new[] { _animator, _adultAnimator };
        _bones = new Transform[2][]; _positions = new Vector3[2][];
        _rotations = new Quaternion[2][]; _scales = new Vector3[2][]; _mouthIndices = new int[2];
        for (int target = 0; target < 2; target++)
        {
            if (skins[target] == null || animators[target] == null)
                throw new System.InvalidOperationException("Both explicit comparison targets are required.");
            animators[target].applyRootMotion = false;
            animators[target].cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _bones[target] = skins[target].bones;
            int count = _bones[target].Length;
            _positions[target] = new Vector3[count]; _rotations[target] = new Quaternion[count]; _scales[target] = new Vector3[count];
            for (int b = 0; b < count; b++)
            {
                _positions[target][b] = _bones[target][b].localPosition;
                _rotations[target][b] = _bones[target][b].localRotation;
                _scales[target][b] = _bones[target][b].localScale;
            }
            _mouthIndices[target] = skins[target].sharedMesh.GetBlendShapeIndex("MouthOpen");
            if (_mouthIndices[target] < 0) throw new System.InvalidOperationException("MouthOpen binding absent.");
        }
    }
    private void Awake() { Initialize(); }
    private void OnEnable() { if (Application.isPlaying) ResetPlayback(); }
    public void RestoreRest()
    {
        Initialize();
        for (int target = 0; target < 2; target++)
            for (int b = 0; b < _bones[target].Length; b++)
            {
                _bones[target][b].localPosition = _positions[target][b];
                _bones[target][b].localRotation = _rotations[target][b];
                _bones[target][b].localScale = _scales[target][b];
            }
        _skin.SetBlendShapeWeight(_mouthIndices[0], 0); _adultSkin.SetBlendShapeWeight(_mouthIndices[1], 0);
    }
    [ContextMenu("Reset Comparison")]
    public void ResetPlayback()
    {
        Initialize(); Finished = false; CompletedStages = 0; _runningMode = _mode;
        Begin(_mode == PlaybackMode.ContinuousIdle ? 1 : _mode == PlaybackMode.RepeatEat ? 2 : 0);
    }
    public void ConfigureTest(bool repeat, bool adapted)
    {
        _mode = PlaybackMode.ComparisonSequence; _repeatSequence = repeat; _adaptJuvenile = adapted; ResetPlayback();
    }
    public void SelectMode(PlaybackMode mode) { _mode = mode; ResetPlayback(); }
    private void Begin(int stage)
    {
        if (_graph.IsValid()) _graph.Destroy();
        RestoreRest(); _stage = stage; _elapsed = 0;
        _graph = PlayableGraph.Create("Adult / Juvenile synchronized source clips");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        _playables = new AnimationClipPlayable[2];
        var animators = new[] { _animator, _adultAnimator };
        for (int target = 0; target < 2; target++)
        {
            var playable = AnimationClipPlayable.Create(_graph, _clips[ClipIndex]);
            playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); playable.SetSpeed(0);
            var output = AnimationPlayableOutput.Create(_graph, target == 0 ? "Juvenile" : "Adult", animators[target]);
            output.SetSourcePlayable(playable); _playables[target] = playable;
        }
        _graph.Play(); EvaluatePose(0);
    }
    private void EvaluatePose(float time)
    {
        RestoreRest();
        float clipTime = _clips[ClipIndex].isLooping ? Mathf.Repeat(time, _clips[ClipIndex].length) : Mathf.Min(time, _clips[ClipIndex].length);
        foreach (var playable in _playables) playable.SetTime(clipTime);
        _graph.Evaluate(0); ApplyPoseAdaptation(ClipIndex, time);
    }
    public void ApplyPoseAdaptation(int clipIndex, float time)
    {
        Initialize();
        if (_adultSecondaryMotion && clipIndex != 2)
        {
            float paddle = Mathf.Sin(time * 2f) * 2.2f;
            float undulate = Mathf.Sin(time * 2f) * 1.2f;
            for (int target = 0; target < 2; target++)
            {
                var bones = _bones[target];
                bones[4].localRotation *= Quaternion.Euler(0, paddle, -paddle * .2f);
                bones[5].localRotation *= Quaternion.Euler(0, -paddle, paddle * .2f);
                if (clipIndex == 0)
                {
                    bones[6].localRotation *= Quaternion.Euler(0, undulate * .25f, 0);
                    bones[7].localRotation *= Quaternion.Euler(0, -undulate * .4f, 0);
                    bones[8].localRotation *= Quaternion.Euler(0, undulate * .65f, 0);
                    bones[9].localRotation *= Quaternion.Euler(0, -undulate * .85f, 0);
                }
            }
        }
        if (!_adaptJuvenile) return;
        for (int b = 0; b < _bones[0].Length; b++)
        {
            float amplitude = b == 3 ? 1 : clipIndex == 2 ? _eatAmplitude : b >= 6 ? _tailAmplitude : b == 4 || b == 5 ? _finAmplitude : _bodyAmplitude;
            _bones[0][b].localRotation = Quaternion.Slerp(_rotations[0][b], _bones[0][b].localRotation, amplitude);
        }
        _bones[0][0].localPosition = Vector3.Lerp(_positions[0][0], _bones[0][0].localPosition, clipIndex == 2 ? _eatAmplitude : _bodyAmplitude);
    }
    private void Update()
    {
        if (_runningMode != _mode) ResetPlayback();
        if (_paused || Finished) return;
        _elapsed += Time.deltaTime;
        float duration = _stage == 0 ? _clips[0].length * 3 : _stage == 1 ? _clips[1].length * 3 : _stage == 2 ? _clips[2].length : _stage == 3 ? .5f : _clips[0].length;
        if (_mode != PlaybackMode.ComparisonSequence)
        {
            float length = _clips[ClipIndex].length;
            if (_elapsed >= length) { CompletedStages += Mathf.FloorToInt(_elapsed / length); _elapsed = Mathf.Repeat(_elapsed, length); }
            EvaluatePose(_elapsed); return;
        }
        EvaluatePose(Mathf.Min(_elapsed, duration));
        if (_elapsed < duration) return;
        CompletedStages++;
        if (_stage < 4) Begin(_stage + 1);
        else if (_repeatSequence) Begin(0);
        else { Finished = true; _graph.Stop(); }
    }
    private void OnDisable()
    {
        if (_graph.IsValid()) _graph.Destroy();
        if (_bones != null) RestoreRest();
    }
}
