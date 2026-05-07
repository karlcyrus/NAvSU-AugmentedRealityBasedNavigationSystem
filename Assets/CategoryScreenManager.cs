using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ---------------------------------------------
//  CategoryScreenManager.cs
//  Attach to UIManager or any active GameObject.
//
//  Controls the two-screen flow inside
//  Panel_Destination:
//   - CategoryScreen  -> shown first, user picks a category
//   - DestinationScreen -> shown after, scrollview filtered
// ---------------------------------------------

public class CategoryScreenManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject _categoryScreen;    // shown first
    [SerializeField] private GameObject _destinationScreen; // shown after category picked

    [Header("Category Cards (in CategoryScreen)")]
    [SerializeField] private Button _cardAll;
    [SerializeField] private Button _cardBuildings;
    [SerializeField] private Button _cardOffices;
    [SerializeField] private Button _cardFacilities;
    [SerializeField] private Button _cardDepartments;

    [Header("Back Button (in DestinationScreen)")]
    [SerializeField] private Button _backButton;

    [Header("Top Text")]
    [SerializeField] private TextMeshProUGUI _selectDestinationText;
    [SerializeField] private string _defaultTitle = "Choose Destination";

    [Header("References")]
    [SerializeField] private DestinationFilter _filter; // drag UIManager or wherever DestinationFilter is
    [SerializeField] private PreplaceWorldObjects _navManager; // drag NavigationManager to clear warmup

    void Start()
    {
        if (_cardAll != null) _cardAll.onClick.AddListener(() => SelectCategory(DestinationFilter.FilterMode.All));
        if (_cardBuildings != null) _cardBuildings.onClick.AddListener(() => SelectCategory(DestinationFilter.FilterMode.Building));
        if (_cardOffices != null) _cardOffices.onClick.AddListener(() => SelectCategory(DestinationFilter.FilterMode.Office));
        if (_cardFacilities != null) _cardFacilities.onClick.AddListener(() => SelectCategory(DestinationFilter.FilterMode.Facility));
        if (_cardDepartments != null) _cardDepartments.onClick.AddListener(() => SelectCategory(DestinationFilter.FilterMode.Department));

        if (_backButton != null) _backButton.onClick.AddListener(GoBack);

        ShowCategoryScreen();
    }

    public void ResetToCategories()
    {
        ShowCategoryScreen();
    }

    private void SelectCategory(DestinationFilter.FilterMode mode)
    {
        // Clear any background warmup objects before user picks a real destination
        if (_navManager != null)
            _navManager.ClearNavigation();

        if (_filter != null)
            _filter.SetFilter(mode);

        if (_selectDestinationText != null)
            _selectDestinationText.text = GetCategoryTitle(mode);

        ShowDestinationScreen();

        Debug.Log($"[CategoryScreen] Selected: {mode}");
    }

    private void GoBack()
    {
        ShowCategoryScreen();
    }

    private void ShowCategoryScreen()
    {
        if (_categoryScreen != null) _categoryScreen.SetActive(true);
        if (_destinationScreen != null) _destinationScreen.SetActive(false);
        if (_selectDestinationText != null) _selectDestinationText.text = _defaultTitle;
    }

    private void ShowDestinationScreen()
    {
        if (_categoryScreen != null) _categoryScreen.SetActive(false);
        if (_destinationScreen != null) _destinationScreen.SetActive(true);
    }

    private string GetCategoryTitle(DestinationFilter.FilterMode mode)
    {
        switch (mode)
        {
            case DestinationFilter.FilterMode.All:
                return "All";
            case DestinationFilter.FilterMode.Office:
                return "Offices";
            case DestinationFilter.FilterMode.Building:
                return "Buildings";
            case DestinationFilter.FilterMode.Facility:
                return "Facilities";
            case DestinationFilter.FilterMode.Department:
                return "Departments";
            default:
                return _defaultTitle;
        }
    }
}
