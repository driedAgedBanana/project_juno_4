using UnityEngine;

public class WeaponSwayAndBob : MonoBehaviour
{
    public static WeaponSwayAndBob Instance;
    private IWeapon currentWeapon;

    private Vector2 moveInput;
    private Vector2 lookInput;

    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    private Vector3 _swayPosition;

    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    private Vector3 _swayEulerRotation;

    public float smoothness = 10f;
    private float _smoothRotation = 12f;

    private float _aimSmoothness = 15f;
    private float _aimSmoothRotation = 12f;

    [Header("Bobbing")]
    public float speedCurve;
    private float _curveSin { get => Mathf.Sin(speedCurve); }
    private float _curveCos { get => Mathf.Cos(speedCurve); }

    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobbingLimit = Vector3.one * 0.01f;
    private Vector3 _bobbingPosition;

    public float bobbingExaggeration;

    [Header("BobbingRotation")]
    public Vector3 multiplier;
    private Vector3 _bobbingEulerRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        speedCurve = 0;
    }

    // Update is called once per frame
    void Update()
    {
        GetInput();

        WeaponSway();
        SwayRotation();
        BobbingOffset();
        BobbingRotation();

        CompositePositionRotation();
    }

    private void GetInput()
    {
        // Getting the input information from the playermovement script
        moveInput = PlayerController.Instance.GetMovementInput().normalized;
        lookInput = PlayerController.Instance.GetLookInput();
    }

    public void SetCurrentWeapon(IWeapon weapon)
    {
        currentWeapon = weapon;
    }

    private void WeaponSway()
    {
        float aimMultiplier = (currentWeapon != null && currentWeapon.IsAiming) ? 0.3f : 1f;

        // Multiplies the mouse input by rotationStep so the weapon move the opposite the camera / mouse movement
        Vector3 invertLook = lookInput * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance * aimMultiplier, maxStepDistance * aimMultiplier); // Clamp to prevent it moves too far
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance * aimMultiplier, maxStepDistance * aimMultiplier);

        _swayPosition = invertLook; // Store the sway position offset
    }

    private void SwayRotation()
    {
        float aimMultiplier = (currentWeapon != null && currentWeapon.IsAiming) ? 0.3f : 1f;

        // The same for Weapon sway but for rotation instead of position
        Vector2 invertLook = lookInput * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep * aimMultiplier, maxRotationStep * aimMultiplier);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep * aimMultiplier, maxRotationStep * aimMultiplier);

        _swayEulerRotation = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    private void CompositePositionRotation()
    {
        if (currentWeapon == null) return;

        if (currentWeapon.IsAiming)
        {
            // Lock to steady aim position/rotation
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.deltaTime * _aimSmoothRotation);
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, Time.deltaTime * _aimSmoothness);
        }
        else
        {
            // Normal sway & bob
            transform.localPosition = Vector3.Lerp(transform.localPosition, _swayPosition + _bobbingPosition, Time.deltaTime * smoothness);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(_swayEulerRotation) * Quaternion.Euler(_bobbingEulerRotation), Time.deltaTime * _smoothRotation);
        }
    }


    private void BobbingOffset()
    {
        //if (currentWeapon != null && currentWeapon.IsAiming) return;

        // Increases speedCurve over time -> drives bobbing animation. Bobbing is stronger if player is moving and grounded
        speedCurve += Time.deltaTime * (PlayerController.Instance.IsGrounded() ? (Mathf.Abs(moveInput.x) + Mathf.Abs(moveInput.y)) * bobbingExaggeration : 1f) + 0.01f;

        // Calculates X/Y/Z offsets based on sine/cosine waves.
        _bobbingPosition.x = (_curveCos * bobbingLimit.x * (PlayerController.Instance.IsGrounded() ? 1 : 0) - moveInput.x * travelLimit.x);
        _bobbingPosition.y = (_curveSin * bobbingLimit.y - (moveInput.y * travelLimit.y));
        _bobbingPosition.z = -(moveInput.y * travelLimit.z);
    }

    private void BobbingRotation()
    {
        //if (currentWeapon != null && currentWeapon.IsAiming) return;

        // Tilts weapon slightly when walking. If standing still, the effect is smaller.
        _bobbingEulerRotation.x = (moveInput != Vector2.zero ? multiplier.x * (Mathf.Sin(2 * speedCurve)) : multiplier.x * (Mathf.Sin(2 * speedCurve) / 2));
        _bobbingEulerRotation.y = ((moveInput != Vector2.zero ? multiplier.y * _curveCos : 0));
        _bobbingEulerRotation.z = ((moveInput != Vector2.zero ? multiplier.z * _curveCos * PlayerController.Instance.GetMovementInput().x : 0));
    }
}
