using System;
using UnityEngine;

namespace KoiPond.GrowthPrototype
{
    public readonly struct KoiGrowthAppearanceContext
    {
        public KoiOffspringRecord Birth { get; }
        public KoiTraits Traits { get; }
        public KoiGrowthStage Stage { get; }
        public SkinnedMeshRenderer Renderer { get; }
        public KoiGrowthAppearanceContext(KoiOffspringRecord birth, KoiGrowthStage stage, SkinnedMeshRenderer renderer)
        { Birth = birth; Traits = KoiGeneticsRules.Resolve(birth.Genome); Stage = stage; Renderer = renderer; }
    }

    [DefaultExecutionOrder(-90)]
    public sealed class KoiStageVisuals : MonoBehaviour
    {
        [SerializeField] private KoiGrowthController _growth;
        [SerializeField] private Transform[] _containers;
        [SerializeField] private GameObject[] _rigs;
        [SerializeField] private SkinnedMeshRenderer[] _skins;
        [SerializeField] private float[] _restLengths;
        private int _active = -1;
        [SerializeField] private Vector3[] _restSizes;
        public Vector3[] RestSizes => _restSizes;
        public KoiGrowthController Growth => _growth;
        public Transform[] Containers => _containers;
        public GameObject[] Rigs => _rigs;
        public SkinnedMeshRenderer[] Skins => _skins;
        public float[] RestLengths => _restLengths;
        public int ActiveIndex => _active;
        public KoiGrowthAppearanceContext Appearance { get; private set; }
        public string VisualLimitation => "Authored stage markings only; no genotype-to-texture rendering. Hirenaga visual variants not supplied.";
        public event Action<int> PreparingStage;
        public event Action<KoiGrowthAppearanceContext> AppearanceBound;

        public void Configure(KoiGrowthController growth, Transform[] containers, GameObject[] rigs,
            SkinnedMeshRenderer[] skins, float[] lengths)
        {
            _growth = growth; _containers = containers; _rigs = rigs; _skins = skins; _restLengths = lengths;
            Validate();
        }
        public void Validate()
        {
            if (_growth == null || _containers == null || _containers.Length != 3 || _rigs == null || _rigs.Length != 3
                || _skins == null || _skins.Length != 3 || _restLengths == null || _restLengths.Length != 3)
                throw new InvalidOperationException("Explicit Fry/Juvenile/Adult visuals required.");
            for (int i = 0; i < 3; i++)
                if (_containers[i] == null || _rigs[i] == null || _skins[i] == null || _skins[i].sharedMesh == null
                    || float.IsNaN(_restLengths[i]) || float.IsInfinity(_restLengths[i]) || _restLengths[i] <= 0)
                    throw new InvalidOperationException("Invalid stage visual or calibrated length.");
        }
        public void CalibrateRest()
        {
            Validate();
            _restSizes = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                _restSizes[i] = MeasureBounds(_skins[i], _containers[i]).size;
                if (_restSizes[i].x <= 0 || _restSizes[i].y <= 0 || _restSizes[i].z <= 0)
                    throw new InvalidOperationException("Degenerate skinned rest envelope.");
                _restLengths[i] = _restSizes[i].z;
            }
        }
        public static Bounds MeasureBounds(SkinnedMeshRenderer skin, Transform reference)
        {
            var mesh = skin.sharedMesh;
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bindposes = mesh.bindposes;
            var bones = skin.bones;
            if (vertices.Length == 0 || weights.Length != vertices.Length)
                throw new InvalidOperationException("Weighted skinned vertices required.");
            var matrices = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                matrices[i] = reference.worldToLocalMatrix * bones[i].localToWorldMatrix * bindposes[i];
            var bounds = new Bounds();
            for (int i = 0; i < vertices.Length; i++)
            {
                var w = weights[i]; var v = vertices[i];
                Vector3 point = matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0
                    + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1
                    + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2
                    + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                if (i == 0) bounds = new Bounds(point, Vector3.zero); else bounds.Encapsulate(point);
            }
            return bounds;
        }
        private void OnEnable()
        {
            Validate(); _growth.Changed += Apply;
            if (_growth.Initialized) Apply();
        }
        public void Apply()
        {
            Validate();
            if (!_growth.Initialized) return;
            if (_restSizes == null || _restSizes.Length != 3) CalibrateRest();
            var e = _growth.Evaluation;
            int incoming = (int)e.Stage;
            // Evaluate the new pose while hidden. Keep previous valid stage if preparation fails.
            if (incoming != _active)
            {
                bool wasActive = _containers[incoming].gameObject.activeSelf;
                bool wasVisible = _skins[incoming].enabled;
                _skins[incoming].enabled = false;
                _containers[incoming].gameObject.SetActive(true);
                try { PreparingStage?.Invoke(incoming); }
                catch
                {
                    _containers[incoming].gameObject.SetActive(wasActive);
                    _skins[incoming].enabled = wasVisible;
                    throw;
                }
                _skins[incoming].enabled = wasVisible;
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 target = _restSizes[2] * e.SizeRatio;
                _containers[i].localScale = new Vector3(target.x / _restSizes[i].x,
                    target.y / _restSizes[i].y, target.z / _restSizes[i].z);
                _containers[i].gameObject.SetActive(i == incoming);
            }
            bool rebound = _active != incoming;
            _active = incoming;
            if (rebound)
            {
                Appearance = new KoiGrowthAppearanceContext(_growth.Individual.Birth, e.Stage, _skins[incoming]);
                AppearanceBound?.Invoke(Appearance);
            }
        }
        private void OnDisable() { if (_growth != null) _growth.Changed -= Apply; }
    }
}
