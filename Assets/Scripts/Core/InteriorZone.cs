using UnityEngine;

/// <summary>
/// Marks a collider zone as an interior area.
/// When the player enters, sprint is disabled.
/// Attach to a GameObject with a BoxCollider set to isTrigger.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class InteriorZone : MonoBehaviour
{
    void Start()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.SetInterior(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.SetInterior(false);
        }
    }
}
