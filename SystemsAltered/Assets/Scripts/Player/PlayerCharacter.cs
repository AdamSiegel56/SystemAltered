using UnityEngine;
using KinematicCharacterController;

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [Space]
    
    [SerializeField] private float walkSpeed = 20.0f;
    [Space]
    
    [SerializeField] private float jumpSpeed = 20.0f;
    [SerializeField] private float gravity = -90.0f;
    
    private Quaternion requestedRotation;
    private Vector3 requestedMovement;
    private bool requestedJump;
    
    public void Initialize()
    {
        motor.CharacterController = this;
    }

    public void UpdateInput(CharacterInput input)
    {
        requestedRotation = input.Rotation;
        
        requestedMovement = new Vector3(input.Move.x, 0, input.Move.y);
        requestedMovement = Vector3.ClampMagnitude(requestedMovement, 1.0f);
        requestedMovement = input.Rotation * requestedMovement;
        
        requestedJump = requestedJump || input.Jump;
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
        
            currentVelocity = groundMovement * walkSpeed;
        }
        else
        {
            currentVelocity += motor.CharacterUp * (gravity * deltaTime);
            
        }

        if (requestedJump)
        {
            requestedJump = false;

            motor.ForceUnground(0.0f);
            
            currentVelocity += motor.CharacterUp * jumpSpeed;
        }
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
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
