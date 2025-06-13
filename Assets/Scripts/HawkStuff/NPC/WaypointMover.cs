using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class WaypointMover : MonoBehaviourPun
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public List<Transform> waypoints = new List<Transform>();
    public float waitTimeAtWaypoint = 1f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    public Vector3 rotationOffsetEuler = Vector3.zero;

    private int currentIndex = 0;
    private bool isMoving = true;
    private bool isWaiting = false;

    private Quaternion rotationOffset => Quaternion.Euler(rotationOffsetEuler);

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || waypoints.Count == 0 || isWaiting || !isMoving)
            return;

        Transform target = waypoints[currentIndex];
        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        if (distance > 0.1f)
        {
            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            // Rotate toward direction + offset
            if (moveDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir) * rotationOffset;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                photonView.RPC("SyncTransform", RpcTarget.Others, transform.position, transform.rotation);
            }
        }
        else
        {
            StartCoroutine(WaitAtWaypoint());
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

    [PunRPC]
    private void SyncTransform(Vector3 pos, Quaternion rot)
    {
        if (PhotonNetwork.IsMasterClient) return;
        transform.position = pos;
        transform.rotation = rot;
    }
}
