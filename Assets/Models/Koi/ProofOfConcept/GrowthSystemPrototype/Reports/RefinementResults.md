# GrowthSystemPrototype refinement results

## Scope
Only GrowthSystemPrototype scripts, prefab, test scene, regression tests and reports were changed. Existing source meshes, rigs, materials, animation clips, genetics, profiles and production scenes were retained. SHA-256 comparison of 2,205 pre-existing files outside this prototype found zero changes and no new external files. The editor finished outside Play Mode with a clean saved prototype scene.

## Growth changes
- Calibration now evaluates bone weights and bind-pose matrices, not raw mesh vertices alone or doubly scaled BakeMesh output.
- Growth containers normalize width, height and length to a shared adult-relative rest envelope. This fixes girth/proportion changes that length-only calibration did not account for.
- Existing timing and continuous size curve remain: Fry 30–60%, Juvenile 60–100%, Adult 100%.
- Prototype prefab and static comparison row were calibrated and saved; source rig transforms and mesh/material references remain unchanged.

## Animation changes
- Persistent Swim/Idle/Eat mixer with 0.3-second smoothstep fades replaces graph rebuilds and hard pose resets on clip selection.
- Independent clip clocks preserve loop motion; stage rebinds preserve playback phase and blend weights.
- Interrupted blends begin from current weights. Repeated requests for the current motion are idempotent.
- Eat reentry during an active fade preserves the visible pose using a finite, smoothly decaying pose correction.
- Rest-pose restoration before evaluation prevents sparse Eat bindings inheriting stale joints or blendshapes. Eat completes and smoothly returns to the selected loop.

## Verification
- Compilation checks passed for all final script changes.
- Existing EditMode suite: PASS, 1,956 assertions, including identity, genetics, snapshots, original rig transforms, source references and all-axis growth calibration.
- Final Play Mode regression: PASS; complete detailed evidence in `RefinementPlayModeResults.json`.
- Real Update-driven growth was observed before deterministic tests.
- 301 actual skinned-renderer rest-pose samples (BakeMesh with useScale=true): all XYZ dimensions monotonic; maximum adult-relative ratio error 0.000000800303.
- Boundary 120 s: outgoing and incoming XYZ envelope approximately (0.18, 0.22, 0.36).
- Boundary 300 s: outgoing and incoming XYZ envelope approximately (0.31, 0.36, 0.60).
- All six directed animation pairs tested on Fry, Juvenile and Adult (18 blends), plus rapid 50 ms interruptions and Eat reentry on each stage.
- Clip-request discontinuity measured 0 degrees and 0 positional displacement. Target mixer weights converged to 1.
- Settled transforms and MouthOpen matched fresh source-clip sampling, including missing Eat tail bindings; no residual pose detected.
- At 120 Hz deterministic sampling, maximum bone rotation step was 2.483501 degrees and maximum local positional step 0.003053803 units. These are sampled pose-change measurements, not a frame-rate guarantee.
- Both stage boundaries preserve animation phase and weights for all three motions.
- Complete Eat return to both Idle and Swim, graph disable/re-enable, single active stage and unchanged birth/genome references passed.

## Visual checks and limitations
The multi-angle EditMode scene inspection visibly showed the static Fry < Juvenile < Adult comparison row with preserved materials. Five Game View screenshots were captured during Play Mode at ages 0, 119.9999, 120, 299.9999 and 300 seconds under `Captures/Refinement_0.png` through `Refinement_4.png`. Runtime screenshot read-back could not resolve Unity 6000.6 image identifiers in the image-inspection tool, so those PNGs were retained for manual review; their pixels were not visually inspected by the assistant. Runtime size and animation verification used actual renderer geometry and pose/state measurements. Continuous subjective motion quality at arbitrary frame rates remains a manual review item.

The first real-time test was interrupted during result retrieval after growth and Fry blend checks. Subsequent deterministic Play Mode tests completed successfully without increasing timeouts or accelerating global time. The temporary bootstrap script was removed after testing.
