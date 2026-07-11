using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// The five gameplay phases for each level.
/// </summary>
public enum GamePhase
{
    Arrival,
    Investigation,
    Hunt,
    Collection,
    Departure
}

/// <summary>
/// Central state machine driving the 5-phase gameplay cycle for Level 0 (Tutorial).
/// Manages phase transitions, scripted sequences, UI prompts, and event coordination.
/// </summary>
public class GamePhaseManager : MonoBehaviour
{
    [Header("Current State")]
    public GamePhase currentPhase = GamePhase.Arrival;

    [Header("Player References")]
    public PlayerMovement playerMovement;
    public Camera mainCamera;

    [Header("NPC References")]
    public LolaCoring lolaCoring;

    [Header("UI References")]
    public LedgerUI ledgerUI;
    public UIPromptManager promptManager;
    public SubtitleUI subtitleUI;

    [Header("Gameplay References")]
    public PresenceSense presenceSense;
    public DebtMark debtMark;
    public CollectionSequence collectionSequence;
    public InvestigationChain investigationChain;

    [Header("Scene References")]
    public GameObject exitDoor; // Front door — locked during Hunt
    public Collider salaEntryTrigger;
    public Collider stairBottomTrigger;
    public Light ambientLight;

    [Header("Fade Overlay")]
    public Image fadeOverlay; // Full-screen black overlay for fade-in/out
    public Image loadingScreenOverlay;
    public TextMeshProUGUI loadingText;

    [Header("Phase Tracking")]
    public bool arrivalComplete = false;
    public bool playerOpenedLedger = false;
    public bool playerEnteredSala = false;
    public bool playerMovedSteps = false;

    private bool isTransitioning = false;
    private int stepCount = 0;

    void Start()
    {
        // Auto-find references if not set
        if (playerMovement == null) playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (ledgerUI == null) ledgerUI = FindAnyObjectByType<LedgerUI>();
        if (promptManager == null) promptManager = FindAnyObjectByType<UIPromptManager>();
        if (subtitleUI == null) subtitleUI = FindAnyObjectByType<SubtitleUI>();
        if (presenceSense == null) presenceSense = FindAnyObjectByType<PresenceSense>();
        if (debtMark == null) debtMark = FindAnyObjectByType<DebtMark>();
        if (collectionSequence == null) collectionSequence = FindAnyObjectByType<CollectionSequence>();
        if (investigationChain == null) investigationChain = FindAnyObjectByType<InvestigationChain>();

        // Disable Ledger tab initially
        if (ledgerUI != null) ledgerUI.SetEnabled(false);

        // Start Phase 1
        Debug.Log(">>> ENTERING PHASE 1: ARRIVAL <<<");
        StartCoroutine(Phase1_Arrival());
    }

    /// <summary>
    /// Called by other systems to transition to a new phase.
    /// </summary>
    public void TransitionToPhase(GamePhase newPhase)
    {
        if (isTransitioning) return;
        currentPhase = newPhase;

        switch (newPhase)
        {
            case GamePhase.Investigation:
                Debug.Log(">>> ENTERING PHASE 2: INVESTIGATION <<<");
                StartCoroutine(Phase2_Investigation());
                break;
            case GamePhase.Hunt:
                Debug.Log(">>> ENTERING PHASE 3: HUNT <<<");
                StartCoroutine(Phase3_Hunt());
                break;
            case GamePhase.Collection:
                Debug.Log(">>> ENTERING PHASE 4: COLLECTION <<<");
                StartCoroutine(Phase4_Collection());
                break;
            case GamePhase.Departure:
                Debug.Log(">>> ENTERING PHASE 5: DEPARTURE <<<");
                StartCoroutine(Phase5_Departure());
                break;
        }
    }

    // ========== PHASE 1 — ARRIVAL ==========

    private IEnumerator Phase1_Arrival()
    {
        isTransitioning = true;
        currentPhase = GamePhase.Arrival;

        // Lock controls initially
        if (playerMovement != null)
            playerMovement.LockControls();

        // Ensure screen is completely black
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = Color.black;
        }

        yield return new WaitForSeconds(2f);

        // === Cinematic Storytelling (Black Screen) ===
        if (subtitleUI != null)
        {
            subtitleUI.ShowSubtitle(
                "Barangay San Isidro, 1998.",
                "", // No translation needed
                3.5f
            );
        }

        yield return new WaitForSeconds(4f);

        if (subtitleUI != null)
        {
            subtitleUI.ShowSubtitle(
                "\"Maliit na bahay. Maliit na utang. Pero utang pa rin.\"",
                "(Small house. Small debt. But a debt all the same.)",
                4.5f
            );
        }

        yield return new WaitForSeconds(5f);

        if (subtitleUI != null)
        {
            subtitleUI.ShowSubtitle(
                "\"Ang utos sa akin: wag babalik nang walang dala.\"",
                "(The order was simple: don't come back empty-handed.)",
                4.5f
            );
        }

        yield return new WaitForSeconds(3f);

        // === Slow fade-in to the environment ===
        if (fadeOverlay != null)
        {
            float fadeDuration = 4.5f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                fadeOverlay.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeOverlay.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(1.5f);

        // === UI Prompt: Movement controls ===
        if (promptManager != null)
        {
            promptManager.ShowPrompt("Use [W][A][S][D] to Move. Use [Mouse] to Look around.", 8f);
        }

        // Unlock movement
        if (playerMovement != null)
            playerMovement.UnlockControls();

        isTransitioning = false;

        // Wait for player to move a few steps
        StartCoroutine(WaitForPlayerMovement());
    }

    private IEnumerator WaitForPlayerMovement()
    {
        // Give the player a few seconds to figure out controls
        yield return new WaitForSeconds(3f);

        playerMovedSteps = true;

        // === UI Prompt: Ledger ===
        if (promptManager != null)
        {
            promptManager.ShowPersistentPrompt("Press [Tab] to open the Ledger.");
        }

        // Enable Ledger
        if (ledgerUI != null)
            ledgerUI.SetEnabled(true);

        // Wait for player to open ledger
        StartCoroutine(WaitForLedgerOpen());
    }

    private IEnumerator WaitForLedgerOpen()
    {
        while (ledgerUI != null && !ledgerUI.IsOpen)
        {
            yield return null;
        }

        playerOpenedLedger = true;

        // Dismiss movement prompt
        if (promptManager != null)
            promptManager.DismissPrompt();

        // Wait for player to close ledger
        yield return new WaitForSecondsRealtime(1f);

        while (ledgerUI != null && ledgerUI.IsOpen)
        {
            yield return null;
        }

        // === UI Prompt: Proceed ===
        if (promptManager != null)
        {
            promptManager.ShowPrompt("Press [Tab] to close the Ledger. Proceed into the house.", 6f);
        }

        arrivalComplete = true;
    }

    // ========== PHASE 2 — INVESTIGATION ==========

    /// <summary>
    /// Called when player enters the Sala trigger zone.
    /// </summary>
    public void OnPlayerEnteredSala()
    {
        if (currentPhase != GamePhase.Arrival || !arrivalComplete) return;
        if (playerEnteredSala) return;

        playerEnteredSala = true;
        TransitionToPhase(GamePhase.Investigation);
    }

    private IEnumerator Phase2_Investigation()
    {
        isTransitioning = true;
        currentPhase = GamePhase.Investigation;

        // Set player as interior (disable sprint)
        if (playerMovement != null)
            playerMovement.SetInterior(true);

        yield return new WaitForSeconds(1f);

        // === UI Prompt ===
        if (promptManager != null)
        {
            promptManager.ShowPrompt(
                "The Debtor is Unaware. Search the environment for the Collateral.\nPress [E] to Examine objects.",
                8f
            );
        }

        // Enable player interaction
        if (playerMovement != null)
            playerMovement.SetInteractionEnabled(true);

        isTransitioning = false;

        // Investigation chain handles the rest via events
    }

    // ========== PHASE 3 — HUNT ==========

    private IEnumerator Phase3_Hunt()
    {
        isTransitioning = true;
        currentPhase = GamePhase.Hunt;

        // Lock the exit door
        if (exitDoor != null)
        {
            // Move door to block the exit or disable its trigger
            BoxCollider doorCollider = exitDoor.GetComponent<BoxCollider>();
            if (doorCollider != null)
                doorCollider.isTrigger = false; // Make it solid
        }

        // Lock controls briefly for camera pan
        if (playerMovement != null)
            playerMovement.LockControls();

        yield return new WaitForSeconds(0.5f);

        // === Camera pan toward Lola Coring ===
        if (mainCamera != null && lolaCoring != null)
        {
            Vector3 originalPos = mainCamera.transform.position;
            Quaternion originalRot = mainCamera.transform.rotation;

            // Look at Lola
            Vector3 lookTarget = lolaCoring.transform.position + Vector3.up * 1.2f;
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - mainCamera.transform.position);

            float panDuration = 1.5f;
            float panElapsed = 0f;
            while (panElapsed < panDuration)
            {
                panElapsed += Time.deltaTime;
                float t = panElapsed / panDuration;
                float eased = t * t * (3f - 2f * t); // Smoothstep
                mainCamera.transform.rotation = Quaternion.Slerp(originalRot, targetRot, eased);
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            // Pan back
            panElapsed = 0f;
            while (panElapsed < panDuration)
            {
                panElapsed += Time.deltaTime;
                float t = panElapsed / panDuration;
                float eased = t * t * (3f - 2f * t);
                mainCamera.transform.rotation = Quaternion.Slerp(targetRot, originalRot, eased);
                yield return null;
            }
        }

        // Unlock controls
        if (playerMovement != null)
            playerMovement.UnlockControls();

        // Trigger Lola's alert state (she handles her own dialogue and prompts)
        if (lolaCoring != null)
        {
            lolaCoring.BecomeAlert();
        }

        isTransitioning = false;
    }

    // ========== PHASE 4 — COLLECTION ==========

    private IEnumerator Phase4_Collection()
    {
        isTransitioning = true;
        currentPhase = GamePhase.Collection;

        yield return new WaitForSeconds(0.5f);

        // === UI Prompt: Collect ===
        if (promptManager != null)
        {
            promptManager.ShowPersistentPrompt("The Debtor is bound. Press [E] to Collect.");
        }

        // Wait for player to press E near debtor
        // We'll use a simple distance + key check
        bool collected = false;
        while (!collected)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame &&
                lolaCoring != null && playerMovement != null)
            {
                float dist = Vector3.Distance(playerMovement.transform.position, lolaCoring.transform.position);
                if (dist < 3f)
                {
                    collected = true;
                }
            }
            yield return null;
        }

        // Dismiss prompt
        if (promptManager != null)
            promptManager.DismissPrompt();

        // === Start Collection Cinematic ===
        if (collectionSequence != null)
        {
            collectionSequence.mainCamera = mainCamera;
            collectionSequence.debtorTransform = lolaCoring.transform;
            collectionSequence.debtorNPC = lolaCoring;
            collectionSequence.ambientLight = ambientLight;
            collectionSequence.ledgerUI = ledgerUI;
            collectionSequence.subtitleUI = subtitleUI;
            collectionSequence.promptManager = promptManager;
            collectionSequence.playerMovement = playerMovement;
            collectionSequence.phaseManager = this;

            collectionSequence.StartCollection();
        }

        isTransitioning = false;
        // CollectionSequence handles the transition to Phase 5
    }

    // ========== PHASE 5 — DEPARTURE ==========

    private IEnumerator Phase5_Departure()
    {
        isTransitioning = true;
        currentPhase = GamePhase.Departure;

        // Return controls to player
        if (playerMovement != null)
            playerMovement.UnlockControls();

        // Unlock exit door
        if (exitDoor != null)
        {
            BoxCollider doorCollider = exitDoor.GetComponent<BoxCollider>();
            if (doorCollider != null)
                doorCollider.isTrigger = true;
        }

        yield return new WaitForSeconds(1f);

        // === UI Prompt ===
        if (promptManager != null)
        {
            promptManager.ShowPersistentPrompt("The work is done. Exit the house.");
        }

        isTransitioning = false;
        // Wait for player to reach stair bottom trigger (handled by OnPlayerReachedStairBottom)
    }

    /// <summary>
    /// Called when player reaches the bottom of the stairs.
    /// </summary>
    public void OnPlayerReachedStairBottom()
    {
        if (currentPhase != GamePhase.Departure) return;

        StartCoroutine(DepartureSequence());
    }

    private IEnumerator DepartureSequence()
    {
        // Lock controls
        if (playerMovement != null)
            playerMovement.LockControls();

        // Dismiss prompt
        if (promptManager != null)
            promptManager.DismissPrompt();

        yield return new WaitForSeconds(1f);

        // === TBD Monologue ===
        if (subtitleUI != null)
        {
            subtitleUI.ShowSubtitle(
                "\"Ginawa ko ang trabaho. Palagi naming ginagawa ang trabaho. Hindi ko na naiisip kung bakit.\"",
                "(I did the work. We always do the work. I no longer think about why.)",
                6f
            );
        }

        yield return new WaitForSeconds(7f);

        // === Ledger cross-out animation ===
        if (ledgerUI != null)
        {
            ledgerUI.PlayCrossOutAnimation();
        }

        yield return new WaitForSecondsRealtime(4f);

        if (ledgerUI != null)
        {
            ledgerUI.ForceClose();
        }

        yield return new WaitForSeconds(1f);

        // === Fade to black ===
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float fadeDuration = 3f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadeOverlay.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
        }

        yield return new WaitForSeconds(1f);

        // === Loading screen text ===
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(true);
            loadingText.text = "Loading Level 1: Bahay ng Mangkukulam";

            // Fade in loading text
            Color textColor = loadingText.color;
            textColor.a = 0f;
            loadingText.color = textColor;

            float textFadeDuration = 2f;
            float textElapsed = 0f;
            while (textElapsed < textFadeDuration)
            {
                textElapsed += Time.deltaTime;
                textColor.a = Mathf.Lerp(0f, 1f, textElapsed / textFadeDuration);
                loadingText.color = textColor;
                yield return null;
            }
        }

        // Level complete — in a full game, this would load the next scene
        Debug.Log("=== LEVEL 0 COMPLETE — Tutorial Finished ===");
    }

    // ========== TRIGGER HANDLERS ==========

    /// <summary>
    /// Generic trigger handler — attach trigger zones and call these methods.
    /// </summary>
    public void OnTriggerZoneEntered(string zoneName)
    {
        switch (zoneName)
        {
            case "SalaEntry":
                OnPlayerEnteredSala();
                break;
            case "StairBottom":
                OnPlayerReachedStairBottom();
                break;
        }
    }
}
