using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GrapplingHook : MonoBehaviour
{
    [Header("Grapple Settings")]
    [SerializeField] private float maxDistance = 35f;
    [SerializeField] private LayerMask grappleLayer;
    [SerializeField] private float pullSpeed = 25f; // How fast you zip to the wall

    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform ropeStartPoint;

    private LineRenderer lr;
    private Rigidbody rb;
    private Vector3 grapplePoint;
    private bool isGrappling;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        rb = GetComponent<Rigidbody>();
        lr.positionCount = 0;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) StartGrapple();
        else if (Input.GetMouseButtonUp(0)) StopGrapple();
    }

    private void FixedUpdate()
    {
        if (isGrappling) ExecuteGrapplePull();
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void StartGrapple()
    {
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hit, maxDistance, grappleLayer))
        {
            grapplePoint = hit.point;
            isGrappling = true;
            rb.useGravity = false; // Disable gravity for a straight pull
        }
    }

    private void ExecuteGrapplePull()
    {
        Vector3 direction = (grapplePoint - transform.position).normalized;
        rb.linearVelocity = direction * pullSpeed;

        // Auto-release if you get too close so you don't stick to the wall
        if (Vector3.Distance(transform.position, grapplePoint) < 2f)
        {
            StopGrapple();
        }
    }

    private void StopGrapple()
    {
        isGrappling = false;
        rb.useGravity = true; // Restore gravity
        lr.positionCount = 0;
    }

    private void DrawRope()
    {
        if (!isGrappling) return;

        lr.positionCount = 2;
        lr.SetPosition(0, ropeStartPoint.position);
        lr.SetPosition(1, grapplePoint);
    }
}