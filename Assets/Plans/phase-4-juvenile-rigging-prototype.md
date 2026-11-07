# Project Overview
- Game Title: KoiPond (project name).
- High-Level Concept: Phase 4 is an isolated juvenile koi rigging feasibility prototype, adapting the adult rig and existing motion to the original juvenile Meshy mesh. It does not change or integrate production gameplay.
- Players: Not applicable to this asset-validation prototype; existing game's player model is outside scope.
- Inspiration / Reference Games: No game reference requested; references are the project's juvenile Meshy model and adult KoiFish_RiggedMesh.
- Tone / Art Direction: Preserve the original juvenile Meshy appearance and anatomy.
- Target Platform: StandaloneWindows64, Unity 6000.6.6f1.
- Screen Orientation / Resolution: Desktop landscape; validation capture resolution recorded with results, not a production setting change.
- Render Pipeline: URP, current PC_RPAsset.

# Game Mechanics
## Core Gameplay Loop
No gameplay changes. Validation loop: inspect rest pose → play/sample Swim, Idle, Eat → review body, tail, both pectoral fins and mouth from multiple angles → refine prototype weights → repeat and document results.
## Controls and Input Methods
No gameplay controls or input changes. Existing project uses the New Input System. Use editor animation sampling and a separate test harness for clip selection, scrub/play and reset; do not attach production swimming/feeding scripts or add player controls.

# UI
No production UI modifications or new menu/HUD assets. Use a minimal editor-only test panel if needed:
```
Juvenile Rig Prototype
[Swim] [Idle] [Eat] [Rest]
Time [-----------------] [Play/Pause]
View: Side / Top / Front / Three-quarter
Status: binding checks | deformation issues
```
This is diagnostic tooling only. Otherwise the Animation window and test harness are sufficient. Capture labels identify clip, time and viewing angle.

# Key Asset & Context
## Read-only sources and inspected state
- Assets/Models/Koi/Meshes/Koi_Stage1_Juvenile.fbx: unrigged MeshRenderer, Mesh_0, 87,391 vertices, no bone weights/bindposes/blendshapes. Imported root has scale 100 and X rotation 270 degrees; mesh is not readable for runtime use. Do not alter original importer settings.
- Assets/Models/Koi/Materials/juvenile texture/Meshy_AI_stylized_koi_fish_20261025_image-to-3d-texture.fbx: also 87,391 vertices with matching inspected metadata. Verify geometry, indices, UVs and transforms before treating these two meshes as equivalent; equal counts alone do not prove correspondence.
- Original juvenile Meshy maps in Assets/Models/Koi/Materials/juvenile texture/: juvenile texture.png, juvenile texture_normal.png, juvenile texture_metallic.png, juvenile texture_roughness.png. These are authoritative. Any packed metallic/smoothness derivative is not an original Meshy texture.
- Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset: 221,941 vertices, 10 bindposes and per-vertex weights, one MouthOpen blendshape. Different vertex counts prohibit direct index-based adult-weight or blendshape copying.
- Assets/Prefabs/Koi_Stage3_Adult.prefab: read-only hierarchy reference and adult renderer setup. Production mesh is also depended upon by Assets/Scenes/PondScene.unity; no changes to either.
- Assets/Models/Koi/Animations/Koi_Swim.anim: 1.15 s, looping.
- Assets/Models/Koi/Animations/Koi_Idle.anim: 3.2 s, looping.
- Assets/Models/Koi/Animations/Koi_Eat.anim: 2.5 s, nonlooping.
- All three clips bind FishMesh/blendShape.MouthOpen and Armature/Root position and bone rotations. Idle and Eat do not key every bone identically; reset to rest before independently sampling clips.
- Assets/Models/Koi/Animations/Koi_AnimatorController.controller: reference only; not edited or used with production behaviour scripts.
- Assets/Editor/KohakuTopologyTransferPoC.cs demonstrates SHA256 protection and isolated-output conventions. Its topology-matching/texture-baking method is NOT applicable to the juvenile/adult mesh mismatch and will not be reused as-is.
- Loaded Assets/Scenes/PondScene.unity was clean at inspection; preserve its loaded/dirty state and do not save it.

## Adult skeleton (preserve paths and bone ordering)
```
Armature/Root                         [0]
  Spine_Front                        [1]
    Head                             [2]
      Mouth                          [3]
    PectoralFin_L                     [4]
    PectoralFin_R                     [5]
  Spine_Back                         [6]
    Spine_Tail1                      [7]
      Spine_Tail2                    [8]
        TailFin                      [9]
FishMesh
```
Adult bones all influence vertices. Inspect detailed spatial weight distribution, joint pivots, MouthOpen deltas and animation ranges during implementation, rather than assuming these are suitable for juvenile anatomy.

## User-selected approach
- Surface transfer + refinement: adapt a copied adult skeleton to juvenile anatomical landmarks, transfer weights using anatomically constrained surface correspondence, then refine body, tail, fin roots and mouth.
- User approved adding a new MouthOpen blendshape only to the separate mesh copy, preserving original rest geometry/topology/UVs/textures.
- Alternative considered: juvenile-specific analytic/manual weights using the adult hierarchy. Offers greater local control but needs more independent tuning. Do not silently replace the selected approach if correspondence fails; document failure and ask before substantial fallback work.

## Proposed outputs (all unique, isolated)
Root: Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/ (choose a unique versioned folder if already present).
- Editor/JuvenileRiggingPrototypeBuilder.cs: source audit, protection manifest, mesh-copy preparation, anatomy mapping, surface weight transfer, rig and prefab generation. No production script changes.
- Editor/JuvenileRiggingPrototypeValidation.cs: preservation, binding, weight, rest-pose and sampled-deformation checks; optional editor test panel.
- Sources/Juvenile_SourceCopy.fbx and copied .meta only if needed to enable read access on a disposable importer; avoid new importer setting changes on originals.
- Meshes/Juvenile_RiggedMesh.asset: same rest geometry, vertex order, indices, UVs, normals and tangents as the chosen juvenile source; new weights, bindposes and copy-only MouthOpen deformation deltas.
- Prefabs/Juvenile_RiggingPrototype.prefab: adapted adult-compatible skeleton and juvenile SkinnedMeshRenderer; no production controller scripts.
- Materials/Juvenile_Prototype_MAT.mat: new prototype-only URP material referencing original juvenile maps. If a packed metallic/smoothness map is necessary, create it only under this folder and explicitly mark it as a derivative; no rebaking/repainting or changing source textures/import settings.
- Animations/Juvenile_Test.controller and separate clip copies only if a test controller/translation adjustment is required. Original clips remain unchanged; report every adjustment and test exact source clips first.
- Scenes/Juvenile_RiggingPrototype_Test.unity: new isolated scene with static unrigged juvenile reference, rigged juvenile and read-only adult reference instances, simple neutral lighting/camera. No production scenes or render pipeline/volume settings changes.
- Reports/SourceProtection.json, Reports/RigAudit.json, Reports/ValidationResults.json, Reports/Phase4_Results.md and Reports/Captures/: provenance, hashes, bone landmarks, transfer metrics, verification outcomes and labeled captures.
Only report files actually created, including .meta files and derivatives, in final results.

# Implementation Steps
1. Establish protection and anatomy baselines.
   - Dependencies: approved plan; read-only source availability.
   - Recheck dependency impact for all relevant sources and existing outputs before edits using available dependency inspection (AssetDatabase.GetDependencies and reverse-reference queries if dedicated tooling is unavailable).
   - Hash original juvenile/adult model files, textures, clips, production prefabs/materials/scenes and their .meta files; record original importer settings, scene setup and dirty state. Guard writes to the unique prototype folder only.
   - Inspect adult rig spatial weight distribution, bindpose consistency, blendshape deltas and curve ranges. Inspect both juvenile models and original screenshots to resolve orientation, scales, fin roots, tail peduncle, head and mouth landmarks.
   - Prefer Koi_Stage1_Juvenile as juvenile geometry source; compare it with the textured Meshy FBX in detail. If they differ in appearance-relevant geometry/UVs and source identity remains ambiguous, stop and ask which model is authoritative.
2. Create and verify a disposable juvenile geometry copy.
   - Dependencies: Step 1.
   - Read mesh data without changing source import settings. If read access fails, copy the FBX to Sources/ and enable readability only there, retaining all geometry-affecting import settings. Confirm exact mesh attribute/index correspondence after reimport.
   - Preserve rest vertices and every UV channel, submesh/index layout, normals and tangents. Do not retopologize, weld, simplify, regenerate, unwrap or transfer adult UVs/textures.
   - Resolve imported scale/rotation through renderer and rig transforms with compatible bindposes, not destructive edits to juvenile vertex positions. Record local-to-test-space mapping explicitly.
3. Adapt a copied adult skeleton and transfer weights.
   - Dependencies: Steps 1–2.
   - Preserve bone names, hierarchy paths, ordering and compatible rotational frames; reposition copied joints to juvenile anatomical landmarks in a common reference space. Fit body/head, tail chain, pectoral pivots and mouth independently, not with a single bounding-box scale.
   - Transfer adult weights via closest source-triangle barycentric interpolation in the anatomical correspondence space, using an accelerated spatial search. Constrain matching to body/tail/left fin/right fin/mouth regions, including surface orientation and distance checks, to avoid matching thin fins to adjacent body or opposite surfaces.
   - Refine transition weights without altering geometry: smooth longitudinal body/tail motion, stable head, localized fin roots and mouth influences. Ensure duplicate seam vertices at the same anatomical point deform consistently without welding them.
   - Normalize valid influences and recompute bindposes from juvenile rest transforms. Report ambiguous/poor-distance matches, fallback assignments, per-bone influence coverage and weight-sum errors. Do not conceal unresolved correspondence errors.
4. Create prototype-only mouth deformation and appearance.
   - Dependencies: Step 3; explicit copy-only MouthOpen approval.
   - Examine adult MouthOpen vertex deltas and curve ranges, then derive a localized juvenile MouthOpen frame informed by juvenile lip anatomy. Do not copy adult deltas by vertex index. Keep all base geometry intact and avoid body/eye/fin movement in mouth deltas.
   - If original juvenile mouth topology cannot plausibly open without tearing, collapse or exposing missing interior surfaces, document this as a limitation; do not add geometry or silently fix topology.
   - Create a new URP test material using original juvenile Meshy maps. Preserve source texture bytes and importer settings. Record normal-map decoding and roughness-to-smoothness conversion if a prototype shader or packed derivative is required.
5. Build isolated preview and animation test assets.
   - Dependencies: Steps 3–4.
   - Save the prototype mesh and prefab, then create the separate test scene. Use fresh test objects/read-only references; do not edit or unpack/save production prefab assets. Remove production gameplay scripts from fresh test instances, never source assets.
   - Test exact Koi_Swim, Koi_Idle and Koi_Eat clips through direct sampling first, validating all paths and MouthOpen renderer binding. Inspect root translation in juvenile units; preserve original clips. If copied translation curves or amplitudes need adjustment, save only test-specific copies and distinguish source-clip compatibility from adjusted results.
   - Use an isolated test controller or harness for continuous playback and transitions, with rest reset and MouthOpen reset between independent samples. No humanoid rig conversion, auto-rig generation, new gameplay control, feeding integration or production controller modification.
6. Verify preservation and deformation; refine within prototype.
   - Dependencies: Step 5.
   - Run checks described below, inspect labeled multi-angle captures and continuous playback, then refine only prototype pivots/weights/deltas. After each change rerun preservation and binding checks.
   - Do not enter or alter the production pond scene for testing. Preserve/restore original editor scene setup without saving production scenes; never discard pre-existing unsaved work.
7. Report and stop.
   - Dependencies: Step 6.
   - Write Phase4_Results.md and final chat summary with exact files created, sources used, clip-by-clip results, anatomical deformation findings, numeric metrics, derivatives, unresolved issues and tests not run.
   - Verify protected file and .meta hashes/importer settings unchanged and no unintended scene/prefab/material/script edits outside prototype and this plan.
   - State whether prototype is acceptable for further review, not production-ready by default. Stop at juvenile prototype: no fry inspection/rigging or downstream integration without separate approval.

# Verification & Testing
## Preservation and structure
- Before/after SHA256 equality for protected source files/.meta; production scenes/prefabs/materials unchanged.
- Prototype rest vertex array, indices, UV channels, normals, tangents and submesh layout match juvenile geometry source exactly in mesh-local coordinates. New data limited to skinning, bindposes and approved blendshape deltas (bounds may expand only for deformation culling).
- Exactly intended skeleton paths and renderer name FishMesh; nonempty bone mapping with correct indices; valid finite bindposes.
- Weight sums within 1e-5 of 1; no negative/nonfinite weights or invalid bone indices; every vertex assigned. Record region coverage and correspondence-distance distributions relative to juvenile body length.
- Bake rest pose and compare to the unrigged juvenile in common test space, target maximum error <= 1e-5 of body length. Any exceedance investigated before animation validation.
- No duplicate seam opening from inconsistent weights/deltas. Fin weights remain left/right anatomically consistent.
## Clip and deformation checks
- Validate every source-clip binding path/property resolves, including MouthOpen; inspect object-reference curves/events before reuse. Confirm expected source clip lengths and loops.
- Independently sample each clip at start/end and at least 32 evenly spaced times plus identified extreme mouth/fin/tail poses; reset all bone transforms/blendshape weights before each independent test. Continuous playback checks between samples remain necessary.
- Swim/Idle: at least three complete loops; inspect loop seams, body bends, tail continuity, fin stability, head jitter, UV stretching and culling.
- Eat: complete start-to-finish playback and return to Idle/Swim; inspect mouth opening/closure, lip stretch, head-body continuity and residual pose/shape state. Test transitions in isolated harness if a controller is created.
- Record pass/warning/fail for body, tail, left/right pectoral fin and mouth separately for each clip, with side/top/front/three-quarter captures of extremes. Inspect pinching, volume loss, tears, fin/body intersections, tail folding and mouth topology limitations.
- Verify finite baked vertices, no explosive bounds, and culling bounds contain sampled geometry. Report maximum displacement, affected vertex fractions and any excessive triangle distortion in critical regions; do not equate numerically valid skinning with visually successful deformation.
- Validate texture identity and original markings/rest appearance under identical neutral lighting. No adult texture/UV substitution.
## Isolation and reporting
- Console free of new compile errors, unresolved animation bindings and test exceptions; document existing errors separately.
- Run editor structural/preservation checks and isolated runtime animation playback where available; explicitly label unperformed tests. No runtime tests in production scene.
- Compare final protected hashes and scene state to baseline. Enumerate every new asset and derivative; do not claim successful deformation until visual playback/captures have actually been reviewed.
- Fry rigging is expressly out of scope and requires user approval after the juvenile report.
