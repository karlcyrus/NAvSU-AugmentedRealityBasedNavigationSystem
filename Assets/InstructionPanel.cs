using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InstructionPanel : MonoBehaviour
{
    [Header("Slides")]
    public RectTransform slidesHolder;
    public float slideWidth = 978f;

    [Header("Dots")]
    public Image[] dots;
    public float activeDotWidth = 60f;
    public float inactiveDotWidth = 16f;
    public Color activeDotColor;
    public Color inactiveDotColor;

    [Header("Buttons")]
    public Button btnNext;
    public TextMeshProUGUI txtNext;

    [Header("Navigation")]
    public GameObject destinationPanel;

    private int currentSlide = 0;
    private int totalSlides = 3;
    private bool isAnimating = false;

    void Start()
    {
        if (slidesHolder == null)
        { Debug.LogError("MISSING: Slides Holder not assigned!"); return; }

        if (btnNext == null)
        { Debug.LogError("MISSING: BTN_Next not assigned!"); return; }

        if (txtNext == null)
        { Debug.LogError("MISSING: TXT_Next not assigned!"); return; }

        if (dots == null || dots.Length == 0)
        { Debug.LogError("MISSING: Dots not assigned!"); return; }

        btnNext.onClick.AddListener(OnNextClicked);
        GoToSlide(0, false);
    }

    void OnNextClicked()
    {
        if (isAnimating) return;

        if (currentSlide < totalSlides - 1)
        {
            GoToSlide(currentSlide + 1, true);
        }
        else
        {
            LaunchApp();
        }
    }

    void GoToSlide(int index, bool animate)
    {
        currentSlide = index;
        float targetX = -index * slideWidth;

        if (animate)
        {
            isAnimating = true;
            StartCoroutine(AnimateSlide(targetX));
        }
        else
        {
            slidesHolder.anchoredPosition =
                new Vector2(targetX, slidesHolder.anchoredPosition.y);
        }

        UpdateDots(index);
        UpdateButton(index);
    }

    System.Collections.IEnumerator AnimateSlide(float targetX)
    {
        float startX = slidesHolder.anchoredPosition.x;
        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            float newX = Mathf.Lerp(startX, targetX, t);
            slidesHolder.anchoredPosition =
                new Vector2(newX, slidesHolder.anchoredPosition.y);
            yield return null;
        }

        slidesHolder.anchoredPosition =
            new Vector2(targetX, slidesHolder.anchoredPosition.y);
        isAnimating = false;
    }

    void UpdateDots(int activeIndex)
    {
        for (int i = 0; i < dots.Length; i++)
        {
            bool isActive = (i == activeIndex);
            dots[i].color = isActive ? activeDotColor : inactiveDotColor;

            RectTransform rt = dots[i].GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(
                isActive ? activeDotWidth : inactiveDotWidth,
                rt.sizeDelta.y
            );
        }
    }

    void UpdateButton(int index)
    {
        bool isLast = (index == totalSlides - 1);
        txtNext.text = isLast ? "Let's Go!" : "Next";
    }

    void LaunchApp()
    {
        gameObject.SetActive(false);
        if (destinationPanel != null)
            destinationPanel.SetActive(true);
    }
}