# Phase 4 — Visual Genetics Step 2
## Result
Implemented ONLY under `Assets/Models/Koi/ProofOfConcept/VisualGeneticsPrototype/`. No production integration. Exact resolved-trait selection uses the existing AppearanceBound event; late enable/hot reload can reconstruct a missing read-only context without changing growth. Per-renderer material references are assigned; no shared Material is modified or instantiated.
## Usage
Open `Scenes/Koi_VisualGenetics_Test.unity` and enter Play. There are 13 independent koi: eight Adult templates, Kohaku carrier, unsupported yellow/red/black hybrid, unsupported Hirenaga, and Fry/Juvenile placeholders. Growth is initially paused; use the existing Growth Inspector controls to reset/advance each same individual. Select its appearance component to see profile and explicit coverage status. Rows/columns are documented in SceneLayout.txt. The source-template root is retained disabled.
## Adult presets
- TaishoSanke: `Assets/Models/Koi/Materials/Taisho_Sanke_Koi_MAT.mat`
- Chagoi: `Assets/Models/Koi/ProofOfConcept/ChagoiTopologyTransfer/Chagoi_OriginalMeshy_ToRigged_PBR_Test.mat`
- Kohaku: `Assets/Models/Koi/ProofOfConcept/KohakuPbrTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_PBR_Test.mat`
- Purachina: `Assets/Models/Koi/ProofOfConcept/PurachinaTopologyTransfer/Purachina_OriginalMeshy_ToRigged_PBR_Test.mat`
- ShiroUtsuri: `Assets/Models/Koi/ProofOfConcept/ShiroUtsuriTopologyTransfer/ShiroUtsuri_OriginalMeshy_ToRigged_PBR_Test.mat`
- ShowaSanshoku: `Assets/Models/Koi/ProofOfConcept/ShowaSanshokuTopologyTransfer/ShowaSanshoku_OriginalMeshy_ToRigged_PBR_Test.mat`
- Tancho: `Assets/Models/Koi/ProofOfConcept/TanchoTopologyTransfer/Tancho_OriginalMeshy_ToRigged_PBR_Test.mat`
- YamabukiOgon: `Assets/Models/Koi/ProofOfConcept/YamabukiOgonTopologyTransfer/YamabukiOgon_OriginalMeshy_ToRigged_PBR_Test.mat`

Compatibility audit: all eight presets use URP/Lit, populated maps and identity UV transforms on the unchanged adult rig. Topology-transfer materials target the adult UV layout rather than blindly using source-variety textures. The eight recognizable authored profiles and the final 13-koi separated layout were reviewed through multi-angle Scene View captures. No missing/magenta shader coloration was observed. Detailed eye/source photometric fidelity was not recertified. No source texture packing, rebaking or importer changes occurred.
## Verified
- Final code edits compiled successfully; final Console contains no errors.
- EditMode lookup: PASS, 284 assertions. Exhaustive 192 expressed Adult signatures: exactly eight presets; all other signatures fall back explicitly. Includes all eight founder profiles, equivalent carriers, both early stages, all eight Hirenaga variants, invalid genomes and mesh mismatch.
- In-PlayMode regression suite: PASS, 463 total assertions (includes the 284 lookup assertions). Thirteen koi tested across both stage boundaries, large skips, same-stage ticks, reset/restore, late and whole-koi re-enable, exactly-once subscriptions, persistent birth/genome references and lineage values, simultaneous eight distinct Adult materials and shared-material immutability.
- Separate multi-frame observation reached BOTH actual Update-driven Fry→Juvenile→Adult transitions, selecting Kohaku and retaining birth/genome references.
- Final protected source verification: Protected=2315 unchanged=2315 changed=0 missing=0 newOutsidePrototype=0.
## Runtime test limitation
Two bounded bootstrap attempts were made with the same 12-second cap; no timeout increases, timescale changes or background setting edits. First attempt passed the full 463-assertion suite and observed the first live transition, but timed out during temporal phase 2. The second, separate temporal-only attempt observed both transitions successfully, then timed out before screenshot capture (phase 3). These are NOT claimed as clean end-to-end bootstrap passes: RuntimeSuiteAndTemporalAttempt1.json and RuntimeResults.json retain the exact outcomes. No runtime screenshot was produced; continuous subjective animation and Game View rendering need manual review. The temporary bootstrap was removed; the editor is outside Play Mode.
## Appearance limitations
- Fry/Juvenile retain their original materials and are explicitly EarlyStage placeholders, not genetically expressed variants.
- Unmatched hybrid phenotypes and every Hirenaga phenotype retain the original authored Adult fallback, with no successful genetic-selection claim. A visually Taisho-like fallback is diagnostic placeholder artwork, never a reassigned genome or inferred variety. Hybrids whose expressed traits exactly match a supported Standard-fin signature are naturally indistinguishable to this traits-only lookup.
- Purachina/Yamabuki select their existing authored platinum/gold presets, but report AuthoredMetallicOnly: packed map red channels are almost entirely zero, so true metallic reflectance is not certified. Other existing shaders/packed derivatives are reused as-is. Original Meshy artwork remains authoritative; derived packed maps are not mislabeled as source artwork.
- Existing growth curves (Fry .20→.35, Juvenile .60→.85, Adult 1.00), rig/fin/mouth constraints and animations are unchanged.
- Population performance is not certified: this small diagnostic caches all three rigs per fish. No production spawning, persistence, feeding or breeding integration was added.
