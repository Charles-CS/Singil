using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Phase 4 — Collection cinematic sequence.
/// Handles the soul collection execution, camera work, lighting changes, and Ledger finale.
/// </summary>
public class CollectionSequence : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Transform debtorTransform;
    public LolaCoring debtorNPC;
    public Light ambientLight;
    public LedgerUI ledgerUI;
    public SubtitleUI subtitleUI;
    public UIPromptManager promptManager;
    public PlayerMovement playerMovement;
    public GamePhaseManager phaseManager;

    [Header("Collection Visual")]
    public GameObject soulLight;  // Glowing light representing the soul

    [Header("Settings")]
    public float cinematicDuration = 12f;
    public Color dimLightColor = new Color(0.3f, 0.35f, 0.5f); // Cold blue
    public float dimLightIntensity = 0.3f;

    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private float originalLightIntensity;
    private Color originalLightColor;

    /// <summary>
    /// Begin the collection cinematic. Called when player presses E on bound debtor.
    /// </summary>
    public void StartCollection()
    {
        StartCoroutine(CollectionCinematic());
    }

    private IEnumerator CollectionCinematic()
    {
        // Lock player controls
        if (playerMovement != null)
            playerMovement.LockControls();

        // Dismiss any prompts
        if (promptManager != null)
            promptManager.HideImmediate();

        // Store original camera state
        if (mainCamera != null)
        {
            originalCameraPos = mainCamera.transform.position;
            originalCameraRot = mainCamera.transform.rotation;
        }

        // Store original lighting
        if (ambientLight != null)
        {
            originalLightIntensity = ambientLight.intensity;
            originalLightColor = ambientLight.color;
        }

        // === CINEMATIC CAMERA: Cut to close-up ===
        yield return new WaitForSeconds(0.5f);

        // Position camera for close-up on debtor
        if (mainCamera != null && debtorTransform != null)
        {
            Vector3 closeUpPos = debtorTransform.position + debtorTransform.forward * 1.2f + Vector3.up * 1.5f;
            mainCamera.transform.position = closeUpPos;
            mainCamera.transform.LookAt(debtorTransform.position + Vector3.up * 1.2f);
        }

        // === TBD places hands on debtor's shoulders ===
        yield return new WaitForSeconds(1.5f);

        // Lola's dialogue: "Teka... teka lang..."
        if (subtitleUI != null)
        {
            subtitleUI.ShowSubtitle(
                "\"Teka... teka lang. Wala na ba talagang ibang paraan? Parang awa mo na... natatakot ako.\"",
                "(Wait... just wait. Is there really no other way? Have mercy... I'm scared.)",
                5f
            );
        }

        yield return new WaitForSeconds(3f);

        // === SOUL COLLECTION: Hand to chest, pull back soul ===

        // Create soul light effect
        if (soulLight != null)
        {
            soulLight.SetActive(true);
            Light soulLightComp = soulLight.GetComponent<Light>();
            if (soulLightComp != null)
            {
                soulLightComp.color = new Color(1f, 0.9f, 0.7f); // Warm golden
                soulLightComp.intensity = 0f;
            }
        }

        // Gradually dim ambient lighting and brighten soul light
        float dimDuration = 4f;
        float dimElapsed = 0f;

        while (dimElapsed < dimDuration)
        {
            dimElapsed += Time.deltaTime;
            float t = dimElapsed / dimDuration;

            // Dim ambient light
            if (ambientLight != null)
            {
                ambientLight.intensity = Mathf.Lerp(originalLightIntensity, dimLightIntensity, t);
                ambientLight.color = Color.Lerp(originalLightColor, dimLightColor, t);
            }

            // Brighten soul light
            if (soulLight != null)
            {
                Light sl = soulLight.GetComponent<Light>();
                if (sl != null)
                {
                    // Bright peak in the middle, then fade
                    float soulIntensity;
                    if (t < 0.6f)
                        soulIntensity = Mathf.Lerp(0f, 3f, t / 0.6f);
                    else
                        soulIntensity = Mathf.Lerp(3f, 0f, (t - 0.6f) / 0.4f);
                    sl.intensity = soulIntensity;
                }

                // Move soul light upward as it's "pulled"
                soulLight.transform.position += Vector3.up * Time.deltaTime * 0.3f;
            }

            yield return null;
        }

        // === Soul fully collected, body slumps ===
        if (soulLight != null)
            soulLight.SetActive(false);

        // NPC collapse
        if (debtorNPC != null)
        {
            debtorNPC.OnCollected();
        }

        yield return new WaitForSeconds(2f);

        // === LEDGER: COLLATERAL SEIZED. DEBT CLOSED. ===
        if (ledgerUI != null)
        {
            ledgerUI.ShowDebtClosed();
        }

        yield return new WaitForSecondsRealtime(3f);

        // Close ledger and transition to Phase 5
        if (ledgerUI != null)
        {
            ledgerUI.ForceClose();
        }

        yield return new WaitForSeconds(1f);

        // Restore camera to player
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPos;
            mainCamera.transform.rotation = originalCameraRot;
        }

        // Transition to Departure
        if (phaseManager != null)
        {
            phaseManager.TransitionToPhase(GamePhase.Departure);
        }
    }
}
