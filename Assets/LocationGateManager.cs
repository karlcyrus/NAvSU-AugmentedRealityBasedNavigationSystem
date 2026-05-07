using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocationGateManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _gatePanel;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _openSettingsBtn;
    [SerializeField] private Button _exitAppBtn;

    [Header("Settings")]
    [Tooltip("How often (seconds) to check location permission while app is running")]
    [SerializeField] private float _checkInterval = 1.5f;
    [Tooltip("Delay before re-checking after returning from Settings")]
    [SerializeField] private float _recheckDelay = 1.0f;

    private bool _lastKnownState = true;

    void Start()
    {
        if (_openSettingsBtn != null)
            _openSettingsBtn.onClick.AddListener(OnOpenSettings);
        if (_exitAppBtn != null)
            _exitAppBtn.onClick.AddListener(OnExitApp);

        if (_gatePanel != null)
            _gatePanel.SetActive(false);

        StartCoroutine(InitialCheck());
        StartCoroutine(ContinuousCheck());
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            StartCoroutine(RecheckAfterReturn());
    }

    private IEnumerator InitialCheck()
    {
        yield return null;
#if UNITY_EDITOR
        Debug.Log("[LocationGate] Editor — skipping gate.");
        yield break;
#endif
        CheckAndUpdate();
    }

    private IEnumerator ContinuousCheck()
    {
#if UNITY_EDITOR
        yield break;
#endif
        while (true)
        {
            yield return new WaitForSeconds(_checkInterval);
            CheckAndUpdate();
        }
    }

    private IEnumerator RecheckAfterReturn()
    {
#if UNITY_EDITOR
        yield break;
#endif
        yield return new WaitForSeconds(_recheckDelay);
        CheckAndUpdate();
    }

    private void CheckAndUpdate()
    {
        bool isEnabled = Input.location.isEnabledByUser;
        if (isEnabled == _lastKnownState) return;
        _lastKnownState = isEnabled;

        if (!isEnabled)
            ShowGate();
        else
        {
            HideGate();
            Debug.Log("[LocationGate] Location services enabled — gate dismissed.");
        }
    }

    private void ShowGate()
    {
        if (_gatePanel != null) _gatePanel.SetActive(true);
        if (_messageText != null)
            _messageText.text = "Location services are required for AR navigation.\n\nPlease enable Location in your device settings to continue.";
        Debug.Log("[LocationGate] Location services off — showing gate.");
    }

    private void HideGate()
    {
        if (_gatePanel != null) _gatePanel.SetActive(false);
    }

    private void OnOpenSettings()
    {
        Debug.Log("[LocationGate] Opening device settings...");
#if UNITY_ANDROID
        using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = new AndroidJavaObject("android.content.Intent",
                                 "android.settings.LOCATION_SOURCE_SETTINGS"))
        {
            activity.Call("startActivity", intent);
        }
#elif UNITY_IOS
        Application.OpenURL("app-settings:");
#else
        Application.OpenURL("app-settings:");
#endif
    }

    private void OnExitApp()
    {
        Debug.Log("[LocationGate] User chose to exit.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}