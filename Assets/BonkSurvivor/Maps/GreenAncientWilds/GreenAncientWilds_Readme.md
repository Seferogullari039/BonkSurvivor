# Green Ancient Wilds — Map Staging (Polytope Studio)

Staging notes for the **Polytope Studio — Low Poly Environment** import.  
This folder is documentation only. **No gameplay scene wiring yet.**

## Source (read-only)

| Path | Contents |
|------|----------|
| `Assets/Polytope Studio/Lowpoly_Environments/` | Nature meshes, materials, textures, 25 prefabs |
| `Assets/Polytope Studio/Lowpoly_Demos/Environment_Free/` | Vendor demo scene + terrain helpers |
| `Assets/Polytope Studio/Lowpoly_Village/` | Modular fence/bridge (optional ruins props) |
| `Assets/Polytope Studio/Welcome_Screen/` | Editor welcome UI (vendor) |

**Do not modify** files under `Assets/Polytope Studio/` — reference prefabs from there or duplicate into a BonkSurvivor-owned scene later.

## Demo scene

| Item | Detail |
|------|--------|
| Scene | `Assets/Polytope Studio/Lowpoly_Demos/Environment_Free/Environment_Free.unity` |
| Ground | Multi-tile **Unity Terrain** (9 tiles) with `Ground_Layer_01` / `Ground_Layer_02` terrain layers |
| Water | Demo **Plane** uses `PT_Water_mat` + `PT_Water_Shader` (custom Polytope water, not a prefab) |
| Sky | `PT_Skybox_mat` / `PT_Skybox_Texture_01` |
| Player | Demo-only `Player` + `PT_PlayerMovement` / `PT_MouseLook` (do not use in BonkSurvivor) |
| Colliders | `colliders` root, terrain colliders, demo cubes/plane mesh colliders, demo `Fence.prefab` (many mesh colliders) |

Open the demo scene in Editor to preview layout and scale. Do not add it to Build Settings.

## Environment prefab inventory (25)

Paths under `Assets/Polytope Studio/Lowpoly_Environments/Prefabs/`.

### Ground / terrain

| Asset | Notes |
|-------|-------|
| Demo terrains | `Lowpoly_Demos/.../Helpers/Terrain/*.asset` — sculpted tiles, not reusable prefabs |
| `PT_Terrain_mat` | Terrain splat material reference |
| `PT_Ground_Grass_Green_01.png`, `PT_Ground_Generic_03.png` | Ground textures for terrain layers |

BonkSurvivor will likely use a **single custom terrain** or large ground mesh — not the demo’s 9-tile grid.

### Trees (14)

- `PT_Pine_Tree_03_green`, `_dead`, `_stump`, `_logs`, `_green_cut`, `_dead_cut`
- `PT_Fruit_Tree_01_green`, `_apples`, `_pears`, `_plums`, `_dead`, `_stump`, `_logs`, `_green_cut`, `_dead_cut`

LOD0/LOD1 mesh renderers; **no colliders** on environment prefabs.

### Rocks (5)

- `PT_Generic_Rock_01`
- `PT_Ore_Rock_01`, `PT_Ore_Rock_01_split`
- `PT_River_Rock_Pile_02`
- `PT_Menhir_Rock_02` (standing stone / ruin feel)

### Grass / plants / flowers (3)

- `PT_Grass_02` (low grass clump)
- `PT_Poppy_02` (flower)
- Demo helpers: `PT_Grass_02_v1/v2`, `PT_High_Grass_02_v1` (scaled variants in demo folder)

### Shrubs (2)

- `PT_Generic_Shrub_01_green`, `PT_Generic_Shrub_01_dead`

### Mushrooms (1)

- `PT_Caesars_Mushroom_01`

### Water (material only)

- `PT_Water_mat`, `PT_Water_Shader`, `PT_Water_NM_01.png`
- Apply to a plane/mesh in a future BonkSurvivor scene; no water prefab in the free pack.

### Ruin / prop (optional — Village pack)

| Prefab | Collider |
|--------|----------|
| `PT_Modular_Fence_Wood_01/02/03` | MeshCollider on segments |
| `PT_Modular_Gate_Wood_01` | MeshCollider |
| `PT_Wooden_Bridge_02` | MeshCollider |

`PT_Runes_02.png` texture available for future decal/sign use.

## Materials & shaders (environment)

`Lowpoly_Environments/Sources/Materials/`: terrain, grass, rocks, tree trunks/leaves, poppy, mushrooms, fruit foliage, skybox, water.

Custom shaders: `PT_Water_Shader`, `PT_Rock_Shader`, `PT_Vegetation_Foliage_Shader`, `PT_Vegetation_Flowers_Shader`, `PT_Vegetation_Opaque_Shader`.

URP `.unitypackage` variants live in `Lowpoly_Environments/URP/` (already extracted in project).

## Collider risks

| Source | Risk |
|--------|------|
| **Environment prefabs** (trees, rocks, grass, etc.) | **No colliders** — safe as pure visuals until we add blocking colliders intentionally |
| **Demo scene** | TerrainCollider on all terrain tiles; invisible blocking cubes; water plane has MeshCollider |
| **Demo Fence helper** | Heavy MeshCollider chain — do not copy into gameplay scene as-is |
| **Village fence/bridge** | MeshColliders per segment — strip or replace before player/enemy nav |

For BonkSurvivor: prefer **TerrainCollider** or a single simplified ground collider; add rock/tree blockers only where needed for gameplay bounds.

## Safe next steps (not done yet)

1. Create `Assets/BonkSurvivor/Maps/GreenAncientWilds/GreenAncientWilds_Staging.unity` (empty or art-only).
2. Place environment prefabs by reference; tune density for survivor camera (top-down / FPS).
3. Add one terrain or ground plane; assign `PT_Terrain_mat` / grass layers from demo helpers.
4. Optional water plane with `PT_Water_mat` away from spawn until gameplay needs it.
5. **Do not** wire into main game scene, spawners, player spawn, or Build Settings until art pass is approved.
6. Add Polytope credit to `Assets/ThirdPartyCredits.md` when map goes live (if not already present).
7. Profile draw calls / batching before enabling on low-end targets.

## Gameplay boundary

**Untouched by this staging pass:** player, weapons, enemies, chests, boss, events, HUD, upgrades, URP settings, packages.
