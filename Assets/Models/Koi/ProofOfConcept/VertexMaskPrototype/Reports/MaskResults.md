# Diagnostic anatomy mask candidate — pending visual approval

Legend: green=conservative body interior; yellow/orange=left/right eye envelope; red=lip envelope; cyan=fin candidate; magenta=uncertain/protected. RGB is diagnostic only; alpha stores a vertex label, NOT opacity or an appearance channel. No genetic compositing.

## Fry
Uncertain vertices: 44151
Body vertices: 6772
LeftEye vertices: 1690
RightEye vertices: 1666
Lips vertices: 194
Fins vertices: 26944
Boundary triangles conservatively guarded: 737
Verified candidate seam groups: 6189; incompatible/unconfirmed groups: 0
Eye envelopes come from original dark-eye surface samples plus an expanded eyelid margin. Lip envelope is localized to the existing Mouth/rest muzzle. Fin domains conservatively combine explicit distal/attachment envelopes and thin-surface orientation. These are candidate selections, not independently certified anatomy. Head/cheek/fin-root uncertainty deliberately remains magenta.

## Juvenile
Uncertain vertices: 37736
Body vertices: 8601
LeftEye vertices: 5361
RightEye vertices: 5158
Lips vertices: 4051
Fins vertices: 26484
Boundary triangles conservatively guarded: 961
Verified candidate seam groups: 6758; incompatible/unconfirmed groups: 0
Eye envelopes come from original dark-eye surface samples plus an expanded eyelid margin. Lip envelope is localized to the existing Mouth/rest muzzle. Fin domains conservatively combine explicit distal/attachment envelopes and thin-surface orientation. These are candidate selections, not independently certified anatomy. Head/cheek/fin-root uncertainty deliberately remains magenta.

## Status
Exact-copy geometry/UV/skinning/blendshape checks and all vertex-label validity checks passed. Six EditMode Swim/Eat samples per stage were finite and retained the mask stream. Continuous Play Mode NOT RUN; existing mouth/fin deformation is not certified.

## Visual review and limitations
The scene-view Top/Right views show both original controls and candidates with distinct eye envelopes, fin candidates, conservative lateral body regions and extensive magenta uncertainty. Both stages retain a large uncertain dorsal/head/attachment domain; the candidate is NOT a complete or approved anatomy mask. Eye and lip envelopes and fin-root boundaries still require user confirmation, especially the small Fry lip region. No automatic threshold result is promoted to reliable anatomical separation.

- Fry: 44,151 uncertain vertices (54.2%); only 6,772 conservative body vertices (8.3%).
- Juvenile: 37,736 uncertain vertices (43.2%); only 8,601 conservative body vertices (9.8%).
- Diagnostic colors are not final genetic mask channels. Broad red patterns and neutral coloring were intentionally NOT created.
- Individual saved PNG read-back through the image inspection tool was unavailable. Multi-angle scene evidence was visually reviewed; the saved 1024x1024 side/top PNGs were checked for nonblank diagnostic content. Detailed saved-camera eye/lip certification remains pending user review.
- One existing Console error originates in the Assistant Markdown table renderer. No package changes or attempted fix were made.

## Captures
`Reports/Captures/` contains Original/MaskCandidate Left, Right, Top, LeftFace, RightFace and FinBase PNGs for each stage, plus each stage's Swim/Eat midpoint diagnostic PNGs. File names identify stage, original/candidate, view and clip. Fixed full-fish framing is matched within each stage; source Fry and Juvenile are equally magnified to similar display lengths for diagnosis, not a growth-size comparison.

## Preservation and stop
All 3,568 baseline files/.meta are byte-identical; zero missing files; zero new files outside this isolated prototype. Existing source meshes/materials/textures/rigs/clips/scenes, genetics/growth systems and Kohaku experiments remain unchanged. See `Preservation.txt` and `CreatedFiles.txt`.

**STOP for diagnostic mask review.** No appearance compositing, neutral Fry colors, Juvenile red markings, growth integration, production changes or deferred appearance step. The original PondScene is restored after the review captures.
