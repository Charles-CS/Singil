using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Full-screen Ledger overlay toggled with Tab.
/// Displays debtor information, debt details, collateral, and investigation annotations.
/// </summary>
public class LedgerUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject ledgerPanel;
    public TextMeshProUGUI debtorNameText;
    public TextMeshProUGUI debtAmountText;
    public TextMeshProUGUI collateralText;
    public TextMeshProUGUI annotationsText;
    public TextMeshProUGUI statusText; // For "COLLATERAL SEIZED. DEBT CLOSED."
    public Image crossOutImage; // Black ink cross-out overlay

    [Header("Ledger Data")]
    public string debtorName = "Escolastica \"Coring\" Mendoza";
    public string debtAmount = "₱4,200 (Compounded)";
    public string collateral = "Rocking Chair / Life";

    [Header("Settings")]
    public float fadeSpeed = 5f;

    private List<string> annotations = new List<string>();
    private bool isOpen = false;
    private bool isEnabled = true;
    private CanvasGroup canvasGroup;
    private float targetAlpha = 0f;

    void Awake()
    {
        // Create canvas group for fade
        if (ledgerPanel != null)
        {
            canvasGroup = ledgerPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = ledgerPanel.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }

        if (crossOutImage != null)
        {
            crossOutImage.gameObject.SetActive(false);
        }

        UpdateDisplay();
    }

    void Update()
    {
        bool togglePressed = false;

        // Check new input system for Tab key
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            togglePressed = true;
        }

        // Legacy input is disabled in Player Settings, so we must rely entirely on Keyboard.current

        // Toggle with Tab
        if (togglePressed)
        {
            if (isEnabled)
            {
                ToggleLedger();
            }
            else
            {
                Debug.Log("LedgerUI: Tab key pressed, but Ledger is NOT enabled yet.");
            }
        }

        // Smooth fade
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            if (targetAlpha == 0f && canvasGroup.alpha < 0.01f)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }

    public void ToggleLedger()
    {
        Debug.Log("LedgerUI: ToggleLedger CALLED!");
        if (isOpen)
            CloseLedger();
        else
            OpenLedger();
    }

    public void OpenLedger()
    {
        Debug.Log("LedgerUI: OpenLedger CALLED!");
        isOpen = true;
        targetAlpha = 1f;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f; // Force instant visibility
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        UpdateDisplay();

        // Pause ambient (placeholder)
        Time.timeScale = 0f;
    }

    public void CloseLedger()
    {
        isOpen = false;
        targetAlpha = 0f;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Force-open the ledger (for scripted sequences).
    /// </summary>
    public void ForceOpen()
    {
        OpenLedger();
    }

    /// <summary>
    /// Force-close the ledger.
    /// </summary>
    public void ForceClose()
    {
        CloseLedger();
    }

    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
    }

    public bool IsOpen => isOpen;

    /// <summary>
    /// Add an annotation to the ledger from examining objects.
    /// </summary>
    public void AddAnnotation(string annotation)
    {
        if (!annotations.Contains(annotation))
        {
            annotations.Add(annotation);
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Show the debt closed status in large text.
    /// </summary>
    public void ShowDebtClosed()
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "COLLATERAL SEIZED.\nDEBT CLOSED.";
        }
        ForceOpen();
    }

    /// <summary>
    /// Play the cross-out animation for Phase 5 departure.
    /// </summary>
    public void PlayCrossOutAnimation()
    {
        ForceOpen();
        if (crossOutImage != null)
        {
            crossOutImage.gameObject.SetActive(true);
            // Simple scale animation - crossOut grows from left to right
            StartCoroutine(AnimateCrossOut());
        }
    }

    private System.Collections.IEnumerator AnimateCrossOut()
    {
        if (crossOutImage == null) yield break;

        RectTransform rt = crossOutImage.GetComponent<RectTransform>();
        float elapsed = 0f;
        float duration = 1.5f;

        // Start from zero width
        Vector2 originalSize = rt.sizeDelta;
        rt.sizeDelta = new Vector2(0, originalSize.y);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Ease out cubic
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rt.sizeDelta = new Vector2(originalSize.x * eased, originalSize.y);
            yield return null;
        }

        rt.sizeDelta = originalSize;
    }

    private void UpdateDisplay()
    {
        if (debtorNameText != null)
            debtorNameText.text = $"Debtor: {debtorName}";
        if (debtAmountText != null)
            debtAmountText.text = $"Debt: {debtAmount}";
        if (collateralText != null)
            collateralText.text = $"Collateral: {collateral}";

        if (annotationsText != null)
        {
            if (annotations.Count > 0)
            {
                string annotationList = "--- Annotations ---\n";
                foreach (string a in annotations)
                {
                    annotationList += $"• {a}\n";
                }
                annotationsText.text = annotationList;
            }
            else
            {
                annotationsText.text = "";
            }
        }
    }

    /// <summary>
    /// Creates the full Ledger UI hierarchy under the given canvas.
    /// Call this from TutorialSceneSetup to build UI at runtime.
    /// </summary>
    public static LedgerUI CreateLedgerUI(Canvas canvas)
    {
        // Ledger panel (full screen dark overlay)
        GameObject panelObj = new GameObject("LedgerPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.06f, 0.04f, 0.92f); // Dark parchment

        // Inner content area
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(panelObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.15f, 0.1f);
        contentRect.anchorMax = new Vector2(0.85f, 0.9f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // Title: "THE LEDGER"
        TextMeshProUGUI titleText = CreateTMPText(contentObj.transform, "Title",
            "THE LEDGER", 36, new Color(0.82f, 0.68f, 0.42f),
            new Vector2(0f, 0.85f), new Vector2(1f, 1f));
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        // Divider line
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(contentObj.transform, false);
        RectTransform divRect = divider.AddComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.1f, 0.83f);
        divRect.anchorMax = new Vector2(0.9f, 0.835f);
        divRect.offsetMin = Vector2.zero;
        divRect.offsetMax = Vector2.zero;
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.82f, 0.68f, 0.42f, 0.5f);

        // Debtor Name
        TextMeshProUGUI debtorText = CreateTMPText(contentObj.transform, "DebtorName",
            "Debtor: ---", 24, new Color(0.9f, 0.85f, 0.75f),
            new Vector2(0f, 0.7f), new Vector2(1f, 0.8f));

        // Debt Amount
        TextMeshProUGUI debtText = CreateTMPText(contentObj.transform, "DebtAmount",
            "Debt: ---", 22, new Color(0.9f, 0.85f, 0.75f),
            new Vector2(0f, 0.6f), new Vector2(1f, 0.7f));

        // Collateral
        TextMeshProUGUI collText = CreateTMPText(contentObj.transform, "Collateral",
            "Collateral: ---", 22, new Color(0.85f, 0.55f, 0.55f),
            new Vector2(0f, 0.5f), new Vector2(1f, 0.6f));

        // Annotations
        TextMeshProUGUI annoText = CreateTMPText(contentObj.transform, "Annotations",
            "", 18, new Color(0.75f, 0.72f, 0.65f),
            new Vector2(0f, 0.1f), new Vector2(1f, 0.48f));
        annoText.alignment = TextAlignmentOptions.TopLeft;

        // Status text (hidden initially)
        TextMeshProUGUI statText = CreateTMPText(contentObj.transform, "StatusText",
            "", 42, new Color(0.85f, 0.2f, 0.15f),
            new Vector2(0f, 0.3f), new Vector2(1f, 0.6f));
        statText.alignment = TextAlignmentOptions.Center;
        statText.fontStyle = FontStyles.Bold;
        statText.gameObject.SetActive(false);

        // Cross-out image (hidden initially)
        GameObject crossOut = new GameObject("CrossOut");
        crossOut.transform.SetParent(contentObj.transform, false);
        RectTransform crossRect = crossOut.AddComponent<RectTransform>();
        crossRect.anchorMin = new Vector2(0f, 0.45f);
        crossRect.anchorMax = new Vector2(1f, 0.85f);
        crossRect.offsetMin = Vector2.zero;
        crossRect.offsetMax = Vector2.zero;
        Image crossImg = crossOut.AddComponent<Image>();
        crossImg.color = new Color(0.1f, 0.08f, 0.06f, 0.85f);
        crossOut.SetActive(false);

        // Add hint text at bottom
        TextMeshProUGUI hintText = CreateTMPText(panelObj.transform, "HintText",
            "Press [Tab] to close", 16, new Color(0.6f, 0.55f, 0.45f, 0.7f),
            new Vector2(0f, 0.02f), new Vector2(1f, 0.06f));
        hintText.alignment = TextAlignmentOptions.Center;

        // Create and configure LedgerUI component
        LedgerUI ledger = panelObj.AddComponent<LedgerUI>();
        ledger.ledgerPanel = panelObj;
        ledger.canvasGroup = panelObj.AddComponent<CanvasGroup>();
        ledger.canvasGroup.alpha = 0f; // Start hidden
        ledger.debtorNameText = debtorText;
        ledger.debtAmountText = debtText;
        ledger.collateralText = collText;
        ledger.annotationsText = annoText;
        ledger.statusText = statText;
        ledger.crossOutImage = crossImg;

        return ledger;
    }

    private static TextMeshProUGUI CreateTMPText(Transform parent, string name,
        string text, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        return tmp;
    }
}
