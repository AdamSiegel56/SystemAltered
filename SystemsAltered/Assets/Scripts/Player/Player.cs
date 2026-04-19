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
<<<<<<< HEAD
        var input = _inputActions.Gameplay;
        var deltaTime = Time.deltaTime;
        
        // Get camera input and update its rotation
        var cameraInput = new CamInput {Look = input.Look.ReadValue<Vector2>()};
=======
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

>>>>>>> main
        playerCam.UpdateRotation(cameraInput);
        playerCam.UpdatePosition(playerCharacter.GetCameraTarget());
    }

    private void UpdateCharacter(PlayerInputActions.GameplayActions gameplay)
    {
        var characterInput = new CharacterInput
        {
            Rotation = playerCam.transform.rotation,
<<<<<<< HEAD
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = input.Crouch.WasPressedThisFrame()
            ? CrouchInput.Toggle
            : CrouchInput.None
=======
            Move     = gameplay.Move.ReadValue<Vector2>(),
            Jump     = gameplay.Jump.WasPressedThisFrame(),
            Crouch   = gameplay.Crouch.WasPressedThisFrame()
                ? CrouchInput.Toggle
                : CrouchInput.None
>>>>>>> main
        };

        playerCharacter.UpdateInput(characterInput);
<<<<<<< HEAD
        playerCharacter.UpdateBody(deltaTime);
=======
        playerCharacter.UpdateBody();
    }
>>>>>>> main

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