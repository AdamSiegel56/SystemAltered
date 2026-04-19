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
    public bool JumpSustain;
    public CrouchInput Crouch;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [Header("References")]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;
    [Space]
    
    [SerializeField] private float walkSpeed = 20.0f;
    [SerializeField] private float crouchSpeed = 7.0f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchResponse = 25f;
    [Space] 
    
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;
    [Space]
    
    
    [SerializeField] private float jumpSpeed = 20.0f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    
    [SerializeField] private float gravity = -90.0f;
    [Space]
    
    [SerializeField] private float standHeight = 2.0f;
    [SerializeField] private float crouchHeight = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float standCamTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCamTargetHeight = 0.7f;

    [SerializeField] private float crouchHeightResponse = 15f;
    
    private Stance _stance;
    
    private Quaternion requestedRotation;
    private Vector3 requestedMovement;
    private bool requestedJump;
    private bool requestedSustainedJump;
    private bool requestedCrouch;
    
    private Collider[] uncrouchOverlapResults;
    
    public void Initialize()
    {
        _stance = Stance.Stand;
        uncrouchOverlapResults = new Collider[8];
        
        motor.CharacterController = this;
    }

    public void UpdateInput(CharacterInput input)
    {
        requestedRotation = input.Rotation;
        
        requestedMovement = new Vector3(input.Move.x, 0, input.Move.y);
        requestedMovement = Vector3.ClampMagnitude(requestedMovement, 1.0f);
        requestedMovement = input.Rotation * requestedMovement;
        
        requestedJump = requestedJump || input.Jump;
        requestedSustainedJump = input.JumpSustain;
        requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None   => _requestedCrouch,
            _                  => _requestedCrouch
        };
    }

    public void UpdateBody(float deltaTime)
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
        
        cameraTarget.localPosition = Vector3.Lerp
        (
            a: cameraTarget.localPosition,
            b: new Vector3(0f, camTargetHeight, 0f),
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
        root.localScale = Vector3.Lerp
        (
            a: root.localScale,
            b: rootTargetScale,
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
    }

    /// <summary>
    /// Queue an external impulse to be applied on the next velocity update.
    /// Used by systems like RagePullSystem.
    /// </summary>
    public void AddExternalImpulse(Vector3 impulse)
    {
        _externalImpulse += impulse;
    }

    // --- ICharacterController ---

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane(
            _requestedRotation * Vector3.forward,
            motor.CharacterUp
        );

        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
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
            
            var response = _stance == Stance.Stand
                ? walkResponse 
                : crouchResponse;
        
            var targetVelocity = groundMovement * speed;
            currentVelocity =  Vector3.Lerp
                (
                    a: currentVelocity, 
                    b: targetVelocity, 
                    t: 1f - Mathf.Exp(-response * deltaTime)
                );
        }
        else
        {
            // Air Control
            if (requestedMovement.sqrMagnitude > 0f)
            {
                var planarMovement = Vector3.ProjectOnPlane
                    (
                        requestedMovement, 
                        motor.CharacterUp
                    ) * requestedMovement.magnitude;

                // Current Velocity
                var currentPlanarVelocity = Vector3.ProjectOnPlane
                (
                    currentVelocity,
                    motor.CharacterUp
                );
                
                // Movement Force
                var movementForce = planarMovement * (airAcceleration * deltaTime);
                
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);
                
                currentVelocity += targetPlanarVelocity - currentPlanarVelocity;
            }
            
            // Gravity
            var effectiveGravity = gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (requestedSustainedJump && verticalSpeed > 0f)
            {
                effectiveGravity *= jumpSustainGravity;
            }
            
            currentVelocity += motor.CharacterUp * (effectiveGravity * deltaTime);
            
        }

        if (_requestedJump)
            ApplyJump(ref currentVelocity);

            motor.ForceUnground(0.1f);
            
            var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
            
            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
        }
    }

    private Vector3 CalculateGroundVelocity()
    {
        var groundMovement = motor.GetDirectionTangentToSurface(
            direction: _requestedMovement,
            surfaceNormal: motor.GroundingStatus.GroundNormal
        );

        var baseSpeed = _stance == Stance.Stand ? walkSpeed : crouchSpeed;
        return groundMovement * baseSpeed * MoveSpeedMult;
    }

    private void ApplyJump(ref Vector3 currentVelocity)
    {
        _requestedJump = false;
        motor.ForceUnground(0f);

        var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
        var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed * JumpForceMult);

        currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        if (_requestedCrouch && _stance == Stance.Stand)
        {
            _stance = Stance.Crouch;
            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
            );
        }
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        if (!_requestedCrouch && _stance is not Stance.Stand)
        {
            _stance = Stance.Stand;
            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
            );

            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;

            if (motor.CharacterOverlap(pos, rot, uncrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0)
            {
                requestedCrouch = true;
                motor.SetCapsuleDimensions
                    (
                        radius: motor.Capsule.radius,
                        height: crouchHeight,
                        yOffset: crouchHeight * 0.5f
                    );
            }
            else
            {
                _stance = Stance.Stand;
            }
        }
        
        
    }

    public void PostGroundingUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) => coll;
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    public Transform GetCameraTarget() => cameraTarget;
}