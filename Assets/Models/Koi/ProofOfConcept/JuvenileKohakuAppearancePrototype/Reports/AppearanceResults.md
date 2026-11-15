# One Juvenile Kohaku appearance — STOP: normal-map contour blocker

Exactly one candidate was authored against Juvenile rest anatomy and baked into unchanged UV0. No adult artwork, texture transfer, vertex-color masks, global tint, genetics/growth or production integration.

## Actual result
- White/cream body with three simple soft red/orange regions: dorsal head, shoulder/upper body, rear upper body. Both sides and top visibly show continuous wrapping; naturally pale dorsal blade is retained.
- Both eyes, face/lips and fins remain registered and free of new red in scene-view inspection. Existing pale pink fin artwork and natural gray face are deliberately preserved.
- **Not a clean visual PASS:** the unchanged original normal-map import copy carries old spot-shaped relief/contours. These remain visible across the cream body and inside the broad shoulder patch, especially from top/left. Disabling the normal contribution diagnostically removes the outlines while retaining the same new albedo and other material settings. The no-normal version is diagnostic only, not a promoted candidate.
- Per the stop gate, no normal repair, blanket normal-strength reduction, extra candidate or further refinement was attempted. Saved candidate material retains the exact source normal and packed-map references/settings.

## Verification
- Source ownership audit: 2,807,472 occupied texels; 478,776 editable texels; 15 overlap samples; zero incompatible color-owner samples at recorded tolerance .035 sRGB channel difference. This is a texel-center audit, not proof of overlap-free geometry at every subpixel/mip footprint.
- Protected occupied texel changes: zero. 12 finite padding rings changed unused UV space only.
- Actual multi-angle scene visual inspection: top, both sides, both pupils and face/lips, dorsal and lateral/ventral fins. Normal-enabled versus normal-disabled views establish the map-dependent contour limitation. Detailed saved capture PNGs are provided for user review, not claimed to have individually passed an unavailable image-asset readback interface.
- Source/candidate prefab structural comparison: exact same mesh reference and full original local rest transforms, bone names/order and scale. All source geometry/UV/skinning/shape/animation data remain unchanged.
- Authoring script compiled successfully. Existing Fry validation source emitted two obsolete-API warnings when compilation refreshed; it was not changed. No new authoring/capture errors.
- Representative animation sampling, continuous runtime playback, exhaustive mip/seam certification and in-memory repeat-bake comparison: NOT RUN after the visual blocker. No claim of temporal certification or scalable genetics composition.

## Reproduction and provenance
`Editor/JuvenileKohakuAppearanceAuthoring.cs` records source-only rest profiles, protection envelopes, cream/red colors, three patch parameters, rasterization tolerance and padding. Raw original Juvenile albedo bytes are decoded without importer changes. Eye/face/lip/fin texels retain original artwork; permitted body skin replaces baked pigmentation. New output albedo is an authored derivative, not a Meshy original. Normal/packed material maps remain existing derivatives; no new such maps were created.

## Captures
`Reports/Captures/`: Right_FullMaterial, Left_FullMaterial, Top_FullMaterial, Face_BothEyes_Lips_FullMaterial, Hero_FullMaterial, RightEye_FullMaterial, LeftEye_FullMaterial, DorsalFins_FullMaterial, VentralFins_FullMaterial, Top_NormalDisabled_Diagnostic, Top_AlbedoOnly_Diagnostic (all .png, 1024 square). Fixed full-fish framing and diagnostic names identify the views; eye/fin crops are magnified, not growth-size evidence.

## Preservation
Protected files checked: 3762
All protected file bytes and metadata unchanged; no unexpected outside files.
Same original mesh and full rig/rest transforms: True

## Scope and final stop
The separate review scene contains the original control and the single saved candidate. Initial Fry four-way scene is restored without saving it. Existing experiments/materials/maps are untouched. No approval or production readiness is claimed. This one baked texture tests visual feasibility only; scalable Juvenile trait composition and Adult hybrid appearances remain separately approved future milestones.
