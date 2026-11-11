using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public sealed class PilotSwimPlayback : MonoBehaviour
{
    [SerializeField] private AnimationClip _clip;
    [SerializeField] private SkinnedMeshRenderer _skin;
    private PlayableGraph _graph;
    private AnimationClipPlayable _playable;
    private Transform[] _bones;
    private Vector3[] _positions;
    private Vector3[] _scales;
    private Quaternion[] _rotations;
    private float _elapsed;

    private void Awake()
    {
        _bones = _skin.bones;
        _positions = new Vector3[_bones.Length];
        _scales = new Vector3[_bones.Length];
        _rotations = new Quaternion[_bones.Length];
        for (int i = 0; i < _bones.Length; i++)
        {
            _positions[i] = _bones[i].localPosition;
            _scales[i] = _bones[i].localScale;
            _rotations[i] = _bones[i].localRotation;
        }
    }

    private void OnEnable()
    {
        var animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        _graph = PlayableGraph.Create("Pilot in-place swim");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        _playable = AnimationClipPlayable.Create(_graph, _clip);
        _playable.SetApplyFootIK(false);
        _playable.SetApplyPlayableIK(false);
        _playable.SetSpeed(0);
        var output = AnimationPlayableOutput.Create(_graph, "Swim", animator);
        output.SetSourcePlayable(_playable);
        _elapsed = 0;
        _graph.Play();
    }

    private void Update()
    {
        _elapsed = Mathf.Repeat(_elapsed + Time.deltaTime, _clip.length);
        _playable.SetTime(_elapsed);
        _graph.Evaluate(0);
        // The shared adult clip must not overwrite Fry/Juvenile rest spacing.
        for (int i = 0; i < _bones.Length; i++)
        {
            _bones[i].localPosition = _positions[i];
            _bones[i].localScale = _scales[i];
        }
    }

    private void OnDisable()
    {
        if (_graph.IsValid()) _graph.Destroy();
        if (_bones == null) return;
        for (int i = 0; i < _bones.Length; i++)
        {
            _bones[i].localPosition = _positions[i];
            _bones[i].localScale = _scales[i];
            _bones[i].localRotation = _rotations[i];
        }
    }
}
