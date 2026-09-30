using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerMovement1))]
public class WallRunning : MonoBehaviour
{
    [Header("Wall Running Tuning")]
    [SerializeField] private float wallRunSpeed = 8f;
    [SerializeField] private float wallGravityDownforce = 2f;
    [SerializeField] private float maxWallRunTime = 1.5f;

    [Header("Wall Jumping")]
    [SerializeField] private float wallJumpUpForce = 7f;
    [SerializeField] private float wallJumpSideForce = 12f;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private float minHeightOffGround = 1.5f;

    [Header("Camera stuff")]
    [SerializeField] private Transform fpsArms;
    [SerializeField] private float camTiltAmount = 15f;
    [SerializeField] private float tiltSpeed = 10f;
    [SerializeField] private Vector3 armOffsetOnWall = new Vector3(0.4f, -0.1f, 0.2f);
    [SerializeField] private Vector3 armRotationOnWall = new Vector3(0f, 0f, 25f);

    [Header("References")]
    [SerializeField] private Transform orientation;

    private Rigidbody rb;
    private PlayerMovement1 pm;
    private float wallRunTimer;

    // Visual State
    private float currentTilt;
    private Vector3 defaultArmLocalPos;
    private Quaternion defaultArmLocalRot;

    // State Transitions
    private enum MovementState { Grounded, Airborne, WallRunning }
    private MovementState currentState;

    private bool wallLeft;
    private bool wallRight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    private bool exitingWall;
    private float exitWallTimer;
    private readonly float exitWallCooldown = 0.2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement1>();
    }

    private void Start()
    {
        if (fpsArms != null)
        {
            defaultArmLocalPos = fpsArms.localPosition;
            defaultArmLocalRot = fpsArms.localRotation;

            // NEW: Hide the arms immediately when the game starts
            fpsArms.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        CheckForWall();
        UpdateState();
        HandleProceduralVisuals();
    }

    private void FixedUpdate()
    {
        if (currentState == MovementState.WallRunning)
        {
            ExecuteWallRunMovement();
        }
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, wallLayer);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, wallLayer);
    }

    private bool IsAboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minHeightOffGround);
    }

    private void UpdateState()
    {
        if (exitingWall)
        {
            if (currentState == MovementState.WallRunning) StopWallRun();
            exitWallTimer -= Time.deltaTime;
            if (exitWallTimer <= 0) exitingWall = false;
            return;
        }

        bool canWallRun = (wallLeft || wallRight) && IsAboveGround();
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (canWallRun && verticalInput > 0)
        {
            if (currentState != MovementState.WallRunning) StartWallRun();
            if (Input.GetKeyDown(KeyCode.Space)) WallJump();
        }
        else if (currentState == MovementState.WallRunning)
        {
            StopWallRun();
        }
        else
        {
            currentState = IsAboveGround() ? MovementState.Airborne : MovementState.Grounded;
        }
    }

    private void HandleProceduralVisuals()
    {
        float targetTilt = 0f;
        Vector3 targetArmPos = defaultArmLocalPos;
        Quaternion targetArmRot = defaultArmLocalRot;

        if (currentState == MovementState.WallRunning)
        {
            float frictionBob = Mathf.Sin(Time.time * 30f) * 0.02f;

            if (wallLeft)
            {
                targetTilt = -camTiltAmount;
                targetArmPos = defaultArmLocalPos + new Vector3(-armOffsetOnWall.x, armOffsetOnWall.y + frictionBob, armOffsetOnWall.z);
                targetArmRot = defaultArmLocalRot * Quaternion.Euler(-armRotationOnWall.x, -armRotationOnWall.y, -armRotationOnWall.z);
            }
            else if (wallRight)
            {
                targetTilt = camTiltAmount;
                targetArmPos = defaultArmLocalPos + new Vector3(armOffsetOnWall.x, armOffsetOnWall.y + frictionBob, armOffsetOnWall.z);
                targetArmRot = defaultArmLocalRot * Quaternion.Euler(armRotationOnWall);
            }
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
        pm.wallTilt = currentTilt;

        if (fpsArms != null && fpsArms.gameObject.activeInHierarchy)
        {
            fpsArms.localPosition = Vector3.Lerp(fpsArms.localPosition, targetArmPos, Time.deltaTime * tiltSpeed);
            fpsArms.localRotation = Quaternion.Lerp(fpsArms.localRotation, targetArmRot, Time.deltaTime * tiltSpeed);
        }
    }

    private void StartWallRun()
    {
        currentState = MovementState.WallRunning;
        wallRunTimer = maxWallRunTime;
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // NEW: Make the arms visible when we attach to the wall
        if (fpsArms != null) fpsArms.gameObject.SetActive(true);
    }

    private void ExecuteWallRunMovement()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.ProjectOnPlane(orientation.forward, wallNormal).normalized;

        rb.linearVelocity = new Vector3(wallForward.x * wallRunSpeed, rb.linearVelocity.y, wallForward.z * wallRunSpeed);
        rb.AddForce(Vector3.down * wallGravityDownforce, ForceMode.Force);
        rb.AddForce(-wallNormal * 100f, ForceMode.Force);

        wallRunTimer -= Time.fixedDeltaTime;
        if (wallRunTimer <= 0) StopWallRun();
    }

    private void StopWallRun()
    {
        currentState = MovementState.Airborne;
        rb.useGravity = true;

        // NEW: Hide the arms again when we fall off or jump off the wall
        if (fpsArms != null) fpsArms.gameObject.SetActive(false);
    }

    private void WallJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallCooldown;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}