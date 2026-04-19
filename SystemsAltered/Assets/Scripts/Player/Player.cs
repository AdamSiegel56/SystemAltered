using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCam playerCam;
    [SerializeField] private Gun gun;
    
    private PlayerInputActions _inputActions;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
        
        playerCharacter.Initialize();
        playerCam.Initialize(playerCharacter.GetCameraTarget());
        gun.Initialize();
    }

    void OnDestroy()
    {
        _inputActions.Dispose();
    }

    void Update()
    {
        var input = _inputActions.Gameplay;
        var deltaTime = Time.deltaTime;
        
        // Get camera input and update its rotation
        var cameraInput = new CamInput {Look = input.Look.ReadValue<Vector2>()};
        playerCam.UpdateRotation(cameraInput);
        playerCam.UpdatePosition(playerCharacter.GetCameraTarget());
        
        // Get character input and update it
        var characterInput = new CharacterInput()
        {
            Rotation = playerCam.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = input.Crouch.WasPressedThisFrame()
            ? CrouchInput.Toggle
            : CrouchInput.None
        };
        playerCharacter.UpdateInput(characterInput);
        playerCharacter.UpdateBody(deltaTime);

        var gunInput = new GunInput
        {
            Shoot = input.Shoot.IsPressed(),
            Reload = input.Reload.WasPressedThisFrame()
        };

        gun.UpdateInput(gunInput);
    }
}
