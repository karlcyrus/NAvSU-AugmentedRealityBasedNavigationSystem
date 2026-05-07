using UnityEngine;

public class NavigationController : MonoBehaviour
{
    // These slots will appear in your Inspector
    public GameObject homePanel, loginPanel, destinationPanel;

    // Call this from the "Explore Now" button
    public void GoToLogin()
    {
        homePanel.SetActive(false);
        loginPanel.SetActive(true);
        destinationPanel.SetActive(false);
    }

    // Call this from the "Guest" button
    public void GoToDestination()
    {
        loginPanel.SetActive(false);
        destinationPanel.SetActive(true);
    }
}