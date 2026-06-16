# Upgrade Content TODO

Items not implemented yet. Do not describe these in upgrade cards until supported in code.

## FPS starter skill upgrades

### Implemented

- **Meteor Focus / Mega Meteor cooldown** (Fire Staff RMB).
- **Whirlwind Training / Sword skill cooldown** (Sword RMB).
- **Arrow Storm / Arrow Rain damage** (Bow RMB).
- **Inferno Ritual / Mega Meteor damage** (Fire Staff RMB).
- **Blade Tempest / Sword skill damage** (Sword RMB).

### Separate tasks

- Skill radius / range / duration / hit count upgrade cards.
- Skill VFX/SFX polish (asset task).
- Arrow Rain cooldown upgrade is on `feature/test-team-sync`; merge separately if needed.
- Weapon mastery / unlock tree progression — future large progression task.
- Skill upgrade cards should continue one at a time with safe runtime hooks.

## Future starter weapon integration

- Bow / Fire Staff / Sword LMB damage upgrade cards need merge from feature branch or re-implementation on main.
- Spread Shot and Piercing Shot currently fit the support / legacy projectile path; FPS starter Bow integration is a separate task.

## Rarity / balance

- Legendary rarity will be evaluated in a separate future task.
- Rarity multiplier currently does not scale Spread Shot, Piercing Shot, or Orbiting Orb repeat picks (only stat upgrades and Rocket / Chain / Laser stack loops use multiplier).

## Pool / UX (separate tasks)

- **Upgrade icon system:** card icons load from `Resources/UpgradeIcons/{iconKey}.png`. Real sprite assets can be added later; missing icons use safe emoji fallback without console errors.
- **Level-up pool balance (main):** weighted picks favor core stat cards (Fire Rate, Projectile Speed, XP Attraction, Global Damage) during levels 1–5. Skill cooldown/damage cards (index 10–14) stay low-weight early. Rocket / Chain / Laser can still appear but lose weight if another support unlock is already on the same menu. Epic roll rate unchanged (5%). Chest menus use the same picker.
- Removing unlocked weapons from the pool, or using separate level-up vs chest pools, is a future task.
- Hover preview and exact next-value previews remain future UX work.

## Unsupported effect ideas (do not fake in descriptions)

- Movement speed, dash cooldown, crit, lifesteal, shield, and similar upgrades require new gameplay logic and are out of scope for text-only polish passes.

## Card text notes

- Fire Rate and Projectile Speed card text affects FPS starter LMB cooldown / projectile speed on main (via PlayerStats hooks).
- Damage, XP attraction, Orbiting Orb, Rocket Launcher, Chain Lightning, and Laser Beam descriptions match currently working gameplay effects.
