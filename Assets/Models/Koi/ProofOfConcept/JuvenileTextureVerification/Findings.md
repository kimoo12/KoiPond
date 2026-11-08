# Juvenile texture verification — findings only

## Verdict
The juvenile does NOT have a rig-induced UV/geometry mismatch. No texture transfer or correction has been applied to the accepted rig. All new work is confined to this separate test-copy directory.

## Fresh structural comparison
Original supplied textured FBX, Koi_Stage1_Juvenile.fbx and accepted Juvenile_RiggedMesh: 87,391 vertices, 159,982 triangles, one submesh; vertex positions, triangle indices, normals, tangents and all eight UV channels match exactly. UV0 has 87,391 entries; UV1–7 are empty in both. Rigging adds weights, bindposes and MouthOpen, not a different rest surface or texture atlas. Display transform scale/rotation was matched without editing source data.

## Material and map audit
Both source FBX embedded materials have empty texture assignments. The accepted juvenile material does correctly reference the supplied juvenile texture.png with white tint and identity texture scale/offset. Its normal import copy is byte-identical to the original normal PNG and is imported as NormalMap/linear. Its existing packed map was freshly verified: R=original metallic.R and A=255-original roughness.R, zero mismatched R/A pixels. That is a derived packed map, not an original Meshy map. No AI maps from the other koi were used. Source normal/metallic/roughness import settings are Default/sRGB, unsuitable for direct PBR assignment; only test copies were imported correctly. These are not used directly by the accepted rig, which already uses corrected copies/packing.

## Controlled rendering
Four 768px views: side, top, front and isometric, plus two head/eye close-ups. A=original with explicitly assigned source maps; B=rig copy with the exact same material; C=rig copy with copied current material; D=unlit supplied albedo; E=original empty embedded material. Same body size, orthographic camera per view, white directional light, flat neutral ambient, no shadows/fog/water/postprocessing. Rest pose only.

A versus B: mean RGB byte error between 0.0000068 and 0.0000300 across each whole image; maximum 1 byte in three views, 3 bytes at one pixel in isometric. No meaningful appearance difference caused by the rig. C differs mildly from A (whole-image mean 0.029–0.138 byte, maxima 10–26), consistent with imported source-albedo compression/material sampling differences; it does not acquire or lose major markings. Exact metrics and scope caveats are in RenderMetrics.txt.

Both original and rig reproduce the same pale/patchy markings and lack defined dark pupils. The unlit albedo-only control also lacks the expected dark eyes; metallic/roughness/normal lighting is therefore not the cause of missing painted pupils or major markings. Empty original material renders plain gray/white, proving automatic texture binding was absent. Raw supplied albedo contains 72,841/4,194,304 pixels with all RGB<51 (1.736665%) and zero pixels passing the strict R>100 and R>1.5G/B red test. Color thresholds are not exact pupil segmentation or proof of a specific export identity.

## Actual issue / next step
The accessible source model/map pairing already has the pale/incorrect appearance before rigging. The accepted material is not pointing to an adult atlas or a different albedo. UV transfer onto identical UVs would reproduce this same appearance, so no deterministic surface transfer was warranted. Binding the original maps fixes its empty imported material for comparison, but does not restore the desired markings/eyes. The available evidence localizes that remaining problem to the supplied albedo/source pairing, not the accepted rig's UVs. It cannot determine whether this is a mismatched export, an incomplete Meshy texture result, or another upstream source problem. A matching original Meshy juvenile export that visibly contains the intended markings and dark pupils is needed to resolve that distinction. No external Meshy viewer/reference screenshot was provided for a fresh exact-equivalence check.

## Preservation / limitations
SHA256 protected files checked=148, changed/missing=0
Protected scope: complete existing JuvenileRiggingPrototype tree (including scene, mesh, prefab, materials, scripts and metadata), original juvenile texture directory, stage source FBX and metadata, and koi animation directory. No original assets/import settings, accepted rig/bones/weights/shapes, animations, production assets, packages or project settings were edited. New maps are byte copies of originals except deterministic channel packing; no repainting or AI generation. No continuous animation/runtime test was needed or performed; findings concern texture registration and Edit Mode rest renders. Original scene restored at completion.

## Evidence
NumericAudit.txt; RenderMetrics.txt; PreservationResult.txt; Captures/Rest_Original_RigSource_RigCurrent_Unlit_Imported.png; Captures/Eyes_Original_RigSource_RigCurrent_Unlit_Imported.png; Captures/Labels.txt. Open Juvenile_TextureVerification.unity for A/B/C side-by-side; D/E and the captured-eye display are inactive diagnostic controls.
