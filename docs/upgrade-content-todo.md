# Upgrade Content TODO

Items not implemented yet. Do not describe these in upgrade cards until supported in code.

## FPS starter skill upgrades

- **Meteor Focus / Mega Meteor cooldown** upgrade is implemented (Fire Staff RMB only).
- Mega Meteor damage upgrade is a separate task.
- Mega Meteor radius / impact upgrade is a separate task.
- Mega Meteor VFX/SFX polish is a separate asset task.
- Arrow Rain cooldown upgrade is on `feature/test-team-sync`; merge separately if needed.
- Sword RMB cooldown / damage upgrades are separate tasks.
- Skill upgrade cards should be added one at a time with safe runtime hooks.

## Future starter weapon integration

- Bow / Fire Staff / Sword LMB damage upgrade cards need merge from feature branch or re-implementation on main.
- Spread Shot and Piercing Shot currently fit the support / legacy projectile path; FPS starter Bow integration is a separate task.

## Rarity / balance

- Legendary rarity will be evaluated in a separate future task.
- Rarity multiplier currently does not scale Spread Shot, Piercing Shot, or Orbiting Orb repeat picks (only stat upgrades and Rocket / Chain / Laser stack loops use multiplier).

## Pool / UX (separate tasks)

- Removing unlocked weapons from the pool, or using separate level-up vs chest pools, is a future task.
- Upgrade icons, hover preview, and exact next-value previews remain future UX work.

## Unsupported effect ideas (do not fake in descriptions)

- Movement speed, dash cooldown, crit, lifesteal, shield, and similar upgrades require new gameplay logic and are out of scope for text-only polish passes.

## Card text notes

- Fire Rate and Projectile Speed card text affects FPS starter LMB cooldown / projectile speed on main (via PlayerStats hooks).
- Damage, XP attraction, Orbiting Orb, Rocket Launcher, Chain Lightning, and Laser Beam descriptions match currently working gameplay effects.
