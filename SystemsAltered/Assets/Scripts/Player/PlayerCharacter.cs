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
    [Header("References")]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private DrugStateController drugState;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float gravity = -90f;

    [Header("Capsule Dimensions")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [Range(0f, 1f)] [SerializeField] private float standCamTargetHeight = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float crouchCamTargetHeight = 0.7f;

    private Stance _stance;
    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedCrouch;
    private Vector3 _externalImpulse;

    // --- Multiplier accessors ---

    private float MoveSpeedMult =>
        drugState?.CurrentState?.moveSpeedMultiplier ?? 1f;

    private float JumpForceMult =>
        drugState?.CurrentState?.jumpForceMultiplier ?? 1f;

    private float GravityScaleMult =>
        drugState?.CurrentState?.gravityScaleMultiplier ?? 1f;

    // --- Lifecycle ---

    public void Initialize()
    {
        _stance = Stance.Stand;
        motor.CharacterController = this;
    }

    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;

        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        _requestedMovement = input.Rotation * _requestedMovement;

        _requestedJump = _requestedJump || input.Jump;

        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None   => _requestedCrouch,
            _                  => _requestedCrouch
        };
    }

    public void UpdateBody()
    {
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;

        var camHeightRatio = _stance == Stance.Stand
            ? standCamTargetHeight
            : crouchCamTargetHeight;

        cameraTarget.localPosition = new Vector3(0f, currentHeight * camHeightRatio, 0f);
        root.localScale = new Vector3(1f, normalizedHeight, 1f);
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
            currentVelocity = CalculateGroundVelocity();
        else
            currentVelocity += motor.CharacterUp * (gravity * GravityScaleMult * deltaTime);

        if (_requestedJump)
            ApplyJump(ref currentVelocity);

        if (_externalImpulse.sqrMagnitude > 0f)
        {
            currentVelocity += _externalImpulse;
            _externalImpulse = Vector3.zero;
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