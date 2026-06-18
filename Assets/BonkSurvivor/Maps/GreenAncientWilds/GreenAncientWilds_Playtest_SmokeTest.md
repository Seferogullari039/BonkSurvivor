# Green Ancient Wilds — Playtest Smoke Test

Scene: `Assets/BonkSurvivor/Maps/GreenAncientWilds/GreenAncientWilds_Playtest.unity`

Open this scene in the Editor (do not modify `SampleScene.unity`).

## Display

- [ ] Game view **1920×1080**
- [ ] UI Scale **1x**

## Play Mode

- [ ] **Play** from menu → **Play Game** (or existing run flow)
- [ ] **Player movement** works (WASD / stick)
- [ ] **Enemy spawn** occurs after run starts
- [ ] **Chest** open / pickup flow unchanged
- [ ] **Boss** spawn path — Console has no red errors
- [ ] **Void portal / event** systems — Console has no red errors
- [ ] Player does **not** snag on Polytope props (visuals have colliders disabled)
- [ ] Center combat area feels **open** (~25m clear); props stay on perimeter

## Scene markers (editor reference only)

| Object | Purpose |
|--------|---------|
| `PlayerStart` | Default spawn reference |
| `BossArena` | North boss staging |
| `ChestZone_A` / `ChestZone_B` | Chest placement guides |
| `PortalEventArea` | Portal event staging |

Markers are empty transforms — **no gameplay wiring yet**.

## Rebuild scene

`Tools → BonkSurvivor → Build Green Ancient Wilds Playtest Scene`

Copies gameplay setup from `SampleScene`, adds Polytope dressing + markers. Does not edit gameplay scripts.
