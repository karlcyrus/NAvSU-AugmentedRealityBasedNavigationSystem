using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class ARWarmupManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject homePanel;           // Drag Panel_Destination here
    public TextMeshProUGUI statusText;     // Drag your status text here
    public Slider progressBar;             // Optional progress bar
    public Button retryButton;             // Drag a Retry button here (hidden by default)

    // Internal
    private PreplaceWorldObjects _nav;
    private Coroutine _warmupCoroutine;

    void Awake()
    {
        _nav = FindObjectOfType<PreplaceWorldObjects>();

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(false);
            retryButton.onClick.AddListener(OnRetry);
        }
    }

    public void StartLoading()
    {
        gameObject.SetActive(true);

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        if (_warmupCoroutine != null)
            StopCoroutine(_warmupCoroutine);

        _warmupCoroutine = StartCoroutine(WarmupRoutine());
    }

    IEnumerator WarmupRoutine()
    {
#if UNITY_EDITOR
        // EDITOR MODE: simulate warmup with a fixed countdown
        float editorTimer = 0f;
        float editorTarget = _nav != null ? _nav.StableSeconds : 10f;

        while (editorTimer < editorTarget)
        {
            editorTimer += Time.deltaTime;
            if (statusText != null)
                statusText.text = $"[Editor] Simulating localization... {editorTarget - editorTimer:F0}s";
            if (progressBar != null)
                progressBar.value = editorTimer / editorTarget;
            yield return null;
        }

        if (statusText != null) statusText.text = "Ready!";
        if (progressBar != null) progressBar.value = 1f;
        yield return new WaitForSeconds(0.5f);
        if (homePanel != null) homePanel.SetActive(false);
        gameObject.SetActive(false);

#else
        // DEVICE MODE: react to real GPS and WPS conditions

        // Phase 1: Wait for GPS to start running
        if (statusText != null) statusText.text = "Acquiring GPS signal...";
        if (progressBar != null) progressBar.value = 0f;

        float gpsTimeout = 15f;
        float gpsWait = 0f;
        while (Input.location.status != LocationServiceStatus.Running && gpsWait < gpsTimeout)
        {
            gpsWait += Time.deltaTime;
            if (statusText != null)
                statusText.text = $"Acquiring GPS signal... ({gpsTimeout - gpsWait:F0}s)";
            if (progressBar != null)
                progressBar.value = gpsWait / gpsTimeout * 0.2f;
            yield return null;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            ShowError("GPS unavailable. Move outdoors and try again.");
            yield break;
        }

        // Phase 2: Wait for WPS to become available
        if (statusText != null) statusText.text = "Connecting to AR positioning...";

        float wpsTimeout = 30f;
        float wpsWait = 0f;
        while (!_nav.IsWpsStable && wpsWait < wpsTimeout)
        {
            wpsWait += Time.deltaTime;
            float acc = _nav.GpsAccuracy;
            string accStr = acc >= 0 ? $"{acc:F0}m" : "--";
            if (statusText != null)
                statusText.text = $"Connecting to AR positioning... GPS: {accStr}";
            if (progressBar != null)
                progressBar.value = 0.2f + (wpsWait / wpsTimeout * 0.3f);
            yield return null;
        }

        if (!_nav.IsWpsStable)
        {
            ShowError("AR positioning failed. Move to an open area and try again.");
            yield break;
        }

        // Phase 3: Wait for GPS accuracy + stability timer
        bool wasReady = false;
        float stableTarget = _nav.StableSeconds;
        float threshold = _nav.GpsAccuracyThreshold;

        while (true)
        {
            bool isReady = _nav.IsLocalizationReady();
            float accuracy = _nav.GpsAccuracy;
            float stableDuration = _nav.StableDuration;

            if (!isReady)
            {
                wasReady = false;
                if (!_nav.IsWpsStable)
                {
                    if (statusText != null)
                        statusText.text = "AR positioning lost. Reconnecting...";
                }
                else
                {
                    string accStr = accuracy >= 0 ? $"{accuracy:F0}m" : "--";
                    if (statusText != null)
                        statusText.text = $"Improving GPS accuracy... {accStr} (need <{threshold:F0}m)\nMove to an open area with clear sky view.";
                }
                if (progressBar != null)
                    progressBar.value = 0.5f;
            }
            else
            {
                if (!wasReady)
                {
                    wasReady = true;
                    if (statusText != null) statusText.text = "Good signal! Stabilizing...";
                }

                float stabilityProgress = stableDuration / stableTarget;
                if (statusText != null)
                    statusText.text = $"Stabilizing AR... {stableDuration:F0}/{stableTarget:F0}s\nGPS accuracy: {accuracy:F1}m";
                if (progressBar != null)
                    progressBar.value = 0.5f + stabilityProgress * 0.5f;

                if (stableDuration >= stableTarget)
                {
                    if (statusText != null) statusText.text = "Localization ready!";
                    if (progressBar != null) progressBar.value = 1f;
                    yield return new WaitForSeconds(0.5f);
                    if (homePanel != null) homePanel.SetActive(false);
                    gameObject.SetActive(false);
                    yield break;
                }
            }

            yield return null;
        }
#endif
    }

    private void ShowError(string message)
    {
        if (statusText != null) statusText.text = message;
        if (progressBar != null) progressBar.value = 0f;
        if (retryButton != null) retryButton.gameObject.SetActive(true);
        Debug.LogWarning("Warmup error: " + message);
    }

    private void OnRetry()
    {
        if (_nav != null)
            _nav.ClearNavigation();
        if (homePanel != null)
            homePanel.SetActive(true);
        gameObject.SetActive(false);
    }
}