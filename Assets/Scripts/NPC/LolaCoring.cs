using UnityEngine;
using System.Collections;

/// <summary>
/// Lola Coring — the debtor NPC for Level 0 (Tutorial).
/// Manages her state machine: Unaware → Alert → Marked → Collected.
/// Sits in the rocking chair at the center of the Sala.
/// </summary>
public class LolaCoring : MonoBehaviour
{
    public enum DebtorState { Unaware, Alert, Marked, Collected }

    [Header("State")]
    public DebtorState currentState = DebtorState.Unaware;

    [Header("References")]
    public Transform rockingChairPosition; // Where she sits
    public SubtitleUI subtitleUI;
    public UIPromptManager promptManager;
    public GamePhaseManager phaseManager;
    public CollectionSequence collectionSequence;
    public PlayerMovement playerMovement;

    [Header("Visual")]
    public Renderer npcRenderer;
    public GameObject npcLabel;  // Floating name label
    public Color unawareColor = new Color(0.75f, 0.65f, 0.55f);  // Warm, natural
    public Color alertColor = new Color(0.6f, 0.55f, 0.65f);      // Slightly cooler
    public Color markedColor = new Color(0.5f, 0.4f, 0.55f);      // Marked, purplish
    public Color collectedColor = new Color(0.4f, 0.4f, 0.4f);    // Gray, lifeless

    [Header("Rocking Animation")]
    public float rockSpeed = 1.5f;
    public float rockAngle = 5f;
    private bool isRocking = true;
    private float rockTimer = 0f;

    [Header("Cinematic")]
    public float markedPauseDuration = 3f; // Unskippable 3-second pause

    void Start()
    {
        if (subtitleUI == null) subtitleUI = FindAnyObjectByType<SubtitleUI>();
        if (promptManager == null) promptManager = FindAnyObjectByType<UIPromptManager>();
        if (phaseManager == null) phaseManager = FindAnyObjectByType<GamePhaseManager>();

        UpdateVisuals();
    }

    void Update()
    {
        // Simple rocking animation
        if (isRocking && currentState != DebtorState.Collected)
        {
            rockTimer += Time.deltaTime * rockSpeed;
            float angle = Mathf.Sin(rockTimer) * rockAngle;
            transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, angle);
        }
    }

    /// <summary>
    /// Called when Investigation phase completes — debtor becomes Alert.
    /// </summary>
    public void BecomeAlert()
    {
        currentState = DebtorState.Alert;
        isRocking = true; // Still rocking, but aware
        rockSpeed = 0.8f; // Slower, more deliberate rocking
        UpdateVisuals();

        StartCoroutine(AlertSequence());
    }

    private IEnumerator AlertSequence()
    {
        yield return new WaitForSeconds(1f);

        // Lola speaks
        if (subtitleUI != null)
        {
            subtitleUI.ShowSubtitle(
                "\"Nandito na kayo. Alam ko namang darating kayo.\"",
                "(You're here. I knew you would come.)",
                4f
            );
        }

        yield return new WaitForSeconds(5f);

        // Show Presence Sense prompt
        if (promptManager != null)
        {
            promptManager.ShowPrompt(
                "The Debtor is Alert. Hold [Right Mouse Button] to use Presence Sense and track their location.",
                8f
            );
        }

        // Enable presence sense and debt mark on player
        PresenceSense sense = FindAnyObjectByType<PresenceSense>();
        if (sense != null)
        {
            sense.SetDebtorTransform(transform);
            sense.Enable();
        }

        yield return new WaitForSeconds(9f);

        // Show debt mark prompt
        if (promptManager != null)
        {
            promptManager.ShowPersistentPrompt(
                "Approach the Debtor and press [Left Mouse Button] to apply the Debt Mark."
            );
        }

        DebtMark mark = FindAnyObjectByType<DebtMark>();
        if (mark != null)
        {
            mark.SetDebtor(transform, this);
            mark.Enable();
        }
    }

    /// <summary>
    /// Called when the player applies the Debt Mark.
    /// </summary>
    public void OnDebtMarked()
    {
        currentState = DebtorState.Marked;
        UpdateVisuals();

        // Dismiss the persistent prompt
        if (promptManager != null)
            promptManager.DismissPrompt();

        // Disable presence sense
        PresenceSense sense = FindAnyObjectByType<PresenceSense>();
        if (sense != null)
            sense.Disable();
    }

    /// <summary>
    /// Called after the glyph flash — play Lola's final plea and 3-second pause.
    /// </summary>
    public void PlayMarkedDialogue()
    {
        StartCoroutine(MarkedDialogueSequence());
    }

    private IEnumerator MarkedDialogueSequence()
    {
        // Lola looks at TBD and speaks
        if (subtitleUI != null)
        {
            subtitleUI.ShowSubtitle(
                "\"Puwede ba akong maupo sandali? Bago ninyo kunin?\"",
                "(May I sit a moment longer? Before you take it?)",
                4f
            );
        }

        yield return new WaitForSeconds(5f);

        // === UNSKIPPABLE 3-SECOND PAUSE ===
        // Lola rocks slowly, closes her eyes, accepting her fate
        rockSpeed = 0.4f;

        // Lock controls during this moment
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.LockControls();

        yield return new WaitForSeconds(markedPauseDuration);

        // Slow to a stop
        isRocking = false;

        yield return new WaitForSeconds(0.5f);

        // Unlock controls and show collection prompt
        if (playerMovement != null)
            playerMovement.UnlockControls();

        // Transition to Collection phase
        if (phaseManager != null)
        {
            phaseManager.TransitionToPhase(GamePhase.Collection);
        }
    }

    /// <summary>
    /// Called during Phase 4 — the soul is collected, body slumps.
    /// </summary>
    public void OnCollected()
    {
        currentState = DebtorState.Collected;
        isRocking = false;
        UpdateVisuals();

        // "Slump" — tilt forward
        StartCoroutine(SlumpAnimation());
    }

    private IEnumerator SlumpAnimation()
    {
        float elapsed = 0f;
        float duration = 2f;
        Quaternion startRot = transform.localRotation;
        // Tilt forward as if slumping in chair
        Quaternion endRot = Quaternion.Euler(25f, transform.localEulerAngles.y, 5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease in — slow start, natural fall
            float eased = t * t;
            transform.localRotation = Quaternion.Slerp(startRot, endRot, eased);
            yield return null;
        }
    }

    private void UpdateVisuals()
    {
        if (npcRenderer == null) return;

        Color targetColor;
        switch (currentState)
        {
            case DebtorState.Alert:
                targetColor = alertColor;
                break;
            case DebtorState.Marked:
                targetColor = markedColor;
                break;
            case DebtorState.Collected:
                targetColor = collectedColor;
                break;
            default:
                targetColor = unawareColor;
                break;
        }

        npcRenderer.material.color = targetColor;
    }
}
