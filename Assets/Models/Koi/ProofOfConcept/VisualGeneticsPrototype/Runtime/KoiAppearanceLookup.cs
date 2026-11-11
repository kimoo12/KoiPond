using System;
using UnityEngine;
using KoiPond.GrowthPrototype;

namespace KoiPond.VisualGeneticsPrototype
{
    [Flags]
    public enum KoiAppearanceLimitation
    {
        None = 0, EarlyStage = 1, UnsupportedPhenotype = 2,
        HirenagaGeometry = 4, IncompatibleRenderer = 8,
        // Existing Ogon presets depict the authored color profile, not verified metallic reflectance.
        AuthoredMetallicOnly = 16
    }

    public readonly struct KoiAppearanceSelection
    {
        public Material Material { get; }
        public string Profile { get; }
        public KoiAppearanceLimitation Limitations { get; }
        public bool HasSupportedPreset => Material != null;
        public KoiAppearanceSelection(Material material, string profile, KoiAppearanceLimitation limitations)
        { Material = material; Profile = profile; Limitations = limitations; }
    }

    /// <summary>Exact expressed-phenotype templates. Never assigns a variety or changes genetics.</summary>
    [CreateAssetMenu(menuName = "Koi/Prototype/Existing Appearance Catalog")]
    public sealed class KoiAppearanceLookup : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string _label;
            [SerializeField] private KoiTraits _traits;
            [SerializeField] private Material _material;
            public string Label => _label;
            public KoiTraits Traits => _traits;
            public Material Material => _material;
            public Entry(string label, KoiTraits traits, Material material)
            { _label = label; _traits = traits; _material = material; }
        }

        [SerializeField] private Mesh _compatibleAdultMesh;
        [SerializeField] private Entry[] _entries;
        public Mesh CompatibleAdultMesh => _compatibleAdultMesh;
        public int Count => _entries == null ? 0 : _entries.Length;
        public Entry GetEntry(int index) => _entries[index];

        public void Configure(Mesh compatibleAdultMesh, Entry[] entries)
        {
            _compatibleAdultMesh = compatibleAdultMesh;
            _entries = entries == null ? null : (Entry[])entries.Clone();
            Validate();
        }

        public void Validate()
        {
            if (_compatibleAdultMesh == null || _compatibleAdultMesh.subMeshCount != 1 || Count != 8)
                throw new InvalidOperationException("Eight audited presets and the single-submesh adult rig mesh are required.");
            for (int i = 0; i < Count; i++)
            {
                var e = _entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.Label) || e.Material == null
                    || e.Material.shader == null || e.Material.shader.name != "Universal Render Pipeline/Lit"
                    || !e.Material.HasProperty("_BaseMap") || e.Material.GetTexture("_BaseMap") == null)
                    throw new InvalidOperationException("Missing or incompatible existing material preset.");
                ValidateTraits(e.Traits);
                if (e.Traits.Fin != KoiFinType.Standard)
                    throw new InvalidOperationException("This catalog has no Hirenaga geometry.");
                for (int j = 0; j < i; j++)
                    if (SameTraits(e.Traits, _entries[j].Traits))
                        throw new InvalidOperationException("Duplicate phenotype signatures are ambiguous.");
            }
        }

        public KoiAppearanceSelection Select(KoiTraits traits, KoiGrowthStage stage, Mesh mesh)
        {
            ValidateTraits(traits);
            if (!Enum.IsDefined(typeof(KoiGrowthStage), stage)) throw new ArgumentOutOfRangeException(nameof(stage));
            var unsupported = traits.Fin == KoiFinType.Hirenaga
                ? KoiAppearanceLimitation.HirenagaGeometry : KoiAppearanceLimitation.None;
            if (stage != KoiGrowthStage.Adult)
                return new KoiAppearanceSelection(null, "Authored stage placeholder", unsupported | KoiAppearanceLimitation.EarlyStage);
            if (unsupported != KoiAppearanceLimitation.None)
                return new KoiAppearanceSelection(null, "Unsupported Hirenaga geometry", unsupported);
            if (mesh == null || mesh != _compatibleAdultMesh || mesh.subMeshCount != 1)
                return new KoiAppearanceSelection(null, "Incompatible adult mesh", KoiAppearanceLimitation.IncompatibleRenderer);
            for (int i = 0; i < Count; i++)
            {
                var e = _entries[i];
                if (SameTraits(traits, e.Traits))
                    return new KoiAppearanceSelection(e.Material, e.Label, traits.IsMetallic
                        ? KoiAppearanceLimitation.AuthoredMetallicOnly : KoiAppearanceLimitation.None);
            }
            return new KoiAppearanceSelection(null, "Unsupported phenotype", KoiAppearanceLimitation.UnsupportedPhenotype);
        }

        public static bool SameTraits(KoiTraits a, KoiTraits b) =>
            a.BasePigmentation == b.BasePigmentation && a.HasRed == b.HasRed && a.HasBlack == b.HasBlack
            && a.Pattern == b.Pattern && a.IsMetallic == b.IsMetallic && a.Fin == b.Fin;

        private static void ValidateTraits(KoiTraits t)
        {
            if (!Enum.IsDefined(typeof(KoiBasePigmentation), t.BasePigmentation)
                || !Enum.IsDefined(typeof(KoiPatternType), t.Pattern) || !Enum.IsDefined(typeof(KoiFinType), t.Fin))
                throw new ArgumentException("Invalid expressed phenotype.");
        }
    }
}
