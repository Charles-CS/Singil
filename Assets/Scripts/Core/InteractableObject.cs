using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for all objects the player can interact with using [E].
/// Attach to any GameObject with a Collider. Configure in Inspector or via code.
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("Object Info")]
    public string objectName = "Object";
    [TextArea(2, 5)]
    public string examineText = "Nothing remarkable.";
    public string ledgerAnnotation = ""; // If set, added to Ledger on examine

    [Header("Key / Lock")]
    public bool requiresKey = false;
    public string requiredKeyId = "";
    public string lockedMessage = "It's locked.";

    [Header("Pickup")]
    public bool isPickup = false;
    public string pickupKeyId = ""; // If set, grants this key ID to InvestigationChain
    public bool destroyOnPickup = true;

    [Header("Container")]
    public bool isContainer = false;
    public InteractableObject[] containedItems; // Items revealed when opened

    [Header("State")]
    public bool hasBeenExamined = false;
    public bool isLocked = true; // Only relevant if requiresKey
    public bool isInteractable = true; // Can be disabled during certain phases

    [Header("Visual Feedback")]
    public Color highlightColor = new Color(1f, 0.9f, 0.6f, 1f); // Warm amber highlight
    public float highlightIntensity = 0.3f;

    [Header("Events")]
    public UnityEvent OnExamined = new UnityEvent();
    public UnityEvent OnUnlocked = new UnityEvent();
    public UnityEvent OnPickedUp = new UnityEvent();

    private Renderer[] renderers;
    private Color[][] originalColors;
    private bool isHighlighted = false;

    void Awake()
    {
        // Cache renderers for highlight effect
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            originalColors[i] = new Color[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                originalColors[i][j] = mats[j].color;
            }
        }

        // Hide contained items initially
        if (isContainer && containedItems != null)
        {
            foreach (var item in containedItems)
            {
                if (item != null)
                    item.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Called when the player presses E while looking at this object.
    /// Returns the text to display.
    /// </summary>
    public virtual string Examine()
    {
        if (!isInteractable)
            return "";

        // Check if locked
        if (requiresKey && isLocked)
        {
            return lockedMessage;
        }

        hasBeenExamined = true;

        // If container, reveal contents
        if (isContainer && containedItems != null)
        {
            foreach (var item in containedItems)
            {
                if (item != null)
                    item.gameObject.SetActive(true);
            }
        }

        // If pickup, handle collection
        if (isPickup)
        {
            OnPickedUp?.Invoke();

            // Grant key to investigation chain if applicable
            if (!string.IsNullOrEmpty(pickupKeyId))
            {
                InvestigationChain chain = FindAnyObjectByType<InvestigationChain>();
                if (chain != null)
                {
                    chain.AcquireKey(pickupKeyId);
                }
            }

            if (destroyOnPickup)
            {
                // Disable instead of destroy so references aren't broken
                gameObject.SetActive(false);
            }
        }

        // Add ledger annotation if set
        if (!string.IsNullOrEmpty(ledgerAnnotation))
        {
            LedgerUI ledger = FindAnyObjectByType<LedgerUI>();
            if (ledger != null)
            {
                ledger.AddAnnotation(ledgerAnnotation);
            }
        }

        OnExamined?.Invoke();
        return examineText;
    }

    /// <summary>
    /// Attempt to unlock this object with a key ID.
    /// </summary>
    public bool TryUnlock(string keyId)
    {
        if (!requiresKey) return true;
        if (keyId == requiredKeyId)
        {
            isLocked = false;
            OnUnlocked?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Show highlight when player is looking at this object.
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (isHighlighted == highlighted) return;
        isHighlighted = highlighted;

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (highlighted)
                {
                    mats[j].color = Color.Lerp(originalColors[i][j], highlightColor, highlightIntensity);
                }
                else
                {
                    mats[j].color = originalColors[i][j];
                }
            }
        }
    }

    /// <summary>
    /// Get the prompt text to show when player looks at this object.
    /// </summary>
    public virtual string GetPromptText()
    {
        if (!isInteractable) return "";

        if (isPickup)
            return $"Press [E] to Pick Up {objectName}";
        if (requiresKey && isLocked)
            return $"Press [E] to Examine {objectName} (Locked)";
        return $"Press [E] to Examine {objectName}";
    }
}
