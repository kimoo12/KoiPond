# Phase 4 Step 3B — Refined Kohaku/Tancho early-stage transfers

## Outcome
Four existing 2048-square albedos re-baked; eye duplication and large fin/tail/body gaps materially corrected. Visual approval pending. Coverage remains technically PARTIAL because tiny explicitly rejected surface samples remain. No expansion or production integration.

- Kohaku and Tancho Fry: rejected=67 (0.002438% rasterized surface); original facial-detail protection=14.19%; overlap conflicts=0. Prior rejected fraction=9.02%.
- Kohaku and Tancho Juvenile: rejected=22 (0.000784% rasterized surface); original facial-detail protection=14.28%; overlap conflicts=0. Prior rejected fraction=6.14%.

## Changes
- Conservative adult-eye source-surface exclusion covers measured eye/eyelid anatomy; source triangle vertices and accepted barycentric points are checked. Every bake asserts zero selected points in the excluded adult-eye volume.
- Original stage eye, eyelid, cheek and lip samples preserved through bounded anatomical ellipsoids, with dorsal inherited markings kept separate. No eye inpainting, tinting or guessed colours. Broader facial protection is explicit (~14% surface), not hidden gap filling.
- Geometry-derived caudal/dorsal/ventral charts added alongside existing pectoral hints. Measured 64x48 angular body cross-section profiles replace global width/height scaling. Empty geometric chart cells interpolate measured radii, never colour data. Fin charts independently fit matching region extents. These automatic region/chart definitions remain approximate and require visual approval.
- Body/caudal charts now share the same peduncle boundary at Z=-.17. This fixes a demonstrated gap caused by old tail-bone extrapolation mapping body samples into caudal-only source triangles.
- Isotropic dorsal-head mapping preserves the source Tancho spot's lateral/longitudinal scale without redrawing a circle.
- Existing rejection distance/normal/side gates unchanged (.075 adult-space distance, .25 normal dot, .045 longitudinal band). Tiny rejected texels remain magenta; none were cosmetically filled. Padding stays in unused UV atlas space.
- Coincident raster overlap now checks alternate source sampling against both adult appearances with 2/255 colour-disagreement threshold; incompatible colours are not averaged.
- Four existing material files/GUIDs, shader settings, normal/roughness textures and non-albedo values unchanged. Source mesh, UV, rig, skinning, clips, prefabs and all systems unchanged.

## Verification
- Both editor scripts compiled successfully after restart and each subsequent edit. No CS2001 remains; orphan baseline source count=0.
- Repeated final bake produced byte-identical PNGs for all four outputs (Determinism.txt). The connection dropped during delivery, but the persisted report confirmed completion; the expensive bake was not repeated on reconnect.
- Left/right/front/top live multi-angle reviews show original stage eyes without a second transferred adult eye, intact Kohaku red/white markings and one Tancho dorsal head spot. Large prior magenta gaps are no longer visible at normal review scale. Opposite-side review confirms both eyes and side markings. Fry review magnification is 2x and is diagnostic only.
- Original dark-pupil vertex samples remain dark at 573/573 and 571/572 on Fry; 460/461 and 452/453 on Juvenile, for both varieties. Mean RGB resampling errors ~.006-.014; max ~.116-.209 at a few boundary samples. This is bilinear finite-resolution sampling, not pixel-perfect eye preservation or complete surface segmentation. See EyeAndMarkingSamples.txt.
- Tancho sampled body-red candidates: Fry=2, Juvenile=0 at Z<.17. Fry red threshold also flags small original facial/skin details and/or atlas-boundary bleed; these 2 samples are not silently erased. Their exact visual significance needs close review. Tancho spot looks round in top review, but exact circularity/outline fidelity is not numerically certified.
- 260 saved PNGs under Captures: full-body and facial-detail left/right/top/front/three-quarter in Lit and albedo-only modes for adult sources, four results and original stage controls, plus representative Swim/Idle/Eat views. All are labeled by target/mode/view. Live review was performed; not every saved PNG was individually inspected.
- 60 independent static clip samples (5 times × 3 source clips × 4 pilots) have finite baked vertices. Full animated extreme/continuous Play Mode and mip/seam certification NOT RUN. This task does not repair pre-existing rig/fin/mouth limitations.

## Remaining limitations
Coverage is PARTIAL: Fry 67 rejected texels (65 body, 2 ventral) and Juvenile 22 body texels remain. Complete zero-hole acceptance is NOT claimed. No overlap conflicts were found at texel centres, but continuous/subpixel/seam and source-UV filtering-footprint freedom remain unverified. Region borders may redistribute markings; fin/body texture joins and original facial retention boundaries need close review. Source-normal and roughness maps were deliberately not transferred or repainted. Population performance and production integration not tested.

## Compilation root cause and recovery
Baseline .cs copies had been renamed through direct filesystem moves. AssetDatabase saw TextAssets but the running compiler source cache retained former .cs paths and regenerated stale csproj/Bee inputs. Two stale generated Compile entries were removed; no asmdef/asmref/rsp asset contained such explicit references. Imports and cache rebuilds did not clear that session's source cache. The user restarted Unity, after which the orphan list was zero and normal incremental compilation succeeded. Actual builder/validator and non-compiling archive text were preserved. No package source or compatibility guards modified.

## Files and preservation
Changed implementation: Editor/EarlyStageAlbedoTransferBuilder.cs, Editor/EarlyStageAlbedoTransferValidation.cs. Updated appearance files: Textures/Kohaku_Fry_Albedo_DERIVED.png, Tancho_Fry_Albedo_DERIVED.png, Kohaku_Juvenile_Albedo_DERIVED.png, Tancho_Juvenile_Albedo_DERIVED.png. Added Step3B profiles, metrics, coverage, eye sampling, determinism, captures and reports; added Scenes/Step3B_LeftReview.unity. Prior baseline evidence retained. Full actual output inventory in Files.txt. Protected outside files/metadata unchanged=2360; material byte checks included in PreservationResults.txt. Generated editor project cleanup was separately authorized compilation repair.

## Stop
Stop at the four-texture pilot for visual approval. Do not expand varieties, hybrid rendering, Hirenaga, source texture changes or production integration.

