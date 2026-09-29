using UnityEngine;

public enum MovementState
{
    Grounded,
    Airborne,
    WallRunning
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 8f;
    public float wallRunSpeed = 12f;
    public float jumpHeight = 2.5f;
    public float gravity = -22f;

    [Header("Wall Running Settings")]
    public float wallCheckDistance = 0.9f;
    public float wallRunGravity = -2.5f;
    public float maxWallRunTime = 2.0f;
    public float wallJumpUpForce = 7f;
    public float wallJumpSideForce = 9f;
    public LayerMask wallLayer;

    [Header("References")]
    public PlayerCamera playerCam; // Drag Main Camera here

    private CharacterController controller;
    private Vector3 velocity;
    private MovementState state;

    private RaycastHit leftWallHit, rightWallHit;
    private bool hasWallLeft, hasWallRight;
    private bool isWallRunning;
    private float wallRunTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        wallRunTimer = maxWallRunTime;
    }

    void Update()
    {
        CheckForWalls();
        UpdateMovementState();
        ExecuteMovement();

        if (playerCam != null)
        {
            playerCam.SetWallRunEffects(isWallRunning, hasWallLeft);
        }
    }

    void CheckForWalls()
    {
        // Diagonal ray directions for smooth angled latching
        Vector3 leftDir = (-transform.right + transform.forward * 0.4f).normalized;
        Vector3 rightDir = (transform.right + transform.forward * 0.4f).normalized;

        // 3 Vertical Height Origins (High, Mid, Low)
        Vector3 highOrigin = transform.position + Vector3.up * 0.6f;
        Vector3 midOrigin = transform.position;
        Vector3 lowOrigin = transform.position - Vector3.up * 0.6f;

        // Left Side Rays (3 Rays)
        bool leftHigh = Physics.Raycast(highOrigin, leftDir, out RaycastHit leftHighHit, wallCheckDistance, wallLayer);
        bool leftMid = Physics.Raycast(midOrigin, leftDir, out RaycastHit leftMidHit, wallCheckDistance, wallLayer);
        bool leftLow = Physics.Raycast(lowOrigin, leftDir, out RaycastHit leftLowHit, wallCheckDistance, wallLayer);

        // Right Side Rays (3 Rays)
        bool rightHigh = Physics.Raycast(highOrigin, rightDir, out RaycastHit rightHighHit, wallCheckDistance, wallLayer);
        bool rightMid = Physics.Raycast(midOrigin, rightDir, out RaycastHit rightMidHit, wallCheckDistance, wallLayer);
        bool rightLow = Physics.Raycast(lowOrigin, rightDir, out RaycastHit rightLowHit, wallCheckDistance, wallLayer);

        // Require Middle ray plus at least High or Low ray (Ensures surface is a tall wall, not a low curb/box)
        hasWallLeft = leftMid && (leftHigh || leftLow);
        hasWallRight = rightMid && (rightHigh || rightLow);

        if (hasWallLeft) leftWallHit = leftMidHit;
        if (hasWallRight) rightWallHit = rightMidHit;
    }

    void UpdateMovementState()
    {
        bool isHoldingForward = Input.GetAxisRaw("Vertical") > 0;

        if (controller.isGrounded)
        {
            state = MovementState.Grounded;
            isWallRunning = false;
            wallRunTimer = maxWallRunTime;

            // Kill stored horizontal drift on landing
            velocity.x = 0f;
            velocity.z = 0f;

            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }
        else if ((hasWallLeft || hasWallRight) && isHoldingForward && wallRunTimer > 0)
        {
            state = MovementState.WallRunning;
            isWallRunning = true;
            wallRunTimer -= Time.deltaTime;
        }
        else
        {
            state = MovementState.Airborne;
            isWallRunning = false;

            // Mid-air friction decay for wall jump impulses
            velocity.x = Mathf.Lerp(velocity.x, 0f, Time.deltaTime * 3f);
            velocity.z = Mathf.Lerp(velocity.z, 0f, Time.deltaTime * 3f);
        }
    }

    void ExecuteMovement()
    {
        switch (state)
        {
            case MovementState.Grounded:
            case MovementState.Airborne:
                PerformStandardMovement();
                break;

            case MovementState.WallRunning:
                PerformWallRun();
                break;
        }

        float currentGravity = isWallRunning ? wallRunGravity : gravity;
        velocity.y += currentGravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    void PerformStandardMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 moveDir = transform.right * x + transform.forward * z;
        controller.Move(moveDir * walkSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void PerformWallRun()
    {
        RaycastHit wallHit = hasWallLeft ? leftWallHit : rightWallHit;

        // Project movement direction along the wall surface
        Vector3 wallDirection = Vector3.ProjectOnPlane(transform.forward, wallHit.normal);
        controller.Move(wallDirection * wallRunSpeed * Time.deltaTime);

        // Directional Wall Jump
        if (Input.GetButtonDown("Jump"))
        {
            Vector3 wallJumpDirection = (wallHit.normal * wallJumpSideForce) + (Vector3.up * wallJumpUpForce) + (transform.forward * 4f);
            velocity = wallJumpDirection;
            wallRunTimer = 0; // Exit wall run state immediately
        }
    }

    // Called during respawn or force resets
    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }

    // Draws all 6 rays in the Scene View for easy debugging and presentation
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 leftDir = (-transform.right + transform.forward * 0.4f).normalized;
        Vector3 rightDir = (transform.right + transform.forward * 0.4f).normalized;

        Vector3 highOrigin = transform.position + Vector3.up * 0.6f;
        Vector3 midOrigin = transform.position;
        Vector3 lowOrigin = transform.position - Vector3.up * 0.6f;

        // Left 3 Rays
        Gizmos.DrawRay(highOrigin, leftDir * wallCheckDistance);
        Gizmos.DrawRay(midOrigin, leftDir * wallCheckDistance);
        Gizmos.DrawRay(lowOrigin, leftDir * wallCheckDistance);

        // Right 3 Rays
        Gizmos.DrawRay(highOrigin, rightDir * wallCheckDistance);
        Gizmos.DrawRay(midOrigin, rightDir * wallCheckDistance);
        Gizmos.DrawRay(lowOrigin, rightDir * wallCheckDistance);
    }
}
