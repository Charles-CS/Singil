using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Displays timed subtitles for NPC dialogue and TBD monologues.
/// Shows Tagalog text with English translation below.
/// </summary>
public class SubtitleUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI mainText;      // Tagalog / main dialogue
    public TextMeshProUGUI subtitleText;   // English translation (italic)
    public CanvasGroup canvasGroup;
    public Image backgroundImage;

    [Header("Settings")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.5f;

    private Coroutine currentSubtitleCoroutine;

    void Start()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Show a subtitle with main text and optional translation.
    /// </summary>
    public void ShowSubtitle(string main, string translation, float duration)
    {
        if (currentSubtitleCoroutine != null)
            StopCoroutine(currentSubtitleCoroutine);

        currentSubtitleCoroutine = StartCoroutine(SubtitleRoutine(main, translation, duration));
    }

    /// <summary>
    /// Show a simple subtitle (no translation).
    /// </summary>
    public void ShowSimpleSubtitle(string text, float duration)
    {
        ShowSubtitle(text, "", duration);
    }

    /// <summary>
    /// Immediately hide subtitles.
    /// </summary>
    public void HideSubtitle()
    {
        if (currentSubtitleCoroutine != null)
            StopCoroutine(currentSubtitleCoroutine);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private IEnumerator SubtitleRoutine(string main, string translation, float duration)
    {
        // Set text
        if (mainText != null)
            mainText.text = main;
        if (subtitleText != null)
        {
            subtitleText.text = translation;
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(translation));
        }

        // Fade in
        yield return Fade(0f, 1f, fadeInDuration);

        // Wait
        yield return new WaitForSecondsRealtime(duration);

        // Fade out
        yield return Fade(1f, 0f, fadeOutDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    /// <summary>
    /// Creates the SubtitleUI hierarchy under the given canvas.
    /// </summary>
    public static SubtitleUI CreateSubtitleUI(Canvas canvas)
    {
        // Subtitle container at bottom-third
        GameObject containerObj = new GameObject("SubtitleContainer");
        containerObj.transform.SetParent(canvas.transform, false);

        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.18f);
        containerRect.anchorMax = new Vector2(0.9f, 0.32f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        CanvasGroup cg = containerObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Background
        Image bgImage = containerObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f);

        // Main text (Tagalog)
        GameObject mainObj = new GameObject("MainText");
        mainObj.transform.SetParent(containerObj.transform, false);

        RectTransform mainRect = mainObj.AddComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.05f, 0.45f);
        mainRect.anchorMax = new Vector2(0.95f, 0.95f);
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;

        TextMeshProUGUI mainTmp = mainObj.AddComponent<TextMeshProUGUI>();
        mainTmp.text = "";
        mainTmp.fontSize = 22;
        mainTmp.color = new Color(0.95f, 0.92f, 0.85f);
        mainTmp.alignment = TextAlignmentOptions.Center;
        mainTmp.fontStyle = FontStyles.Italic;
        mainTmp.enableWordWrapping = true;

        // Translation text (English)
        GameObject subObj = new GameObject("TranslationText");
        subObj.transform.SetParent(containerObj.transform, false);

        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.05f, 0.05f);
        subRect.anchorMax = new Vector2(0.95f, 0.45f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "";
        subTmp.fontSize = 17;
        subTmp.color = new Color(0.7f, 0.68f, 0.62f);
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.fontStyle = FontStyles.Italic;
        subTmp.enableWordWrapping = true;

        // Create manager component
        SubtitleUI subtitle = containerObj.AddComponent<SubtitleUI>();
        subtitle.mainText = mainTmp;
        subtitle.subtitleText = subTmp;
        subtitle.canvasGroup = cg;
        subtitle.backgroundImage = bgImage;

        return subtitle;
    }
}
