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

# Revision — Juvenile Rig: Match Adult Behavior
This revision continues the existing isolated prototype rather than creating a new project or rebuilding production assets. It supersedes earlier instructions to create a new prototype folder, retain adult-comparable display size, or treat the adult reference as static. Historical results remain a baseline, not evidence that this revision passed.

## Project Overview (revision)
- Game Title: KoiPond.
- High-Level Concept: Refine the juvenile fish to follow the working adult rig's natural Swim, Idle and Eat behavior, with juvenile-proportioned joints, stable skinning and natural mouth opening/closure. Establish original Meshy appearance through assignment/UV/provenance diagnosis rather than repainting.
- Players: Asset-validation prototype only; no production gameplay change.
- Inspiration / Reference Games: Existing KoiFish_RiggedMesh and adult rig/clips; juvenile Meshy export and juvenile_side/top/angle screenshots.
- Tone / Art Direction: Original Meshy juvenile appearance, including markings and dark pupils where present in the correct source pairing.
- Target Platform: StandaloneWindows64, Unity 6000.6.6f1.
- Screen Orientation / Resolution: Desktop landscape; full comparison captures plus mouth/eye close-ups. No project resolution changes.
- Render Pipeline: Existing URP PC_RPAsset; no global pipeline/post-processing edits.

## Game Mechanics (revision)
### Core Gameplay Loop
Diagnostic loop: compare original rest appearance → animate adult and juvenile at synchronized clip times → inspect regional motion and mouth → refine only juvenile → rerun preservation and playback checks. No pond movement, feeding integration, fry work or production controller changes.
### Controls and Input Methods
Use a prototype-only comparison harness for play/pause, reset, source/adapted mode, and Swim/Idle/Eat selection. Existing New Input System remains unchanged; preferably use serialized harness settings/editor controls, with no new player input mappings. Loop Swim/Idle continuously, play Eat to completion and return to Idle/Swim; also provide a repeat-Eat diagnostic mode with an explicit reset between cycles.

## UI (revision)
No production menu/HUD changes. Diagnostic layout (editor controls or inspector, not a new game UI):
```
Adult reference             Juvenile — 60% body length
[synchronized Swim / Idle / Eat, shared clock]
[Source baseline / Juvenile adapted] [Play/Pause] [Reset]
View: side / top / front / three-quarter / mouth-eye close-up
Status: clip, loop count, timing, bindings, warnings
```
Use identical lighting and fixed camera scale for size comparison; separate detail captures may enlarge both fish equally or clearly label magnification. Preserve the original unrigged juvenile as a rest/appearance control, using a distinct material baseline so a shared incorrect material cannot masquerade as source-appearance verification.

## Key Asset & Context (revision)
### Inspected facts
- Existing outputs are present at Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/; refine these, with baseline backups/reports inside this folder.
- Loaded scene is Scenes/Juvenile_RiggingPrototype_Test.unity, not PondScene. Recheck and preserve current scene setup/unsaved state before editing.
- Adult and juvenile both have the same ten named bones and FishMesh path. Juvenile FishMesh currently has import rotation and uniform scale 31.57; display length is approximately 0.601 units, effectively adult-sized.
- Editor/JuvenileRiggingPrototypeBuilder.cs uses DisplayScale=0.3157, region-constrained surface transfer and landmark pivots. Keep the selected transfer foundation; do not replace it with a fresh global transfer or auto-rig.
- Editor/JuvenileRiggingPrototypeRefinement.cs performs 96 additional global adjacency smoothing passes after 24 earlier passes, diffusing both weights and mouth deltas. Replace repeated global smoothing with protected, regional refinements; ensure rebuild/refine is deterministic and cannot silently accumulate repeated edits.
- Existing report records fin/lip edge ratios around 4–5 after refinement, approximate correspondence with opposing-normal matches, and a provisional muzzle/lip MouthOpen rather than convincing opening. These are unresolved baseline warnings.
- Runtime/JuvenilePrototypePlayback.cs animates only the juvenile, sequences three Swim loops, three Idle loops, Eat once, Idle for 0.5 seconds, then stops. It has no synchronized animated adult comparison.
- Editor/JuvenileRiggingPrototypeValidation.cs assumes exactly one playback component and hides all other renderers for captures. Refactor those assumptions for two explicitly identified animated targets, preservation of activation state, and paired captures.
- Adult source clips bind rotations, Root translation and FishMesh/blendShape.MouthOpen; Swim 1.15 s, Idle 3.2 s, Eat 2.5 s. Idle omits Mouth rotation; Eat omits intermediate tail rotations. Reset unkeyed channels deliberately rather than inheriting old poses.
- Adult KoiFishController.cs adds modest procedural fin paddling/spine motion after animation and changes animation speed by state. The mesh alone does not supply those dynamics. Inspect these read-only; distinguish source-clip fidelity from any prototype-only reproduction of these secondary offsets. Never attach production pond-control logic to this comparison scene.
- Prototype material currently uses juvenile texture.png as _BaseMap/_MainTex, a normal import copy, and a packed DERIVED metallic/smoothness map, white tint, smoothness/metallic scalar 1, and double-sided rendering. Albedo importer is sRGB, compressed, max size 2048. These are audit observations, not a proven cause of pale patches.
- Both source juvenile FBXs currently expose embedded URP/Lit materials with empty texture slots. Their existing imported material state therefore cannot independently prove the Meshy export appearance.
- Original base-color, normal, metallic and roughness maps are authoritative. Packed _MetallicSmoothness maps are derivatives, not original Meshy maps; verify the particular prototype map's recorded deterministic packing separately from other AI-generated packed assets.
- Dependency inspection using AssetDatabase.GetDependencies and reverse asset references found the prototype mesh/material/prefab used by the isolated scene, while production adult prefab is referenced by PondScene. Recheck impact immediately before edits; do not modify original references.

### Allowed modifications / outputs
Only files within Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/ may be modified or added for implementation, plus this planning document. Relevant existing files: Editor/JuvenileRiggingPrototypeBuilder.cs, Editor/JuvenileRiggingPrototypeRefinement.cs, Editor/JuvenileRiggingPrototypeValidation.cs, Runtime/JuvenilePrototypePlayback.cs, Meshes/Juvenile_RiggedMesh.asset, Prefabs/Juvenile_RiggingPrototype.prefab, Materials/Juvenile_Prototype_MAT.mat, Scenes/Juvenile_RiggingPrototype_Test.unity, and Reports/. Any necessary prototype texture-import copies, appearance diagnostic material/shader, comparison tooling or juvenile-adjusted clip copies must also remain here. Preserve old result evidence via baseline/versioned reports rather than relabeling old captures as new.
Read-only: all original juvenile FBXs/maps/screenshots, adult mesh/prefab/material/controller/clips/scripts, production scenes, other prototypes, project/package settings and all source metadata. No fry asset investigation or rigging.

### User-selected approach
- Targeted refinement of the existing adult-derived transfer (selected over a fresh constrained transfer). Advantage: least disruptive and preserves prior approved architecture. Limitation: cannot overcome missing mouth interior/topology by weighting alone.
- Juvenile body length: 60% of adult (selected over 50% or 70%). Use a uniform outer prototype scale, initially 0.6 if measured rest lengths match, outside the keyed animation root. Do not shrink mesh vertices, change FBX import settings, or scale only FishMesh. Preserve juvenile internal bone/renderer/bindpose correspondence.
- First correct anatomy and weights while retaining exact adult clips. If excessive motion remains, allow narrowly scoped juvenile-only motion attenuation without changing clip timing/phase or globally slowing Swim. Keep an exact-source comparison mode and report adaptation values. Prefer normalized quaternion interpolation from rest for regional angular attenuation, not scaling quaternion components. Root translations need deliberate juvenile-length scaling so normalized bob/feeding lean remains adult-like.

## Implementation Steps (revision)
1. Protect sources and establish paired baselines.
   - Dependencies: approval of this revised plan.
   - Recheck dependencies/dependents for every intended edit. Hash original/source/production files and metadata against both a fresh baseline and the existing SourceProtection.tsv where applicable. Capture current scene setup and dirty state; never discard unsaved work or save production scenes.
   - Record adult/juvenile rest body lengths, bone ordering, coordinate frames, regional weights, MouthOpen frame localization and curve extremes. Save previous prototype evidence under Reports/Baseline/ or a versioned equivalent.
2. Diagnose original appearance BEFORE changing appearance or rig behavior.
   - Dependencies: Step 1.
   - Recompare FBX geometry, all UV channels, vertex/index/submesh layout and prototype equivalence. Inspect mouth and both eye surfaces: do their UVs address dark pupil areas in the correct source albedo? Check multi-material/submesh assignments, material remaps, slot semantics, UV scale/offset and export texture identity/version.
   - Compare original PNG bytes/source dimensions with imported resolution/compression and color-space behavior. Validate a prototype-only unlit base-color view against URP/Lit with normals/metallic temporarily disabled, then reintroduce validated maps individually. Use isolated importer copies only if needed; retain unchanged source bytes and metadata.
   - Check normal decoding/Y convention, metallic/roughness channels and deterministic packing. Do not repaint, generate albedo, borrow adult/fry textures, bake screenshot colors, change source UVs or apply a cosmetic tint to hide a wrong pairing.
   - Repair proven assignment/import/shader issues only in the prototype. If correct UV/texture export pairing is unavailable or ambiguous, report exact evidence and ask for the matching original Meshy export; do not claim screenshot equivalence or block independently authorized rig work silently.
3. Refine juvenile pivots and regional skin weights.
   - Dependencies: Step 1; Step 2 audit completed.
   - Update Builder/Refinement and juvenile mesh/prefab together. Keep hierarchy paths/order/rotation frames compatible with the adult; fit head/mouth, pectoral attachments and tail chain independently to juvenile landmarks.
   - Use current adult-transferred weights as the baseline. Revisit localized poor correspondences only where evidence supports it. Protect the head/eyes, maintain smooth increasing tail propulsion, restrict each fin to its own side with a controlled attachment falloff, and prevent mouth influence leaking into eyes/body.
   - Replace unconstrained repeated smoothing with masked, bounded adjacency corrections and deterministic baseline input. Preserve seam consistency without welding. Normalize influences and recompute bindposes whenever rest pivots change; retain original rest vertices/UVs/normals/tangents/indices.
   - Test exact source clips and record before/after normalized head sway, body/tail excursions and regional stretch. If excessive wiggling persists, implement narrowly scoped prototype-only angular/translation attenuation in the harness or isolated clip copies; keep the adult timing, phase relationships and diagnostic source mode unchanged. Do not add independent high-amplitude procedural wiggle.
4. Refine MouthOpen to adult-like opening/closure.
   - Dependencies: Step 3, plus adult mouth analysis from Step 1.
   - Inspect adult lip displacement direction, jaw/Mouth-bone contribution, shape weight timing, closure and opening ratio relative to head width. Inspect actual juvenile lip topology/interior before defining deltas.
   - Replace diffuse muzzle expansion with localized upper/lower lip motion informed by juvenile anatomy, coordinated with Mouth-bone rotation. Protect cheeks, eyes and fin/body vertices; preserve base geometry and shape binding name. Verify shape weight 0 restores the exact rest surface.
   - Preserve adult breathing versus Eat timing/ranges unless a documented juvenile-only amplitude adjustment is needed. Verify continuous closure after Eat, not merely an endpoint key value.
   - Missing cavity or inseparable lip topology is a hard visual limitation: do not invent geometry, tear the mesh or paint a cavity. Report and request separate approval for topology/interior work if necessary.
5. Apply prototype size and build synchronized comparison playback.
   - Dependencies: Steps 2–4.
   - Measure and set the outer juvenile scale so rest body length/adult rest body length = 0.60; update static juvenile control consistently. Keep adult display scale unchanged. Record renderer-space and scaled-world-space validation mappings explicitly.
   - Extend Runtime/JuvenilePrototypePlayback.cs and scene assembly for explicit adult/juvenile references, a shared clock and playback stage control. Animate a reconstructed read-only adult reference hierarchy using the original mesh/material/clips; modify only the prototype scene, not adult assets.
   - Support uninterrupted Swim/Idle, finite Eat with return, and repeat-Eat diagnostics. Reset all rest position/rotation/scale and shape channels between independent runs; correctly dispose/recreate Playables on disable/re-enable. Resolve shape index by name, not hard-coded index zero.
   - Run first at source speed 1.0 for both. If matching production secondary dynamics, reproduce only the inspected modest motion offsets in prototype tooling under an explicit separate mode, derived from the freshly evaluated pose each frame with no cumulative rotation drift. Do not import wandering/water constraints, gameplay controls or feeding effects. Label source-only versus behavior-adapted evidence.
6. Verify side-by-side deformation, appearance and real continuous playback.
   - Dependencies: Step 5.
   - Refactor Validation to handle both targets explicitly rather than Single(playback), and compare at common times with consistent scaling. Capture paired adult/juvenile side/top/front/three-quarter views and mouth/eye close-ups; preserve exact original active states and camera values during capture cleanup.
   - Independently sample >=33 times per clip plus exact extremes; run continuous Play Mode comparison for >=3 Swim loops and >=3 Idle loops, full Eat and Eat-to-Idle/Swim returns. Record actual loop progression, displayed deformation, culling, reset and transition behavior. Add repeated sequence/disable-re-enable checks.
   - Previous automated playback failed to enter Play Mode. Use supported test tooling if available, otherwise perform a manual isolated playback check. If execution is unavailable, mark it NOT RUN and request the user's manual check; do not substitute static samples or extend failed polling indefinitely.
7. Report and stop at juvenile review.
   - Dependencies: Step 6.
   - Save versioned BehaviorMatchResults.md, structural/appearance/runtime results and paired captures under Reports/. Enumerate actual created/modified files, exact derivative provenance, scale ratio, adaptation parameters and clip-by-region pass/warning/fail results.
   - Verify protected hashes and metadata unchanged, no production or other prototype edits, and no fry work. Clearly report incomplete appearance pairing, fin-root/mouth limitations and any unavailable runtime verification; do not claim production readiness.

## Verification & Testing (revision)
- Preservation: source/production files and metadata unchanged; juvenile rest vertices/all UV channels/normals/tangents/indices/submeshes exactly match original. Only prototype skinning/bindposes/approved shape deltas, bounds and outer display scaling change.
- Structure: exact adult hierarchy paths/order; all clip bindings and properties resolve on both targets; finite bindposes and valid nonnegative bone weights summing to 1 within 1e-5. Test index validity and every vertex assigned, not only aggregate sums.
- Rest pose: BakeMesh space mapping is measured, not assumed. World-space rest error <=1e-5 of juvenile body length; body-length ratio 0.60 within measurement tolerance (target <=0.01 absolute ratio error).
- Regional motion: compare normalized excursions and head stability against adult and saved baseline. Fin roots should materially improve rather than merely redistribute distortion; no new seam openings, contralateral leakage, tail folding or head/eye wobble. Document thresholds/measurements and visually inspect; numerical validity is not an aesthetic pass.
- Mouth: independently test weights 0/25/50/90/100 plus actual authored curves and Mouth-bone rotations; exact closure, localized lip motion, eyes/cheeks protected, no tears/inversions. Adult-like opening is a visual acceptance criterion; lack of interior is reported rather than hidden.
- Appearance: isolate albedo-only versus Lit results, inspect each eye UV-to-pupil mapping and source image identity, compare rest original/rigged surfaces under matching light. Restore all source maps with verified semantics; no repainting or unverified texture substitution.
- Continuous animation: >=3 loops Swim/Idle and complete Eat, return to Idle and Swim, repeated Eat, pose reset, disable/re-enable, pause/resume, frame-time variation and offscreen/onscreen culling. No leaked graphs, cumulative secondary motion, stale unkeyed channels or lingering MouthOpen.
- Captures/results: label adult/juvenile, clip/time, size ratio, source/adapted motion and any magnification. Separate sampled Edit Mode evidence from actual runtime tests. Console must have no new compile/binding/test errors; report pre-existing errors separately.
- Final isolation: compare hashes and scene state to baseline, enumerate changed prototype assets and derivatives, preserve earlier evidence, report remaining limitations and stop. No fry rigging or production integration.
