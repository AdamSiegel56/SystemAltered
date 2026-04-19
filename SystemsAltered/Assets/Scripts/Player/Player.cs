using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCam playerCam;
    [SerializeField] private Gun gun;

    private PlayerInputActions _inputActions;

    // --- Lifecycle ---

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _inputActions = new PlayerInputActions();
        _inputActions.Enable();

        playerCharacter.Initialize();
        playerCam.Initialize(playerCharacter.GetCameraTarget());
        gun.Initialize();
    }

    private void OnDestroy()
    {
        _inputActions.Dispose();
    }

    private void Update()
    {
        var gameplay = _inputActions.Gameplay;

        UpdateCamera(gameplay);
        UpdateCharacter(gameplay);
        UpdateGun(gameplay);
    }

    // --- Per-system input handlers ---

    private void UpdateCamera(PlayerInputActions.GameplayActions gameplay)
    {
        var cameraInput = new CamInput
        {
            Look = gameplay.Look.ReadValue<Vector2>()
        };

        playerCam.UpdateRotation(cameraInput);
        playerCam.UpdatePosition(playerCharacter.GetCameraTarget());
    }

    private void UpdateCharacter(PlayerInputActions.GameplayActions gameplay)
    {
        var characterInput = new CharacterInput
        {
            Rotation = playerCam.transform.rotation,
            Move     = gameplay.Move.ReadValue<Vector2>(),
            Jump     = gameplay.Jump.WasPressedThisFrame(),
            Crouch   = gameplay.Crouch.WasPressedThisFrame()
                ? CrouchInput.Toggle
                : CrouchInput.None
        };

        playerCharacter.UpdateInput(characterInput);
        playerCharacter.UpdateBody();
    }

    private void UpdateGun(PlayerInputActions.GameplayActions gameplay)
    {
        var gunInput = new GunInput
        {
            Shoot  = gameplay.Shoot.IsPressed(),
            Reload = gameplay.Reload.WasPressedThisFrame()
        };

        gun.UpdateInput(gunInput);
    }
}