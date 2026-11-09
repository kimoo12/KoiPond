using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KoiPond.GrowthPrototype
{
    [DefaultExecutionOrder(-100)]
    public sealed class KoiGrowthAnimationAdapter : MonoBehaviour
    {
        [SerializeField] private KoiStageVisuals _visuals;
        [SerializeField] private AnimationClip[] _clips;
        [SerializeField] private float _speed = 1;
        [SerializeField] private int _loopClip;
        [SerializeField, Min(0.01f)] private float _blendDuration = .3f;
        private readonly double[] _times = new double[3];
        private readonly float[] _weights = { 1, 0, 0 };
        private readonly float[] _blendStart = { 1, 0, 0 };
        private double _blendElapsed;
        private Vector3[] _restartPositionOffsets;
        private Quaternion[] _restartRotationOffsets;
        private float[] _restartShapeOffsets;
        private double _restartElapsed;
        private Transform[][] _transforms;
        private Vector3[][] _positions, _scales;
        private Quaternion[][] _rotations;
        private Animator[] _animators;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable[] _playables;
        private int _stage = -1, _clip;
        private double _clock;
        private bool _subscribed;
        public float BlendDuration => _blendDuration;
        public float GetClipWeight(int clip) => _weights[clip];
        public int ClipIndex => _clip;
        public int BoundStage => _stage;
        public double Clock => _clock;
        public double Phase => _clips == null ? 0 : SampleTime(_clip) / _clips[_clip].length;
        public AnimationClip[] Clips => _clips;
        public bool GraphValid => _graph.IsValid();
        public float Speed => _speed;

        public void Configure(KoiStageVisuals visuals, AnimationClip[] clips)
        { _visuals = visuals; _clips = clips; }
        public void Initialize()
        {
            if (_transforms != null) return;
            _visuals.Validate();
            if (_clips == null || _clips.Length != 3) throw new InvalidOperationException("Swim/Idle/Eat clips required.");
            foreach (var c in _clips) if (c == null || c.length <= 0) throw new InvalidOperationException("Invalid source clip.");
            _transforms = new Transform[3][]; _positions = new Vector3[3][];
            _scales = new Vector3[3][]; _rotations = new Quaternion[3][]; _animators = new Animator[3];
            for (int s = 0; s < 3; s++)
            {
                _animators[s] = _visuals.Rigs[s].GetComponent<Animator>();
                if (_animators[s] == null || _visuals.Skins[s].bones.Length != 10
                    || _visuals.Skins[s].sharedMesh.GetBlendShapeIndex("MouthOpen") < 0)
                    throw new InvalidOperationException("Compatible ten-bone rig, Animator and MouthOpen required.");
                _transforms[s] = _visuals.Rigs[s].GetComponentsInChildren<Transform>(true);
                int n = _transforms[s].Length;
                _positions[s] = new Vector3[n]; _scales[s] = new Vector3[n]; _rotations[s] = new Quaternion[n];
                for (int i = 0; i < n; i++)
                { var t = _transforms[s][i]; _positions[s][i] = t.localPosition; _scales[s][i] = t.localScale; _rotations[s][i] = t.localRotation; }
            }
        }
        private void OnEnable()
        {
            Initialize(); Subscribe();
            if (_visuals.ActiveIndex >= 0) Bind(_visuals.ActiveIndex);
        }
        public void Subscribe()
        {
            if (_subscribed) return;
            _visuals.PreparingStage += Bind; _subscribed = true;
        }
        public void RestoreRest(int stage)
        {
            Initialize();
            for (int i = 0; i < _transforms[stage].Length; i++)
            { var t = _transforms[stage][i]; if (t == null) continue; t.localPosition = _positions[stage][i]; t.localRotation = _rotations[stage][i]; t.localScale = _scales[stage][i]; }
            var skin = _visuals.Skins[stage];
            for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++) skin.SetBlendShapeWeight(i, 0);
        }
        public void Bind(int stage)
        {
            Initialize();
            var next = PlayableGraph.Create("Koi Growth Blended Animation");
            try
            {
                next.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                RestoreRest(stage);
                var mixer = AnimationMixerPlayable.Create(next, 3);
                var playables = new AnimationClipPlayable[3];
                for (int i = 0; i < 3; i++)
                {
                    var p = AnimationClipPlayable.Create(next, _clips[i]);
                    p.SetApplyFootIK(false); p.SetApplyPlayableIK(false); p.SetSpeed(0);
                    p.SetTime(SampleTime(i));
                    next.Connect(p, 0, mixer, i);
                    mixer.SetInputWeight(i, _weights[i]);
                    playables[i] = p;
                }
                var output = AnimationPlayableOutput.Create(next, "Stage", _animators[stage]);
                output.SetSourcePlayable(mixer);
                next.Play(); next.Evaluate(0);
                if (_graph.IsValid()) _graph.Destroy();
                if (_stage >= 0 && _stage != stage) RestoreRest(_stage);
                _graph = next; _mixer = mixer; _playables = playables; _stage = stage;
                ApplyRestartCorrection();
            }
            catch { if (next.IsValid()) next.Destroy(); throw; }
        }
        private double SampleTime(int clip) => _clips[clip].isLooping
            ? _times[clip] % _clips[clip].length : Math.Min(_times[clip], _clips[clip].length);
        public void SelectClip(int clip)
        {
            if (clip < 0 || clip > 2) throw new ArgumentOutOfRangeException(nameof(clip));
            Initialize();
            if (clip != 2) _loopClip = clip;
            // Repeated requests must not restart an active motion or Eat pose.
            if (_clip == clip) return;
            bool preserveRestart = clip == 2 && _weights[2] > 0 && _graph.IsValid();
            Vector3[] previousPositions = null;
            Quaternion[] previousRotations = null;
            float[] previousShapes = null;
            if (preserveRestart)
            {
                var transforms = _transforms[_stage];
                previousPositions = new Vector3[transforms.Length];
                previousRotations = new Quaternion[transforms.Length];
                for (int i = 0; i < transforms.Length; i++)
                { previousPositions[i] = transforms[i].localPosition; previousRotations[i] = transforms[i].localRotation; }
                var skin = _visuals.Skins[_stage];
                previousShapes = new float[skin.sharedMesh.blendShapeCount];
                for (int i = 0; i < previousShapes.Length; i++) previousShapes[i] = skin.GetBlendShapeWeight(i);
                _restartElapsed = _blendDuration;
            }
            Array.Copy(_weights, _blendStart, 3);
            _blendElapsed = 0;
            _clip = clip; _clock = 0;
            if (clip == 2) _times[2] = 0;
            EvaluatePose();
            if (preserveRestart)
            {
                var transforms = _transforms[_stage];
                _restartPositionOffsets = previousPositions;
                _restartRotationOffsets = previousRotations;
                for (int i = 0; i < transforms.Length; i++)
                {
                    _restartPositionOffsets[i] -= transforms[i].localPosition;
                    _restartRotationOffsets[i] *= Quaternion.Inverse(transforms[i].localRotation);
                }
                _restartShapeOffsets = previousShapes;
                var skin = _visuals.Skins[_stage];
                for (int i = 0; i < previousShapes.Length; i++) _restartShapeOffsets[i] -= skin.GetBlendShapeWeight(i);
                _restartElapsed = 0;
                ApplyRestartCorrection();
            }
        }
        private void ApplyRestartCorrection()
        {
            if (_restartPositionOffsets == null || _restartElapsed >= _blendDuration || _stage < 0) return;
            float p = Mathf.Clamp01((float)(_restartElapsed / Math.Max(.01, _blendDuration)));
            float remaining = 1 - p * p * (3 - 2 * p);
            var transforms = _transforms[_stage];
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].localPosition += _restartPositionOffsets[i] * remaining;
                transforms[i].localRotation = Quaternion.Slerp(Quaternion.identity, _restartRotationOffsets[i], remaining)
                    * transforms[i].localRotation;
            }
            var skin = _visuals.Skins[_stage];
            for (int i = 0; i < _restartShapeOffsets.Length; i++)
                skin.SetBlendShapeWeight(i, skin.GetBlendShapeWeight(i) + _restartShapeOffsets[i] * remaining);
        }
        private void Step(double delta)
        {
            _restartElapsed += delta;
            _clock += delta;
            for (int i = 0; i < 3; i++) _times[i] += delta;
            _blendElapsed += delta;
            float p = Mathf.Clamp01((float)(_blendElapsed / Math.Max(.01, _blendDuration)));
            float blend = p * p * (3 - 2 * p);
            for (int i = 0; i < 3; i++) _weights[i] = Mathf.Lerp(_blendStart[i], i == _clip ? 1 : 0, blend);
        }
        public void AdvanceClock(double delta)
        {
            if (double.IsNaN(delta) || double.IsInfinity(delta) || delta < 0 || float.IsNaN(_speed) || float.IsInfinity(_speed) || _speed < 0)
                throw new ArgumentOutOfRangeException(nameof(delta));
            Initialize();
            double scaled = delta * _speed;
            if (_clip == 2 && _clock + scaled >= _clips[2].length)
            {
                double remaining = Math.Max(0, _clips[2].length - _clock);
                Step(remaining);
                SelectClip(_loopClip);
                Step(scaled - remaining);
            }
            else Step(scaled);
            EvaluatePose();
        }
        public void EvaluatePose()
        {
            if (!_graph.IsValid()) return;
            // Sparse Eat bindings must never inherit transforms or shapes from the previous pose.
            RestoreRest(_stage);
            for (int i = 0; i < 3; i++)
            {
                _playables[i].SetTime(SampleTime(i));
                _mixer.SetInputWeight(i, _weights[i]);
            }
            _graph.Evaluate(0);
            ApplyRestartCorrection();
        }
        private void Update() { AdvanceClock(Time.deltaTime); }
        private void OnDisable() { StopPlayback(); }
        public void StopPlayback()
        {
if (_subscribed && _visuals != null) _visuals.PreparingStage -= Bind;
            _subscribed = false;
            if (_graph.IsValid()) _graph.Destroy();
            if (_transforms != null) for (int i = 0; i < 3; i++) RestoreRest(i);
            // Keep the rest cache and logical clock for safe re-enable.
            _stage = -1;
        }
        private void OnDestroy() { if (_graph.IsValid()) _graph.Destroy(); }
    }
}
