using System;
using UnityEngine;
using KoiPond.GrowthPrototype;

namespace KoiPond.VisualGeneticsPrototype
{
    [DefaultExecutionOrder(-80)]
    public sealed class KoiGeneticAppearanceBinder : MonoBehaviour
    {
        [Header("Isolated visual references")]
        [SerializeField] private KoiStageVisuals _visuals;
        [SerializeField] private KoiAppearanceLookup _catalog;
        [Tooltip("Explicit original Fry, Juvenile, Adult materials; never capture a previously selected preset as the fallback.")]
        [SerializeField] private Material[] _authoredMaterials;
        private bool _subscribed;
        public KoiStageVisuals Visuals => _visuals;
        public KoiAppearanceLookup Catalog => _catalog;
        public KoiAppearanceSelection Selection { get; private set; }
        public bool HasBinding { get; private set; }
        public event Action<KoiAppearanceSelection> Applied;

        public void Configure(KoiStageVisuals visuals, KoiAppearanceLookup catalog, Material[] authoredMaterials)
        {
            if (_subscribed) throw new InvalidOperationException("Configure while inactive.");
            _visuals = visuals;
            _catalog = catalog;
            _authoredMaterials = authoredMaterials == null ? null : (Material[])authoredMaterials.Clone();
            Validate();
        }

        public void Validate()
        {
            if (_visuals == null || _catalog == null || _authoredMaterials == null || _authoredMaterials.Length != 3)
                throw new InvalidOperationException("Explicit stage visuals, catalog and authored materials required.");
            _visuals.Validate();
            _catalog.Validate();
            for (int i = 0; i < 3; i++)
                if (_authoredMaterials[i] == null || _visuals.Skins[i].sharedMaterials.Length != 1
                    || _visuals.Skins[i].sharedMesh.subMeshCount != 1)
                    throw new InvalidOperationException("Only the existing single-material stage renderers are supported.");
        }

        private void OnEnable()
        {
            Validate();
            _visuals.AppearanceBound += Bind;
            _subscribed = true;
            RefreshCurrent();
        }

        public void RefreshCurrent()
        {
            if (_visuals == null || !_visuals.Growth.Initialized || _visuals.ActiveIndex < 0) return;
            var context = _visuals.Appearance;
            // Hot reload can retain the active index but lose the readonly context's managed references.
            if (context.Birth == null || context.Renderer == null)
                context = new KoiGrowthAppearanceContext(_visuals.Growth.Individual.Birth,
                    (KoiGrowthStage)_visuals.ActiveIndex, _visuals.Skins[_visuals.ActiveIndex]);
            Bind(context);
        }

        private void Bind(KoiGrowthAppearanceContext context)
        {
            int index = (int)context.Stage;
            if (index < 0 || index >= 3 || context.Renderer == null || context.Renderer != _visuals.Skins[index])
                throw new InvalidOperationException("Appearance context does not belong to this individual's stage renderer.");
            var selection = _catalog.Select(context.Traits, context.Stage, context.Renderer.sharedMesh);
            // This changes ONLY this renderer's reference. Never access .material or mutate any Material.
            context.Renderer.sharedMaterial = selection.HasSupportedPreset ? selection.Material : _authoredMaterials[index];
            Selection = selection;
            HasBinding = true;
            Applied?.Invoke(selection);
        }

        private void OnDisable()
        {
            if (_subscribed && _visuals != null) _visuals.AppearanceBound -= Bind;
            _subscribed = false;
            if (_visuals != null && _authoredMaterials != null && _authoredMaterials.Length == 3)
                for (int i = 0; i < 3; i++)
                    if (_visuals.Skins[i] != null) _visuals.Skins[i].sharedMaterial = _authoredMaterials[i];
            HasBinding = false;
        }
    }
}
