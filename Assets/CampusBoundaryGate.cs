using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ---------------------------------------------
//  CampusBoundaryGate.cs
//  Blocks the app if the user is outside campus.
//
//  Setup:
//  1. Attach to an active GameObject (e.g. UIManager)
//  2. Assign the gate panel, message text, buttons
//  3. Panel shows if user is outside campus + 40m buffer
//  4. Auto-dismisses when user enters the boundary
// ---------------------------------------------

public class CampusBoundaryGate : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _gatePanel;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _exitButton;

    [Header("Campus Boundary (from CampusMap.pgw)")]
    [SerializeField] private double _campusMinLat = 14.39968116407;
    [SerializeField] private double _campusMaxLat = 14.40531232318;
    [SerializeField] private double _campusMinLng = 120.86452330938;
    [SerializeField] private double _campusMaxLng = 120.86769160989;

    [Header("Settings")]
    [Tooltip("Buffer zone in meters. Users within this distance of the boundary can still use the app.")]
    [SerializeField] private float _bufferMeters = 40f;
    [Tooltip("How often (seconds) to re-check location while gate is shown.")]
    [SerializeField] private float _recheckInterval = 3f;

    // Buffer converted to degrees (approximate)
    private double _latBuffer;
    private double _lngBuffer;

    // State
    private bool _isGateShown = false;
    private bool _initialCheckDone = false;

    void Start()
    {
        // Convert meter buffer to approximate degree offsets
        // 1 degree latitude ≈ 111,000 meters
        // 1 degree longitude ≈ 111,000 * cos(latitude) meters
        _latBuffer = _bufferMeters / 111000.0;
        double midLat = (_campusMinLat + _campusMaxLat) / 2.0;
        _lngBuffer = _bufferMeters / (111000.0 * System.Math.Cos(midLat * System.Math.PI / 180.0));

        if (_retryButton != null)
            _retryButton.onClick.AddListener(OnRetry);
        if (_exitButton != null)
            _exitButton.onClick.AddListener(OnExit);

        // Start hidden — will show after first GPS check
        if (_gatePanel != null)
            _gatePanel.SetActive(false);

        StartCoroutine(WaitForGPSThenCheck());
    }

    private IEnumerator WaitForGPSThenCheck()
    {
#if UNITY_EDITOR
        // In editor, skip boundary check
        Debug.Log("[BoundaryGate] Editor — skipping boundary check.");
        yield break;
#endif

        // Wait for GPS to become available
        float timeout = 20f;
        float waited = 0f;
        while (Input.location.status != LocationServiceStatus.Running && waited < timeout)
        {
            waited += 1f;
            yield return new WaitForSeconds(1f);
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            // GPS not available — don't block (LocationGateManager handles this)
            Debug.Log("[BoundaryGate] GPS not running, skipping boundary check.");
            yield break;
        }

        // Initial check
        CheckBoundary();
        _initialCheckDone = true;

        // Continuous re-check while gate is shown
        while (true)
        {
            yield return new WaitForSeconds(_recheckInterval);
            if (_isGateShown)
                CheckBoundary();
        }
    }

    private void CheckBoundary()
    {
        if (Input.location.status != LocationServiceStatus.Running)
            return;

        double userLat = Input.location.lastData.latitude;
        double userLng = Input.location.lastData.longitude;

        bool isInside = userLat >= (_campusMinLat - _latBuffer) &&
                        userLat <= (_campusMaxLat + _latBuffer) &&
                        userLng >= (_campusMinLng - _lngBuffer) &&
                        userLng <= (_campusMaxLng + _lngBuffer);

        Debug.Log($"[BoundaryGate] User at ({userLat:F6}, {userLng:F6}) — {(isInside ? "INSIDE" : "OUTSIDE")} campus boundary.");

        if (isInside && _isGateShown)
        {
            HideGate();
            Debug.Log("[BoundaryGate] User entered campus — gate dismissed.");
        }
        else if (!isInside && !_isGateShown)
        {
            ShowGate();
        }
    }

    private void ShowGate()
    {
        _isGateShown = true;
        if (_gatePanel != null)
            _gatePanel.SetActive(true);
        if (_messageText != null)
            _messageText.text = "You are outside the campus.\n\nPlease move within CCAT campus to use NaVSU.";
        Debug.Log("[BoundaryGate] User is outside campus — showing gate.");
    }

    private void HideGate()
    {
        _isGateShown = false;
        if (_gatePanel != null)
            _gatePanel.SetActive(false);
    }

    private void OnRetry()
    {
        Debug.Log("[BoundaryGate] Retry pressed — re-checking location.");
        CheckBoundary();
    }

    private void OnExit()
    {
        Debug.Log("[BoundaryGate] User chose to exit.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
