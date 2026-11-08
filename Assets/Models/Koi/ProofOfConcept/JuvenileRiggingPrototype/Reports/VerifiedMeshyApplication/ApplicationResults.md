# Juvenile verified Meshy textures — applied

## Changes
Updated only existing Juvenile_Prototype_MAT.mat texture references: _BaseMap and _MainTex now use original Assets/Models/Koi/New Juvenile/new_juvenile_texture.png directly; _BumpMap uses byte-identical fresh original normal copied into Materials/NewMeshy and imported as linear NormalMap; _MetallicGlossMap uses verified deterministic packed derivative copied into Materials/NewMeshy. R=original metallic.R, A=255-original roughness.R (fresh validation before copying: zero mismatches). No repainting, AI generation, texture transfer, source PNG/importer edits, scalar/color/keyword/shader changes, mesh/rig/weights/bindpose/MouthOpen/animation edits or scaling changes. Existing material GUID retained, so accepted prefab and live isolated scene keep their existing references. Historical baseline prefab also references this same material and therefore sees its updated appearance; no baseline prefab file was edited.

## Verification
Visual review of final scale-correct sampled images shows original red/orange markings and defined dark pupils during Swim, Idle and Eat poses. Eyes remain registered, including actual Eat MouthOpen peak. Scene lighting remains unchanged (bright neutral setup); exact Meshy photometric equivalence is not claimed. Existing mouth/fin behavior is preserved, not redesigned or newly certified as production-quality deformation.

34 independent poses per clip (33 evenly spaced including endpoints plus maximum MouthOpen key), with original clips and existing juvenile adaptation in a file copy of the isolated scene. All clips: zero unresolved bindings, nonfinite vertices, or sampled vertices outside juvenile bounds. Rest world error=5.330531e-8. Swim max displacement=.0346625, mouthPeak=29.99223; Idle=.02732664, mouthPeak=25; Eat=.09018905, mouthPeak=89.82552. These values match prior unchanged behavior; mouth weights are percent blendshape values. Full sampled metrics in Verified_SampledMetrics.txt.

## Runtime limitation — NOT a continuous playback pass
Attempt 1 entered Play Mode but timed out after 30 seconds: 249 editor samples, stage did not advance, zero movement/shape weights, zero completed stages. Diagnostics found Unity unfocused and runtime background execution disabled. This is consistent with player-loop suspension, not a texture/material-induced animation change. Attempt 2 enabled runInBackground only within the temporary runtime test, without changing Player Settings, timeout or timescale; it remained WaitingForCompile/NOT_READY with compiling=false and never produced new runtime evidence. Stopped further retries. Temporary test script and state removed, editor returned to Edit Mode, background flag false. Continuous Swim/Idle/Eat loops and transitions remain unverified through this editor connection. Manual Play in the original isolated scene is needed to certify continuous playback. The saved default remains its original synchronized comparison sequence; no gameplay code or playback configuration was changed.

## Test-scaling correction
An initial fallback sampler incorrectly used BakeMesh(useScale:false), causing an invalid rest error of 3.305898 and oversized snapshots. No rig data was changed. Corrected test uses the existing project's BakeMesh(useScale:true) convention; rest error returned to 5.33e-8 and all finite/bounds checks passed. Only Verified_*.png are final evidence. Earlier Sampled_*.png and SampledResults.json are discarded test diagnostics, not rig failures or valid animation evidence.

## Preservation
Pre-existing files checked=569, allowed material changes=1, unexpected changed/missing=0
ALLOWED material-only change: Assets/Models/Koi\ProofOfConcept\JuvenileRiggingPrototype\Materials\Juvenile_Prototype_MAT.mat
Material YAML differs only in texture GUID references=True
New normal copy matches original bytes=True; packed derivative matches verified packing bytes=True
Live juvenile/adult world length ratio=0.6
Original isolated scene restored; isPlaying=False; background=False; temporary runtime script exists=False

## Evidence
Captures/Verified_Sampled_Swim_SideTopIsoEyes.png
Captures/Verified_Sampled_Idle_SideTopIsoEyes.png
Captures/Verified_Sampled_Eat_SideTopIsoEyes.png
Captures/Verified_SwimIdleEat_MarkingsEyes.png
Captures/VerifiedLabels.txt
Verified_SampledMetrics.txt; PreservationResult.txt
RuntimeAttempt1_BackgroundSuspended.json records failed runtime attempt. RuntimeResults.json retains that attempt; no newer success is claimed. VisualEvidence.unity displays final sampled captures; Sampled_TestScene.unity is the restored file copy used for sampling. Original accepted scene is open.

No fry work was performed.

