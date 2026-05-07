using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SearchFilter : MonoBehaviour
{
    public Transform contentParent; // Drag your 'Content' object here
    private List<GameObject> locationButtons = new List<GameObject>();

    void Start()
    {
        // Gather all buttons inside the Content folder at the start
        foreach (Transform child in contentParent)
        {
            locationButtons.Add(child.gameObject);
        }
    }

    public void OnValueChange(string input)
    {
        string searchText = input.ToLower();

        foreach (GameObject button in locationButtons)
        {
            // Check if the button name contains the typed text
            // (Make sure your buttons are named "Main Library", "Science Complex", etc.)
            bool isMatch = button.name.ToLower().Contains(searchText);

            // Show the button if it matches, hide it if it doesn't
            button.SetActive(isMatch);
        }
    }
}