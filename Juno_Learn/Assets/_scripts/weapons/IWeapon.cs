using UnityEngine;

public interface IWeapon
{
    bool IsAiming { get; }
    Transform WeaponTransform { get; }
}
