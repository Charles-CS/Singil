using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Debt Mark mechanic - LMB click to bind a debtor when within range.
/// Shows a supernatural glyph flash on application.
/// Once applied, cannot be removed.
/// </summary>
public class DebtMark : MonoBehaviour
{
    [Header("Settings")]
    public float markRange = 3f;       // Must be within this distance to mark
    public float glyphFlashDuration = 1.5f;

    [Header("UI References")]
    public Image glyphOverlay;         // Supernatural glyph flash image

    [Header("State")]
    public bool isEnabled = false;     // Enabled during Hunt phase only
    public bool hasMarked = false;     // One-shot action

    private Transform debtorTransform;
    private LolaCoring debtorNPC;
    private GamePhaseManager phaseManager;

    void Start()
    {
        phaseManager = FindAnyObjectByType<GamePhaseManager>();
    }

    void Update()
    {
        if (!isEnabled || hasMarked) return;

        // Check LMB click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryMarkDebtor();
        }
    }

    public void SetDebtor(Transform debtor, LolaCoring npc)
    {
        debtorTransform = debtor;
        debtorNPC = npc;
    }

    public void Enable()
    {
        isEnabled = true;
    }

    public void Disable()
    {
        isEnabled = false;
    }

    private void TryMarkDebtor()
    {
        if (debtorTransform == null) return;

        float distance = Vector3.Distance(transform.position, debtorTransform.position);
        if (distance <= markRange)
        {
            ApplyMark();
        }
    }

    private void ApplyMark()
    {
        hasMarked = true;
        isEnabled = false;

        // Visual: supernatural glyph flash
        StartCoroutine(GlyphFlashRoutine());

        // Update debtor state
        if (debtorNPC != null)
        {
            debtorNPC.OnDebtMarked();
        }
    }

    private IEnumerator GlyphFlashRoutine()
    {
        if (glyphOverlay != null)
        {
            glyphOverlay.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < glyphFlashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / glyphFlashDuration;

                // Flash bright then fade
                float alpha;
                if (t < 0.2f)
                {
                    // Quick flash in
                    alpha = Mathf.Lerp(0f, 0.9f, t / 0.2f);
                }
                else
                {
                    // Slow fade out
                    alpha = Mathf.Lerp(0.9f, 0f, (t - 0.2f) / 0.8f);
                }

                Color c = glyphOverlay.color;
                c.a = alpha;
                glyphOverlay.color = c;

                // Pulsing scale
                float scale = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.1f * (1f - t);
                glyphOverlay.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            glyphOverlay.gameObject.SetActive(false);
        }

        // Short delay then trigger Lola's dialogue
        yield return new WaitForSeconds(0.5f);

        // Lola's response and cinematic pause
        if (debtorNPC != null)
        {
            debtorNPC.PlayMarkedDialogue();
        }
    }

    /// <summary>
    /// Creates Debt Mark UI elements under the given canvas.
    /// </summary>
    public static DebtMark CreateDebtMarkUI(Canvas canvas, GameObject playerObj)
    {
        // Glyph overlay (center screen flash)
        GameObject glyphObj = new GameObject("GlyphOverlay");
        glyphObj.transform.SetParent(canvas.transform, false);

        RectTransform glyphRect = glyphObj.AddComponent<RectTransform>();
        glyphRect.anchorMin = new Vector2(0.25f, 0.2f);
        glyphRect.anchorMax = new Vector2(0.75f, 0.8f);
        glyphRect.offsetMin = Vector2.zero;
        glyphRect.offsetMax = Vector2.zero;

        Image glyphImage = glyphObj.AddComponent<Image>();
        // Supernatural amber/gold color
        glyphImage.color = new Color(0.9f, 0.7f, 0.2f, 0f);
        glyphImage.raycastTarget = false;
        glyphObj.SetActive(false);

        // Create component on player
        DebtMark mark = playerObj.AddComponent<DebtMark>();
        mark.glyphOverlay = glyphImage;

        return mark;
    }
}
