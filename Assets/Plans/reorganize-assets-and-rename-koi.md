# Project Overview
- Game Title: Koi Pond
- High-Level Concept: Relaxing, stylized 3D koi pond simulation with realistic, organic swimming dynamics and camera controls.
- Players: Single player
- Inspiration / Reference Games: Koi pond relaxation apps, zen garden simulators.
- Tone / Art Direction: Serene, stylized 3D aesthetic with water shaders and natural foliage.
- Target Platform: PC (StandaloneWindows64)
- Screen Orientation / Resolution: Landscape 1920x1080
- Render Pipeline: URP (PC_RPAsset)

# Game Mechanics
## Core Gameplay Loop
The player observes and interacts with organic koi fish swimming peacefully in a garden pond basin. The player can feed the fish (triggering natural surface eating behaviors), orbit and zoom the camera around the pond, and watch the fish glide, turn, bank, and drift autonomously within natural water depth levels and pond bed constraints.

## Controls and Input Methods
- Camera Orbit: Right-mouse click + drag, A/D or Arrow keys.
- Camera Zoom: Mouse scroll wheel, Q/E keys.
- Feeding: E, Space key, or Left-mouse click.
- State Toggle: T key to toggle swimming vs idle hovering.

# UI
- On-screen GUI overlay displaying Koi status (swimming/idle, current water depth) with buttons for feeding and state toggling.
- Camera control hint overlay in top-right screen corner.

# Key Asset & Context
- Target Rename:
  - Scene GameObject `KoiFish` in `Assets/Scenes/PondScene.unity` -> `Koi_Stage3_Adult`
  - Prefab `Assets/Koi Fish Models/Prefabs/KoiFish.prefab` -> `Koi_Stage3_Adult.prefab`
- Folder Reorganization (`Standard Clean Structure`):
  - Consolidate game assets out of loose folders into clean, standard Unity structure:
    - `Assets/_Project/` or standard top-level structure:
      - `Assets/Scripts/`: Moving scripts (`KoiFishController.cs`, `PondCameraController.cs`) out of `Assets/Koi Fish Models/Scripts/`.
      - `Assets/Models/Koi/`: Moving models, meshes, animations, materials from `Assets/Koi Fish Models/` into `Assets/Models/Koi/`.
      - `Assets/Prefabs/`: Relocating prefabs (`Koi_Stage3_Adult.prefab`).
      - `Assets/Input/`: Moving `InputSystem_Actions.inputactions`.
    - Retain 3rd-party assets (`Assets/Downloaded Assets/`, `Assets/AI Toolkit/`, `Assets/TutorialInfo/`, `Assets/Settings/`, `Assets/Scenes/`) safely in place without breaking dependencies.
    - Remove emptied legacy folder `Assets/Koi Fish Models`.
- References to verify & preserve:
  - `PondScene.unity` references to the renamed prefab, scripts, and controllers.
  - Script GUIDs preserved using `AssetDatabase.MoveAsset` so Inspector references do not break.

# Implementation Steps
1. **Rename Prefab & Scene GameObject**:
   - Use `AssetDatabase.RenameAsset("Assets/Koi Fish Models/Prefabs/KoiFish.prefab", "Koi_Stage3_Adult")`.
   - Rename the `KoiFish` GameObject in `PondScene.unity` to `Koi_Stage3_Adult`.
   - Dependency: None.
2. **Create Standard Target Directories**:
   - Ensure `Assets/Scripts/`, `Assets/Prefabs/`, `Assets/Models/Koi/`, `Assets/Input/` directories exist using `AssetDatabase.CreateFolder`.
   - Dependency: Step 1.
3. **Move Assets via AssetDatabase**:
   - Move scripts (`KoiFishController.cs`, `PondCameraController.cs`) to `Assets/Scripts/`.
   - Move the renamed prefab `Koi_Stage3_Adult.prefab` to `Assets/Prefabs/`.
   - Move `InputSystem_Actions.inputactions` to `Assets/Input/`.
   - Move koi model FBX files (`Koi_Stage3_Adult.fbx`, `Koi_Stage3_Adult_Hirenaga.fbx`), animations, materials, meshes, and textures into `Assets/Models/Koi/`.
   - Clean up empty source directories under `Assets/Koi Fish Models/`.
   - Dependency: Step 2.
4. **Save & Refresh AssetDatabase**:
   - Run `AssetDatabase.SaveAssets()` and save `PondScene.unity`.
   - Dependency: Step 3.

# Verification & Testing
- Verify through C# script query that `Koi_Stage3_Adult` exists both as a scene GameObject in `PondScene` and as a prefab at `Assets/Prefabs/Koi_Stage3_Adult.prefab`.
- Verify that `KoiFishController` and `PondCameraController` components in `PondScene` remain intact with zero missing script references.
- Verify that all materials, textures, and animator controllers on `Koi_Stage3_Adult` maintain valid references.
- Check Unity Console for zero errors or warnings after folder reorganization.
