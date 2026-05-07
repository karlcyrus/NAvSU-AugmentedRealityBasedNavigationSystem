using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NavigationTopCardUI : MonoBehaviour
{
    [SerializeField] private PreplaceWorldObjects nav;
    [SerializeField] private TextMeshProUGUI destinationText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private CanvasGroup cardCanvasGroup;

    [Tooltip("Add any sibling panels here that should show/hide together with TopNavCard")]
    [SerializeField] private List<CanvasGroup> extraPanels = new List<CanvasGroup>();

    private float _timer;
    private int _lastShownMeters = -1;
    private string _lastDestinationLabel = "";

    void Start()
    {
        if (nav == null) nav = FindObjectOfType<PreplaceWorldObjects>();

        if (cardCanvasGroup == null)
            cardCanvasGroup = GetComponent<CanvasGroup>();
        if (cardCanvasGroup == null)
            cardCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Start hidden — shows automatically once navigation is active
        SetCardVisible(false);
    }

    void Update()
    {
        if (nav == null || !nav.HasActiveNavigation)
        {
            SetCardVisible(false);
            _lastShownMeters = -1;
            _lastDestinationLabel = "";
            return;
        }

        SetCardVisible(true);

        // Throttle updates to every updateInterval seconds
        _timer += Time.deltaTime;
        if (_timer < updateInterval) return;
        _timer = 0f;

        if (destinationText == null || distanceText == null) return;

        // Update destination label only if it changed
        string label = nav.CurrentDestinationLabel;
        if (label != _lastDestinationLabel)
        {
            _lastDestinationLabel = label;
            destinationText.text = label;
        }

        // Update distance
        if (!nav.TryGetCurrentUserLatLng(out double userLat, out double userLng) ||
            !nav.TryGetDestinationLatLng(out double destLat, out double destLng))
        {
            distanceText.text = "-- m";
            return;
        }

        float rawMeters = (float)nav.HaversineMeters(userLat, userLng, destLat, destLng);

        // Round to nearest 5m bucket (50m, 45m, 40m...)
        int bucketMeters = Mathf.Max(0, Mathf.RoundToInt(rawMeters / 5f) * 5);

        // Only update text when bucket changes to avoid flickering
        if (bucketMeters != _lastShownMeters)
        {
            _lastShownMeters = bucketMeters;
            if (bucketMeters == 0)
                distanceText.text = "You have arrived";
            else
                distanceText.text = $"{bucketMeters} m";
        }
    }

    private void SetCardVisible(bool visible)
    {
        // TopNavCard visibility
        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.alpha = visible ? 1f : 0f;
            cardCanvasGroup.interactable = visible;
            cardCanvasGroup.blocksRaycasts = visible;
        }

        // Extra sibling panels — all show/hide together with TopNavCard
        foreach (var panel in extraPanels)
        {
            if (panel == null) continue;
            panel.alpha = visible ? 1f : 0f;
            panel.interactable = visible;
            panel.blocksRaycasts = visible;
        }
    }
}