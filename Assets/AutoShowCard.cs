using UnityEngine;
using System.Collections;

public class AutoShowCard : MonoBehaviour
{
    public GameObject cardPanel;
    public float delaySeconds = 3f;

    // This static variable stays "true" even if you switch scenes or screens
    private static bool hasShownWelcome = false;

    void Start()
    {
        // Only start the timer if we haven't shown it yet this session
        if (cardPanel != null && !hasShownWelcome)
        {
            cardPanel.SetActive(false);
            StartCoroutine(ShowCardAfterDelay());
        }
        else if (hasShownWelcome)
        {
            // Make sure it stays hidden if it already showed once
            cardPanel.SetActive(false);
        }
    }

    IEnumerator ShowCardAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);

        cardPanel.SetActive(true);

        // Mark it as shown so it never happens again
        hasShownWelcome = true;

        Animator anim = cardPanel.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Show");
        }
    }
}