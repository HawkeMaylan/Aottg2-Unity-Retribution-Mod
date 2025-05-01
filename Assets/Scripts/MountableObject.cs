using UnityEngine;
using Characters;
using UnityEngine.UI;
using Photon.Pun;

public class DirectMountBundled : MonoBehaviourPunCallbacks
{
    [Header("Mount Target")]
    public Transform mountPoint;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("UI Prompt")]
    public Text promptText;

    private Human humanInTrigger;
    private bool isMounted = false;
    private bool hasExitedAfterUnmount = false;

    private void Start()
    {
        if (promptText != null)
            promptText.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human.IsMine())
        {
            humanInTrigger = human;
            hasExitedAfterUnmount = false;

            if (promptText != null)
            {
                promptText.text = "Press G to Mount";
                promptText.enabled = true;
            }

            Debug.Log("[DirectMountBundled] Human entered: " + human.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human == humanInTrigger)
        {
            if (!isMounted)
            {
                hasExitedAfterUnmount = true;
                humanInTrigger = null;
            }

            if (promptText != null)
                promptText.enabled = false;

            Debug.Log("[DirectMountBundled] Human exited trigger.");
        }
    }

    private void Update()
    {
        if (humanInTrigger != null && Input.GetKeyDown(KeyCode.G))
        {
            if (!isMounted && !hasExitedAfterUnmount)
                AttachHuman();
            else if (isMounted)
                DetachHuman();
        }
    }

    private void AttachHuman()
    {
        if (humanInTrigger == null || mountPoint == null) return;

        //  DIRECTLY assign mounting properties manually
        humanInTrigger.MountedTransform = mountPoint;
        humanInTrigger.MountedMapObject = null;
        humanInTrigger.MountedPositionOffset = positionOffset;
        humanInTrigger.MountedRotationOffset = rotationOffset;
        humanInTrigger.MountState = HumanMountState.MapObject;
        humanInTrigger.SetInterpolation(false);

        isMounted = true;
        hasExitedAfterUnmount = false;

        if (promptText != null)
            promptText.text = "Press G to Unmount";

        Debug.Log("[DirectMountBundled] Mounted manually to: " + mountPoint.name);
    }

    private void DetachHuman()
    {
        if (humanInTrigger == null) return;

        humanInTrigger.Unmount(true);

        isMounted = false;

        if (promptText != null)
        {
            promptText.text = "";
            promptText.enabled = false;
        }

        Debug.Log("[DirectMountBundled] Unmounted.");
    }
}
