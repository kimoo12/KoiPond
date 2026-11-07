# Phase 4 — Juvenile Rigging Prototype Results

## Verdict
**Isolated prototype created; source preservation and source-clip binding checks pass. NOT production-ready.** Body/tail motion is demonstrated in sampled poses; pectoral roots retain localized distortion, and mouth motion is a provisional muzzle/lip deformation rather than a convincing open mouth. Continuous Play Mode playback could not be verified in this editor session. No fry rigging, production integration, or original-asset edits were performed.

## Sources and adult inspection
- Juvenile geometry: `Assets/Models/Koi/Meshes/Koi_Stage1_Juvenile.fbx`, Mesh_0, **87,391 vertices / 159,982 triangles**.
- Textured juvenile Meshy reference: `Assets/Models/Koi/Materials/juvenile texture/Meshy_AI_stylized_koi_fish_20261025_image-to-3d-texture.fbx`. Its vertices, triangle indices, UV0, normals and tangents match the juvenile geometry source exactly. Both are unrigged and lack MouthOpen.
- Adult mesh: `Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset`, **221,941 vertices**, 10 bindposes/bones, weighted vertices on every bone, one MouthOpen frame. Different topology: no vertex-index weight or blendshape copy was used.
- Adult skeleton reference: `Assets/Prefabs/Koi_Stage3_Adult.prefab`. Bone order is Root, Spine_Front, Head, Mouth, PectoralFin_L, PectoralFin_R, Spine_Back, Spine_Tail1, Spine_Tail2, TailFin. Paths are retained beneath Armature/Root; renderer is FishMesh. Adult rotations are identity in rest pose; the rig is non-humanoid.
- Adult spatial weight inspection identified longitudinal body/head/tail regions and separate dominant left/right fin regions. Original MouthOpen affects 6,212 vertices with maximum local delta magnitude 0.01678378.
- Original clips used without editing/copying: Koi_Swim (1.15 s, looping), Koi_Idle (3.2 s, looping), Koi_Eat (2.5 s, nonlooping). No animation events or object-reference curves. All use root translation, bone rotations and FishMesh/MouthOpen; Idle omits Mouth-bone rotation. Each independent sample restores rest transforms and shape weight.
- Original juvenile side/top screenshots were visually inspected for silhouette/fin proportions. They show red markings and dark pupils not reproduced by the supplied juvenile texture set in this test; see appearance limitation below.

## Adaptation performed
- Created an exact, readable standalone juvenile mesh copy through mesh data access in Edit Mode. **No original FBX importer readability/scale/rotation changes**, and no disposable FBX copy was needed.
- Retained original rest vertices, triangle topology/order, UV channels, normals and tangents. No retopology, welding, smoothing of geometry, unwrap, adult UV substitution or texture repainting.
- Imported juvenile coordinate system remains on FishMesh: X rotation 270 degrees and import scale 100, multiplied by a test display scale **0.3157**. Display body length is about 0.601 world units, comparable to the adult reference, not a production juvenile size decision. Vertices themselves are unchanged.
- Copied adult bone hierarchy only, not production controller/behaviour components. Fitted front/head/mouth, body/tail-chain and two fin pivots independently to juvenile anatomical regions. Recorded coordinates in RigAudit.json and FinalBoneCoverage.txt.
- Region-constrained, BVH-accelerated closest-triangle barycentric adult-weight transfer with a surface-normal penalty and piecewise longitudinal/lateral correspondence. Left/right fin leakage is suppressed. Identical-position seam vertices share weights without changing mesh topology.
- Initial correspondence was imperfect: mean source-space distance **0.0142553**, p95 **0.0483373**, max **0.0742716** (adult body length ~0.601). 5,833 matches exceed 0.04; 7,686 have opposing matched normals. These are recorded warnings, not exact anatomical correspondence claims.
- Two refinement rounds: 24 topology-neighbor weight passes, then 96 additional weight and lip-delta passes (0.55 blend, inverse-edge-length neighbor weighting). This reduces but does not eliminate fin-root/lip strain.
- Added approved **copy-only MouthOpen** from anatomically transported/localized adult lip deltas, then topology-aware delta refinement. 2,272 vertices have measurable nonzero deltas. Base geometry is unchanged. No inner-mouth cavity or new lip geometry was added.
- Prototype prefab contains transforms and SkinnedMeshRenderer only. The separate scene adds an Animator and JuvenilePrototypePlayback harness using original clips through Playables. It is deliberately not wired into pond behaviour, feeding, spawning, breeding or production animation controllers.

## Preservation / structural results
| Check | Result |
|---|---|
| Original Assets files including .meta checked by SHA256 | **1,802 checked; 0 changed** |
| Juvenile rest vertices, indices, normals, tangents | Exact equality |
| All UV channels | Exact equality |
| Skin weights / indices | Normalized, finite; valid indices 0–9 |
| Maximum weight-sum error | 8.95e-8 |
| Same-position seam weights | Consistent |
| Bindpose maximum matrix error at rest | 0 |
| Rest baked-to-original world-position maximum error | 1.52e-7 (well below 1e-5 of body length) |
| Missing source-clip bindings | 0 for each clip |
| Nonfinite baked vertices | 0 for each clip |
| Baked vertices outside prototype culling bounds | 0 across 33 uniform samples per clip |
| Final C# edits | Tool compilation checks passed |

The initial test implementation accidentally measured BakeMesh output in the wrong scale space; that was a **test-space bug**, not a rest-geometry change. Explicit `BakeMesh(..., true)` was tested against source local coordinates and the metrics were corrected before final reporting.

Temporary preview object creation marked the loaded pond scene dirty, despite no serialized differences. A separate temporary scene-copy comparison proved byte-for-byte serialized equality (26,182 lines) before the pond was reloaded without saving its original file. The temporary comparison scene was deleted. Original pond/prefab/material hashes remain unchanged. The pond is restored at task end; no unsaved production work was discarded.

## Clip-by-clip results
**Verification performed: 34 independent Edit Mode samples per clip (33 evenly spaced including both endpoints + exact maximum MouthOpen key), numerical deformation checks, and visual review of side/top/front/three-quarter CPU-baked pose captures. These are not continuous Play Mode tests.**

| Clip | Body / head | Tail | Left/right pectoral fins | Mouth | Overall |
|---|---|---|---|---|---|
| Koi_Swim | Body sway visible, head/body stays coherent at sampled poses | Chain and tail fin visibly bend; no gross detached tail | Both move; localized attachment stretching remains | Breathing binding works; provisional lip motion | Demonstrated with warnings |
| Koi_Idle | Gentle sway/bob visible, silhouette largely stable | Low-amplitude tail motion visible | Both flutter; attachment strain still present | Binding works, small muzzle/lip motion | Demonstrated with warnings |
| Koi_Eat | Head lift/feeding lean visible; adult amplitude is strong for the round juvenile | Motion follows feeding pose | Fins respond; some folding/attachment strain | MouthOpen reaches 89.8255 but changes the muzzle/lip pad more than opening a convincing mouth | **Mouth visual quality fails production acceptance** |

Final numeric metrics (world-space displacement includes authored root motion and head lift):
| Clip | Max vertex displacement | Max tested edge-length ratio | MouthOpen max | Loop endpoint error |
|---|---:|---:|---:|---:|
| Swim | 0.0622029 | 5.1852 | 29.9922 | 6.29e-8 |
| Idle | 0.0538525 | 4.3806 | 25.0 | 5.98e-8 |
| Eat | 0.1740493 | 4.3046 | 89.8255 | Nonlooping |

Before refinement, tested maximum edge ratios were approximately 105 / 97 / 135. Remaining >2 ratios and collapsed edges are **real localized warnings**, not a clean deformation pass. The diagnostic excludes rest edges shorter than 0.00006 world units; counts in ValidationResults.json accumulate triangle-edge samples and are not unique affected vertices. High distortion was localized near a pectoral/body transition (~x 0.25, y -0.34, z 0.22 in full imported juvenile units) and front lip. The last two refinement rounds improved these without changing geometry. Further manual anatomical weight/pivot review is needed before production use.

Sampled **Eat end → Idle start** without an intervening rest reset gives zero residual bone-rotation difference and MouthOpen=0 compared with a freshly reset Idle start. This confirms endpoint compatibility only; crossfade/continuous transitions remain unverified.

### Mouth close-up
`Eat_Mouth_RestPeak_FrontIso.png` compares rest and the exact maximum-mouth key at **1.033333 s**. Columns: rest front, peak front, rest three-quarter, peak three-quarter. The lower muzzle/lip pad expands and changes angle, but no convincing dark mouth cavity or separation is demonstrated. Do not call this a finished eating mouth. Creating a real cavity or changing lip topology would require new approval because geometry preservation was required.

### Appearance limitation
Original `juvenile texture.png` is used directly, with original UVs and white tint. Normal-map PNG bytes are preserved in a test-only import copy; metallic/roughness are deterministically packed, not regenerated. Both static original-geometry and rigged test renderers use the same new material. Nonetheless, the supplied map/UV pairing produces patchy pale colour and weak/missing pupil detail, unlike the juvenile Meshy screenshots' red markings and black pupils. The source albedo itself is pale/patchy. **Screenshot-level appearance equivalence is not established.** Do not repaint, borrow adult textures, change source UVs or claim rigging fixed the source appearance. Exact export/texture-version pairing should be reviewed separately before accepting appearance. Normal-map Y convention and Meshy-viewer lighting equivalence were not independently established.

## Continuous runtime verification limitation
Three complete provisioning attempts were made, using reload, real compilation, then a self-contained explicit bootstrap entry. Every attempt remained WaitingForCompile / NOT_READY despite the test type being compiled and domain reload enabled. The editor never entered Play Mode; there is no RuntimeResults.json and no Runtime_*.png. No actual runtime deformation assertion failed, because no test began.

Stopped instead of escalating timeouts or altering settings. Removed the pending prototype callback, cleared session state and deleted the temporary bootstrap script. Editor is not in Play Mode. **Three Swim/Idle loops, full real-time Eat playback, continuous culling/rendering and Eat-to-Idle transition playback remain unverified.** The separate scene's playback harness is left for manual testing. Open the test scene and press Play: it is configured to play Swim three loops, Idle three loops, Eat once, then Idle for 0.5 s. This configuration is compiled but not represented as runtime-verified.

## Captures
All final captures are actual sampled **CPU-baked static mesh poses**. This avoids stale same-frame GPU skinning in synchronous editor captures; no runtime screenshots are claimed.
- Swim_SideTopFrontIso_0_25_50_75.png — 2048×2048.
- Idle_SideTopFrontIso_0_25_50_75.png — 2048×2048.
- Eat_SideTopFrontIso_0_25_50_75.png — 2048×2048.
- Eat_Mouth_RestPeak_FrontIso.png — 2048×512.
For contact sheets, columns are side/top/front/three-quarter; rows are 0%, 25%, 50%, 75% of source-clip duration. Poses reset independently. Scene layout was separately inspected in a multi-angle scene capture.

## Files created and retained
All prototype outputs are under `Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/`:
- `Meshes/Juvenile_RiggedMesh.asset`
- `Prefabs/Juvenile_RiggingPrototype.prefab`
- `Scenes/Juvenile_RiggingPrototype_Test.unity`
- `Materials/Juvenile_Prototype_MAT.mat`
- `Materials/Juvenile_Normal_ImportCopy.png` — same original PNG bytes, different test-only normal-map import settings.
- `Materials/Juvenile_MetallicSmoothness_DERIVED.png` — **derived**, R=original metallic.r, A=255-original roughness.r, linear/uncompressed import. Not a new Meshy texture or AI generation.
- `Editor/JuvenileRiggingPrototypeBuilder.cs`
- `Editor/JuvenileRiggingPrototypeRefinement.cs`
- `Editor/JuvenileRiggingPrototypeValidation.cs`
- `Runtime/JuvenilePrototypePlayback.cs`
- `Reports/SourceProtection.tsv`
- `Reports/SourceProtectionResult.txt`
- `Reports/RigAudit.json` — initial correspondence audit; post-refinement coverage is in FinalBoneCoverage.txt.
- `Reports/FinalBoneCoverage.txt`
- `Reports/ValidationResults.json`
- `Reports/Refinement.txt`
- `Reports/Phase4_Results.md`
- `Reports/CreatedFiles.txt` — full generated file/.meta inventory.
- Four PNGs under `Reports/Captures/`, as listed above.
- Unity-generated `.meta` files for each imported output and new folder.

Additional planning document: `Assets/Plans/phase-4-juvenile-rigging-prototype.md` and associated folder/file metadata.
Temporary files created then removed: `Reports/PondStateComparison_Temporary.unity` and `Editor/PlayModeTestRunner.cs` (and their metadata).
No source FBX copies, retopology assets, generated models, new animation clips, animation-controller assets, gameplay scripts outside the prototype, or production material/prefab/scene changes were made.

## Approval boundary
**Stopped at juvenile prototype. No fry rigging or fry asset changes.** Recommended next review is juvenile fin-root/mouth quality plus source export/texture pairing and manual isolated playback. Any geometry change, production integration or fry rigging requires separate approval.
