# Upgrade Content TODO

Items not implemented yet. Do not describe these in upgrade cards until supported in code.

## Rarity / balance

- Legendary rarity tier (enum + roll rates + card color).
- Rarity multiplier currently does not scale Spread Shot, Piercing Shot, or Orbiting Orb repeat picks (only stat upgrades and Rocket / Chain / Laser stack loops use multiplier).
- Consider showing a small "rarity has no extra effect" note on weapon unlock cards if design stays this way.

## FPS vs legacy auto weapons

- Fire Rate upgrade still primarily affects the legacy auto-aim `ProjectileWeapon` path on this branch.
- Projectile Speed upgrade affects legacy `Projectile.cs` and FPS Bow / Fire Staff LMB projectiles.
- Bow / Fire Staff / Sword **basic attack (LMB) damage** upgrade cards are implemented (Sharpened Arrows, Ember Core, Honed Blade).
- **Rain Caller / Arrow Rain cooldown** upgrade is implemented (Bow RMB only).
- Arrow Rain damage upgrade is a separate task.
- Arrow Rain duration / hit count upgrades are separate tasks.
- Mega Meteor cooldown / damage upgrades are separate tasks.
- Sword RMB cooldown / damage upgrades are separate tasks.
- Skill upgrade cards should be added one at a time with safe runtime hooks.
- Spread / Pierce FPS starter integration is still a separate task.

## Content / UX ideas

- Unified Turkish or English naming for all upgrade titles.
- Show exact next values on card (rocket AoE radius, chain target count preview on first pick).
- Separate upgrade pools for level-up vs chest rewards.
- Duplicate weapon picks removed from pool after unlock (currently only early-level bias, not full exclusion).
- Upgrade icons per card.
- Card hover preview of current player stats.
- Weapon mastery / unlock tree progression — future large progression task.

## Unsupported effect ideas (do not fake in descriptions)

- Crit chance / crit damage upgrades.
- Lifesteal or shield upgrades.
- Movement speed or dash upgrades via level-up menu.
- Skill-specific damage upgrades (Arrow Rain, Mega Meteor, Whirlwind) until wired in code.
