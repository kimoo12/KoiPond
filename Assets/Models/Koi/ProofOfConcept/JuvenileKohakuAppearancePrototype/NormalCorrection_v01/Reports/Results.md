# Single Juvenile Kohaku normal correction

One localized correction only. Existing normal, approved albedo, material, prefab, scene, rig, mesh, animations and previous evidence remain unchanged. A new normal copy and material are used only by the separately saved correction review scene.

## Method and preservation
Source original Juvenile pigmentation identifies old spot boundaries; small isolated color flecks are rejected. Normal repair uses fixed 28-pixel contour bands within confirmed body interiors, with every protected UV owner taking precedence and a three-pixel anatomical/island guard. A fixed harmonic solve reconstructs tangent-space normals from unchanged neighboring normals; vectors are normalized and RGB encoded. This is one numerical correction, not a sequence of visual refinements. No global normal-strength reduction, albedo edits, mesh changes or AI regeneration.

177,204 contour-band texels selected; 176,321 normal texels changed (about 4.2% of the map). Zero out-of-mask/protected normal changes, including eyes/face/lips/fins; alpha preserved. Normalized-vector numerical error <=5.96e-8 before encoding. Local smoothing is confined to the old-contour bands; fine relief inside those bands is reconstructed rather than claimed byte-identical. All other normal texels remain exact original bytes. Existing anatomical geometry and normal detail outside repair bands remain intact.

## Verification and outcome
Actual multi-angle scene inspection reviewed right, left and top, both eyes, lips and fins. The prominent old spot outlines across the shoulder patch and body are removed in these static views; natural face/gill and fin shapes remain. No second correction or tuning was performed. Subtle residual relief at protected transitions and exhaustive close-range/mip behavior are not certified by these full-fish views.

Matched 1024-square before/corrected captures for Right, Left, Top and Hero are in `Reports/Captures/`; both versions use the exact approved albedo, same normal strength/maps and identical single key light. Runtime animation, exhaustive seam/mip tests and independent artistic acceptance are NOT RUN.

The correction authoring script compiled successfully. The first preview-open command safely stopped when the original scene was found dirty after asset preparation. The correction was not rerun; a separate additive preview was created while retaining the original unsaved scene. Temporary renderer visibility/light/rotation changes were restored, and no original scene was saved or discarded.

## Final protection
Checked 3812 pre-existing files. All original bytes/metadata unchanged; no outside files created.
All material properties/maps except normal reference unchanged: True

## Outputs and stop
New files are limited to this NormalCorrection_v01 subfolder: authoring script, corrected normal PNG, material copy, separate review scene, protection/audit/results/inventory and eight diagnostic captures plus metadata. Original Meshy normal import copy remains the source; corrected normal is an explicitly new derivative. No production/genetics integration. STOP after this single correction and verification for user review.
