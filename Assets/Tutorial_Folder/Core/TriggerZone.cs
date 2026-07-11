using UnityEngine;

/// <summary>
/// Simple trigger zone that notifies the GamePhaseManager when the player enters.
/// Attach to a GameObject with a BoxCollider set to isTrigger.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TriggerZone : MonoBehaviour
{
    public string zoneName = "";
    public bool oneShot = true;

    private bool hasTriggered = false;
    private GamePhaseManager phaseManager;

    void Start()
    {
        phaseManager = FindAnyObjectByType<GamePhaseManager>();

        // Ensure collider is trigger
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (oneShot && hasTriggered) return;

        // Check if it's the player
        if (other.GetComponent<PlayerMovement>() != null || other.GetComponent<CharacterController>() != null)
        {
            hasTriggered = true;

            if (phaseManager != null)
            {
                phaseManager.OnTriggerZoneEntered(zoneName);
            }
        }
    }
}
