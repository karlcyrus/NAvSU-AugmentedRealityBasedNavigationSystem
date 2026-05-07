using UnityEngine;

// ---------------------------------------------
//  MinimapToggle.cs
//  Controls the full screen map panel toggle.
//  Notifies MinimapController so it can activate
//  the separate fullscreen render pipeline.
//
//  Setup:
//  1. Attach to UIManager
//  2. Assign _fullScreenMapPanel (the full screen panel)
//  3. Assign _minimapController (MinimapCamera)
//  4. Wire map button OnClick -> Toggle()
// ---------------------------------------------

public class MinimapToggle : MonoBehaviour
{
    [SerializeField] private GameObject _fullScreenMapPanel;
    [SerializeField] private MinimapController _minimapController;

    private bool _isVisible = false;

    void Start()
    {
        if (_fullScreenMapPanel != null)
            _fullScreenMapPanel.SetActive(false);
    }

    public void Toggle()
    {
        _isVisible = !_isVisible;
        if (_fullScreenMapPanel != null)
            _fullScreenMapPanel.SetActive(_isVisible);

        if (_minimapController != null)
            _minimapController.SetFullScreenActive(_isVisible);

        Debug.Log("[MinimapToggle] Full screen map " + (_isVisible ? "shown" : "hidden"));
    }

    public void Hide()
    {
        _isVisible = false;
        if (_fullScreenMapPanel != null)
            _fullScreenMapPanel.SetActive(false);

        if (_minimapController != null)
            _minimapController.SetFullScreenActive(false);
    }
}
