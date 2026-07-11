using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Presence Sense mechanic activated by holding Right Mouse Button.
/// Creates a radial proximity pulse indicating debtor nearness.
/// Peripheral screen distortion shows proximity but not direction.
/// </summary>
public class PresenceSense : MonoBehaviour
{
    [Header("Settings")]
    public float maxRange = 20f;
    public float pulseInterval = 1.5f;   // Time between pulses
    public float pulseDuration = 0.8f;   // How long each pulse visual lasts
    public float minDistortion = 0.1f;   // Distortion at max range
    public float maxDistortion = 0.8f;   // Distortion at close range

    [Header("UI References")]
    public Image vignetteOverlay;         // Full-screen vignette image
    public Image pulseRingImage;          // Expanding ring visual

    [Header("State")]
    public bool isEnabled = false;        // Enabled during Hunt phase only
    public bool isActive = false;

    private Transform debtorTransform;
    private float pulseTimer = 0f;
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;

    void Update()
    {
        if (!isEnabled) return;

        // Check RMB hold
        if (Mouse.current != null)
        {
            bool holdingRMB = Mouse.current.rightButton.isPressed;

            if (holdingRMB && !isActive)
            {
                ActivateSense();
            }
            else if (!holdingRMB && isActive)
            {
                DeactivateSense();
            }
        }

        if (isActive)
        {
            UpdateSense();
        }

        // Smooth intensity transitions
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 5f);
        UpdateVisuals();
    }

    public void SetDebtorTransform(Transform debtor)
    {
        debtorTransform = debtor;
    }

    public void Enable()
    {
        isEnabled = true;
    }

    public void Disable()
    {
        isEnabled = false;
        DeactivateSense();
    }

    private void ActivateSense()
    {
        isActive = true;
        pulseTimer = 0f;
    }

    private void DeactivateSense()
    {
        isActive = false;
        targetIntensity = 0f;
    }

    private void UpdateSense()
    {
        if (debtorTransform == null) return;

        // Calculate distance to debtor
        float distance = Vector3.Distance(transform.position, debtorTransform.position);
        float normalizedDistance = Mathf.Clamp01(distance / maxRange);

        // Closer = more intense
        float intensity = Mathf.Lerp(maxDistortion, minDistortion, normalizedDistance);

        // Pulse timing
        pulseTimer += Time.deltaTime;
        if (pulseTimer >= pulseInterval)
        {
            pulseTimer = 0f;
            StartCoroutine(PulseEffect(intensity));
        }

        // Continuous subtle vignette while holding
        targetIntensity = intensity * 0.3f; // Subtle constant effect
    }

    private IEnumerator PulseEffect(float intensity)
    {
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;

            // Pulse grows then fades: sin curve
            float pulseStrength = Mathf.Sin(t * Mathf.PI) * intensity;
            targetIntensity = pulseStrength;

            // Update pulse ring scale
            if (pulseRingImage != null)
            {
                float scale = Mathf.Lerp(0.3f, 1.5f, t);
                pulseRingImage.transform.localScale = Vector3.one * scale;
                Color ringColor = pulseRingImage.color;
                ringColor.a = (1f - t) * 0.5f * intensity;
                pulseRingImage.color = ringColor;
            }

            yield return null;
        }

        targetIntensity = 0f;
    }

    private void UpdateVisuals()
    {
        // Vignette overlay
        if (vignetteOverlay != null)
        {
            Color c = vignetteOverlay.color;
            c.a = currentIntensity * 0.6f;
            vignetteOverlay.color = c;
        }
    }

    /// <summary>
    /// Creates Presence Sense UI elements under the given canvas.
    /// </summary>
    public static PresenceSense CreatePresenceSenseUI(Canvas canvas, GameObject playerObj)
    {
        // Vignette overlay (full screen, radial gradient simulated with solid color)
        GameObject vignetteObj = new GameObject("PresenceVignette");
        vignetteObj.transform.SetParent(canvas.transform, false);

        RectTransform vigRect = vignetteObj.AddComponent<RectTransform>();
        vigRect.anchorMin = Vector2.zero;
        vigRect.anchorMax = Vector2.one;
        vigRect.offsetMin = Vector2.zero;
        vigRect.offsetMax = Vector2.zero;

        Image vigImage = vignetteObj.AddComponent<Image>();
        vigImage.color = new Color(0.3f, 0.0f, 0.15f, 0f); // Dark purple, invisible initially
        vigImage.raycastTarget = false;

        // Pulse ring (center of screen)
        GameObject ringObj = new GameObject("PulseRing");
        ringObj.transform.SetParent(canvas.transform, false);

        RectTransform ringRect = ringObj.AddComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0.35f, 0.3f);
        ringRect.anchorMax = new Vector2(0.65f, 0.7f);
        ringRect.offsetMin = Vector2.zero;
        ringRect.offsetMax = Vector2.zero;

        Image ringImage = ringObj.AddComponent<Image>();
        ringImage.color = new Color(0.7f, 0.3f, 0.5f, 0f); // Transparent initially
        ringImage.raycastTarget = false;

        // Create component on player
        PresenceSense sense = playerObj.AddComponent<PresenceSense>();
        sense.vignetteOverlay = vigImage;
        sense.pulseRingImage = ringImage;

        return sense;
    }
}
