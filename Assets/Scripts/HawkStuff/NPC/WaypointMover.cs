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

    [Header("Path Options")]
    public bool loopPath = false;
    public float finalWaitTime = 2f;
    public bool pingPongPath = false;
    public bool randomOrderPath = false;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    public Vector3 rotationOffsetEuler = Vector3.zero;

    [Header("Animation Settings")]
    public Animator targetAnimator;
    public string moveBoolName = "IsMoving";

    [Header("Slope Detection")]
    public float slopeRaycastDistance = 1.5f;
    public LayerMask groundMask;

    [Header("Stuck Jump Settings")]
    public float stuckSpeedThreshold = 0.2f;
    public float stuckTimeBeforeJump = 2f;
    public float jumpForce = 5f;
    public float forwardJumpBoost = 2f;

    private int currentIndex = 0;
    private bool isMoving = true;
    private bool isWaiting = false;
    private bool movingForward = true;

    private float stuckTimer = 0f;
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

            if (rb.velocity.magnitude < stuckSpeedThreshold)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= stuckTimeBeforeJump)
                {
                    JumpNudge(slopeAdjustedDir);
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
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

        bool isLast = currentIndex == waypoints.Count - 1;
        float wait = isLast && (loopPath || pingPongPath || randomOrderPath) ? finalWaitTime : waitTimeAtWaypoint;
        yield return new WaitForSeconds(wait);

        if (randomOrderPath)
        {
            currentIndex = GetRandomNextIndex(currentIndex);
        }
        else if (pingPongPath)
        {
            if (movingForward)
            {
                if (currentIndex >= waypoints.Count - 1)
                {
                    movingForward = false;
                    currentIndex--;
                }
                else currentIndex++;
            }
            else
            {
                if (currentIndex <= 0)
                {
                    movingForward = true;
                    currentIndex++;
                }
                else currentIndex--;
            }
        }
        else if (loopPath)
        {
            if (currentIndex >= waypoints.Count - 1)
                currentIndex = 0;
            else
                currentIndex++;
        }
        else
        {
            if (currentIndex >= waypoints.Count - 1)
                isMoving = false;
            else
                currentIndex++;
        }

        isWaiting = false;
    }

    private int GetRandomNextIndex(int excludeIndex)
    {
        if (waypoints.Count <= 1) return excludeIndex;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, waypoints.Count);
        } while (newIndex == excludeIndex); // avoid immediate repeat

        return newIndex;
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

    private void JumpNudge(Vector3 moveDirection)
    {
        Vector3 jumpVector = Vector3.up * jumpForce + moveDirection * forwardJumpBoost;
        rb.AddForce(jumpVector, ForceMode.VelocityChange);
    }
}
