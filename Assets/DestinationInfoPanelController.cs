using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class BuildingDescriptionCollection
{
    public List<BuildingDescriptionEntry> buildings;
}

[Serializable]
public class BuildingDescriptionEntry
{
    public string office_id;
    public string title;
    public string description;
    public string hours;
}

public class DestinationInfoPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _infoPanel;
    [SerializeField] private PreplaceWorldObjects _navManager;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _hoursText;

    [Header("Fallback Text")]
    [SerializeField] private string _missingDescriptionText = "No description available yet.";
    [SerializeField] private string _missingHoursText = "Hours not available.";

    private readonly Dictionary<string, BuildingDescriptionEntry> _descriptionMap = new();
    private bool _isVisible;

    void Start()
    {
        if (_navManager == null)
            _navManager = FindObjectOfType<PreplaceWorldObjects>();

        LoadDescriptions();
        Hide();
    }

    public void Toggle()
    {
        if (_isVisible)
        {
            Hide();
            return;
        }

        RefreshContent();
        Show();
    }

    public void Hide()
    {
        _isVisible = false;
        if (_infoPanel != null)
            _infoPanel.SetActive(false);
    }

    private void Show()
    {
        _isVisible = true;
        if (_infoPanel != null)
            _infoPanel.SetActive(true);
    }

    private void LoadDescriptions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("building_descriptions");
        if (jsonFile == null)
        {
            Debug.LogWarning("[DestinationInfo] building_descriptions.json not found in Assets/Resources/.");
            return;
        }

        BuildingDescriptionCollection data = JsonUtility.FromJson<BuildingDescriptionCollection>(jsonFile.text);
        if (data?.buildings == null)
            return;

        _descriptionMap.Clear();
        foreach (var entry in data.buildings)
        {
            if (!string.IsNullOrWhiteSpace(entry.office_id))
                _descriptionMap[entry.office_id] = entry;
        }
    }

    private void RefreshContent()
    {
        string currentOfficeId = _navManager != null ? _navManager.CurrentOfficeId : "";
        string fallbackTitle = _navManager != null ? _navManager.CurrentDestinationLabel : "Destination";

        BuildingDescriptionEntry entry = null;
        if (!string.IsNullOrEmpty(currentOfficeId))
            _descriptionMap.TryGetValue(currentOfficeId, out entry);

        if (_titleText != null)
            _titleText.text = entry != null && !string.IsNullOrWhiteSpace(entry.title)
                ? entry.title
                : fallbackTitle;

        if (_descriptionText != null)
            _descriptionText.text = entry != null && !string.IsNullOrWhiteSpace(entry.description)
                ? entry.description
                : _missingDescriptionText;

        if (_hoursText != null)
            _hoursText.text = entry != null && !string.IsNullOrWhiteSpace(entry.hours)
                ? entry.hours
                : _missingHoursText;
    }
}
