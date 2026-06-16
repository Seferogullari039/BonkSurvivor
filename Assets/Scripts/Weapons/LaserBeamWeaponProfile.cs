using UnityEngine;

public class LaserBeamWeaponProfile : MonoBehaviour
{
    [SerializeField] private AudioClip laserFireClip;
    [SerializeField] private float laserVolume = 1.1f;

    public AudioClip LaserFireClip => laserFireClip;
    public float LaserVolume => laserVolume;
}
