using System.Net.Http.Headers;
using UnityEngine;
using KinematicCharacterController;

public enum CrouchInput
{
    None, 
    Toggle
}

public enum Stance
{
    Stand,
    Crouch
}

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public CrouchInput Crouch;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;
    [Space]
    
    [SerializeField] private float walkSpeed = 20.0f;
    [SerializeField] private float crouchSpeed = 7.0f;
    [Space]
    
    [SerializeField] private float jumpSpeed = 20.0f;
    [SerializeField] private float gravity = -90.0f;
    [Space]
    
    [SerializeField] private float standHeight = 2.0f;
    [SerializeField] private float crouchHeight = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float standCamTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCamTargetHeight = 0.7f;
    
    private Stance _stance;
    
    private Quaternion requestedRotation;
    private Vector3 requestedMovement;
    private bool requestedJump;
    private bool requestedCrouch;
    
    public void Initialize()
    {
        _stance = Stance.Stand;
        motor.CharacterController = this;
    }

    public void UpdateInput(CharacterInput input)
    {
        requestedRotation = input.Rotation;
        
        requestedMovement = new Vector3(input.Move.x, 0, input.Move.y);
        requestedMovement = Vector3.ClampMagnitude(requestedMovement, 1.0f);
        requestedMovement = input.Rotation * requestedMovement;
        
        requestedJump = requestedJump || input.Jump;
        requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !requestedCrouch,
            CrouchInput.None => requestedCrouch,
        };
    }

    public void UpdateBody()
    {
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;
        var camTargetHeight = currentHeight *
        (
            _stance == Stance.Stand 
                ? standCamTargetHeight 
                : crouchCamTargetHeight
        );
        var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);
        
        cameraTarget.localPosition = new Vector3(0f, camTargetHeight, 0f);
        root.localScale = rootTargetScale;
    }
    

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane
            (
                requestedRotation * Vector3.forward, 
                motor.CharacterUp
            );
        
        if(forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // if on the ground
        if (motor.GroundingStatus.IsStableOnGround)
        {
            var groundMovement = motor.GetDirectionTangentToSurface
            (
                direction: requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            );
            
            var speed =_stance == Stance.Stand 
                ? walkSpeed 
                : crouchSpeed;
        
            currentVelocity = groundMovement * speed;
        }
        else
        {
            currentVelocity += motor.CharacterUp * (gravity * deltaTime);
            
        }

        if (requestedJump)
        {
            requestedJump = false;

            motor.ForceUnground(0.0f);
            
            var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
            
            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
        }
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Crouch
        if (requestedCrouch && _stance == Stance.Stand)
        {
            _stance = Stance.Crouch;
            motor.SetCapsuleDimensions
                (
                    radius: motor.Capsule.radius,
                    height: crouchHeight,
                    yOffset: crouchHeight * 0.5f
                );
        }
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        // Uncrouch
        if (!requestedCrouch && _stance is not Stance.Stand)
        {
            _stance = Stance.Stand;
            motor.SetCapsuleDimensions
            (
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
            );
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return coll;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
        Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
    
    public Transform GetCameraTarget() => cameraTarget;
}
