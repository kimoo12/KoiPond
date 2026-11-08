# Fresh juvenile Meshy reimport verification — findings before accepted-asset changes

## Verdict
PASS texture registration and desired markings/dark eyes on separate accepted-rig copies. NO accepted juvenile change has been made. NO deterministic surface transfer is needed, because fresh-export geometry and UVs match exactly.

## Source
Fresh assets: Assets/Models/Koi/New Juvenile/new_juvenile.fbx and new_juvenile_texture{,_normal,_metallic,_roughness}.png. Stored references: Assets/Models/Koi/Meshy Screenshots/juvenile_side.jpg, juvenile_top.jpg, juvenile_angle.jpg.

## Geometry / UVs
Fresh FBX versus accepted Juvenile_RiggedMesh: 87,391 vertices, 159,982 triangles, one submesh. Exact vertex positions, triangle indices, normals, tangents and all eight UV channels. UV0 has 87,391 entries; UV1–7 are empty in both. Full rig copies retain accepted hierarchy and local transforms, original weights/bindposes and all MouthOpen frame deltas. Only material assignment on test copies changes. No UV flip or reparameterization.

## Appearance verification
Fresh source and accepted rig copy were rendered with the same new-map PBR material at equal world size and identical orthographic camera/light settings. Four views: side/top/front/isometric. Fresh source versus rig copy whole-image mean RGB byte error: 0.00000127–0.00002797; maximum 1 byte in top/front and 3 bytes at one pixel in each side/isometric view. No meaningful marking/eye registration difference.

Visible red/orange dorsal and side markings, gray/dark patches and well-defined dark pupils are now present on BOTH the fresh source and the test rig. New-albedo unlit control reproduces the same marking locations and dark pupils, and matches the stored Meshy screenshot pattern visually. Head close-ups show dark pupils under both PBR and unlit; old-material control retains pale/patchy eyes. Top reference orientation differs from our capture; orientation is not a UV mismatch. Meshy screenshot lighting differs, and our neutral PBR render is darker than its viewer/unlit reference. This is a visual pattern/registration pass, not exact photometric or pixel equivalence with Meshy. Normal-map green/Y convention and exact viewer lighting are not conclusively verified.

## Actual cause / material setup
The new albedo differs from the previous supplied albedo and contains the previously missing markings/pupils. Fresh FBX still imports with empty embedded map assignments, so maps must be explicitly bound. Original imported normal/metallic/roughness files are Default/sRGB; those originals were NOT modified. Test copies use sRGB albedo, NormalMap/linear normal, linear metallic and roughness. URP metallic/smoothness is deterministically packed from the fresh raw maps: R=metallic.R, A=255-roughness.R; no AI, repainting, recoloring or surface rebake. Test textures are PNG byte copies except that mechanical channel packing.

## Recommended next step — approval required
A material-only replacement using the verified new source maps can resolve the accepted juvenile appearance. Preserve the accepted mesh, UVs, bones, weights, bindposes, MouthOpen, animation clips and scene/gameplay setup. No topology transfer is warranted. This report does NOT authorize or perform that accepted-asset replacement.

## Preservation
SHA256: 512 pre-existing files checked; 0 changed/missing.
Test copy retains exact geometry, weights, bindposes and blendshape frames=True

Protection covers every pre-existing file and metadata under Assets/Models/Koi, including originals, fresh import, accepted prototype, animation directory, Meshy references, previous verification and other koi. New outputs are confined to this sibling verification folder. Project settings/packages and production objects outside that folder were not edited. Original loaded scene is restored at completion in Edit Mode. No continuous animation/playback test performed; these are texture/UV/rest-render findings.

## Evidence
Juvenile_MeshyReimport_TEST.unity — saved A fresh source/new material, B accepted rig COPY/new material, C rig COPY/old material; D unlit and evidence displays are inactive controls.
Captures/Controlled_Original_RigNew_RigOld_Unlit_MeshyReference.png
Captures/ReferenceComparison_NewPBR_Unlit_Meshy.png
Captures/Eyes_RigNew_Unlit_RigOld.png
Captures/Labels.txt, RenderMetrics.txt, NumericAudit.txt, PreservationResult.txt.

## Raw-map audit
Fresh FBX versus accepted rig: 87391 vertices, 159982 triangles, one submesh; positions, indices, normals, tangents and UV0–7 exact. No transfer required.
New raw albedo: 2048x2048; dark RGB<51=59606 (1.42111778259277%); red-dominant R>100 R>1.5G/B=20970 (0.499963760375977%). Not pupil segmentation.
New/old albedo byte equality=False

