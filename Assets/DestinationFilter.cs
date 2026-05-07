using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ---------------------------------------------
//  DestinationFilter.cs
//  Attach to UIManager or any active GameObject.
// ---------------------------------------------

public class DestinationFilter : MonoBehaviour
{
    public enum FilterMode { All, Office, Building, Facility, Department }

    [Header("Filter Buttons")]
    [SerializeField] private Button _btnAll;
    [SerializeField] private Button _btnOffices;
    [SerializeField] private Button _btnBuildings;
    [SerializeField] private Button _btnFacilities;
    [SerializeField] private Button _btnDepartments;

    [Header("Active Tab Colors")]
    [SerializeField] private Color _activeColor = new Color(0.18f, 0.80f, 0.44f, 1f);
    [SerializeField] private Color _inactiveColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _activeTextColor = Color.black;
    [SerializeField] private Color _inactiveTextColor = Color.white;

    [Header("Search")]
    [SerializeField] private TMP_InputField _searchInput;

    [Header("Content")]
    [SerializeField] private Transform _scrollContent;

    // Internal
    private FilterMode _currentFilter = FilterMode.All;
    private string _searchQuery = "";
    private List<DestinationButton> _allButtons = new();

    void Start()
    {
        _allButtons.AddRange(_scrollContent.GetComponentsInChildren<DestinationButton>(true));

        // Wire filter buttons
        if (_btnAll != null) _btnAll.onClick.AddListener(() => SetFilter(FilterMode.All));
        if (_btnOffices != null) _btnOffices.onClick.AddListener(() => SetFilter(FilterMode.Office));
        if (_btnBuildings != null) _btnBuildings.onClick.AddListener(() => SetFilter(FilterMode.Building));
        if (_btnFacilities != null) _btnFacilities.onClick.AddListener(() => SetFilter(FilterMode.Facility));
        if (_btnDepartments != null) _btnDepartments.onClick.AddListener(() => SetFilter(FilterMode.Department));

        // Wire search
        if (_searchInput != null)
            _searchInput.onValueChanged.AddListener(OnSearchChanged);

        // Default to All
        SetFilter(FilterMode.All);
    }

    // ---------------------------------------------
    //  Public — called by CategoryScreenManager
    //  when a category card is tapped
    // ---------------------------------------------
    public void SetFilter(FilterMode mode)
    {
        _currentFilter = mode;
        UpdateTabColors();
        ApplyFilter();
    }

    // Convenience overload for Inspector OnClick string calls if needed
    public void SetFilterAll() => SetFilter(FilterMode.All);
    public void SetFilterOffices() => SetFilter(FilterMode.Office);
    public void SetFilterBuildings() => SetFilter(FilterMode.Building);
    public void SetFilterFacilities() => SetFilter(FilterMode.Facility);
    public void SetFilterDepartments() => SetFilter(FilterMode.Department);

    // ---------------------------------------------
    //  Called when search text changes
    // ---------------------------------------------
    private void OnSearchChanged(string query)
    {
        _searchQuery = query.ToLower().Trim();
        ApplyFilter();
    }

    // ---------------------------------------------
    //  Apply current filter + search
    // ---------------------------------------------
    private void ApplyFilter()
    {
        foreach (var btn in _allButtons)
        {
            bool categoryMatch = _currentFilter == FilterMode.All ||
                                 btn.category.ToString() == _currentFilter.ToString();

            bool searchMatch = string.IsNullOrEmpty(_searchQuery) ||
                               btn.gameObject.name.ToLower().Contains(_searchQuery) ||
                               GetButtonLabel(btn).ToLower().Contains(_searchQuery);

            btn.gameObject.SetActive(categoryMatch && searchMatch);
        }
    }

    private string GetButtonLabel(DestinationButton btn)
    {
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        return tmp != null ? tmp.text : btn.gameObject.name;
    }

    // ---------------------------------------------
    //  Update tab colors
    // ---------------------------------------------
    private void UpdateTabColors()
    {
        SetTabStyle(_btnAll, _currentFilter == FilterMode.All);
        SetTabStyle(_btnOffices, _currentFilter == FilterMode.Office);
        SetTabStyle(_btnBuildings, _currentFilter == FilterMode.Building);
        SetTabStyle(_btnFacilities, _currentFilter == FilterMode.Facility);
        SetTabStyle(_btnDepartments, _currentFilter == FilterMode.Department);
    }

    private void SetTabStyle(Button btn, bool isActive)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = isActive ? _activeColor : _inactiveColor;
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.color = isActive ? _activeTextColor : _inactiveTextColor;
    }
}