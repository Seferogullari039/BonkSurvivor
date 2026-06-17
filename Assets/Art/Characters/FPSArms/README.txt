FPS Arms / Hand ViewModel — Asset Prep (no gameplay binding)

Drop a first-person arms or hands model here, then run:
Tools > BonkSurvivor > Build FPS Arms ViewModel Assets

Preferred layout:
  Models/     FBX or OBJ source (arms/hands only — not full body)
  Textures/   Optional albedo/normal maps referenced by the model

Output prefab (when a model is present):
  Assets/Prefabs/Characters/FPS_Arms_ViewModel.prefab

Rules:
  Visual-only viewmodel. Colliders disabled. No active Rigidbody.
  Animator kept on asset but disabled until a later milestone.
  Not wired to WeaponMount / ViewModelRoot yet.

Credits:
  If the asset uses CC Attribution, add one entry to Assets/ThirdPartyCredits.md
  (do not duplicate existing credits).
