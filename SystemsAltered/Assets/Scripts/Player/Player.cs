using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCam playerCam;
    
    private PlayerInputActions _inputActions;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
        
        playerCharacter.Initialize();
        playerCam.Initialize(playerCharacter.GetCameraTarget());
    }

    void OnDestroy()
    {
        _inputActions.Dispose();
    }

    void Update()
    {
        var input = _inputActions.Gameplay;
        
        // Get camera input and update its rotation
        var cameraInput = new CamInput {Look = input.Look.ReadValue<Vector2>()};
        playerCam.UpdateRotation(cameraInput);
        playerCam.UpdatePosition(playerCharacter.GetCameraTarget());
        
        // Get character input and update it
        var characterInput = new CharacterInput()
        {
            Rotation = playerCam.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame()
        };
        playerCharacter.UpdateInput(characterInput);
    }
}
