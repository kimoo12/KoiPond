# Phase 4 Visual Genetics Step 3A — Texture Transfer Pilot

## Outcome: PARTIAL / NOT APPROVED
Four 2048x2048 derivative albedos were baked: Kohaku/Tancho × Fry/Juvenile. No expansion or system integration. All four fail complete surface-coverage acceptance; magenta intentionally exposes rejected regions rather than concealing them. Cyan would indicate conflicting UV samples.

- Kohaku Fry: PARTIAL; rejected=247948 (9.02% surface texels); protected original detail=2.14%; UV conflicts=0; non-albedo map/shader reference equality=True.
- Tancho Fry: PARTIAL; rejected=247948 (9.02% surface texels); protected original detail=2.14%; UV conflicts=0; non-albedo map/shader reference equality=True.
- Kohaku Juvenile: PARTIAL; rejected=172331 (6.14% surface texels); protected original detail=3.64%; UV conflicts=0; non-albedo map/shader reference equality=True.
- Tancho Juvenile: PARTIAL; rejected=172331 (6.14% surface texels); protected original detail=3.64%; UV conflicts=0; non-albedo map/shader reference equality=True.

## Method and provenance
Adult inputs are existing adult-UV Kohaku/Tancho topology-transfer albedos, checked against the live catalog. They are derivatives of original Meshy artwork, not new AI generations. Piecewise bone-anchor longitudinal mapping and independently anchored left/right pectoral regions feed accelerated BVH closest-triangle barycentric correspondence. Side, normal-dot >= .25, distance <= .075 in adult-length .60101 units and longitudinal band .045 reject poor matches. Destination normal mapping uses local inverse-transpose Jacobian. Existing skin-weight fin classifications are anatomical hints, not certified segmentation. Pupil candidate centroids determine narrowly bounded .026 eye spheres; .022 lip spheres retain original stage albedo explicitly. These inferred masks require visual approval.

## Numeric quality
Fry mean accepted distance .04110 (~6.84% adult length), Juvenile .02934 (~4.88%); worst accepted .075 (~12.48%). These residuals are relatively large and are a warning, not proof of accurate marking transfer. UV summed raster overlap events Fry=14, Juvenile=15; no separated-position conflicts at squared separation threshold 1e-8. Subpixel faces Fry=39071/Juvenile=34721. No claim of continuous exact overlap freedom or complete seam certification. Accepted samples use bilinear source sampling; eight-pixel padding touches unused atlas only, never rejected surface regions. No failed surface region was filled.

## Visual review
Multi-angle scene review showed recognizable Kohaku red/white body markings and Tancho dorsal head spots on both stages. Dark lateral eyes are visible in review. Major remaining magenta defects occur at pectoral/other fin edges, tail and dorsal features, with front-facing rejected strips. Therefore neither fin-root continuity, mouth-detail masking nor full marking accuracy is certified. Broad bone-anchor fitting and unboned fin correspondence are the likely next refinement areas; raising rejection thresholds would conceal this limitation and was not done. Tancho inherited head spot looks plausible in top view but exact size/position, absence of all unintended protected-source red, both pupils and lip registration need close user review.

## Captures and tests
Reports/Captures contains 96 labeled 1024-square PNGs: Lit and albedo-only rest Side/Top/Front for adult inputs, four pilots and original-stage controls, plus Side/Top/Front at halfway through Swim/Idle/Eat for four pilots. Filename records target, mode, clip/time and view. Review scene Pilot_MagnifiedReview uses 2x Fry magnification solely for readability. Standard comparison scene preserves authored stage scale.
Five independent sample times per source clip per pilot produced 60 finite baked-mesh checks; original clips/rigs unchanged. Continuous Play Mode NOT RUN; full extreme/loop mouth and fin certification NOT RUN. Deterministic repeat-bake, full continuous seam-surface audit and source-UV/color ambiguity enumeration NOT RUN. Overlap audit currently rejects separated destination positions rather than evaluating second-source color disagreement for coincident samples; further validation needed before acceptance.
Both new scripts compiled successfully. Capture cleanup initially warned about unsupported last-scene unload; cleanup was corrected. Initial image-reference retrieval used an obsolete API and was corrected to current EntityId; no source asset was affected. Saved images could not be opened through the image-content API because its legacy IDs do not match current EntityId; visual review used live multi-angle scene captures, not claims of reviewing every saved PNG.

## Scope and preservation
No original geometry, UV, rig, skinning, blendshape, normal/roughness map, source material, animation, catalog, growth/genetics system or production scene was edited. Original materials copied with only albedo reference replacement; no new normal/roughness/packed map. Read-only source references retained. Profiles are recorded JSON landmark/threshold audits rather than the proposed ScriptableObject asset, avoiding unneeded runtime configuration. Metadata and bytes checked against protection manifest after implementation. See PreservationResults.txt and CreatedFiles.txt for authoritative counts/inventory.

## Stop
Do not approve expansion based on these partial results. Request review of marking placement and protected anatomy first; remaining regional correspondence defects need a separately reviewed refinement pass. Hirenaga, other varieties, hybrid compositing and production integration deferred.

