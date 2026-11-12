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

# Revision — Phase 4: Fry Rigging Prototype
This revision adds the user-requested fry phase to the existing plan. Earlier juvenile-only stop boundaries remain historical and are superseded ONLY for the isolated fry work below after approval. All original assets and existing adult/juvenile outputs remain read-only. This is not approval to refine juvenile further, integrate production gameplay, or repair topology.

# Project Overview
- Game Title: KoiPond.
- High-Level Concept: Build an isolated fry feasibility prototype from the original Meshy fry export, using the adult ten-bone rig and completed juvenile prototype as references. Validate original appearance and natural Swim, Idle and Eat behavior in a synchronized three-stage comparison, with explicit limitations rather than assumed acceptance.
- Players: Asset-validation prototype; no player model or gameplay changes.
- Inspiration / Reference Games: Original fry Meshy FBX/maps and fry_side/top/angle screenshots; existing adult rig/source clips and current juvenile prototype.
- Tone / Art Direction: Preserve original stylized fry proportions, textures and dark eyes; no generated or repainted textures, recoloring, texture transfer or new model generation.
- Target Platform: StandaloneWindows64, Unity 6000.6.6f1.
- Screen Orientation / Resolution: Desktop landscape; fixed-scale three-stage captures and separately labeled detail close-ups. Do not change production resolution settings.
- Render Pipeline: Existing URP PC_RPAsset; no pipeline, package, lighting-project or post-processing changes.

# Game Mechanics
## Core Gameplay Loop
Diagnostic loop: inspect original rest anatomy/appearance -> sample synchronized adult/juvenile/fry poses -> inspect continuous Swim/Idle/Eat and regional deformation -> refine fry-only weights, pivots and mouth deltas -> repeat checks -> report. No pond locomotion, feeding integration or gameplay controller changes.
## Controls and Input Methods
The project uses New Input System; no new player controls or input assets. Use a fry-folder-only Inspector/editor harness for clip selection, pause/resume, reset, source/adapted comparison, continuous Swim/Idle, finite Eat with return, and reset-separated repeated Eat.

# UI
No production UI changes. Diagnostic Inspector/editor layout:
```
Adult — 100%       Juvenile — 60%       Fry — 30% of adult
                 Shared clip clock
[Swim] [Idle] [Eat -> Idle/Swim] [Repeat Eat]
[Play/Pause] [Reset] [Exact source / Stage-adapted]
View: Side / Top / Front / Three-quarter / Eyes / Mouth / Fins
Status: clip, time, loop count, target bindings, regional warnings
```
Shared-scale views establish size progression; magnified close-ups are labeled and not used as size evidence. Keep a static original fry appearance/rest control with independently validated material settings.

# Key Asset & Context
## Read-only source findings
- Preferred fry source: `Assets/Models/Koi/Materials/fry texture/Meshy_AI_stylized_cute_fish_20261024_image-to-3d-texture.fbx`, paired with the four maps in that directory. The source model is non-humanoid and unrigged: MeshRenderer/MeshFilter, 81,417 vertices, 149,086 triangles, one submesh, no bindposes or blendshapes. No Humanoid Avatar or generic auto-rig service is needed.
- `Assets/Models/Koi/Meshes/Koi_Stage2_Fry.fbx` is a second, read-only source reference. Imported vertices, normals, tangents, triangle indices and all eight UV channel arrays match the Meshy-folder FBX exactly. UV0 contains 81,417 entries in [0,1]; UV1–7 are empty. FBX file hashes differ despite matching imported attributes. Do not rename assets to correct the counterintuitive Stage1_Juvenile/Stage2_Fry naming.
- Fry vertex SHA256: `287BAEC8DEAB710DF85605C6AEE91FA14AA926C6C44A392A49F99E2A5AB9C60B`; UV0 SHA256: `2AD39ACA30146BDCBD0DEC76A28B665C12832717BAC3BA4062AB2718DDDD1F06`; index SHA256: `5DDBB67FA286DFA3871BD23317A93807C047902941ACFC9D45EFBE86E0A9A285`. Recompute baselines during implementation.
- Imported fry coordinates have X rotation 270 degrees and scale 100. Full-import extent is (0.963870, 1.208080, 1.902562); width/length=.5066, height/length=.6350. Corresponding juvenile extent is (0.770876, 1.007501, 1.903176); width/length=.4050, height/length=.5294. Fry is about 25% wider and 20% taller at near-equal full-import length; total bounds include fins and are not segmented body/head measurements.
- Longitudinal fry bounds indicate a broad anterior profile and narrower posterior region. Actual eye centers, lips, fin attachments and tail peduncle still need multi-angle visual landmark validation. No degenerate triangles were detected at squared cross-product threshold 1e-20 in full-import space; this is not a manifold, mouth-cavity or topology-quality pass.
- Original fry maps: `Meshy_AI_stylized_cute_fish_20261024_image-to-3d-texture.png`, `_normal.png`, `_metallic.png`, `_roughness.png`. All raw/imported dimensions are 2048x2048; current importers are Default, sRGB=true, compressed, not CPU-readable. Embedded URP/Lit materials have empty base/normal/metallic slots. Correct only disposable prototype copies/imports/materials, never source settings.
- Read-only raw PNG decoding: base-color has 72,841 dark pixels (1.737%, each RGB channel <.2). Broad lateral front eye-candidate UV samples found left 505/6,979 and right 508/7,364 dark values with original UVs. These are deliberately broad masks, NOT pupil segmentation or visual proof of correct dark eyes. Raw metallic is all zero; roughness range 54–158/255, mean 121.75/255. Verify visual eye registration and shader semantics before claiming appearance success.
- Initial raw-decoder tooling required a transient compilation correction; subsequent read-only decoding succeeded. No decoder asset or texture was saved.
- Adult reference: `Assets/Prefabs/Koi_Stage3_Adult.prefab`, `Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset` (221,941 vertices, ten bones, one MouthOpen), original material/controller and `Assets/Scripts/KoiFishController.cs`. Adult prefab is used by PondScene; do not edit either.
- Juvenile reference: `Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Prefabs/Juvenile_RiggingPrototype.prefab`, its mesh/material/runtime/editor scripts, scene and reports. Current mesh has 87,391 vertices, ten bones and MouthOpen. Current material uses the verified `Assets/Models/Koi/New Juvenile/new_juvenile_texture.png`, not the old mismatched juvenile albedo.
- Latest juvenile evidence: `Reports/VerifiedMeshyApplication/ApplicationResults.md` supersedes the earlier appearance failure: sampled markings and dark pupils were reviewed successfully. Mouth aperture and fin-root issues were not redesigned or recertified. Continuous playback remains unverified; earlier BehaviorMatch report explicitly records unsuccessful convincing mouth aperture, residual local strain and runtime-tool limitations. Preserve these distinctions.
- Original Meshy maps are authoritative. Existing koi `_MetallicSmoothness` maps include Unity AI-generated derivatives; the juvenile NewMeshy packed derivative has separately verified deterministic provenance. None is a fry texture source. No new packed texture is allowed by this fry plan.
- Current clean loaded scene: `Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Scenes/Juvenile_RiggingPrototype_Test.unity`. Adult rest length=.601010, juvenile=.360606, ratio=.60. Preserve scene setup and any subsequent unsaved changes.
- Dependency/dependent inspection using AssetDatabase.GetDependencies and reverse-reference queries found no current scene/prefab dependents for the two fry FBXs. Juvenile output is referenced by its isolated scene and saved sampled evidence; adult mesh also by PondScene. Dedicated dependency tooling is unavailable; use the same read-only API fallback and recheck before any edit.

## Skeleton and animation compatibility
Preserve exact paths and bone ordering:
```
Armature/Root [0]
  Spine_Front [1]
    Head [2]
      Mouth [3]
    PectoralFin_L [4]
    PectoralFin_R [5]
  Spine_Back [6]
    Spine_Tail1 [7]
      Spine_Tail2 [8]
        TailFin [9]
FishMesh / blendShape.MouthOpen
```
Original clips remain read-only: Swim=1.15s looping, Idle=3.2s looping, Eat=2.5s nonlooping. MouthOpen keyed peaks approximately 29.99223/25/89.82552. Idle lacks Mouth rotation; Eat omits intermediate tail rotations. Reset all rest transforms and shape channels for independent sampling and transitions. Different vertex counts prohibit direct adult/juvenile weight or shape copying by index.

## User-selected approach and scale
- Selected: adult anatomically constrained surface-weight transfer followed by fry-specific regional refinement. Juvenile provides motion/anatomy methodology and comparison, not blindly copied weights or mouth deltas. Advantages: continuity with adult hierarchy and existing architecture; risks: poor thin-fin/head correspondence and inherited local strain. Refit region maps to fry rather than reusing juvenile hard-coded landmark constants.
- Alternative considered: fry-specific anatomy-based regional weights with the same hierarchy; offers local control and avoids inherited transfer defects but requires more independent tuning. Do not silently switch the whole weighting method if correspondence fails; report evidence and seek approval for substantial fallback work. Small bounded regional corrections are part of the selected method.
- Selected size: fry rest length / juvenile rest length = **.50**. With juvenile/adult=.60, adult:juvenile:fry lengths are **1:.60:.30**, target fry length approximately **.180303** at current display scale. This is a stylized stage-readability choice, not a biological measurement. Alternatives .40 or .60 of juvenile trade stronger progression contrast against eye/fin/mouth readability (MDA aesthetics).
- Apply uniform scale outside the keyed rig root using measured final rest length. Never alter rest vertices, source importer scale or only the renderer scale. Do not assume a literal outer .5 scale unless pre-scaled internal reference lengths justify it. Root translation must scale once consistently with display length, not twice.

## Isolated outputs
Root: `Assets/Models/Koi/ProofOfConcept/FryRiggingPrototype/`. If unexpectedly present, inspect and choose a unique versioned folder; never overwrite unrelated work.
- `Editor/FryRiggingPrototypeBuilder.cs`: protection, copy validation, fry anatomy masks, constrained adult surface transfer, deterministic localized corrections, copied skeleton/bindposes and prefab creation. Adapt methodology by creating independent uniquely named code; do not edit or run the juvenile builder/refinement against its completed outputs.
- `Editor/FryRiggingPrototypeValidation.cs`: exact preservation, bindings, seam/region weights, rest and sampled deformation, raw texture audit, labeled capture/report generation.
- `Runtime/FryStageComparisonPlayback.cs`: three explicit targets, shared time, source/adapted modes, rest reset, loop/transition lifecycle and optional modest secondary motion. Must not modify or attach a second writer to completed juvenile playback; reconstructed test reference instances are controlled only by this new harness.
- `Meshes/Fry_RiggedMesh.asset`: original mesh attributes unchanged, new ten-bone weights/bindposes and localized MouthOpen only.
- `Prefabs/Fry_RiggingPrototype.prefab`: compatible skeleton/renderer and measured outer scale, no production pond behavior.
- `Sources/Fry_SourceCopy.fbx` only if needed for disposable read access; new GUID, copied geometry-affecting importer values, readability changed only on copy. Edit-mode source mesh reads currently work despite isReadable=false; avoid copying if unnecessary.
- `Materials/Fry_Prototype_MAT.mat`, independent albedo-only diagnostic material, and `Shaders/FryOriginalMaps.shader` only if needed: original albedo and separately sampled original roughness/metallic, normal-map support and smoothness=1-roughness at shader evaluation. Metallic is confirmed zero; disable metallic contribution without discarding roughness variation. No rendered/packed/generated texture output.
- Byte-identical normal/roughness/metallic PNG import copies under `Materials/OriginalMapImportCopies/` ONLY if necessary for correct NormalMap/linear decoding. No PNG creation from pixels, repainting, recompression of source files or source importer changes. Original base color used directly unless a byte-identical diagnostic import copy is necessary.
- `Scenes/Fry_RiggingPrototype_Test.unity`: new neutral comparison scene, adult/juvenile/fry synchronized references, static original fry rest control, views/cameras and labels. Read-only adult/juvenile mesh/material references or separate baseline material copies where needed; do not save changes into completed prefab assets.
- `Reports/SourceProtection.tsv`, anatomy/appearance/rig audits, validation metrics, `Fry_Results.md`, actual created-file inventory and labeled `Captures/`. Capture images are diagnostic render evidence, not texture assets or appearance replacements.
- No original clip edits. Only isolated fry clip copies if strongly justified, with source mode retained and every change reported; prefer runtime rest-relative angular attenuation over asset duplication.

# Implementation Steps
1. **Protect and re-establish actual context.** Dependencies: approval of this revision.
   - Recheck all source/reference/output dependency impact; inventory every original and completed adult/juvenile asset and metadata, source images/models/clips/scripts/materials, scenes, project/package settings. Hash protected files before writes; allow implementation writes only beneath the unique fry folder.
   - Record scene setup/dirty states and current reference length/material/playback configuration. Do not save or discard user changes. Source inspection and reports above are planning findings, not completed implementation tests.
2. **Validate fry anatomy, topology and original appearance.** Dependencies: Step 1.
   - Inspect original fry and screenshots from side/top/front/three-quarter and close-up; identify exact eye surfaces/UVs, lip crease, upper/lower lip connectivity, existing cavity, both pectoral roots, dorsal/other fins and tail peduncle. Document reference-space coordinates and labeled landmarks; do not infer a usable mouth cavity from bounds or counts.
   - Audit material slot semantics, identity UV transform, original texture pairing, normal convention and linear roughness sampling. Compare albedo-only versus Lit with maps introduced individually under neutral light. Correct only proven prototype assignment/import/shader issues. Stop appearance certification and request the matching original export if pupil/marking pairing is ambiguous; never tint/repaint/generate a replacement.
3. **Prepare exact mesh copy and fry-fitted skeleton.** Dependencies: Steps 1–2.
   - Duplicate mesh data without changing source importer; if source read access fails, use the disposable copied FBX and verify exact rest attributes. Preserve vertex order, all UV channels, indices/submeshes, normals and tangents. No retopology, welding, smoothing geometry, re-unwrapping or new cavity geometry.
   - Copy adult hierarchy/order/rotational frames to the new fry prefab. Fit root/body/head/mouth/tail joints and both pectoral pivots independently to verified fry landmarks, not a bounding-box-scaled juvenile. Account explicitly for import rotation/scale and Root zero-based translation keys.
4. **Transfer and locally refine fry skinning.** Dependencies: Step 3.
   - Use accelerated closest-triangle barycentric transfer from adult weights in piecewise fry-to-adult anatomical space. Separate body, tail, left/right fin and mouth regions with normal/distance/side constraints; transform normals correctly in correspondence space. Reject opposing-side or unrelated thin-surface matches rather than merely hiding them with unrestricted smoothing.
   - Use deterministic source baselines, masked bounded regional corrections, protected head/eyes, localized lower lip/Mouth influence and graded tail motion. Fin joints rotate their own fins with controlled attachment falloff; dorsal/other unboned fins follow nearby body/tail without inventing extra bones.
   - Normalize valid weights, preserve coincident seam consistency without welding, recompute bindposes. Report match distances, ambiguous/rejected matches, region counts, bone coverage and all fallback assignments. Request approval before wholesale alternative weighting if selected transfer cannot be made reliable.
5. **Create localized fry MouthOpen and appearance assets.** Dependencies: Steps 2–4.
   - Analyze adult mouth aperture, shape deltas and Mouth-bone contribution; use juvenile as a warning/reference, not a deltas template. Derive upper/lower lip motion in fry local anatomy while protecting eyes, cheeks, fins and body. Resolve shape by name and preserve exact rest at weight zero.
   - Inspect shape-only weights 0/25/50/90/100 and combined source Mouth rotation/shape peaks. Do not add missing cavity geometry or paint darkness. If original topology cannot plausibly open, report FAIL/LIMITED mouth acceptance and seek separate topology authorization instead of forcing tearing or diffuse muzzle expansion.
   - Build prototype material/shader using original maps or byte-identical import copies; no texture packing, texture generation or repainting. Record source-copy equality and normal/roughness semantics.
6. **Build measured-scale synchronized three-stage preview.** Dependencies: Steps 3–5.
   - Measure final fry rest length and set outer scale for .50 juvenile length; retain adult/juvenile display sizes unchanged. Use reconstructed test reference rigs or fresh test instances with existing mesh/material references; no edits to completed assets or inherited gameplay behavior. Add independently verified unrigged fry appearance/rest control.
   - Implement the new three-target harness: original source clips first at speed 1.0, shared clip times, continuous Swim/Idle, full Eat -> Idle/Swim, reset-separated repeated Eat, pause/resume/reset and safe disable/re-enable/graph disposal.
   - In stage-adapted mode reproduce only the documented juvenile existing rest-relative body .70/tail .85/fin .70 and Eat .85 skeletal/translation behavior on its test instance, mouth source-driven. Do not modify juvenile assets. Keep source-only mode for all stages. Tune fry angular/feeding amplitudes ONLY after anatomy tests demonstrate a need; preserve adult clip timing/phase. Apply proportional root translation once. Optional inspected modest adult secondary offsets stay separately labeled and OFF for source comparisons.
7. **Verify preservation, appearance and temporal quality.** Dependencies: Step 6.
   - Run the detailed tests below, inspect captures visually and actual continuous playback. Refine only fry pivots/weights/shape/harness/material as justified; rerun preservation/rest/binding checks after each iteration.
   - Prior editor-bridge runtime tests have stalled/unfocused or failed readiness. Use one bounded supported attempt with runtime progress evidence; if unavailable, stop retries and mark continuous verification NOT RUN, with manual fry-scene Play instructions. Never substitute static samples for a runtime pass or change persistent background/Player Settings to conceal the limitation.
8. **Report and stop.** Dependencies: Step 7.
   - Write Fry_Results.md: source identity/provenance, anatomy, chosen ratio and measured lengths, exact outputs, clip-by-region pass/warning/fail, mouth/eye/fin findings, adaptations, numerical metrics, labeled three-stage captures, actual runtime status and remaining blockers.
   - Recheck all protected hashes/importers/completed assets and restore original scene setup without saving protected scenes or discarding user work. Enumerate every created file/.meta/import copy/shader; distinguish new renders from texture assets. Stop at fry prototype review; no production integration or juvenile/adult repair.

# Verification & Testing
## Preservation and structural checks
- Protected original and completed adult/juvenile files/.meta/import settings unchanged; no writes outside fry folder except this plan. Baseline hashes and final diff inventory must identify unexpected changes as failures.
- Fry rest vertices, normals, tangents, UV0–7, submesh/index arrays exactly match the selected original; only skinning/bindposes/MouthOpen and justified culling bounds added. Exactly ten intended bone paths/order; FishMesh and every source clip property resolve. Weight sums within 1e-5 of 1, finite/nonnegative influences, indices 0–9 and every vertex assigned.
- Fin side masks correct, zero protected eye/body leakage from mouth deformation, coincident seams deform consistently. Shape zero reproduces original exactly. Finite nonsingular bindposes and identity rest skinning verified.
- Calibrate BakeMesh/common-space conversion first: existing prototype validation required useScale=true; do not assume useScale=false. Target maximum rest world error <=1e-5 of fry length. Measure adult/juvenile/fry lengths using the same anatomical/bounds convention, report fin-inclusive extent and body-only landmark length separately if available; ratio fry/juvenile=.50 within <=.01 absolute ratio error.
## Appearance, anatomy and sampled animation
- Verify original source bytes for each map and byte-identical import copy; shader samples correct UV0/map semantics, white base tint, original markings and dark pupils on BOTH eyes in rest and animated extremes. Albedo-only diagnosis precedes Lit review; identical neutral lighting for all stages. No exact Meshy photometric match claimed without evidence.
- Sample >=33 independent times/clip plus actual mouth/fin/tail curve extrema on all three targets, with full reset each time. Verify source and selected adapted modes separately. Mouth shape-only weights 0/25/50/90/100/0 plus combined Mouth rotation tests; record opening/closure and whether an aperture/interior genuinely exists.
- Inspect side/top/front/three-quarter paired views and magnified eye/mouth/fin-root close-ups: stable head/eyes, smooth propulsion, attached side-correct fins, tail continuity, no new seams, pinching, collapse, tears, inversions or unbounded stretching. Record normalized regional excursions, edge/triangle strain and culling containment; finite metrics alone are not natural-motion acceptance.
- Three-stage contact sheets label targets, clip/time, source/adapted mode, ratios and any magnification. Rest appearance controls must not share an unverified incorrect setup that invalidates the comparison.
## Continuous and lifecycle tests
- Actual isolated Play Mode: >=3 Swim loops and >=3 Idle loops, complete Eat, Eat-to-Idle and Eat-to-Swim, repeated Eat with complete closure, pause/resume, reset, repeated sequence, disable/re-enable, variable frame time and offscreen/onscreen return. Track clock advancement and visible deformation, not merely successful Play Mode entry.
- No stale unkeyed channels, lingering MouthOpen, cumulative rotations, double root-translation scaling, duplicate animation writers, leaked PlayableGraphs or new compile/binding/test errors. Record pre-existing Console issues separately.
- If runtime execution or visual capture/review is unavailable, label exact tests NOT RUN; provide manual instructions and do not claim natural animations, dark-eye equivalence or convincing mouth success based only on numeric samples.
- Final acceptance report distinguishes structural success, visual/temporal success and topology limitations. Preserve completed adult/juvenile assets even when reference limitations are observed; stop for user review.

# Revision — Phase 4: Koi Growth System (Final Approval Plan)
This revision is the current scope for the requested Fry → Juvenile → Adult progression. Earlier rigging sections are historical context, NOT permission to rebuild/refine any completed rig, texture, animation or prototype. This revision authorizes implementation only after approval, confined to a new growth-system prototype. Production integration requires a separate review and approval. No implementation has been performed during this planning phase.

# Project Overview
- Game Title: KoiPond.
- High-Level Concept: Add configurable, gradual growth to individual koi while retaining their identity, genome and lineage for their entire lifetime. Validate continuous size progression and stage-model changes using existing completed stage assets in a separate scene before proposing production integration.
- Players: This deliverable is an isolated system-validation prototype; it does not change the production game's player model or multiplayer behavior.
- Inspiration / Reference Games: Existing KoiPond genetics, breeding records and three completed stage rigs; no external game reference requested.
- Tone / Art Direction: Preserve existing stylized koi anatomy, markings, textures and authored animation. Stage model shape/markings may differ as already authored; this task does not invent genetic visual matching.
- Target Platform: StandaloneWindows64, Unity 6000.6.6f1, existing .NET Standard 2.0 API compatibility.
- Screen Orientation / Resolution: Existing desktop configuration, unchanged. Diagnostic shared-scale captures may use 1920×1080, clearly labeled; no Player Settings changes.
- Render Pipeline: Existing URP `Assets/Settings/PC_RPAsset.asset`, unchanged.

# Game Mechanics
## Core Gameplay Loop
In the isolated scene, initialize an individual from an existing breeding record → advance active simulation age → continuously increase body length → switch only the visual stage at each threshold → reach Adult and stop increasing size. Observe identity, lineage, phenotype and animation continuity throughout. The production-facing intent is readable maturation and long-term attachment (MDA progression aesthetics), but feeding, rewards, maturity-based breeding eligibility and economy balancing are outside this revision.

### Growth timing and stage semantics
- Two independently configurable, finite, strictly positive durations in seconds: `fryDurationSeconds` and `juvenileDurationSeconds`. Adult is the terminal stage with no further growth.
- New diagnostic profile defaults: **120 seconds Fry → Juvenile**, **180 seconds Juvenile → Adult**; total **300 active simulation seconds** to adulthood. These are test defaults proposed for approval, not final production pacing. Expose both values in the new profile; no existing settings are modified.
- Isolated runtime uses scaled active-game time (`Time.deltaTime`); `timeScale=0`, explicit growth pause, or disabled growth controller suspends advancement. No wall-clock/offline growth, background settings changes, cloud save or time service.
- Store absolute elapsed growth age as a `double`, separate from genetics. Derive stage/progress/size from age and the profile, rather than accumulating scale multipliers or restarting a timer on every stage switch.
- For `F = fryDurationSeconds`, `J = juvenileDurationSeconds`, age `a`:
  - `0 ≤ a < F`: Fry model; size ratio `0.30 + 0.30 × (a/F)`.
  - `F ≤ a < F+J`: Juvenile model; size ratio `0.60 + 0.40 × ((a-F)/J)`.
  - `a ≥ F+J`: Adult model; size ratio exactly `1.00`; stored growth age saturates at `F+J`.
- Thus Fry starts at 30%, reaches 60% immediately before its model transition; Juvenile starts at 60% and reaches 100%; Adult remains 100%. Exact boundaries select the later stage. Linear interpolation is the initial monotonic growth policy; no curve overshoot or stage shrinkage. Different growth rates across the threshold are expected when durations differ.
- Reject invalid durations, nonfinite/negative ages and negative/nonfinite advance deltas with clear diagnostics. Zero delta is a no-op. Large finite deltas may cross both thresholds in one update: preserve leftover time, emit each crossed transition once in order, and present only the final applicable visual stage.
- The profile is fixed for an initialized individual. Editing a shared profile during runtime must not silently remap existing age or shrink a fish; require explicit diagnostic reset/reinitialization. Future production balancing/migration is outside scope.
- Initializing/restoring an already juvenile/adult koi applies its current stage directly without replaying historical transition events. Rebinding a visual or disabling/re-enabling does not reset growth.

### Gradual scaling and model transitions
- Keep one persistent individual GameObject and identity/state throughout its life. Growth never replaces that root or respawns the individual.
- Recommended implementation: three cached visual children built from the existing stage hierarchies, with only one visible/animated at a time. Prepare them once; switch visibility and rebind the prototype animation adapter on thresholds. Advantage: no repeated prefab instantiation/destruction or identity recreation at thresholds; cost: all three meshes/hierarchies are resident. Suitable for this small isolated test, not a population-memory optimization claim.
- Alternative considered: instantiate/destroy only the current stage at transitions. Lower retained memory, but greater transition allocations and rebind/lifecycle risk. Defer this optimization until measured population requirements justify it; it is not the initial implementation.
- Place growth scaling on a dedicated parent above each keyed rig. Preserve each stage's complete internal transforms, bone ordering, renderer transforms, mesh, bindposes and existing authored outer scale.
- Calibrate a new visual-container multiplier from **rest vertices**, not animated renderer/culling bounds. Let `Lstage` be the stage's rest length including its existing prefab transform scales, `Ladult` the reference adult rest length, and `r(a)` the desired ratio. Container uniform scale is `Ladult × r(a) / Lstage`. This is relative to the common individual-root reference space. Do not apply `.30`/`.60` again to already scaled prefabs.
- At age zero and exact stage-entry thresholds the existing stage prefabs are effectively at their authored baseline sizes. Between thresholds the current stage grows uniformly. At a transition, the new model has the same target longitudinal length as the outgoing model; inherent stage anatomy changes are allowed, but length must not jump.
- All stage containers share an explicit local root/origin and forward-axis convention derived from the actual rig transforms. Compare body/root anchors; do not silently align by animated bounds or shift the persistent root.
- Use an atomic visibility switch; geometry/material crossfading and morphing are NOT part of this task. Shape and authored marking changes at model swaps are reported as existing asset behavior, not confused with identity changes.
- Preserve animation clip identity, speed, logical clip time and normalized phase in a new isolated adapter. Evaluate the incoming stage's current pose before exposing it. Reset unkeyed transform and blendshape channels from cached rest so Idle/Eat omissions cannot leak old poses.
- Reuse original Swim/Idle/Eat clips read-only. Preserve source animation amplitudes initially; no rig refinement, clip edits, procedural motion redesign or attenuation tuning. The adapter uses one animation writer per active rig and scales Root translation only through the established hierarchy, not an additional growth multiplier.

## Controls and Input Methods
- Verified active backend is New Input System (`activeInputHandler=1`). No new runtime keyboard/touch bindings, input assets or changes to production controls.
- Use a new Inspector/editor diagnostic panel for duration selection, pause/resume growth, reset an existing individual's age, bounded manual time advance, snapshot/restore and Swim/Idle/Eat selection.
- Growth pause affects growth age only; animation can continue. Global game pause follows existing scaled time behavior. Manual diagnostic advancement is explicit and uses the same growth rules; it does not run from production input.

# UI
No production menu, HUD, label, font or UI asset modifications. Provide an editor-only panel/Inspector, not a new in-game interface:
```
Growth Prototype — Persistent Individual
ID: ...     Parents: ... / ...     Breeding seed: ...
Stage: Fry / Juvenile / Adult     Active age: ... s
Stage progress: ... %            Size: ... % of Adult
Fry duration: 120 s              Juvenile duration: 180 s
[Pause growth] [Resume] [Advance +1 s / +10 s] [Reset age]
[Snapshot] [Restore]             [Swim] [Idle] [Eat]
Genome/lineage unchanged: PASS / FAIL
```
Arrange the growing koi beside static Fry 30%, Juvenile 60%, Adult 100% references using the same view scale. Reference objects are fresh reconstructed visual instances, not edits to completed assets. Shared-scale captures document growth; any magnified close-up is labeled.

# Key Asset & Context
## Confirmed inspection findings
- Existing `Assets/Scripts/KoiData.cs` defines schema-version-1 `KoiGenome` with six diploid loci and mesh-independent `KoiTraits`. It explicitly contains no growth or Unity appearance references. Allele IDs and genome schema remain unchanged.
- `Assets/Scripts/KoiGeneticsRules.cs` validates/resolves genomes, including independent Standard/Hirenaga fin traits. It is not an appearance renderer and must not be expanded into growth logic.
- `Assets/Scripts/KoiBreedingService.cs` defines `KoiOffspringRecord` with stable `Id`, `FirstParentId`, `SecondParentId`, `BreedingSeed`, and `Genome`. `Breed` generates a fresh ID for each birth/replay; NEVER call it during stage transitions or restoration merely to recreate an individual.
- No growth controller, individual lifecycle owner, or genetic appearance rendering implementation was found in `Assets/Scripts/`. Existing genetics is data-only; the growth-to-appearance boundary must be explicit without claiming complete phenotype rendering exists.
- Adult prefab: `Assets/Prefabs/Koi_Stage3_Adult.prefab`, mesh `Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset`; 221,941 vertices, ten bones, MouthOpen. It includes Animator, `KoiFishController`, and CapsuleCollider and is referenced by `Assets/Scenes/PondScene.unity`.
- Juvenile prefab: `Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Prefabs/Juvenile_RiggingPrototype.prefab`; 87,391 vertices, ten bones, MouthOpen, no prefab Animator/production controller. Its material uses the verified `Assets/Models/Koi/New Juvenile/new_juvenile_texture.png` and existing derived maps. It is used by isolated juvenile scenes.
- Fry prefab: `Assets/Models/Koi/ProofOfConcept/FryRiggingPrototype/Prefabs/Fry_RiggingPrototype.prefab`; 81,417 vertices, ten bones, MouthOpen, no prefab Animator/production controller. Existing original-map shader/material and byte-identical import copies remain untouched.
- Fresh exact transformed-rest-vertex Z lengths: Adult **0.60100980**, Juvenile **0.36060590**, Fry **0.18030300**. Authored root uniform scales: Adult **1.00000000**, Juvenile **0.60017690**, Fry **0.30018530**. Ratios confirm **1 : .60 : .30**; literal rounded root scales are not a substitute for calibration. Renderer/culling bounds are larger than actual geometry and are unsuitable for growth calibration.
- Counterintuitive unrigged FBX names remain unchanged: `Koi_Stage1_Juvenile.fbx`, `Koi_Stage2_Fry.fbx`. Use explicit stage mapping to completed rigged prefabs, not filename numeric ordering.
- Original animation bindings use Armature/Root hierarchy and FishMesh/MouthOpen. Source Swim=1.15 s looping, Idle=3.2 s looping, Eat=2.5 s finite. Idle omits Mouth rotation and Eat omits some tail rotations; reset omitted channels deliberately.
- Existing `FryStageComparisonPlayback.cs` has a three-target shared-clock/rest-cache model, but its `Stage` refers to diagnostic playback sequence, NOT biological growth. Read as a reference only; do not repurpose or edit it, and do not attach it as a second animation writer in the growth scene.
- `Reports/Fry_Results.md` records unresolved fry strain and failed convincing mouth aperture. Juvenile also has documented mouth/fin limitations. Continuous playback was not successfully verified through prior tooling. Preserve these warnings; growth implementation does not recertify or repair the rigs.
- Current loaded scene is `Fry_RiggingPrototype_Test.unity`, clean at inspection. The inspected Console contains no warnings/errors. Recheck scene state and Console before implementation; never overwrite subsequent user work.
- Dependencies/dependents were inspected through `AssetDatabase.GetDependencies` and reverse serialized asset references because dedicated dependency tooling is unavailable. Script-to-script type usage requires source search too; empty asset dependency results for genetics scripts do not prove no code consumers.
- Existing tests under `Assets/Tests/Editor/` are menu-driven static EditMode suites; follow that convention without installing packages or changing assembly definitions.
- Original Meshy textures are authoritative. Existing `_MetallicSmoothness` files include Unity AI-generated derivatives; verified juvenile deterministic derivatives have separate provenance. No texture generation, packing, reassignment or shader edits are required for growth.

## Identity/genetics separation
- Add a persistent individual-state wrapper that holds the supplied `KoiOffspringRecord` and a separate growth-state object. Keep the original birth record/genome object references during runtime; never rebuild them at model swaps.
- Expose birth identity/parents/seed/genome read-only. Growth routines receive only growth state and duration data, not mutation/breeding services, meshes, textures or phenotype selection rules.
- Growth does not alter alleles, resolve a new genome, assign a founder variety, change lineage, reroll randomness, or create a new ID. Test both ordinary and mutated/non-starter genomes.
- Stage visual binding provides a separate read-only appearance context: persistent ID, unchanged genome/resolved traits, current stage, new renderer reference. The isolated binder leaves completed source materials unchanged. A future genetic appearance renderer may consume this boundary; no genotype-to-texture or Hirenaga rig creation occurs here.
- Standard/Hirenaga genomes retain their exact fin allele/phenotype at every stage even if no matching completed visual variant exists. Mark unsupported visual variants explicitly rather than silently changing genes or claiming standard geometry depicts Hirenaga.
- Add a local diagnostic snapshot containing the existing birth record values, growth schema version, age, and frozen duration values. Restore without calling the birth constructor/Breed or generating IDs. Serialization preserves value identity/genome/lineage; restored managed object references need not equal pre-serialization references. This is a serialization test, not production save integration.

## New outputs and write boundary
Implementation root: **`Assets/Models/Koi/ProofOfConcept/GrowthSystemPrototype/`**. Recheck for conflicts before creation; if occupied, inspect and use a unique versioned sibling for all new outputs. Existing source folders/files remain read-only.
- `Runtime/KoiGrowthData.cs`: explicit stage enum (Fry=0, Juvenile=1, Adult=2), separate growth state/snapshot schema and persistent individual wrapper. Do not reuse `KoiState` or comparison playback `Stage`.
- `Runtime/KoiGrowthRules.cs`: deterministic age advancement, stage/progress/size derivation and ordered threshold reporting, independent of genetic appearance and scene objects.
- `Runtime/KoiGrowthProfile.cs`: new ScriptableObject duration profile and validation; snapshot effective timing at individual initialization.
- `Runtime/KoiGrowthController.cs`: persistent MonoBehaviour owner, age tick/pause/reset/restore, read-only accessors and transition notifications; no birth generation or material logic.
- `Runtime/KoiStageVisuals.cs`: explicit stage-to-visual references, measured scale calibration, visibility switching and appearance-context handoff; no genetics expression/material repainting.
- `Runtime/KoiGrowthAnimationAdapter.cs`: independent isolated clip clock, rest reset, stage rebinding, phase-preserving pose evaluation and graph cleanup. Existing runtime/editor playback scripts unchanged.
- `Editor/KoiGrowthPrototypeBuilder.cs`: source protection manifest and new scene/prefab assembly. Reconstruct visual hierarchies using read-only mesh/material references; do not instantiate live production behavior and then allow Awake/OnValidate to find pond systems. Adult production controller/collider are excluded from the new visual-only hierarchy by construction. Validate all copied transforms/bone paths and mesh/material references.
- `Editor/KoiGrowthPrototypeInspector.cs`: editor-only diagnostics; no production UI or input.
- `Editor/KoiGrowthEditModeTests.cs`: menu-driven deterministic growth, identity, serialization and scale/structure checks following existing test conventions. New tests only; existing suites unchanged.
- `Profiles/Growth_Diagnostic.asset`: 120/180-second timing, clearly diagnostic.
- `Prefabs/Koi_GrowthPrototype.prefab`: persistent root and three cached visual-only children. No edits to completed stage prefabs.
- `Scenes/Koi_GrowthSystem_Test.unity`: separate neutral scene with the persistent growing koi, static stage references, camera/light and isolated diagnostic setup; no production feeding, wandering, pond systems or genetic appearance changes.
- `Reports/SourceProtection.tsv`, `GrowthValidationResults.json`, `GrowthSystemResults.md`, actual output inventory and labeled diagnostic captures. Record runtime tests NOT RUN when appropriate.
- Unity metadata for these new assets/folders. No mesh, texture, animation clip, material or shader output is planned; source asset references are reused.

# Implementation Steps
1. **Establish protection and exact calibration baseline.**
   - Dependencies: approval of this final revision; read-only reference assets available.
   - Recheck dependencies/dependents, source consumers, scene setup/dirty states and Console. Hash completed rigs/prefabs/meshes/materials/textures/animations/genetics/tests/production scenes and metadata, plus project/package settings. Allow writes only in the new prototype root; this existing plan is the only planning-file revision.
   - Verify stage hierarchy paths, rest vertices/transforms, root anchors, source clip bindings and explicit stage mappings. Record exact rest-length ratios; do not use expanded culling bounds. Confirm no new folder conflict.
2. **Implement deterministic growth data/profile/rules.**
   - Dependencies: Step 1.
   - Create KoiGrowthData/Profile/Rules and the diagnostic profile. Implement the specified piecewise size/timing semantics, input validation, ordered crossing notifications, terminal adult clamp and initialization/restore behavior.
   - Add menu-driven data tests first: boundaries, split versus large time advances, invalid data, independent fish, pause-equivalent zero delta and monotonic ratios.
3. **Implement persistent individual ownership and preservation checks.**
   - Dependencies: Step 2.
   - Create KoiGrowthController holding the supplied existing birth record and separate growth state. Bind records explicitly once; no automatic rebirth on Awake/OnEnable/transition. A diagnostic harness may call existing Breed once to produce a fixture, or supply a serialized record; do not alter Breed.
   - Add read-only identity/lineage/genome access and local snapshot/restore. Growth reset resets age of the same fish, not identity. Test unchanged runtime object references and byte/value snapshots through both thresholds; restored snapshots retain exact IDs/alleles/parents/seed.
4. **Build visual-only stage caches and gradual scaling.**
   - Dependencies: Steps 1–3.
   - Create the builder, visual binder and new prefab, preserving all copied internal transforms, rest pose, bone order, meshes/material references and authored outer scales. Exclude production controller/colliders, completed diagnostic scripts and duplicate animators/writers from new instances by construction.
   - Apply calibrated uniform container scale above keyed rigs. Align verified root frames, switch visibility atomically, and pass the read-only appearance context without changing materials/genetics.
   - Test current-stage ratio at entry/midpoint/boundary and outgoing/incoming equal target length. Keep persistent individual root position/rotation/scale unchanged.
5. **Implement phase-preserving isolated animation binding.**
   - Dependencies: Step 4; verified source clip bindings from Step 1.
   - Create independent adapter referencing original clips. Cache and restore rest channels, resolve MouthOpen by name, evaluate incoming model at the retained shared clip time, and clean up graphs on lifecycle changes.
   - Verify Swim/Idle loops and finite Eat across both stage thresholds; same speed/phase, no doubled translation, pose drift, stale MouthOpen or second animation writer. Missing mappings fail visibly and retain the previous valid visual; report mismatch rather than modifying completed rigs.
6. **Assemble and exercise the isolated growth scene.**
   - Dependencies: Steps 3–5.
   - Create the new scene and editor diagnostics with the persistent koi and static three-stage visual references. Preserve current Fry test scene and production scenes; handle unsaved user work without discarding it.
   - Run the 300-second diagnostic schedule and accelerated/manual-delta scenarios through the same rules. Capture entry/midpoint/threshold/adult comparisons at shared camera scale. No production scene integration, feeding hooks, breeding eligibility changes or input additions.
7. **Verify regressions, actual runtime and preservation.**
   - Dependencies: Step 6.
   - Run new data/structural tests and existing breeding/mutation/multi-generation suites through their existing entry points. Verify no existing system changes or RNG stream perturbation from growth.
   - Attempt one bounded supported isolated runtime check. Previous editor runtime limitations must not trigger repeated long polling, package edits or global background settings changes. If unavailable, mark runtime assertions NOT RUN and provide manual new-scene review steps.
   - Verify final protected hashes/metadata, no external writes and unchanged completed scene assets. Restore original editor scene setup without saving protected scenes or discarding user work.
8. **Report and stop before production integration.**
   - Dependencies: Step 7.
   - Save GrowthSystemResults.md with timing, actual sizes, transition/phase results, identity/lineage/genome invariants, snapshot behavior, exact created files/metadata, test statuses, captures and unchanged source manifest.
   - Distinguish growth success from pre-existing rig/mouth/strain and unavailable genetic visual variant limitations. List concrete production integration follow-ups only; do not execute them or claim production readiness.

# Verification & Testing
## Data/timing
- With diagnostic F=120/J=180: ages 0/60/120/210/300 yield ratios .30/.45/.60/.80/1.00 and stages Fry/Fry/Juvenile/Juvenile/Adult. Test exact boundaries and epsilon before/after.
- Many small advances versus one equal-total delta produce equivalent age/stage/ratio within declared double/float tolerance. A delta beyond adulthood emits Fry→Juvenile then Juvenile→Adult once each and clamps; subsequent ticks emit no further stage event.
- Pause/resume and disable/re-enable preserve age; global timeScale=0 does not grow. No hidden offline catch-up. Growth pause can leave animation running.
- Reject NaN/infinity/negative values and nonpositive durations, including deserialized invalid data. Zero advance valid. Profile edits require explicit reinitialization, not automatic shrinkage.
- Two individuals sharing a profile progress independently. Initialization/restore at a later age does not emit historical threshold events or duplicate initialization.
## Genetics/identity/serialization
- Runtime persistent root, birth-record reference and genome reference stay identical through both switches. ID, parent IDs, breeding seed, schema and all six allele pairs remain exactly unchanged by serialized snapshots.
- Test carrier, mutated, hybrid/non-starter and Hirenaga records; traits before/after remain equal. No founder reassignment or genotype-to-material inference. Unsupported visuals remain explicit limitations.
- Snapshot/restore at Fry midpoint, exact Juvenile threshold, Juvenile midpoint and Adult preserves identity/lineage/genome, timing and age; resume correctly. No Breed/Guid generation on restore or transition. Invalid snapshot schema rejected.
- Growth makes no calls into Unity/System random streams. Existing breeding, mutation and multi-generation suites still pass; report actual execution rather than infer from compilation.
## Size/visual transitions
- Measure rest geometry in the common individual-root reference frame. Target entry ratios .30/.60/1.00 and midpoint ratios as above within **0.001 absolute ratio**. Monotonic longitudinal size with no reset, overshoot or double prefab scaling.
- Outgoing and incoming boundary rest lengths agree within **0.001 of adult reference length**; persistent root transform does not move. Stage anatomical shape changes are expected and documented separately.
- Exactly one stage renderer set visible after a successful transition; cached inactive stages have no active competing animation writer. No stage-root/controller duplication or per-frame instantiate/destroy.
- Source mesh/material references and internal bone/renderer transforms remain unchanged. No renderer-only scaling, importer edits, geometry updates or shared material writes.
## Animation/lifecycle/runtime
- Swim and Idle: at least three loops; finite Eat and return to selected loop. Cross both growth thresholds during Swim and during Eat, including a large-step skip; verify logical clip time/normalized phase and MouthOpen behavior on the incoming rig.
- Reset all omitted source channels; test disable/re-enable, growth pause/resume, snapshot restore, repeated diagnostic reset and scene exit. No leaked graphs, cumulative pose drift, extra Root translation scaling or new compile/binding exceptions.
- Observe actual isolated runtime age advancement and visible size/model progression, not just Play Mode entry. Static sample tests do not certify continuous playback.
- Manual fallback: open only Koi_GrowthSystem_Test.unity, run default 120/180 profile or explicit diagnostic increments, review 30→60→100 sizing, both transitions, identity/lineage status, pause/resume and Swim/Eat continuity. Record which checks were user-reviewed versus automated.
## Preservation/reporting
- Completed adult/juvenile/fry rigs, textures/importers, animations, genetics/breeding/mutation scripts/tests, existing prototypes/production scenes, packages and project settings have unchanged protected file/metadata hashes.
- Any unexpected external edit is a failure requiring disclosure, not silently accepted. Enumerate every new output and tests not executed.
- No rig/topology/texture repair, appearance renderer implementation, production feeding/locomotion integration, offspring auto-spawning, maturity gating, production persistence, offline growth, memory optimization or unrelated modifications.
- Final stop: isolated growth-system review and separate approval before production integration.

# Revision — Phase 4 Visual Genetics, Step 1
Inspection/proposal only. User selected existing-material lookup over full three-stage expression. This revision does not authorize implementation or production integration. Only this planning document is revised now.

# Project Overview
- Game Title: KoiPond.
- High-Level Concept: Map inherited expressed traits to existing compatible appearances without modifying source assets. Start with Adult material selection; explicitly disclose missing early-stage and hybrid visual coverage.
- Players: Isolated diagnostic prototype; production player model unchanged.
- Inspiration / Reference Games: Existing KoiPond founder profiles and Meshy appearances.
- Tone / Art Direction: Preserve original stylized koi artwork.
- Target Platform: StandaloneWindows64, Unity 6000.6.6f1.
- Screen Orientation / Resolution: Existing desktop configuration, unchanged.
- Render Pipeline: Existing URP PC_RPAsset, unchanged.

# Game Mechanics
## Core Gameplay Loop
Existing breeding supplies genome → KoiGeneticsRules.Resolve supplies phenotype → exact appearance lookup → apply on stage binding. No breeding rerolls, genome changes, growth changes or new RNG. Existing-material lookup is simplest and supports progression readability (MDA); full independent expression would require additional stage-specific appearance assets.

| Stage | Initial appearance policy |
|---|---|
| Fry | Existing Fry_Prototype_MAT; explicitly mark genetic appearance unsupported. |
| Juvenile | Existing Juvenile_Prototype_MAT with verified new_juvenile_texture; explicitly mark genetic appearance unsupported. |
| Adult | Exact supported color/pattern/metallic phenotype selects existing adult-compatible material; unknown combinations retain authored baseline with explicit unsupported status. |

Match expressed traits, not parent variety labels or hidden carrier alleles. Founder-profile color signatures: Taisho Sanke=white/red/black/distributed/nonmetallic; Kohaku=white/red/no black/distributed/nonmetallic; Tancho=white/red/no black/head spot/nonmetallic; Chagoi=brown/no red/no black/solid/nonmetallic; Purachina=white/no red/no black/solid/metallic; Yamabuki Ogon=yellow/no red/no black/solid/metallic; Shiro Utsuri=black/no red/black/distributed/nonmetallic; Showa Sanshoku=black/red/black/distributed/nonmetallic. These are lookup templates, NOT genome reassignment. Pattern remains part of the exact key even without markings; do not silently collapse arbitrary combinations.
Fin is orthogonal. Material choice cannot express Hirenaga geometry; retain genes and report unsupported fin form, even when color matches. No closest-variety guessing or global tinting of baked markings.

## Controls and Input Methods
No controls/input changes. Existing New Input System remains intact. Inspector-only diagnostic selection and growth advancement reuse existing prototype behavior.

# UI
No production UI. Proposed diagnostic Inspector: `ID | Stage | Resolved traits | Material | Color coverage | Fin coverage`. Clearly distinguish represented, authored placeholder and unsupported states.

# Key Asset & Context
- Read: Assets/Scripts/KoiData.cs, KoiGeneticsRules.cs, KoiBreedingService.cs. Six diploid loci resolve into mesh-independent traits; arbitrary valid hybrid phenotypes are allowed.
- Existing GrowthSystemPrototype/Runtime/KoiStageVisuals.cs exposes AppearanceBound(KoiGrowthAppearanceContext): birth record, resolved traits, stage and SkinnedMeshRenderer. A separate consumer can bind materials without changing growth/genetics scripts. Growth controller retains birth identity through transitions.
- Loaded Koi_GrowthSystem_Test.unity is clean and wired to growth controller, stage visuals and animation adapter; all three stages currently have fixed materials. Dependency inspection used AssetDatabase.GetDependencies and reverse-reference queries; production Adult prefab is referenced by PondScene.
- Adult Taisho_Sanke_Koi_MAT and seven existing topology-transfer test materials are lookup candidates, not newly certified visual assets. Original variety textures cannot be blindly assigned to another UV layout; existing transfer tooling explicitly remaps source UVs into adult rig UVs.
- Fry uses KoiPrototype/FryOriginalMaps, with metallic hard-coded zero and no base tint. Juvenile uses URP/Lit and verified new juvenile albedo. Neither has a library of genetically distinct stage appearances. No shared shader-property assumption across stages.
- Original Meshy textures are authoritative. Adult packed _MetallicSmoothness maps include Unity AI-generated derivatives; juvenile deterministic derivatives and topology-transfer outputs have distinct provenance. Shader scalar values alone do not establish genetic metallic appearance.
- Current KoiGrowthRules.cs differs from historical reports: Fry .20→.35, Juvenile .60→.85, Adult 1.00. Preserve actual behavior; do not repair growth in this appearance task or cite old reports as current verification.
- Proposed implementation, only after separate approval: new Assets/Models/Koi/ProofOfConcept/VisualGeneticsPrototype/Runtime/KoiAppearanceLookup.cs and KoiGeneticAppearanceBinder.cs; Profiles/ExistingAppearanceCatalog.asset; Editor/KoiAppearanceLookupTests.cs; a separate copied diagnostic scene; Reports/AppearanceCoverage.md. No existing implementation files need edits.

# Implementation Steps
1. Recheck source/material dependencies and reverse references; capture scene state and protected hashes. Audit candidate Adult materials against the actual adult mesh/UVs, markings and metallic semantics. Dependencies: separate implementation approval.
2. Implement pure exact-trait lookup and explicit coverage results; catalog only visually verified existing materials. Keep Hirenaga support separate from color support. Dependencies: Step 1.
3. Add an isolated binder that subscribes/unsubscribes to AppearanceBound and applies the current valid context when enabled. Assign existing material references only to fresh diagnostic renderers; never mutate shared materials. Retain/reset the renderer's authored baseline for unsupported mappings. Dependencies: Step 2.
4. Test on a separate scene copy with founder, carrier, hybrid and Hirenaga fixtures across Fry→Juvenile→Adult and snapshot/reset/re-enable. Existing growth scenes, prefabs, meshes, rigs, textures, shaders, animations and production scenes remain untouched. Dependencies: Step 3.
5. Report supported signatures and limitations, verify protected hashes, and stop. Full Fry/Juvenile inheritance, arbitrary hybrid markings, Hirenaga rigs and production integration require later approval. Dependencies: Step 4.

# Verification & Testing
- Exact signatures map consistently; equivalent expressed phenotypes with different carrier alleles select the same material. Invalid genomes rejected; unsupported combinations explicitly flagged, never nearest-matched.
- All eight founder color profiles audited on the adult rig; UV registration, eyes, markings and metallic appearance visually reviewed before catalog acceptance.
- Fry/Juvenile placeholders and Hirenaga limitations remain visible in diagnostics; no claim of full three-stage visual inheritance.
- Birth ID, genome, lineage and seed unchanged; no RNG/Breed calls. Stage transitions, large skips, reset/restore and re-enable bind the correct renderer without duplicate subscriptions or stale materials.
- No shared material mutation, source/importer edits, rig/mesh/clip changes or production scene changes. Preserve current growth and animation behavior, including known discrepancies. Tests and visual review are proposed, not performed in Step 1.

# Revision — Phase 4 Visual Genetics Step 3A: Texture Transfer Pilot
This revision proposes implementation of only Kohaku and Tancho adult-to-Fry/Juvenile albedo transfer after approval. Previous implementation scopes are historical, not authorization to edit their outputs. Stop for visual approval before expansion.

# Project Overview
- Game Title: KoiPond.
- High-Level Concept: Validate inherited starter markings on original early-stage anatomy using four offline UV-aware derivative albedos and isolated diagnostic materials. No production or genetics/growth integration.
- Players: Asset-validation prototype only.
- Inspiration / Reference Games: Existing adult Kohaku/Tancho appearances and original Meshy stage anatomy.
- Tone / Art Direction: Preserve stylized stage proportions and anatomical detail; reuse authored markings, not generated artwork.
- Target Platform: StandaloneWindows64; Unity 6000.6.6f1.
- Screen Orientation / Resolution: Existing desktop settings unchanged; labeled side/top/front diagnostic captures.
- Render Pipeline: Existing URP PC_RPAsset unchanged.

# Game Mechanics
## Core Gameplay Loop
Inspect rest-stage anatomy → establish constrained adult correspondence → bake into unchanged stage UV0 → inspect coverage and registration → refine correspondence only → review four outcomes. No genome, identity, trait resolution, growth or RNG changes.
## Controls and Input Methods
Editor-only builder/validation commands. No new player controls or input assets; existing New Input System unchanged.

# UI
No production UI. Diagnostic layout:
```
Adult source | Fry result + original control | Juvenile result + original control
Kohaku row / Tancho row
Views: Side / Top / Front; eyes/mouth/fin-root close-ups
Status: valid transfer / protected source detail / rejected / UV conflict
```
Diagnostic controls remain editor-only; no UI asset or package dependency.

# Key Asset & Context
- Adult source mesh: Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset (221,941 vertices, 417,156 triangles).
- Source albedos: Assets/Models/Koi/ProofOfConcept/KohakuTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_BaseColor.png and Assets/Models/Koi/ProofOfConcept/TanchoTopologyTransfer/Tancho_OriginalMeshy_ToRigged_BaseColor.png; confirm against current catalog material references before baking. These are already adult-UV-compatible derivatives, not original Meshy exports. Record upstream provenance.
- Fry reference: Assets/Models/Koi/ProofOfConcept/FryRiggingPrototype/Prefabs/Fry_RiggingPrototype.prefab and its mesh/material dependencies (81,417 vertices, 149,086 triangles).
- Juvenile reference: Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Prefabs/Juvenile_RiggingPrototype.prefab and its mesh/material dependencies (87,391 vertices, 159,982 triangles). Current albedo is Assets/Models/Koi/New Juvenile/new_juvenile_texture.png.
- All three have one submesh, UV0 only, ten bindposes and one blendshape. Inspection found no zero-area UV triangles at the inspected tolerance; UV overlap freedom and accurate surface correspondence remain unverified.
- Existing adult topology-transfer code requires triangle bijection; it cannot be run unchanged against early-stage topology. Implement independent anatomy-constrained surface mapping, not index copying or global bounds projection.
- Fry shader retains existing stage normals/roughness and zero metallic; Juvenile retains existing stage normals and packed map. No normal, roughness, metallic or shader output/change in this nonmetallic pilot.
- Loaded VisualGeneticsPrototype/Scenes/Koi_VisualGenetics_Test.unity was clean at inspection. Preserve editor setup and subsequent unsaved work.
- Read-only dependency inspection uses AssetDatabase.GetDependencies and reverse asset-reference queries because dedicated dependency tooling is unavailable.

## New output boundary
Use Assets/Models/Koi/ProofOfConcept/EarlyStageAlbedoTransferPilot/; if occupied, inspect and choose a unique versioned sibling. All implementation writes confined here, except this plan revision.
- Editor/EarlyStageAlbedoTransferBuilder.cs: protection manifest, landmark/region correspondence, accelerated triangle search, UV rasterization and four derivative bakes.
- Editor/EarlyStageAlbedoTransferValidation.cs: coverage/conflict audits, deterministic checks, preservation and view capture tooling.
- Profiles/TransferLandmarks.asset: explicit stage/adult anatomical landmarks, protected region masks and recorded rejection thresholds.
- Textures/Kohaku_Fry_Albedo_DERIVED.png, Kohaku_Juvenile_Albedo_DERIVED.png, Tancho_Fry_Albedo_DERIVED.png, Tancho_Juvenile_Albedo_DERIVED.png. Start at 2048 square; higher resolution only if reported sampling evidence warrants it. Exactly four appearance albedos.
- Materials/: four copies of existing stage materials with only base albedo references changed; synchronize legacy base slot only where relevant. Diagnostic unlit material/shader may be created here for albedo review, never replacing source shaders.
- Scenes/EarlyStage_AlbedoTransfer_Test.unity: fresh visual-only hierarchies using unchanged mesh/rig/material references and neutral local lighting; no production behavior scripts.
- Reports/: hashes, source/derivative provenance, landmark and threshold records, coverage/conflict metrics, quality results, inventory and side/top/front captures. Coverage images are report diagnostics, not additional appearance textures.
- No existing catalog/binder, growth/genetics code, source material/importer, prefab, mesh, animation, scene, package or project-setting edits. Hirenaga and other varieties excluded.

# Implementation Steps
1. **Protect sources and recheck context.** Dependencies: approval.
   - Recheck dependencies/reverse references of all read-only sources. Hash source meshes/prefabs/maps/materials/clips, existing systems/scenes and metadata. Record loaded/dirty scenes and prevent external writes. Read meshes in EditMode without changing importers; if unavailable, stop and report rather than alter sources.
2. **Establish anatomical correspondence.** Dependencies: Step 1.
   - Inspect adult/Fry/Juvenile rest geometry in a measured common orientation/scale. Identify body/head boundaries, eyes, lips, both fin roots, tail peduncle and fins; record landmarks and protected masks explicitly.
   - Fit a piecewise, non-destructive destination-to-adult correspondence space, separating body/head/tail/left and right fins. Use accelerated closest-triangle barycentric queries constrained by region, side, normal agreement and normalized distance. Record thresholds before full baking; reject ambiguous/opposing/poor matches. No adult mesh deformation or destination UV edits.
   - Preserve original-stage eye/pupil and lip detail through explicit narrowly bounded protected-region sampling of original stage albedo. Label this intentional source-detail retention separately from adult transfer; do not use it to conceal failed mappings. Protect fin-root registration via regional constraints, not broad retention of old colored markings.
3. **Audit and bake four derivatives.** Dependencies: Step 2.
   - Rasterize destination UV0 at texel centers; recover destination surface positions and evaluate correspondence per sample, not just per vertex. Sample adult UV albedo with documented color-space/filtering semantics; no tinting or new pattern synthesis.
   - Audit UV overlap owners, source-coordinate/color disagreements, subpixel triangles, unmapped/rejected samples and finite values. Incompatible overlap samples remain explicit conflicts, not averaged or arbitrarily first-wins accepted.
   - Mark rejected/conflicting surface texels with conspicuous diagnostic color and report masks/counts. Such textures remain failed/partial pilot outputs, never certified appearances. Unused atlas space is not an unmapped surface.
   - Allow finite nearest-edge padding only outside valid UV interiors for seam/mip sampling; never pad across rejected surface regions or overwrite conflicts. Check coincident geometric seams without welding or changing mesh data.
4. **Create isolated diagnostic materials and scene.** Dependencies: Step 3.
   - Copy stage materials, replacing only albedo references while retaining all stage normal/roughness settings and shader semantics. Preserve source materials and shared assets byte-for-byte.
   - Construct fresh visual-only hierarchies preserving source transforms/bone paths, using original stage meshes and clips. Include adult references and independent original-stage controls. No catalog or system hookup.
5. **Validate and capture.** Dependencies: Step 4.
   - Capture labeled side/top/front for both varieties and both stages under identical local lighting; add eye/mouth/fin-root/seam close-ups and albedo-only views. Review coverage masks alongside rendered results.
   - Verify Tancho head-spot location and lack of unintended body red; compare Kohaku body marking distribution with adult source at matched anatomical positions. Compare landmark positions/normalized correspondence residuals, not just similarity of screenshots.
   - Sample unchanged Swim/Idle/Eat at representative/extreme poses to inspect eye/mouth preservation and seam/fin-root stability; do not repair existing rig defects. Label continuous playback NOT RUN if unavailable; static captures are not runtime certification.
6. **Report and stop.** Dependencies: Step 5.
   - Report each of four textures as PASS/PARTIAL/FAIL with numeric coverage/conflicts, protected-source-detail fraction, registration findings, finite-resolution/resampling limitations and unperformed checks. Verify protected hashes and enumerate every new file and metadata.
   - Restore editor scene setup without saving protected scenes or discarding user work. Stop for user visual approval; do not expand varieties, implement hybrid compositing, Hirenaga or production integration.

# Verification & Testing
- Exact source bytes/importer metadata, stage geometry/UVs/normals/tangents/indices/weights/bindposes/blendshapes, rigs, clips and original materials unchanged. No writes outside isolated folder apart from this plan.
- Deterministic repeated bake with identical configuration; barycentric validity, finite UVs/results, side and region consistency. Record per-region accepted/rejected coverage and correspondence-distance/normal distributions normalized to body length.
- Quantify UV overlap events and incompatible surface/color samples, degenerate/subpixel footprints, seam disagreement, protected-source sampling and unused atlas pixels separately. A PASS requires no unresolved failed/conflicting visible-surface regions and no concealed extrapolation; report remaining holes as PARTIAL/FAIL.
- Both pupils and lips retain source registration; no inherited body patches on eyes, opposite fins or mouth detail. Fin-root transitions and UV seams reviewed at close range and with filtering/mips. Any shared-UV conflict with protected anatomy blocks certification rather than changing UVs.
- Kohaku red/white distribution and Tancho head spot reviewed on all four outputs in side/top/front views. Numerical correspondence alone cannot certify visual accuracy; record human approval pending.
- Diagnostic materials retain original normals, roughness, shader and all non-albedo settings. Source Meshy versus upstream adult derivative versus new stage derivative provenance explicitly distinguished.
- Capture actual results and list quality limitations, source-asset animation defects and tests NOT RUN. No claim of complete inherited genetics or production readiness. Four-texture pilot only, then stop.

# Revision — Phase 4 Step 3B: Refine Four Pilot Transfers
Supersedes Step 3A correspondence and eye-protection implementation only within the existing isolated pilot after approval. Re-bake the same four Kohaku/Tancho Fry/Juvenile albedos; no AI repainting, generated replacement artwork, guessed-color filling, other varieties or integration.

# Project Overview
- Game Title: KoiPond.
- High-Level Concept: Remove duplicate adult-eye artwork and repair incomplete anatomical correspondence while retaining original early-stage eyes, facial detail and inherited Kohaku/Tancho markings. Review the same four pilot appearances before expansion.
- Players: Isolated asset-validation prototype; no gameplay changes.
- Inspiration / Reference Games: Original stage Meshy textures/anatomy and current adult Kohaku/Tancho source appearances.
- Tone / Art Direction: Existing stylized anatomy; source-sampled inherited markings.
- Target Platform: StandaloneWindows64, Unity 6000.6.6f1.
- Screen Orientation / Resolution: Existing settings unchanged; labeled diagnostic views and close-ups.
- Render Pipeline: Existing URP PC_RPAsset unchanged.

# Game Mechanics
## Core Gameplay Loop
Compare baseline → inspect anatomical failures and eye sources → refine surface correspondence/protection → re-bake four albedos → audit coverage and markings → review multi-angle results. No genetics, growth, identity or random-state changes.
## Controls and Input Methods
Existing editor-only pilot commands; New Input System unchanged. No new runtime controls.

# UI
No production UI. Extend diagnostic review to left and right face, front, top, both sides, fin roots/tail/seams. Labels distinguish Step 3A baseline from Step 3B results, Lit versus albedo-only and diagnostic magnification.

# Key Asset & Context
- Write boundary: Assets/Models/Koi/ProofOfConcept/EarlyStageAlbedoTransferPilot/ only, plus this plan. Preserve all outside sources and metadata.
- Modify Editor/EarlyStageAlbedoTransferBuilder.cs and Editor/EarlyStageAlbedoTransferValidation.cs; update only the four existing Textures/*_Albedo_DERIVED.png and pilot-only settings/reports as needed. Preserve existing material GUIDs, shader and non-albedo settings; do not delete/recreate materials during rebake.
- Profiles/: versioned numeric anatomical landmarks/region definitions and source-eye exclusion masks. Reports/Step3B/: new captures, metrics, verification, inventory and results. Preserve Step 3A reports/captures and baseline copies of changed pilot outputs under Reports/Step3B/Baseline/.
- Existing read-only adult mesh/albedos, Fry/Juvenile prefabs/meshes/source albedos, normals/roughness, rigs, animation clips and all genetics/growth/appearance systems remain unchanged.
- Current builder uses only body/left-pectoral/right-pectoral triangle classification, global width/height ratios, longitudinal bone anchors and small stage eye/lip spheres. There is no source adult-eye exclusion. These are plausible causes of duplicate eyes and poorly mapped unboned fins, not yet independently certified diagnoses.
- Previous rejected surface fractions: Fry 9.02%, Juvenile 6.14%. Mean accepted distances .04110/.02934 in adult body length .60101. Previous overlap audit omits coincident-source color disagreement and deterministic repeat testing.
- Current loaded scene: pilot Scenes/Pilot_MagnifiedReview.unity. Recheck dirty state; preserve setup and never discard unsaved user edits. Inspect current validator before editing, including capture isolation, cleanup and missing views.

# Implementation Steps
1. **Protect and diagnose.** Dependencies: approval.
   - Recheck dependencies and reverse references for intended pilot edits via read-only AssetDatabase APIs and code usage search where appropriate. Hash all outside protected files/metadata; record pilot baseline and source provenance. Preserve current editor state and baseline images before writes.
   - Inspect original adult/Fry/Juvenile eyes, facial surfaces, mouth, dorsal head region, fin roots/tips/edges, tail peduncle and current rejected samples from both sides. Locate actual source UV footprints; dark thresholds and existing skin weights are only candidate hints, not final segmentation.
2. **Separate inherited markings from eye/facial artwork.** Dependencies: Step 1.
   - Define reviewed left/right adult eye artwork exclusion in source surface/UV space, including eyelid artwork and bilinear/mip sampling footprint; evaluate exclusions per variety if footprints differ. Never allow inherited adult-eye samples outside original stage eyes.
   - Replace small centroid spheres with anatomically bounded stage eye/eyelid and facial-detail masks sampled directly from the matching original stage albedo. Preserve lips and local original facial detail without copying broad original colored head/body patches. Keep the dorsal head marking domain separate so Tancho's spot and Kohaku head markings are not erased.
   - For transferred regions previously reaching adult eyes, correct facial correspondence to verified corresponding non-eye source surfaces. If a valid eye-free source correspondence is unavailable, mark unresolved; do not manufacture white pixels, blur/inpaint eyes away, substitute arbitrary nearby colors or silently enlarge retained stage regions to hide it.
3. **Refit body and all fin/tail regions.** Dependencies: Steps 1–2.
   - Add anatomy-based segmentation for head/body, left/right pectoral, dorsal, ventral/other fins, tail peduncle and caudal fin using geometry/connectivity/landmarks, with skin weights as supporting hints only.
   - Fit local body cross-section profiles and explicit fin root/tip/leading/trailing-edge coordinate charts to matching adult anatomy; preserve side and thin-surface orientation. Tail fin maps to adult caudal surface, not extrapolated body chain. Keep destination mesh/UVs untouched.
   - Use accelerated constrained barycentric source-surface queries after local chart fitting; record mapping Jacobians, region-specific distance/normal/side gates and ambiguity. Correct bad segmentation/chart geometry rather than globally raising thresholds or filling holes.
   - Ensure boundaries and coincident seams share compatible mappings without welding or changing indices. If a stage region lacks a compatible adult surface, retain explicit unresolved status and request review, not guessed colors.
4. **Re-bake idempotently and audit.** Dependencies: Steps 2–3.
   - Re-bake the same four 2048-square albedos; update existing pilot materials only if needed, keeping GUIDs and non-albedo configuration. No scene/system rebuild unless required for isolated diagnostics.
   - Track source triangle/UV/region and protection provenance per sample. Check UV overlap including source-coordinate/color disagreement for coincident samples, rejected regions, duplicate-eye source leakage, filtering footprint and seams. Never average incompatible overlaps or pad across failed surface regions.
   - Repeat the same bake twice and compare decoded appearance pixels/configuration hashes; prevent output/material duplication and cumulative edits. Keep padding outside valid UV interiors only; report subpixel/mip limitations.
5. **Verify anatomy and markings visually.** Dependencies: Step 4.
   - Capture all four results against original stage controls and adult sources: top/front/left/right/three-quarter plus left-eye, right-eye, lips, fin-root/tail/seam close-ups. Use identical neutral local lighting with albedo-only and Lit views. Preserve source normal/roughness maps and scalar properties.
   - Verify exactly the original left/right eye artwork remains, without a second adult eye on cheek/head. Compare protected pixels to original stage samples and assert zero adult-eye-mask sampling outside protection.
   - Verify Kohaku marking boundaries/distribution against the adult source, and Tancho one circular dorsal head spot with no accidental retained stage red elsewhere. Compare landmark registration and top-view spot outline; do not redraw a circle or force colors to obtain a pass.
   - Sample original Swim/Idle/Eat at relevant eye/mouth/fin/tail extremes on diagnostic instances; distinguish pre-existing deformation defects from texture defects. Mark unavailable continuous runtime tests NOT RUN.
6. **Report and stop.** Dependencies: Step 5.
   - Save Reports/Step3B/RefinementResults.md with per-texture baseline/new rejection fractions, region residuals, overlap conflicts, eye leakage, protection coverage, spot/marking findings, determinism, captures, files changed/created and remaining issues.
   - Recheck all outside protected bytes/metadata and unchanged stage geometry/UVs/rig/clip/material references. Restore initial editor setup without saving outside scenes or discarding unsaved work.
   - Stop for visual approval. No expansion, hybrid rendering, Hirenaga, source repainting or production integration.

# Verification & Testing
- Outside pilot protected files and metadata unchanged; stage mesh/UV/rig/animations/normals/roughness untouched. Existing pilot materials retain GUIDs and shader/non-albedo properties. Baseline evidence preserved, new evidence separately versioned.
- No adult-eye exclusion-mask sample or its filtering footprint contributes to transferred non-eye regions; original two stage eyes and facial detail preserved and reviewed from both sides. Protected stage-source pixels match documented original UV sampling.
- Explicit per-region valid/rejected/conflicting coverage. A complete coverage PASS requires zero unresolved visible-surface holes, no cross-anatomy matches and reviewed seam continuity; magenta remaining means PARTIAL/FAIL, never silently hidden.
- Repeat-bake decoded pixels identical; no duplicate material assets or changed source references. Finite barycentrics/Jacobians, correct side/normal constraints and source correspondence provenance recorded.
- Kohaku markings preserved; Tancho head spot remains one circular dorsal feature with no extra eye/spot/body-red contamination. Numeric checks support, not replace, labeled multi-angle visual review.
- Recheck Console; disclose implementation warnings/failures and checks NOT RUN. Do not claim eye, coverage or marking success until actually reviewed. Stop at four-texture visual approval.

# Revision — Assign Approved Kohaku Fry v03 and Trial Juvenile Red Pattern
This revision supersedes earlier full-albedo/re-bake scopes for this task only. Fry v03 is user-approved; produce exactly one new Juvenile candidate using the accepted red-pattern extraction and UV-gutter cleanup. Execution requires approval of this saved revision. Do not proceed to Step 3C.

# Project Overview
- Game Title: KoiPond.
- High-Level Concept: Display the approved Kohaku Fry appearance only in the isolated pilot, then test coherent inherited red markings on original Juvenile anatomy without altering existing systems or source assets.
- Players: Asset-review prototype; no gameplay changes.
- Inspiration / Reference Games: Adult Kohaku Meshy appearance and approved Fry v03; no external game references required.
- Tone / Art Direction: Existing stylized stage details with adult-derived red-pattern placement.
- Target Platform: StandaloneWindows64, Unity 6000.6.6f1.
- Screen Orientation / Resolution: Existing desktop settings unchanged; labeled side/top/three-quarter review captures.
- Render Pipeline: Existing URP PC_RPAsset unchanged.

# Game Mechanics
## Core Gameplay Loop
Review adult/original-stage/candidate appearances, validate protected detail and seam continuity, and stop for human visual approval. No growth, genetics, feeding, locomotion or identity changes.
## Controls and Input Methods
Editor-only bake and review commands; retain existing New Input System and all pilot playback components unchanged.

# UI
No UI assets or production interface edits. Review contact sheet:
```
                     Side          Top          Three-quarter
Adult Kohaku         reference     reference    reference
Original Juvenile    baseline      baseline     baseline
Juvenile candidate   result        result       result
```
Provide both side views or an additional opposite-side detail capture to verify both eyes. Use isolated camera culling, neutral consistent local lighting, and labels identifying stage and version.

# Key Asset & Context
- Inspected active scene: Assets/Models/Koi/ProofOfConcept/EarlyStageAlbedoTransferPilot/Scenes/Pilot_MagnifiedReview.unity, clean. Kohaku_Fry_Pilot and Kohaku_Juvenile_Pilot still use their original pilot derivative albedos.
- Approved read-only Fry texture: Textures/Kohaku_Fry_RedPattern_Candidate_v03.png under the pilot folder; exists and remains unassigned.
- Existing editor references: KohakuFryPatternTrial.cs extracts only red chroma into a 192x128 anatomical chart using surface peduncle/widest-shoulder/nose landmarks; KohakuFrySeamCleanup.cs extends UV gutters by 12 pixels while preserving every covered texel. Preserve these scripts and prior candidates unchanged; do not run their hardcoded Fry bake methods.
- Adult source: KohakuTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_BaseColor.png, an adult-UV-compatible derivative with original Meshy artwork as upstream authority. Not an original export or packed metallic/smoothness source.
- Juvenile source: JuvenileRiggingPrototype/Prefabs/Juvenile_RiggingPrototype.prefab and Meshes/Juvenile_RiggedMesh.asset. Original albedo resolves to Assets/Models/Koi/New Juvenile/new_juvenile_texture.png; existing normal and metallic/smoothness derivative references stay unchanged.
- Create a new pilot-only material copy, Materials/Kohaku_Fry_v03_PilotOnly.mat, preserving all shader/non-albedo settings. Assign it only to Kohaku_Fry_Pilot in Pilot_MagnifiedReview.unity. Do not mutate shared Kohaku_Fry_Pilot.mat: other scenes may reference it.
- New independent pilot-only editor helper: Editor/KohakuJuvenileRedPatternTrial.cs (or a unique name if occupied); no edits to existing builders, playback or systems.
- Exactly one new appearance texture: Textures/Kohaku_Juvenile_RedPattern_Candidate_v01.png, using a unique next version if that path exists. Never overwrite a candidate.
- Reports: versioned validation/preservation manifest and side/top/three-quarter comparison images. Diagnostic captures are not additional appearance textures. Candidate Juvenile material is transient and not assigned to the saved pilot.

# Implementation Steps
1. Protect and recheck state. Dependencies: approval of this revision.
   - Inspect source/output dependencies and reverse references using available read-only AssetDatabase queries; record hashes for sources, previous candidates/materials/scripts and metadata. Preserve current scene setup and unsaved work.
   - Read complete Fry pattern and seam helpers; verify candidate output collision and material non-albedo configuration.
2. Assign approved Fry in the isolated scene. Dependencies: Step 1.
   - Create a unique pilot-only copy of the current Fry pilot material. Replace only its albedo slot with approved Fry v03, synchronizing a legacy albedo slot only if actually used by its shader.
   - Assign only Kohaku_Fry_Pilot; preserve positions, camera, all controls, Adult/Tancho/other-stage renderers and animation configuration. Save only Pilot_MagnifiedReview.unity. Existing shared/source materials stay byte-identical.
3. Adapt red-pattern mapping to Juvenile. Dependencies: Step 1.
   - Reuse the accepted adult red chroma extraction and chart/filter parameters, but measure Juvenile surface landmarks and stage-space transforms afresh. Do not copy Fry coordinates or broad face thresholds blindly.
   - Use pristine new_juvenile_texture.png as baseline. Explicitly protect Juvenile eyes, eyelids, lips, lateral face, fins and original texture detail; separate dorsal head markings from protected facial artwork.
   - Audit classification, anatomical chart continuity, interpolation fraction, finite values and overlapping UV ownership/marking agreement before saving. Retain all rig/mesh/UV/normal/roughness data. Do not return to full-albedo radial projection.
4. Produce one candidate with gutter cleanup. Dependencies: Step 3 validation.
   - Apply red chroma while retaining stage luminance/detail; use accepted smoothing with no unapproved pattern redesign. Apply 12-pixel ring-wise padding exclusively outside valid UV interiors, with no changes to protected surface texels.
   - Complete transfer and padding in memory, then save one versioned 2048-square candidate. No intermediate appearance texture, original overwrite, shared material edit or saved Juvenile assignment.
   - Record sampled/interpolated chart cells, conflicts, protected-pixel equality, gutter edits and deterministic configuration. If protection or mapping checks fail, stop and report rather than create a misleading accepted result.
5. Capture and inspect. Dependencies: Steps 2 and 4.
   - Use transient visual-only reference hierarchies and material copies, isolated camera culling and local lighting. Capture Adult/original Juvenile/candidate side/top/three-quarter comparisons and opposite-eye view as needed. Restore editor state; no other scene changes.
   - Review cohesive pattern shapes, seams, edge quality, stretching, magenta gaps, both eyes, lips and fins. Existing pilot swim components remain unchanged; inspect representative sampled swim poses if available and clearly distinguish static sampling from continuous runtime verification.
6. Report and stop. Dependencies: Step 5.
   - Verify Fry assignment is confined to the isolated pilot and Juvenile remains unassigned. Check protected file hashes, shader/non-albedo settings and scene diffs; enumerate the only intended modified scene plus new helper/material/candidate/report files.
   - Present captures and exact limitations; request Juvenile visual approval. No Step 3C, production integration, further varieties or additional candidate iterations without authorization.

# Verification & Testing
- Fry source v03, prior candidates and original shared materials unchanged; only the new material copy and isolated scene reference change.
- Juvenile geometry, UVs, rigs, animations and source maps unchanged. Original two eyes, facial details and fins preserved by protected texel comparisons and multi-angle review.
- Monotonic finite surface landmarks; periodic chart continuity; report interpolation explicitly rather than calling inferred cells directly sampled. No incompatible protected/shared UV conflicts.
- Gutter cleanup changes zero covered texels relative to the pre-padding candidate. Check bilinear/mip seam behavior and shape/placement consistency.
- Exactly one new Juvenile appearance candidate; no saved assignment until visual approval. Captures free of background scene contamination.
- No new compile errors; distinguish existing rig limitations and tests not run from new texture issues. Static views do not certify animation.
- Final stop before Step 3C.

# Revision — Kohaku Fry v03 and Juvenile v01 Localized Albedo Refinement
This revision supersedes the previous candidate-only stop scope solely for the two corrections below, after implementation approval. It does not authorize Step 3C or assigning candidates to existing saved scenes/materials.

# Project Overview
- Game Title: KoiPond.
- High-Level Concept: Refine two existing Kohaku candidates locally so adult-consistent red wraps across the upper body, and Juvenile red patches no longer inherit unwanted dark Meshy markings. Preserve all other artwork and anatomy.
- Players: Isolated asset review; no gameplay changes.
- Inspiration / Reference Games: Existing adult Kohaku reference, Fry v03 and Juvenile v01.
- Tone / Art Direction: Existing stylized koi with natural surface detail; no pattern redesign.
- Target Platform: StandaloneWindows64, Unity 6000.6.6f1.
- Screen Orientation / Resolution: Existing desktop settings unchanged; diagnostic views at least 768 pixels per panel, with close-ups.
- Render Pipeline: Existing URP PC_RPAsset unchanged.

# Game Mechanics
## Core Gameplay Loop
Compare adult and current candidates → identify exact defective surface regions → correct only those albedo regions → validate preservation → present versioned comparisons → stop for user visual approval.
## Controls and Input Methods
No controls, gameplay, genetics, growth or input changes. Retain New Input System and all existing playback tooling.

# UI
No interface assets changed. Labeled contact sheets for each stage:
```
                         Left side   Top   Right side   Dorsal close-up
Adult reference          reference   ref   reference    reference
Existing v03 / v01       before      before before      before
Candidate v04 / v02      after       after after        after
```
Include Juvenile spot close-ups and corresponding albedo-only comparisons. Use fixed per-stage framing between before/after, consistent neutral lighting, and explicit magnification/version labels.

# Key Asset & Context
- Root: Assets/Models/Koi/ProofOfConcept/EarlyStageAlbedoTransferPilot/ (all new implementation outputs confined here).
- Read-only inputs: Textures/Kohaku_Fry_RedPattern_Candidate_v03.png and Textures/Kohaku_Juvenile_RedPattern_Candidate_v01.png; original stage maps, adult Kohaku albedo/reference material, all existing candidates, scripts, materials, scenes, rigs, meshes and clips.
- Inspected KohakuFrySeamCleanup.cs only dilates 12-pixel unused UV gutters and explicitly changes zero covered texels. It cannot repair a white strip painted on occupied body surface.
- Inspected KohakuFryPatternTrial.cs and KohakuJuvenileRedPatternTrial.cs protect all non-body classified regions and apply red chroma using clamped original red-channel brightness. Broad classification near the dorsal attachment and chart interpolation must be investigated; they are possible causes, not a confirmed diagnosis. Original brightness retention can preserve dark artwork inside Juvenile red patches.
- Adult source is the existing adult-compatible KohakuTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_BaseColor.png derivative. Original Meshy exports remain upstream appearance authority; AI-generated _MetallicSmoothness maps are not original Meshy sources.
- Add independent Editor/KohakuLocalizedAlbedoRefinement.cs and Editor/KohakuLocalizedAlbedoRefinementValidation.cs; do not modify or rerun old hardcoded candidate writers.
- Exactly two appearance outputs: Textures/Kohaku_Fry_RedPattern_Candidate_v04.png and Textures/Kohaku_Juvenile_RedPattern_Candidate_v02.png. If occupied, refuse overwrite and use the next available version, recording it explicitly.
- Profiles/LocalizedKohakuRefinement/: versioned body-space defect masks, protected masks, thresholds and source correspondence metadata. Reports/LocalizedKohakuRefinement/: preservation manifest, mask/change diagnostics, comparisons and results. Diagnostic mask PNGs are reports, not extra appearance variants.
- Optional two new candidate-only material copies preserve every non-albedo property and reference; use transient review hierarchies. No existing saved scene/material assignment changes.
- Selected narrow approach: deterministic geometry/UV-aware masked editing, using neighboring valid red and adult marking evidence. Alternative masked AI inpainting risks moving boundaries and inventing detail, so is not proposed. No model regeneration, global recoloring, broad smoothing or UV/rig edits.

# Implementation Steps
1. **Protect and diagnose actual surface defects.** Dependencies: approval of this revision.
   - Recheck dependencies/dependents for inputs, references and any proposed outputs using available dependency queries and AssetDatabase fallback; inspect actual renderer/material slots, UV transforms and scene wiring before attributing defects to albedo.
   - Record loaded scenes and dirty state, protected file/.meta hashes and import settings. Never save or discard existing scene changes. Inventory output collisions.
   - Compare adult, Fry v03 and Juvenile v01 from both sides/top in albedo-only and Lit views. Rasterize UV triangle ownership and body-space coordinates. Determine whether the strip occupies body texels, protected fin texels, UV gutters, overlapping UVs or is a lighting/normal artifact.
   - Identify intentional white pattern gaps separately from the unwanted strip. Record exact body-only correction domains and protections for eyes, face, lips, fin surfaces, and all genuine pattern boundaries. If fixing it requires fin recoloring, incompatible shared-UV edits or non-albedo changes, stop and disclose the conflict rather than weaken protection.
2. **Repair dorsal red continuity locally.** Dependencies: Step 1 verified diagnosis and masks.
   - Start from decoded current candidate pixels at existing resolution, never a fresh global transfer. Map only confirmed defective dorsal-base body texels to normalized anatomical surface coordinates.
   - Extend matching existing red patch color across the narrow defective body strip only where adult evidence and neighboring patch ownership support continuity. Use surface-space continuity across UV islands; do not dilate all red or bridge intentional white gaps.
   - Preserve each existing patch outline outside the explicit repair mask, fin artwork, eyes and all other covered pixels. Derive red color and detail from valid local patch samples; do not transfer adult eye artwork or synthesize a new pattern.
   - Audit all UV owners; reject incompatible protected/body overlap rather than arbitrary first-owner writes. Store before/after mask diagnostics and source attribution.
3. **Remove Juvenile unwanted dark spot pigmentation within red patches.** Dependencies: Steps 1–2.
   - Isolate low-frequency gray/dark Meshy marking contamination using original-map comparison, connected spot shapes and local red-patch luminance context, not a global darkness threshold. Protect genuine scale creases, fine texture, natural shading and all red/white edges.
   - Replace only identified unwanted pigmentation within existing red interiors using robust nearby clean red baseline color/luminance; retain bounded high-frequency detail residuals. Do not preserve the unwanted spot simply because it is present in the original brightness channel, and do not flatten every red patch.
   - Record spot masks, changed texel counts and detail-preservation checks. Ambiguous spot-versus-surface-detail regions remain flagged for review, not silently erased.
4. **Save exactly two versioned candidates and isolated previews.** Dependencies: Steps 2–3 preservation/conflict checks passed.
   - Finish each refinement in memory. Refresh only necessary UV gutters outside covered/protected surfaces with finite nearest-island padding; no padding across intentional boundaries or unresolved conflicts.
   - Save the two new candidates without overwriting input assets. Match relevant resolution/color-space/filtering/import settings on new assets only; do not change original importers. Keep alpha unchanged on covered pixels.
   - Use new or transient candidate materials with only relevant base-albedo references changed. Do not assign to Pilot_MagnifiedReview.unity or any existing saved material/scene.
5. **Capture and verify.** Dependencies: Step 4.
   - Generate labeled Adult/current/new left/right/top comparison sheets and dorsal/spot close-ups, with identical before/after framing and neutral lighting. Include albedo-only evidence to distinguish texture from shading artifacts.
   - Inspect rest and representative unchanged sampled Swim poses for seam continuity, protected eye/fin detail and texture stretching; distinguish existing rig limitations. Restore all temporary resources and original scene state with exception-safe cleanup.
   - Run determinism, preservation and conflict tests below. Do not claim continuous runtime verification from static samples.
6. **Report and stop for visual approval.** Dependencies: Step 5.
   - Present exact new files and comparisons, describe masked edits and remaining limitations, and mark candidates pending user approval. Verify protected hashes/importers/scene assignments unchanged.
   - Hard stop before Step 3C. No catalog hookup, genetics integration, production assignment, additional varieties, rig edits or automatic candidate promotion. Implementation approval is not visual acceptance of the results.

# Verification & Testing
- Protected existing file/.meta hashes and import settings unchanged, including prior candidates, source textures, materials, scripts, meshes, rigs, clips and saved scenes. All writes restricted to new isolated outputs and this plan.
- Covered pixels outside the union of explicit approved correction masks match current candidate byte-for-byte; protected eyes, fins, facial artwork, intentional white gaps and alpha have zero unintended changes.
- Audit UV overlap across every triangle owner; zero unresolved incompatible protected/body edits. Record uncovered atlas pixels separately from visible-surface defects.
- Dorsal red continuity matches adult-supported patch segments from top and both sides, without bridging authentic white boundaries, bleeding onto fins or introducing seam halos. Check bilinear and mip behavior in new candidate imports.
- Juvenile unwanted dark pigmentation removed only inside targeted red interiors; compare close-ups for retained scale texture and natural detail. Numeric luminance/detail checks supplement, never replace, human visual review.
- Deterministic rerun from unchanged inputs/configuration produces identical decoded candidate pixels; no cumulative smoothing or overwrite. Padding changes zero occupied surface texels relative to pre-padding outputs.
- Candidate material shader/normal/roughness/metallic/scalars identical to corresponding baseline except base-albedo slot(s). No global rendering/post-processing changes.
- Console has no new compile/test errors. Record sampled checks, unavailable runtime/visual tests and remaining uncertainty honestly.
- Final acceptance gate: user reviews side/top comparisons before any Step 3C work or saved assignment of new candidates.

# Revision — Refine Kohaku Fry v04 and Juvenile v02: Fill, Shading and Fin Boundaries
This is the current revision for this task; earlier candidate builders and outputs remain read-only. Implement only after approval. Stop for visual approval, with no Step 3C or existing saved scene assignment.
User review direction: keep diagnostics and reports concise, reuse existing compatible read-only/capture utilities, and avoid redundant audits. Perform one fresh protection/dependency baseline and one final preservation check; retain required map attribution, UV-owner checks and in-memory repeatability tests. Create versioned candidates only; show close-ups of red coverage, residual outlines and dorsal-fin edges.

# Project Overview
- Game Title: KoiPond.
- High-Level Concept: Correct patchy red interiors, residual unwanted spot contours and dorsal-edge bleeding on two existing candidates. Diagnose albedo, normal-map and UV contributions first, then produce narrowly masked, versioned review assets without changing anatomy or approved pattern shapes.
- Players: Isolated asset review only; existing gameplay unchanged.
- Inspiration / Reference Games: Adult Kohaku reference and existing stage artwork; no external games requested.
- Tone / Art Direction: Preserve natural stylized surface detail, approved red/white pattern shapes, eyes and original fins.
- Target Platform: StandaloneWindows64, Unity 6000.6.6f1.
- Screen Orientation / Resolution: Existing desktop settings unchanged. At least 1024 pixels per close-up panel, with scale and view labels.
- Render Pipeline: Existing URP PC_RPAsset, unchanged.

# Game Mechanics
## Core Gameplay Loop
Diagnose current candidates → attribute each artifact to its actual source → refine only verified masks → compare against unchanged controls → verify preservation and repeatability → stop for user approval. No gameplay/genetics/growth changes.
## Controls and Input Methods
Editor-only diagnostics and capture commands. No new input bindings; New Input System remains unchanged.

# UI
No UI assets. Produce genuinely close-framed comparisons rather than just elevated full-fish views:
```
                       Side close-up   Opposite side   Top/base close-up
Current Fry v04        before          before          before
New Fry v05            candidate       candidate       candidate
Current Juvenile v02   before          before          before
New Juvenile v03       candidate       candidate       candidate
```
Separate sheets isolate albedo-only, Lit without normal, Lit with normal and full material. Label stage/version, viewing direction, lighting, normal state, crop and magnification in the image or companion layout key. Include adult fin-base reference and source-fin close-ups.

# Key Asset & Context
- Root for all new implementation outputs: Assets/Models/Koi/ProofOfConcept/EarlyStageAlbedoTransferPilot/.
- Read-only current candidates: Textures/Kohaku_Fry_RedPattern_Candidate_v04.png and Textures/Kohaku_Juvenile_RedPattern_Candidate_v02.png. All earlier candidates, helpers, reports, source maps, materials, meshes, rigs, UVs, clips and saved scenes are protected.
- Existing Editor/KohakuLocalizedAlbedoRefinement.cs uses a roof estimate, hard positional attachment gates and nearest vertex donor colors. It is not a verified fin-blade segmentation; it can create patchy/jagged boundaries. Do not rerun its hardcoded writes or weaken its gates globally.
- Its spot repair carries original high-frequency residuals; the later Juvenile correction was a separate editor command. Final v02 cannot be reproduced solely by calling the helper's Bake method. New tooling must take actual v04/v02 bytes as explicit inputs and be fully reproducible.
- Inspected Fry material: KoiPrototype/FryOriginalMaps; normal is FryRiggingPrototype/Materials/OriginalMapImportCopies/Fry_normal.png, NormalMap, linear, uncompressed, mipmapped, scale 1; roughness is a separate linear uncompressed map. Texture transforms are identity.
- Inspected Juvenile material: URP/Lit; normal is JuvenileRiggingPrototype/Materials/NewMeshy/Juvenile_NewMeshy_Normal.png, NormalMap, linear, uncompressed, mipmapped, scale 1; packed metallic/smoothness map is a DERIVED asset. Texture transforms are identity. Correct importer categories alone do not prove artifact-free normal contents.
- Original Meshy maps remain upstream authority; packed _MetallicSmoothness provenance must not be conflated with original maps.
- Current Pilot_MagnifiedReview.unity is clean. Kohaku Fry still uses the v03 pilot material; Juvenile uses its older pilot material, not v02. Diagnosis must explicitly bind current candidates on transient review instances, not mistake saved-scene appearances for requested inputs.
- Add independent Editor/KohakuSurfaceArtifactRefinement.cs and Editor/KohakuSurfaceArtifactValidation.cs. No edits to old helpers, shaders, package source or runtime systems.
- Exactly two new appearance albedos: Textures/Kohaku_Fry_RedPattern_Candidate_v05.png and Textures/Kohaku_Juvenile_RedPattern_Candidate_v03.png. Check collisions and choose the next unused version only if necessary; never overwrite.
- User-selected approach: allow localized versioned map copies if diagnosis proves a normal-map artifact. Alternative albedo-only has lower risk but cannot remove normal-driven contours. No AI regeneration or blanket smoothing.
- Optional Textures/Kohaku_[stage]_Normal_Candidate_[version].png only for a proven artifact; no automatic new normals for both stages. New candidate-only material copies bind these derivatives while retaining all unaffected settings/maps. Other implicated map types require explicit follow-up authorization rather than silently adding roughness/metallic repairs.
- Profiles/SurfaceArtifactRefinement/: versioned geometric segmentation, defect masks, UV ownership, source references, thresholds and configuration. Reports/SurfaceArtifactRefinement/: protection manifest, map attribution, diagnostic comparisons, test results and exact output inventory. Use a unique versioned report folder if occupied.

# Implementation Steps
1. **Re-establish sources, dependencies and protection.** Dependencies: approval of this revision.
   - Inspect dependencies and dependents of every input and planned output using dedicated tooling where available, otherwise AssetDatabase.GetDependencies and reverse references; source-search code consumers as needed.
   - Record current candidate bytes/importers, original map identity, shader slots and transforms, scene setup/dirty state. Hash protected assets and metadata before any edits. Never save/discard existing scene work.
   - Explicitly bind v04/v02 to fresh isolated review hierarchies using the correct unchanged stage mesh/material. Preserve rig/mesh/UV/normal/tangent/weight data.
2. **Complete map and UV attribution BEFORE editing appearance.** Dependencies: Step 1.
   - Compare albedo-only, Lit with normal disabled, Lit with normal enabled, and full-map views under identical neutral lighting. Rotate lighting to distinguish baked pigmentation, normal-driven outlines and legitimate geometry shading. Introduce roughness/metallic separately as diagnosis only.
   - Inspect raw source normal encoding, imported decoding/Y convention and candidate-scale mip/compression behavior without editing source importers. Correlate unwanted contour footprints across albedo, normals and UV-surface coordinates; do not treat every scale edge as a defect.
   - Rasterize all UV owners at texel centers and filtering footprints; inspect coincident seams, island boundaries, overlaps and subpixel triangles. Distinguish occupied blade texels from gutters and body skin.
   - Replace heuristic fin/body masks with reviewed triangle connectivity, local geometry thickness/orientation and anatomical root landmarks. Preserve distinct body, attachment and blade ownership. If shared UV owners prevent independent correction, report a blocker; no UV or mesh edits.
   - Save attribution findings and reviewed masks. If shading is geometry-driven or another unauthorized map is responsible, report the limitation and stop that correction rather than paint compensating shadows.
3. **Repair albedo fills and restore fin artwork.** Dependencies: Step 2 completed.
   - Start from actual v04/v02 pixels. Freeze approved red/white boundary contours, intentional white gaps, eye/face/lip artwork and unaffected fins. Limit fill/shading edits to explicitly identified red-interior defects; no global brightness normalization or patch redesign.
   - Correct low-frequency fill discontinuities and residual unwanted pigmentation using clean same-patch, same-surface neighbors. Use surface-aware smooth reconstruction with explicit seam consistency; retain verified fine scale detail, not the unwanted spot outline residual itself.
   - Restore proven red-contaminated dorsal-blade/edge texels from the matching pristine stage albedo. This is restoration, not a new fin pattern. Preserve red continuously on actual body skin to the attachment; retain the naturally pale blade/top-view line.
   - Feather only within permissible repair interiors. Do not blur across red/white shapes or blade/body ownership. Use owner-aware padding outside surfaces and check bilinear/mip leakage at both sides of the fin base.
4. **Correct proven normal artifacts in copies only, if needed.** Dependencies: Step 2 attribution; Step 3 masks.
   - If normal-map spot contours or fill seams are proven, create a versioned derivative from the correctly decoded original normal. Restrict repair to those defect footprints and blend with compatible clean tangent-space neighborhood normals; renormalize vectors and encode correctly.
   - Preserve fine natural scales, genuine crease shading, eye/fin normals and all texels outside the documented map-specific mask. Do not flatten normal strength globally or erase natural geometry shadows.
   - Use new NormalMap import settings and candidate material references only; leave original normal files/importers/materials unchanged. If no normal artifact is proven, create no normal candidate.
5. **Build and verify new review assets reproducibly.** Dependencies: Steps 3–4.
   - Bake each new albedo and any justified normal derivative once from unchanged inputs/configuration. Audit all protected/shared owners before saving. No overwrite of current candidates or historical evidence.
   - Create new candidate-only materials preserving shader/non-albedo settings except explicitly approved normal reference changes. Do not assign to any existing saved scene/catalog.
   - Run the same full pipeline twice in memory and compare decoded pixels/configuration hashes, including every corrective pass and padding. Persist masks and parameters so final candidates reproduce without ad hoc editor commands.
6. **Capture close-ups and test surface boundaries.** Dependencies: Step 5.
   - Capture current/new close-up side, opposite side and top views focused on red interiors, former spot outlines and dorsal attachment/edges; include consistent full-fish pattern controls and map-isolation sheets.
   - Check UV seam, padding and fin-edge behavior at mip levels/realistic distances as well as magnification. Inspect representative unchanged Swim poses; do not repair pre-existing deformation defects or equate static samples with continuous runtime testing.
   - Use exception-safe temporary resource disposal and restore scene/editor state. Any unexecuted tests must be explicitly marked NOT RUN.
7. **Report and stop.** Dependencies: Step 6.
   - Report actual artifacts fixed, remaining issues, map-specific masks/counts, visual attribution, determinism, preservation and exact created files/.meta. Record which original maps versus derivatives were used.
   - Present close-up comparisons for approval; no automated promotion or saved-scene assignment. Stop before Step 3C, other varieties, production integration or further candidate iterations outside this request.

# Verification & Testing
- Protected current/earlier assets and metadata unchanged; no shared material/importer changes. Existing scene assignments and scene dirty state preserved.
- Geometry, indices, all UVs, normals/tangents, skinning, rig transforms, blendshapes and animations unchanged. Normal texture derivatives are distinguished from mesh normals.
- Approved pattern contours and intentional white gaps preserved. Covered pixels outside explicit repair masks match current input bytes; restored fin artwork matches documented original stage sampling. Eyes, lips and unaffected fins unchanged.
- Zero unresolved incompatible UV owner edits; audit filtering footprints, visible seams and gutters separately. Dorsal blade has no unintended red contamination in side/top close-ups or mip views, while body red remains continuous to its actual base.
- No patchy fill or unwanted dark spot outline in albedo-only review; no proven normal-driven unwanted outlines in normal-enabled review. Natural scale relief and expected directional shading remain visible. Do not claim perfectly uniform shading on naturally curved geometry.
- Normal repairs confined to verified masks; decoded finite vectors, correct tangent-space encoding/import, normalized reconstructed vectors, seamless transition to untouched data. Verify retained fine detail rather than relying only on magnitude checks.
- Fully deterministic final bake: repeated decoded albedo/normal candidates and masks identical. No correction hidden solely in a one-off command; all inputs/parameters recorded.
- Candidate materials retain unaffected map references, UV transforms, shader/scalars and rendering settings. No global lighting/pipeline or package edits to conceal defects.
- Console has no new compile/binding/test errors; record baseline warnings and unavailable tests. Static Swim captures are not continuous animation certification.
- Final gate: user visual approval of new side/top close-ups. No Step 3C or production readiness claim.

