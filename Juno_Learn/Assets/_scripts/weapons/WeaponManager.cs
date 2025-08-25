using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public WeaponSwayAndBob swayAndBob;

    [Header("Settings")]
    [SerializeField] private float switchTime = 0.2f;

    private Transform[] _weapons;
    private int _currentWeaponIndex = 0;
    private float _timeSinceLastSwitch = 0f;

    private void Start()
    {
        SetWeapons();
        Select(_currentWeaponIndex);
    }

    private void Update()
    {
        _timeSinceLastSwitch += Time.deltaTime;
    }

    private void SetWeapons()
    {
        _weapons = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            _weapons[i] = transform.GetChild(i);
        }
    }

    // Called by Input System for scroll wheel
    public void OnSwitchingWeapon(InputAction.CallbackContext ctx)
    {
        if (_timeSinceLastSwitch < switchTime) return; // prevent fast switching

        Vector2 scrollValue = ctx.ReadValue<Vector2>();
        if (scrollValue.y > 0.01f)
        {
            NextWeapon();
        }
        else if (scrollValue.y < -0.01f)
        {
            PreviousWeapon();
        }
    }

    // Called by Input System for number keys
    public void OnSelectWeaponByNumber(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (_timeSinceLastSwitch < switchTime) return;

        // Assuming you bind keys 1, 2, 3... to send the index as float
        float numberPressed = ctx.ReadValue<float>(); // e.g., pressing "1" gives 1
        int weaponIndex = Mathf.RoundToInt(numberPressed) - 1;

        if (weaponIndex >= 0 && weaponIndex < _weapons.Length)
        {
            Select(weaponIndex);
        }
    }

    private void NextWeapon()
    {
        _currentWeaponIndex = (_currentWeaponIndex + 1) % _weapons.Length;
        Select(_currentWeaponIndex);
    }

    private void PreviousWeapon()
    {
        _currentWeaponIndex = (_currentWeaponIndex - 1 + _weapons.Length) % _weapons.Length;
        Select(_currentWeaponIndex);
    }

    private void Select(int weaponIndex)
    {
        for (int i = 0; i < _weapons.Length; i++)
        {
            _weapons[i].gameObject.SetActive(i == weaponIndex);
        }

        _timeSinceLastSwitch = 0f;
        _currentWeaponIndex = weaponIndex;

        // Pass new weapon to sway system
        EquippingWeapon(weaponIndex);
    }

    public void EquippingWeapon(int index)
    {
        IWeapon weapon = _weapons[index].GetComponent<IWeapon>();
        if (weapon != null)
        {
            swayAndBob.SetCurrentWeapon(weapon);
        }
    }
}
