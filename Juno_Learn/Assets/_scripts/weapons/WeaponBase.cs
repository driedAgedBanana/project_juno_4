using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponBase : MonoBehaviour, IWeapon
{
    private bool _isAlive;

    private bool _isAiming;
    public bool IsAiming => _isAiming;
    public Transform WeaponTransform => weapon;

    [Header("Aiming")]
    public Camera mainCam;
    [HideInInspector] public bool isAiming => _isAiming;
    public Transform weapon;
    public Transform defaultPosition;
    public Transform aimingPosition;
    public float aimSpeed;
    protected float aimTime;

    // Zooming when ADS
    public int zoomInFOV;
    public int normalFOV;
    public float smoothness;

    private bool _isZooming;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _isAlive = PlayerController.Instance.isAlive;
    }

    // Update is called once per frame
    void Update()
    {
        if (_isAlive)
        {
            Aiming();
        }
    }

    public void Aiming()
    {
        Transform targetTransform = _isAiming ? aimingPosition : defaultPosition;
        float targetPOV = _isAiming ? zoomInFOV : normalFOV;

        // Smoothly transtition aiming time between 0 and 1
        aimTime = Mathf.Clamp01(aimTime + Time.deltaTime * aimSpeed * (_isAiming ? 1 : -1));

        // Lerp the weapon's position and rotation smoothly between default and aiming positions
        weapon.position = Vector3.Lerp(defaultPosition.position, aimingPosition.position, aimTime);
        weapon.rotation = Quaternion.Slerp(defaultPosition.rotation, aimingPosition.rotation, aimTime);

        // Camera POV transistion between aiming or not
        float currentPOV = mainCam.fieldOfView;
        mainCam.fieldOfView = Mathf.SmoothDamp(currentPOV, targetPOV, ref smoothness, 0.1f);
    }

    public void OnAim(InputAction.CallbackContext ctx)
    {
        _isAiming = ctx.ReadValue<float>() > 0;
    }
}
