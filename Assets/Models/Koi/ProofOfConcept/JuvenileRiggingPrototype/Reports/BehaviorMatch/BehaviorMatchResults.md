# Juvenile Rig — Adult Behavior Match: Refinement Results

## Verdict
**Juvenile prototype refined and structurally verified, but NOT fully accepted or production-ready.** Adult-compatible binding/hierarchy, smaller display size, reduced sampled wiggle and improved localized skinning are established. Original Meshy appearance is **not restored**, convincing adult-like mouth opening is **not achieved**, and continuous runtime playback is **not verified**. These are explicit remaining limitations, not claimed passes.

All implementation changes are confined to `Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/`. The approved existing plan was revised separately. No original/production assets, source import settings, package/project settings or fry rigging were changed.

## Refinement
- Retained the adult ten-bone paths/order and `FishMesh/blendShape.MouthOpen` binding. Original adult mesh, prefab, controller, material, clips and gameplay script are read-only.
- Retained the existing adult-derived surface weight transfer as the foundation. Replaced cumulative global smoothing with deterministic corrections from `Baseline/Juvenile_BaselineMesh.asset`: protected head/eyes, localized jaw, same-side fin masks and twelve bounded pectoral-attachment smoothing passes. Body/tail transfer remains intact.
- Refitted Head and Mouth pivots and both pectoral attachments; recomputed juvenile bindposes. Final full-import landmark coordinates: Head (0,-.035,.70), Mouth (0,-.14,.86), left/right pectoral (+/-.22,-.29,.46). Body/tail pivots retain their prior juvenile landmarks.
- Measured the actual central lip profile, including its indentation around y=-.14, rather than treating the furthest nose point as the lip. Final copy-only MouthOpen has 3,848 affected vertices, no lateral inflation, a localized upper/lower lip mask and bounded shape amplitudes: upper +.012 Y, lower -.030 Y and lower +.004 Z in full imported juvenile units before coordinate conversion. No geometry/cavity was added.
- Jaw skin influence is at most .30 on localized lower lip and does not leak into measured protected eye/cheek regions. Shape zero restores the original rest geometry.
- Juvenile outer prototype scale **0.6001769**, measured body length **0.3606059**, adult length **0.6010098**, final ratio **0.6000000**. Scaling is outside the keyed animation root; no mesh vertex scaling, source FBX importer edits or renderer-only shrinking. Prototype prefab and scene agree.
- Added synchronized adult/juvenile comparison playback with one manually evaluated PlayableGraph and shared clock. Every evaluated frame starts from cached rest position/rotation/scale and reset MouthOpen to avoid stale unkeyed channels and cumulative offsets.
- Source diagnostic mode retains exact source clips at speed 1.0. Adapted juvenile mode keeps timing/phase/shape curves unchanged and interpolates angular motion from rest: body/root/head .70, tail chain .85, fins .70; Eat skeletal/root translation amplitude .85. Mouth-bone rotation and shape weights remain source-driven. Outer display scaling already scales translational excursions; it is not applied twice.
- Optional `_adultSecondaryMotion` reproduces inspected neutral adult fin paddling (2.2 degrees) and spine secondary offsets (1.2 degrees with adult factors) on freshly evaluated poses for both targets. It defaults OFF for controlled source comparisons. It does not reproduce pond locomotion, steering, water constraints, controller state speeds, feeding/ripples or wandering. This option is compiled but not runtime-verified.

## Verified preservation / structure
- Fresh source/production/project/package protection baseline: **1,832 files and metadata checked, 0 changed/missing**.
- Previous protection manifest: **1,802 checked**; only changed entry is the approved `Assets/Plans/phase-4-juvenile-rigging-prototype.md`. Other **1,801 protected entries unchanged**.
- Original juvenile/source-textured FBX geometry attributes and all UV channels match. Prototype rest vertices, indices, normals, tangents, submesh count and all eight UV channels remain unchanged.
- Skin weights are finite, nonnegative, normalized within 1e-5, with valid indices 0–9; equal-position seam weights and shape deltas remain consistent.
- Juvenile rest-bake world error: **5.33e-8**, well below 1e-5 of body length.
- **0 unresolved bindings** for all tested targets/clips. No nonfinite sampled vertices; no juvenile sampled vertices outside its tested bounds. Protected eye/cheek mouth influence/delta leakage: **0 vertices** under the recorded z<.8 guard.
- Mouth weights 0/25/50/90/100/0 baked finite on both adult and juvenile. Source shape timing/ranges remain Swim peak ~29.9922, Idle 25, Eat ~89.8255.
- Final implementation edits passed tool compilation checks; final Console error check is recorded separately. Editor is in Edit Mode, isolated scene saved cleanly, no temporary diagnostic scene objects or pending test callbacks retained.

## Paired sampled animation comparison
**Evidence is Edit Mode sampling, not continuous runtime.** 34 independent samples per target/mode/clip: 33 evenly spaced including endpoints plus exact maximum MouthOpen key. Targets: saved juvenile baseline, final juvenile exact source, final juvenile adapted, adult exact source. Rest channels are reset between samples.

| Clip | Baseline juvenile max tested edge ratio | Final juvenile exact source | Final juvenile adapted | Adapted displacement / body length | Adult displacement / body length |
|---|---:|---:|---:|---:|---:|
| Swim | 5.1852 | 3.7708 | **2.8108** | .09612 | .09715 |
| Idle | 4.3807 | 3.0250 | **2.3536** | .07578 | .06391 |
| Eat | 4.3046 | 3.5482 | **3.1089** | .25010 | .34466 |

Normalized displacement includes authored root motion. It is not a direct head-jitter measure. Edge ratios exclude rest edges shorter than 1e-4 of body length. These are maxima, not mean distortion or acceptance thresholds; different adult topology produces much larger isolated short-edge ratios and is not directly comparable. Remaining juvenile >2 ratios are real localized warnings, not a clean deformation pass.

Visual review of paired Swim/Idle/Eat side/top/front/three-quarter contact sheets shows coherent body/tail silhouettes, noticeably smaller juvenile size and less exaggerated juvenile motion at sampled poses. Fins remain attached, but local attachment strain/folding is not fully eliminated. No claim of smooth temporal motion is made from stills.

| Region / requirement | Result |
|---|---|
| Hierarchy and animation bindings | PASS |
| 60% juvenile size | PASS |
| Reduced sampled body/tail/fin exaggeration | Improved, with warnings |
| Fin-root distortion | Improved, still not production-accepted |
| Adult-like natural mouth timing | Bindings/source timing preserved; runtime not verified |
| Adult-like convincing mouth aperture/interior | **FAIL visual acceptance** |
| Original Meshy markings and dark eyes | **FAIL appearance equivalence** |
| Continuous Swim/Idle/Eat playback/transitions | **NOT RUN successfully** |

### Culling limitation
Juvenile sampled bounds containment passes. Adult-reference checks report vertices outside the renderer's rest/local bounds when sampled; attempts to set expanded reference-only bounds were not retained by the renderer's automatic update behavior. Adult reference uses `updateWhenOffscreen=true` and AlwaysAnimate, but actual onscreen/offscreen culling remains unverified. These counts are diagnostic rest-bound warnings, not proof that the working production adult is visibly clipped. No adult mesh/importer/production culling settings were altered.

## Mouth visual limitation
Close-ups compare adult and juvenile rest versus actual Eat peak (~1.033333s), and isolated shape weights 50/100. The adult demonstrates a broad convincing upper/lower mouth opening. Juvenile shape deformation now follows the measured lip crease with less diffuse muzzle motion, but the result still reads as shallow lip/muzzle movement rather than the adult's visible open cavity. The pale mismatched appearance further obscures the mouth.

A narrow crease split initially increased Eat local edge strain (~5.10); final widened/bounded lip motion brought it back to ~3.11 adapted. This avoids exaggerating a visually unsuccessful opening. The original lip surface/interior cannot be treated as a finished eating-mouth asset. No tears, painted cavity, generated geometry or unauthorized topology changes were introduced. A genuine aperture/interior may require separately approved mesh/topology work after the correct original export is supplied.

## Original Meshy appearance audit — not repainted
- Both original juvenile FBXs have matching geometry/UVs and one submesh. Their embedded URP/Lit materials have empty map assignments, so the imported material state cannot prove screenshot-level source pairing.
- Prototype uses original `juvenile texture.png` directly with white tint, identity UV offset/scale and sRGB albedo import (2048 resolution). No adult textures/UVs, painting, recoloring, texture generation or screenshot color baking.
- Raw original PNG: **2048x2048**, ~1.7367% pixels darker than .2, only **0.005388% red-dominant pixels**. This is inconsistent with the Meshy screenshots' readily visible red markings.
- Raw UV-to-albedo samples in a broad measured eye-candidate region: **46/6,667 dark** with identity UVs, versus **11/6,667** with flipped V. These are candidate-region statistics, not exact pupil segmentation. Neither identity nor V-flipped unlit diagnostic reproduced dark eyes/markings. Flipped V was rejected and not adopted.
- Unlit albedo diagnostic removes lighting, normal and metallic contributions; it still displays the pale patchy source pairing. Therefore lighting/normal/PBR changes alone cannot restore the screenshot markings. No cosmetic tint was applied to conceal the mismatch.
- Existing normal import copy matches original PNG bytes exactly.
- Existing prototype packed metallic/smoothness derivative was verified pixel-for-pixel: **R=original metallic.R, A=255-original roughness.R, 0 mismatched pixels**. It remains a DERIVED map, not an original Meshy texture. No new packed texture was generated in this revision. Other project `_MetallicSmoothness` maps have different provenance and were not used.
- Normal Y convention and exact Meshy viewer lighting remain unverified, but neither can supply missing albedo markings/pupils.

**Needed to finish appearance:** the exact original Meshy juvenile export (FBX/GLB plus matching base-color/normal/metallic/roughness maps) that produced `juvenile_side/top/angle` screenshots. Available evidence does not establish that the supplied albedo belongs to that appearance/export. Source/production assets were preserved rather than repainted.

## Continuous runtime verification — blocked
Two self-contained multi-frame test provisioning approaches compiled but remained `WaitingForCompile / NOT_READY / isPlaying=False`. Requested domain reload and an explicitly armed delayed entry never ran through this editor bridge. Domain reload was enabled; no runtime deformation assertion executed. Stopped instead of further polling/retries or project-setting changes.

The pending callback was removed, SessionState cleared and temporary `Editor/PlayModeTestRunner.cs` plus metadata deleted. **No RuntimeResults.json or Runtime_*.png exists and no runtime success is claimed.** Three continuous Swim loops, three Idle loops, full Eat, Eat-to-Idle/Swim returns, repeated Eat, pause/resume, disable/re-enable and actual culling/rendering remain unverified. Final mouth corrections are also unverified in runtime.

### Manual isolated playback
Open `Scenes/Juvenile_RiggingPrototype_Test.unity` and press Play. Juvenile and adult should animate from a shared clip clock. The saved default comparison sequence repeats: Swim three loops → Idle three loops → Eat once → Idle .5s → Swim one loop → repeat.
Select `Juvenile_Animated_Test` to access diagnostic Inspector controls:
- Mode: `ComparisonSequence`, `ContinuousSwim`, `ContinuousIdle`, `RepeatEat`.
- Paused: stop/resume the comparison clock.
- Adapt Juvenile: exact source versus proportionate reduced-amplitude juvenile.
- Adult Secondary Motion: optional neutral production-like secondary motion; OFF for source-only comparisons.
- Context menu `Reset Comparison` resets the sequence/rest channels.
The static unrigged control remains in the scene but is disabled by default to keep the animated pair clear; enable it for appearance/rest comparison. Its baseline material is separate from the live prototype material.

## Retained outputs / modified files
Modified existing files under the prototype:
- `Editor/JuvenileRiggingPrototypeBuilder.cs`
- `Editor/JuvenileRiggingPrototypeRefinement.cs`
- `Editor/JuvenileRiggingPrototypeValidation.cs` (scene-builder compatibility; legacy single-target validation remains historical)
- `Runtime/JuvenilePrototypePlayback.cs`
- `Meshes/Juvenile_RiggedMesh.asset`
- `Prefabs/Juvenile_RiggingPrototype.prefab`
- `Scenes/Juvenile_RiggingPrototype_Test.unity`

New:
- `Editor/JuvenileBehaviorMatchValidation.cs` — current paired structural/sample/capture checks.
- `Reports/BehaviorMatch/` — protection manifests/results, immutable mesh/material/code baselines, UV diagnostic materials, appearance/PBR/refinement/scale notes, structural/sample JSON, runtime-blocked note, labels, this report and file inventory.
- Six final 4096x2048 paired contact sheets: Swim/Idle/Eat × Source/Adapted. Each 1024px group is adult LEFT / juvenile RIGHT with equal orthographic scale .37; groups side/top/front/isometric, rows 0/25/50/75% of clip.
- Two final 2048x1024 mouth sheets: Adult/Juvenile, columns rest/shape50/shape100/actual Eat peak; rows front/isometric. Detail magnification differs (.065 adult/.05 juvenile); these are not size-comparison evidence.
- Associated Unity-generated folder/asset metadata. Full retained inventory is `CreatedFiles.txt`.

Unchanged: original source/production assets and metadata, original textures/importers, the live prototype material/normal copy/packed derivative, earlier Phase4 results/captures. Prior evidence is not relabeled as new. Temporary display quads/pose meshes/materials were removed; temporary runtime runner was removed. No animation clip copies, generated models, new textures, production gameplay changes or fry rigging.

## Stop boundary
Further acceptance requires correct Meshy appearance pairing, mouth aperture/interior resolution and actual continuous playback review. This remains an isolated juvenile review prototype. **Do not proceed to fry rigging or production integration.**
