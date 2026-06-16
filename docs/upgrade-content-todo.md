# Upgrade Content TODO

Items not implemented yet. Do not describe these in upgrade cards until supported in code.

## Rarity / balance

- Legendary rarity tier (enum + roll rates + card color).
- Rarity multiplier currently does not scale Spread Shot, Piercing Shot, or Orbiting Orb repeat picks (only stat upgrades and Rocket / Chain / Laser stack loops use multiplier).
- Consider showing a small "rarity has no extra effect" note on weapon unlock cards if design stays this way.

## FPS vs legacy auto weapons

- Fire Rate and Projectile Speed upgrades affect the legacy auto-aim `ProjectileWeapon` path.
- `ProjectileWeapon.Fire()` returns early while FPS mode is active, so these two upgrades may not help starter Bow / Staff / Sword runs until a FPS-facing effect exists.

## Content / UX ideas

- Unified Turkish or English naming for all upgrade titles.
- Show exact next values on card (rocket AoE radius, chain target count preview on first pick).
- Separate upgrade pools for level-up vs chest rewards.
- Duplicate weapon picks removed from pool after unlock (currently only early-level bias, not full exclusion).
- Upgrade icons per card.
- Card hover preview of current player stats.

## Unsupported effect ideas (do not fake in descriptions)

- Crit chance / crit damage upgrades.
- Lifesteal or shield upgrades.
- Fire Staff / Bow / Sword specific upgrade cards.
- Movement speed or dash upgrades via level-up menu.
