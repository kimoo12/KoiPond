# Phase 4 — Fry Rigging Prototype Results

## Verdict
**Isolated prototype implemented; structurally valid, but NOT fully visually accepted or production-ready.** Original fry geometry/UVs and Meshy texture bytes are preserved. The ten-bone adult-compatible rig, selected half-juvenile scale and synchronized three-stage scene exist. Sampled Swim/Idle/Eat bindings, finite skinning and bounded geometry pass. Dark pupils are visible in reviewed views. Convincing eating-mouth aperture fails visual acceptance; local skin strain remains high; actual continuous playback could not run through the editor connection.

No further rig-method fallback, topology repair, texture repainting/generation or production integration was performed. Stop for review before additional changes.

## Sources and isolation
- Meshy fry FBX: `Assets/Models/Koi/Materials/fry texture/Meshy_AI_stylized_cute_fish_20261024_image-to-3d-texture.fbx`.
- Alternate `Assets/Models/Koi/Meshes/Koi_Stage2_Fry.fbx` imported geometry/attributes match as established in planning; names are unchanged.
- Adult: original `Assets/Prefabs/Koi_Stage3_Adult.prefab`, `KoiFish_RiggedMesh.asset`, original material and Koi_Swim/Idle/Eat clips.
- Juvenile: completed JuvenileRiggingPrototype prefab/mesh/material, including verified original `New Juvenile/new_juvenile_texture.png` material assignment. Juvenile's prior mouth/fin/runtime limitations are not treated as successful reference results.
- Every implementation asset is under `Assets/Models/Koi/ProofOfConcept/FryRiggingPrototype/`; existing source/adult/juvenile assets are read-only. New scene targets reconstruct source armatures/renderers without pond behavior or colliders. One new harness writes all three test rigs; no completed playback/controller changes.

## Anatomy and rig
Original fry has 81,417 vertices, 149,086 triangles, one submesh, UV0 only, no original skinning or blendshape. The prototype keeps exact rest vertices, normals, tangents, triangle indices and all eight UV-channel arrays. No retopology, welding, geometry smoothing, added cavity or UV/texture transfer.

The source has import X rotation 270 degrees and scale 100; these are retained through renderer transforms. Fry width/length=.5066 and height/length=.6350 (fin-inclusive full bounds), appreciably broader/taller than juvenile. Independently fitted full-import landmarks are in Builder.Joints and AnatomyBaseline.txt; roots, head/mouth, side fins and tail chain are not a uniform juvenile skeleton shrink.

Ten original paths/order are preserved: Root, Spine_Front, Head, Mouth, PectoralFin_L/R, Spine_Back, Spine_Tail1/2, TailFin. Bindposes are recomputed from fry rest transforms. The source armature offset keeps animated Root position zero-based.

### Selected weight transfer and two bounded corrections
Initial weights came from accelerated region-constrained closest adult triangles with barycentric interpolation and orientation gating. Body/tail, each fin and mouth correspondence are separate; no accepted opposing-normal matches or silent fallback assignments. Query normals use an approximate inverse-transpose of local anatomical scaling; exact surface correspondence remains imperfect. Metrics are in RigAudit.txt.

The initial sampled edge maxima were Swim ~78.36, Idle ~62.53, Eat ~102.09. A diagnostic exposed nearby body vertices receiving drastically different adjacent tail weights. Correction 1 regenerates the selected transfer deterministically and relaxes only detected local discontinuities plus four adjacency rings, for 24 bounded neighbor passes; z>=.57 head/eye and mouth areas are excluded. Coincident seams are grouped without welding.

A second diagnostic exposed a hard pectoral/body region boundary: near-identical points around (x=.345,y=-.375,z=.616) alternated between pure fin and body/head weights. Correction 2 separately transfers body attachment weights and smoothly blends each fin with X/Y/Z boundary falloffs. This is a local transfer refinement, not wholesale analytic/manual weighting. No further correction was attempted after these two failing-stage refinements.

Final maximum edge ratios improve substantially but remain **12.72 Swim / 4.80 Idle / 9.35 Eat**. These are tested edge maxima above a minimum rest edge length of 1e-4 of fish length, not average strain or a visual pass. Local strain needs further review and may need a separately agreed transfer/refinement revision. Head/eyes remain protected from mouth influence/deltas; coincident seams match exactly.

## Scale
Measured untouched-geometry world Z extents:
- Adult: **.60100985**
- Juvenile: **.36060593**
- Fry: **.18030296**
- Fry/juvenile: **.50000000**; adult:juvenile:fry **1:.60:.30**.

Uniform outer scaling is outside keyed transforms; internal renderer/bone/bindpose correspondence is preserved. Internal scale=.3157; actual outer scale recorded in Scale.txt. No source importer/vertex resizing, renderer-only shrinking or twice-applied root translation. This is a stylized stage-readability ratio. Length convention is fin-inclusive Z extent; a separate body-only anatomical length was not segmented.

## Original appearance — no generation or repainting
- Original fry albedo referenced directly with identity UV0 and no cosmetic tint.
- Original normal and roughness PNGs copied byte-for-byte into OriginalMapImportCopies. Only the new importers are corrected: normal is linear NormalMap, roughness linear Default, both uncompressed. Originals are untouched.
- FryOriginalMaps.shader evaluates original roughness as `smoothness=1-roughness` without creating a packed texture. Original metallic was independently measured as all zero, so metallic contribution is zero; no adult/juvenile/AI-generated packed texture is borrowed.
- Original albedo-only source view shows pale peach/orange markings and both dark pupils. Final sampled Swim, Idle and Eat multi-angle views show dark, registered pupils. These are sampled visual observations, not exact Meshy photometric equivalence or continuous eye stability certification.
- Lit appearance is visibly faceted. Original normals/tangents are unchanged; exact normal-map convention and source viewer lighting equivalence remain unverified. No geometry-normal regeneration, texture repainting or cosmetic compensation was used to hide this.

## Mouth
A localized copy-only MouthOpen uses fry lip anatomy rather than adult/juvenile vertex-index deltas. Full-import upper/lower Y amplitudes are +.006/-.012, lower Z +.0015; localized lower-jaw bone influence at most .18 before normalization. Source shape timing/ranges are retained. MouthDesign.txt records affected vertex count.

Shape-only 0/25/50/90/100/0 tests produce finite baked geometry and exact return to shape-zero rest. Protected eye/cheek/body leakage is zero under the recorded x/z mask. Actual source Eat peak (~1.033333s, weight ~89.8255) was visually reviewed.

**FAIL convincing aperture:** the result reads as shallow lip/muzzle movement, not the adult's broad open eating-mouth cavity. Usable original cavity/lip separation was not established. No new topology/interior or painted dark cavity was added. The small bounded movement avoids forcing an exaggerated tear; do not equate correct bindings and closure with natural eating-mouth acceptance.

## Structural and sampled verification
- Rest vertices/normals/tangents/indices/all UV arrays: **exact match**.
- Invalid weights: **0**; maximum weight sum error **1.19209e-7**.
- Coincident seam mismatches: **0**; protected mouth leakage: **0**.
- Rest-bake world maximum errors: adult **6.32203e-8**, juvenile **6.24750e-8**, fry **6.36578e-8**. Calibration confirmed BakeMesh(useScale:true) then renderer.TransformPoint for this project; rotation-only mapping would be wrong.
- Independently reset and sample 33 uniformly spaced times plus authored mouth peak: Swim/Eat **34**, Idle **33** (peak already lies on the uniform grid), all three targets in source and stage-adapted modes.
- All target/clip bindings resolve; **0** nonfinite vertices. Fry and juvenile sampled vertices outside tested local bounds: **0**.
- Adult-reference local rest-bound diagnostics remain nonzero under animated poses, as previously documented; `updateWhenOffscreen=true` and AlwaysAnimate are reference-instance settings, not a production culling certification. Adult assets/settings were not edited.

| Fry clip | Length / loop | Maximum displacement / length | Final maximum edge ratio | Regional result |
|---|---|---:|---:|---|
| Swim | 1.15s / looping | .10010 | 12.72 | Body/tail silhouette coherent at reviewed pose; local strain WARN/FAIL acceptance; fins attached in reviewed still |
| Idle | 3.2s / looping | .08325 | 4.80 | Fin/head silhouette coherent at reviewed pose; local strain WARN/FAIL acceptance |
| Eat | 2.5s / finite | .31117 | 9.35 | Binding/mouth timing and shape reset PASS; convincing aperture FAIL; local strain WARN/FAIL acceptance |

Fry amplitudes remain exact source (all 1.0); no motion attenuation was used to mask bad weights. Adapted mode reproduces existing juvenile rest-relative amplitudes on the new test instance only: body .70/tail .85/fins .70/Eat skeleton+translation .85; mouth remains source-driven. Adult exact clips and timing are unchanged. No production procedural secondary offsets are enabled or claimed.

## Continuous runtime — NOT RUN successfully
One supported self-contained bootstrap attempt remained **WaitingForCompile / NOT_READY**, with compiling=false and playing=false after the bounded wait. No runtime progression, loops, transitions or lifecycle assertion completed. Stopped rather than raising timeouts, changing timescale/background Player Settings or repeatedly polling. Session state was cleared; temporary PlayModeTestRunner.cs removed; editor is in Edit Mode. RuntimeBlocked.txt records the result. There is no successful RuntimeResults.json or runtime footage.

Unverified: three continuous Swim/Idle loops, full Eat and returns, repeat Eat, pause/resume, disable/re-enable/graph lifecycle, variable frame time, offscreen culling and temporal naturalness. Compilation and static samples do not substitute for these tests.

### Manual isolated review
Open `Scenes/Fry_RiggingPrototype_Test.unity`, press Play, select `ThreeStagePlayback`.
- ComparisonSequence: three Swim loops -> three Idle loops -> Eat -> Idle .5s -> Swim one loop -> repeat.
- ContinuousSwim / ContinuousIdle: uninterrupted loop diagnostics.
- EatToIdle / EatToSwim: complete Eat then selected continuous state.
- RepeatEat: reset between complete Eat cycles.
- Paused / AdaptStages and context menu Reset Comparison.
- Enable the disabled independent Fry_Original_Rest_Control for original albedo/rest comparison, hiding Fry_Animated to avoid overlap.

Please review mouth aperture and residual deformation before authorizing more rig/topology work. Runtime availability/manual review is required before natural animation acceptance.

## Captures and visual evidence limitations
Reports/Captures contains eight 1920x1600 diagnostic camera-rendered sheets: Rest, Swim/Idle/Eat x Source/Adapted, and Eat_EyesMouth_Closeup. Columns adult/juvenile/fry, rows side/top/front/three-quarter; labels record exact sample time and scale. Normal sheets use shared orthographic size .36; closeups intentionally use stage-specific magnification and are not size evidence. These are diagnostic screenshots, not generated replacement textures.

Original and final fry multi-angle views were actually reviewed for rest, Swim .2875s, Idle .8s and Eat 1.033333s. Initial full-scene overview showed all three stages together. Detailed direct PNG readback failed because image handles were unresolved; a temporary sheet-display board also produced an empty tool capture and was removed. Therefore not every saved contact sheet was visually certified. Raw PNG files remain available for manual review. Some automatic focused multi-angle views crop extremities and do not independently establish shared scale; numeric lengths/shared-scale sheets are the scale evidence.

## Preservation
Fresh baseline protected **2,140** files/metadata across Assets, ProjectSettings and Packages. **2,138 unchanged**, **0 changed**. Two missing entries are an ephemeral assistant reference image and its metadata, not any koi source/completed asset:
- `Assets/AI Toolkit/Temp/AssistantImageReferences/c6acff887cf1e702a59508fdaf498434cd0406ef.png`
- corresponding `.meta`.

These transient files expired during the disconnected tooling session; this discrepancy is explicitly retained in PreservationResult.txt rather than represented as a perfect all-file hash pass. All inventoried original fry/adult/juvenile models, textures, rigs, scripts, clips, materials, prefabs, scenes and project/package settings are unchanged. Both new PNG import copies match originals exactly. No new generated/packed/repainted texture exists. The original clean juvenile scene setup is restored without saving it after completion.

## Retained outputs
- Editor/FryRiggingPrototypeBuilder.cs
- Editor/FryRiggingPrototypeValidation.cs
- Runtime/FryStageComparisonPlayback.cs
- Shaders/FryOriginalMaps.shader
- Meshes/Fry_RiggedMesh.asset
- Prefabs/Fry_RiggingPrototype.prefab
- Materials/Fry_AlbedoOnly.mat, Fry_Prototype_MAT.mat
- Materials/OriginalMapImportCopies/Fry_normal.png, Fry_roughness.png and new import metadata
- Scenes/Fry_RiggingPrototype_Test.unity
- Reports: source protection/context, anatomy/scale/transfer/region correction/mouth designs, baseline and final validation JSON, mouth tests, visual/runtime/preservation findings, capture sheets/labels, this report and CreatedFiles.txt.
- Unity-generated asset/folder metadata; exact inventory in CreatedFiles.txt.

Not created/retained: source FBX copy (unnecessary), animation clip copies, packed maps, generated models/textures, topology/interior repair, temporary runtime runner or diagnostic display board. Initial ValidationBaseline.json is failure evidence, not the final result.

**Stop boundary:** isolated fry prototype review only. Further weight-method changes, aperture/interior work and production acceptance require review; adult and juvenile stay unchanged.
