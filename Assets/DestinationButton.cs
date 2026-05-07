using UnityEngine;

// ---------------------------------------------
//  DestinationButton.cs
//  Attach to every destination button GameObject.
//  Set the Category in the Inspector.
// ---------------------------------------------

public class DestinationButton : MonoBehaviour
{
    public enum DestinationCategory
    {
        Office,
        Building,
        Facility,
        Department
    }

    [Tooltip("Set the category of this destination button")]
    public DestinationCategory category;
}