# Phase 4 — Koi Growth System Results

## Verdict
**Implemented and verified in the isolated GrowthSystemPrototype. Stop before production integration.** Growth data/identity/scaling tests pass, existing genetics regression suites pass, and one automated multi-frame Play Mode attempt completed successfully. All 2,227 protected files remain byte-identical; no unrelated files were added outside the permitted root and its Unity folder metadata.

This is not certification of the pre-existing fry/juvenile mouth anatomy, deformation quality, or genetic visual matching. Those assets were reused unchanged.

## Deliverable and usage
Root: `Assets/Models/Koi/ProofOfConcept/GrowthSystemPrototype/`.
Open `Scenes/Koi_GrowthSystem_Test.unity`, enter Play, and select `Persistent_Koi_Growth`.
The custom Inspector shows read-only birth ID, parents, seed, current age/stage/progress/size and a birth-record invariant check. Controls support growth-only pause, manual +1/+10-second advances, same-identity age reset, explicit timing reinitialization, snapshot/restore and Swim/Idle/Eat selection.
The rear reference row is Fry 30%, Juvenile 60%, Adult 100%; the growing individual is in front. The original clean Fry rigging scene is restored at completion; the growth scene remains available separately.

## Timing and scaling
New `Profiles/Growth_Diagnostic.asset` defaults to 120 active seconds Fry→Juvenile and 180 active seconds Juvenile→Adult; total 300 seconds. Both durations are independently configurable and frozen into each initialized individual. These are diagnostic defaults, not production balancing.
- Fry: 30%→60% adult rest length, linearly over 120 seconds.
- Juvenile: 60%→100%, linearly over 180 seconds.
- Adult: exactly 100%; age saturates at adulthood.
- Exact thresholds choose the later stage. Large advances retain elapsed time and emit crossed transitions once, in order. Initialization/restoration does not replay historical transitions.
- Automatic advancement uses scaled active simulation time. Growth pause/disable freezes age; global timeScale zero also pauses animation. Manual advance is an explicit diagnostic operation and may be used while growth is paused. No offline growth.

Calibration uses transformed rest vertices in the individual-root reference space, not enlarged culling bounds. Completed baseline lengths are approximately Fry .180303, Juvenile .360606, Adult .601010, confirming .30/.60/1.00. Each stage retains its original authored scale, bones, renderer transforms and bindposes. A separate parent multiplier computes adultLength × desiredRatio / stageBaselineLength. No double .30/.60 scaling, renderer-only shrinking, importer edits or mesh modification.

One persistent root owns three cached visual-only stage hierarchies. Exactly one is visible/animated at a time; stage swaps preserve target length and clip clock/phase. Anatomy and markings may visibly change because the source models differ; this is not mesh morphing or genetic appearance matching.

## Identity and separation
The existing `KoiOffspringRecord` and its genome are retained by reference during growth, resets and same-individual restore. ID, both parent IDs, breeding seed, genome schema, all six allele pairs and resolved traits remain unchanged. A managed-reference individual owner prevents Unity inline serialization from creating a false initialized birth record.
Growth rules operate only on duration/age data. They contain no breeding, mutation, texture assignment or random generation. The visual layer supplies a separate read-only appearance context with the original birth/genome/traits and active renderer, without changing source materials.
Snapshot/restore validates the growth schema, timing, identity and genetics, and never calls Breed or generates IDs. Restoring an already bound controller must match its unchanged birth record. Serialization tests preserve values; independently deserialized managed references are not assumed to equal earlier references.
Hirenaga traits, carriers, mutated and hybrid/non-starter genomes are preserved. Matching Hirenaga stage visuals and genotype-to-texture rendering were not supplied/implemented and are explicitly flagged.

## Animation integration
Original Swim/Idle/Eat clips are reused unchanged at source speed/amplitude. The new adapter uses a manual shared clip clock and one active PlayableGraph output, resets omitted source channels and all blendshapes to cached rest before evaluation, resolves MouthOpen by name, evaluates incoming poses before revealing them and cleans up graphs on disable/scene exit. No production wandering, feeding, collider or procedural motion controller is attached.

## Test results
### EditMode — PASS
**1,354 assertions** in `Editor/KoiGrowthEditModeTests.cs`:
- Diagnostic ages 0/60/120/210/300 yield size ratios .30/.45/.60/.80/1.00 and expected stages.
- Exact/epsilon thresholds, invalid ages/durations/deltas/schema, adult saturation and ordered exactly-once large-step crossings.
- 21,000 small time advances versus an equivalent large advance; zero delta and independent data states.
- A 0→300-second diagnostic age sweep, calibrated rest-length measurements and monotonic scaling, exactly one active stage and persistent root transforms.
- Outgoing/incoming boundary length continuity within .001 adult length.
- Exact source mesh/material references, original internal rig transforms and all original clip paths/components/MouthOpen bindings.
- Same runtime birth/genome references and unchanged serialized ID/lineage/genetics/traits, including mutated and Hirenaga/hybrid fixtures; ancestor genomes remain unchanged.
- Snapshot/restore and rejection of mismatched/unsupported data.
- Actual shared-profile edits do not remap an initialized individual's age/size; explicit timing reinitialization adopts the new profile without changing birth.
- Sampled source clock/phase continuity during Swim/Idle/Eat transitions, logical loops/Eat returns, large skip to Adult and no Unity RNG consumption.
- Custom diagnostic Inspector constructs successfully.

### Existing genetics regressions — PASS
- Breeding: 10 tests, **20,800 assertions**.
- Mutation: 7 tests, **32,552 assertions**.
- Multi-generation: 5 groups, **63,855 assertions**, 3,148 unique offspring records including deterministic replays.
All existing test scripts and genetic services remain unchanged.

### Actual Play Mode — PASS (one attempt)
`Reports/RuntimeResults.json` is the authoritative result. A self-contained bootstrap entered only the growth scene, observed multi-frame state, saved results, exited Play and was removed afterward.
Verified:
- Real Update advances age.
- Growth-only pause freezes age while animation continues.
- timeScale=0 freezes both; original timescale restored without changing project settings or increasing simulation speed.
- Disable destroys the graph and preserves age/clock; re-enable recreates valid playback.
- Both growth thresholds crossed during actual Swim and during actual Eat.
- At least three real Swim loops and three real Idle loops.
- Complete Eat→Idle and a second complete Eat→Swim, without stale Eat MouthOpen.
- Persistent birth/genome references and all identity/lineage values, same-individual snapshot restore, one active visual and large age advance.
Observed maximum sampled fin rotation change: **29.46292 degrees**; runtime test progressed through phase 8 and recorded 8 crossing notifications across deliberate resets/skips. These resets explain the count; ordinary single-lifetime exactly-once crossings are separately tested.
The first retrieval happened while the same test was still InPlayMode; the subsequent bounded retrieval returned PASS. `RuntimeBlocked.txt` retains this intermediate observation and is explicitly marked SUPERSEDED. No second runtime test was run.

### Visual evidence
Multi-angle Scene View checks were actually reviewed at Fry 30% and Juvenile 80%. Four intended koi are present, the reference row is separated from the growing fish, size progression is readable in common-scale views, and no unintended layout overlap/missing-material coloration was observed. Some orthographic projections naturally align objects at different depths; the Top view confirms separation.
Five 1920×1080 fixed-camera EditMode rest captures cover ages 0/60/120/210/300 and ratios 30/45/60/80/100%. Filenames and `Captures/Labels.txt` label state, size and shared camera scale. A 1024×512 actual runtime screenshot was also saved/imported after the lifecycle test.
**The saved PNG contents were not directly visually reviewed through the image-readback interface.** The reviewed multi-angle scene checks and numerical/temporal assertions do not constitute full visual certification of every saved PNG or continuously natural deformation. These are diagnostic screenshots, not generated replacement textures.

## Preservation and final state
`Reports/PreservationResults.txt`: protected **2,227**, unchanged **2,227**, changed **0**, missing **0**, new unrelated files outside approved root **0**.
Original adult/juvenile/fry meshes, rigs, textures/importers, materials/shaders, animations, genetics/breeding/mutation scripts/tests, completed prototypes, production scenes, packages and project settings are untouched. No production prefab was instantiated with its live behavior: new visual hierarchies were reconstructed from read-only transforms/mesh/material references.
Compilation is clean for the new code. Two existing obsolete-API warnings in the unchanged `FryRiggingPrototypeValidation.cs` may reappear on recompilation; no new growth compile error remains. The successful runtime log contains no new error/exception.
The original clean Fry scene setup is restored and the editor finishes outside Play Mode. The new growth scene is saved at age zero, growth unpaused, with authored rest pose.

## Retained outputs
- Runtime: KoiGrowthData.cs, KoiGrowthRules.cs, KoiGrowthProfile.cs, KoiGrowthController.cs, KoiStageVisuals.cs, KoiGrowthAnimationAdapter.cs.
- Editor: KoiGrowthPrototypeBuilder.cs (guarded new-only builder and incomplete-checkpoint continuation), KoiGrowthPrototypeInspector.cs, KoiGrowthEditModeTests.cs (includes permanent runtime assertion bridge).
- Profiles/Growth_Diagnostic.asset.
- Prefabs/Koi_GrowthPrototype.prefab.
- Scenes/Koi_GrowthSystem_Test.unity.
- Reports: source protection, original scene setup/calibration, assembly, growth and genetics results, runtime results/intermediate observation, preservation, this report, exact CreatedFiles.txt inventory and six diagnostic PNGs with labels.
- Unity-generated asset/folder metadata, including the new root folder metadata.
Temporary PlayModeTestRunner.cs was removed. No new meshes, source textures, packed texture maps, animation clips, materials, shaders, source import copies or production integration assets were created.

## Remaining scope/limitations
- A full uninterrupted five-minute default-duration run was not performed; the entire diagnostic age schedule is covered by EditMode advancement and both thresholds were also crossed during real multi-frame animation using seeded near-threshold ages.
- No dedicated offscreen/culling or controlled variable-render-framerate stress test was performed. Split/large time-step logic is tested; this is not a population performance certification.
- Continuous visible deformation/mouth quality and every saved PNG need manual review. Existing fry/juvenile rig strain and mouth-aperture limitations remain unchanged.
- No production locomotion/feeding integration, breeding maturity gating, genetic appearance renderer, Hirenaga rig creation, production persistence, offline growth, economy changes or memory optimization.

**Stop here. Production integration requires separate approval.**
