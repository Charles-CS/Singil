using UnityEngine;

/// <summary>
/// Manages the 2-step linked investigation chain for Level 0.
/// Chain 1: Locked Tin Box (Sala Cabinet) → Key elsewhere
/// Chain 2: Key (Bedroom Chest) → Unlock Tin Box → Original Debt Contract
/// </summary>
public class InvestigationChain : MonoBehaviour
{
    [Header("Chain Items")]
    public InteractableObject lockedTinBox;
    public InteractableObject woodenChest;
    public InteractableObject keyItem;
    public InteractableObject debtContract;
    public InteractableObject childsDrawing; // Optional examine

    [Header("State")]
    public bool tinBoxExamined = false;
    public bool keyAcquired = false;
    public bool contractFound = false;

    [Header("References")]
    public GamePhaseManager phaseManager;
    public LedgerUI ledgerUI;
    public UIPromptManager promptManager;
    public SubtitleUI subtitleUI;

    private bool hasShownKeyHint = false;

    void Start()
    {
        if (phaseManager == null) phaseManager = FindAnyObjectByType<GamePhaseManager>();
        if (ledgerUI == null) ledgerUI = FindAnyObjectByType<LedgerUI>();
        if (promptManager == null) promptManager = FindAnyObjectByType<UIPromptManager>();
        if (subtitleUI == null) subtitleUI = FindAnyObjectByType<SubtitleUI>();
    }

    /// <summary>
    /// Called when the player examines the locked tin box for the first time.
    /// </summary>
    public void OnTinBoxExamined()
    {
        if (tinBoxExamined) return;
        tinBoxExamined = true;

        // Ledger update
        if (ledgerUI != null)
        {
            ledgerUI.AddAnnotation("Personal effects. Key elsewhere.");
        }

        // Show hint prompt
        if (promptManager != null && !hasShownKeyHint)
        {
            hasShownKeyHint = true;
            promptManager.ShowPrompt("The Box is locked. Investigate other rooms to find the Key.\nHold [Shift] to Sprint (Outdoors only. Disabled in interiors).", 8f);
        }
    }

    /// <summary>
    /// Called when the player picks up a key item.
    /// </summary>
    public void AcquireKey(string keyId)
    {
        if (keyId == "bedroom_key")
        {
            keyAcquired = true;

            // Unlock the tin box
            if (lockedTinBox != null)
            {
                lockedTinBox.TryUnlock("bedroom_key");
            }

            // Show prompt
            if (promptManager != null)
            {
                promptManager.ShowPrompt("Key Acquired. Return to the Sala and unlock the Tin Box.", 6f);
            }
        }
    }

    /// <summary>
    /// Called when the player examines the now-unlocked tin box and finds the contract.
    /// </summary>
    public void OnContractFound()
    {
        if (contractFound) return;
        contractFound = true;

        // Ledger update
        if (ledgerUI != null)
        {
            ledgerUI.AddAnnotation("Debt confirmed. Price: Life.");
        }

        // Trigger Phase 3 — Hunt
        if (phaseManager != null)
        {
            phaseManager.TransitionToPhase(GamePhase.Hunt);
        }
    }

    /// <summary>
    /// Called when the player examines the child's drawing (optional).
    /// </summary>
    public void OnChildsDrawingExamined()
    {
        // TBD Monologue
        if (subtitleUI != null)
        {
            subtitleUI.ShowSubtitle(
                "\"Pamilya. Lagi silang may pamilya.\"",
                "(Family. They always have family.)",
                4f
            );
        }
    }
}
