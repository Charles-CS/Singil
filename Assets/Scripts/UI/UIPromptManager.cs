using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Displays bottom-center UI prompts with fade-in/fade-out.
/// Prompts guide the player through tutorial actions.
/// </summary>
public class UIPromptManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI promptText;
    public CanvasGroup canvasGroup;
    public Image backgroundImage;

    [Header("Settings")]
    public float fadeInDuration = 0.4f;
    public float fadeOutDuration = 0.6f;
    public float defaultDuration = 5f;

    private Coroutine currentPromptCoroutine;
    private bool isPersistent = false; // If true, prompt stays until manually dismissed

    void Start()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Show a prompt for a specified duration.
    /// </summary>
    public void ShowPrompt(string text, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        isPersistent = false;

        if (currentPromptCoroutine != null)
            StopCoroutine(currentPromptCoroutine);

        currentPromptCoroutine = StartCoroutine(ShowPromptRoutine(text, duration));
    }

    /// <summary>
    /// Show a persistent prompt that stays until DismissPrompt() is called.
    /// </summary>
    public void ShowPersistentPrompt(string text)
    {
        isPersistent = true;

        if (currentPromptCoroutine != null)
            StopCoroutine(currentPromptCoroutine);

        currentPromptCoroutine = StartCoroutine(ShowPersistentRoutine(text));
    }

    /// <summary>
    /// Dismiss the current prompt.
    /// </summary>
    public void DismissPrompt()
    {
        isPersistent = false;

        if (currentPromptCoroutine != null)
            StopCoroutine(currentPromptCoroutine);

        currentPromptCoroutine = StartCoroutine(FadeOut());
    }

    /// <summary>
    /// Immediately hide the prompt without fade.
    /// </summary>
    public void HideImmediate()
    {
        isPersistent = false;

        if (currentPromptCoroutine != null)
            StopCoroutine(currentPromptCoroutine);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private IEnumerator ShowPromptRoutine(string text, float duration)
    {
        // Set text
        if (promptText != null)
            promptText.text = FormatPromptText(text);

        // Fade in
        yield return FadeInRoutine();

        // Wait for duration
        yield return new WaitForSecondsRealtime(duration);

        // Fade out
        yield return FadeOutRoutine();
    }

    private IEnumerator ShowPersistentRoutine(string text)
    {
        if (promptText != null)
            promptText.text = FormatPromptText(text);

        yield return FadeInRoutine();
        // Stays visible until DismissPrompt() is called
    }

    private IEnumerator FadeIn()
    {
        yield return FadeInRoutine();
    }

    private IEnumerator FadeOut()
    {
        yield return FadeOutRoutine();
    }

    private IEnumerator FadeInRoutine()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutRoutine()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Format text with key icons styled differently.
    /// Wraps [KEY] text in a highlight color tag.
    /// </summary>
    private string FormatPromptText(string text)
    {
        // Replace [KEY] patterns with colored versions
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]",
            "<color=#E8C86A>[$1]</color>");
        return text;
    }

    /// <summary>
    /// Creates the UIPromptManager UI hierarchy under the given canvas.
    /// </summary>
    public static UIPromptManager CreatePromptUI(Canvas canvas)
    {
        // Prompt container at bottom center
        GameObject containerObj = new GameObject("PromptContainer");
        containerObj.transform.SetParent(canvas.transform, false);

        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.15f, 0.05f);
        containerRect.anchorMax = new Vector2(0.85f, 0.15f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        CanvasGroup cg = containerObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Background
        Image bgImage = containerObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.65f);

        // Prompt text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(containerObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.1f);
        textRect.anchorMax = new Vector2(0.95f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 20;
        tmp.color = new Color(0.9f, 0.88f, 0.82f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;

        // Create manager
        UIPromptManager manager = containerObj.AddComponent<UIPromptManager>();
        manager.promptText = tmp;
        manager.canvasGroup = cg;
        manager.backgroundImage = bgImage;

        return manager;
    }
}
