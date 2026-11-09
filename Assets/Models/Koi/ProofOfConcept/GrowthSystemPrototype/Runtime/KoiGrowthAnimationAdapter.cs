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
        private Transform[][] _transforms;
        private Vector3[][] _positions, _scales;
        private Quaternion[][] _rotations;
        private Animator[] _animators;
        private PlayableGraph _graph;
        private AnimationClipPlayable _playable;
        private int _stage = -1, _clip;
        private double _clock;
        private bool _subscribed;
        public int ClipIndex => _clip;
        public int BoundStage => _stage;
        public double Clock => _clock;
        public double Phase => _clips == null ? 0 : (_clock % _clips[_clip].length) / _clips[_clip].length;
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
            // Build a replacement before disposing the old graph. Rebinding never resets clock.
            var next = PlayableGraph.Create("Koi Growth Source Animation");
            try
            {
                next.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var p = AnimationClipPlayable.Create(next, _clips[_clip]);
                p.SetApplyFootIK(false); p.SetApplyPlayableIK(false); p.SetSpeed(0);
                var output = AnimationPlayableOutput.Create(next, "Stage", _animators[stage]); output.SetSourcePlayable(p);
                RestoreRest(stage);
                double time = _clips[_clip].isLooping ? _clock % _clips[_clip].length : Math.Min(_clock, _clips[_clip].length);
                p.SetTime(time); next.Play(); next.Evaluate(0);
                if (_graph.IsValid()) _graph.Destroy();
                if (_stage >= 0 && _stage != stage) RestoreRest(_stage);
                _graph = next; _playable = p; _stage = stage;
            }
            catch { if (next.IsValid()) next.Destroy(); throw; }
        }
        public void SelectClip(int clip)
        {
            if (clip < 0 || clip > 2) throw new ArgumentOutOfRangeException(nameof(clip));
            if (clip != 2) _loopClip = clip;
            _clip = clip; _clock = 0;
            if (_visuals.ActiveIndex >= 0) Bind(_visuals.ActiveIndex);
        }
        public void AdvanceClock(double delta)
        {
            if (double.IsNaN(delta) || double.IsInfinity(delta) || delta < 0 || float.IsNaN(_speed) || float.IsInfinity(_speed) || _speed < 0)
                throw new ArgumentOutOfRangeException(nameof(delta));
            _clock += delta * _speed;
            if (_clip == 2 && _clock >= _clips[2].length)
            {
                _clock -= _clips[2].length; _clip = _loopClip;
                if (_visuals.ActiveIndex >= 0) Bind(_visuals.ActiveIndex);
            }
            EvaluatePose();
        }
        public void EvaluatePose()
        {
            if (!_graph.IsValid()) return;
            RestoreRest(_stage);
            double time = _clips[_clip].isLooping ? _clock % _clips[_clip].length : Math.Min(_clock, _clips[_clip].length);
            _playable.SetTime(time); _graph.Evaluate(0);
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
