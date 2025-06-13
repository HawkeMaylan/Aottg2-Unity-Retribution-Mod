using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView), typeof(Rigidbody), typeof(Collider))]
public class WaypointMover : MonoBehaviourPun
{
    [Header("Movement Settings")]
    public float moveForce = 50f;
    public float maxSpeed = 5f;
    public List<Transform> waypoints = new List<Transform>();
    public float waitTimeAtWaypoint = 1f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    public Vector3 rotationOffsetEuler = Vector3.zero;

    [Header("Animation Settings")]
    public Animator targetAnimator;
    public string moveBoolName = "IsMoving";

    [Header("Slope Detection")]
    public float slopeRaycastDistance = 1.5f;
    public LayerMask groundMask;

    private int currentIndex = 0;
    private bool isMoving = true;
    private bool isWaiting = false;

    private Quaternion rotationOffset => Quaternion.Euler(rotationOffsetEuler);
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || waypoints.Count == 0 || isWaiting || !isMoving)
        {
            SetMovingState(false);
            return;
        }

        Transform target = waypoints[currentIndex];
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance > 0.5f)
        {
            SetMovingState(true);
            Vector3 slopeAdjustedDir = GetSlopeAdjustedDirection(direction.normalized);
            MoveWithPhysics(slopeAdjustedDir);
            RotateToward(slopeAdjustedDir);
        }
        else
        {
            SetMovingState(false);
            StartCoroutine(WaitAtWaypoint());
        }
    }

    private void MoveWithPhysics(Vector3 moveDir)
    {
        if (rb.velocity.magnitude < maxSpeed)
        {
            rb.AddForce(moveDir * moveForce, ForceMode.Acceleration);
        }
    }

    private void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction) * rotationOffset;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private IEnumerator WaitAtWaypoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTimeAtWaypoint);
        currentIndex++;
        if (currentIndex >= waypoints.Count)
            isMoving = false;
        isWaiting = false;
    }

    private void SetMovingState(bool state)
    {
        if (targetAnimator != null && targetAnimator.GetBool(moveBoolName) != state)
            targetAnimator.SetBool(moveBoolName, state);
    }

    private Vector3 GetSlopeAdjustedDirection(Vector3 inputDirection)
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, slopeRaycastDistance, groundMask))
        {
            Vector3 normal = hit.normal;
            return Vector3.ProjectOnPlane(inputDirection, normal).normalized;
        }

        return inputDirection;
    }
}
