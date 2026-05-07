using UnityEngine;
using UnityEngine.UI;

public class UIGlowEffect : MonoBehaviour
{
    public Image glowImage;
    public float speed = 2.0f;
    [Range(0, 1)] public float minOpacity = 0.1f;
    [Range(0, 1)] public float maxOpacity = 0.8f;

    private Color tempColor;

    void Start()
    {
        if (glowImage == null) glowImage = GetComponent<Image>();
        tempColor = glowImage.color;
    }

    void Update()
    {
        // Calculate the pulsing alpha value
        float pulse = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        float alpha = Mathf.Lerp(minOpacity, maxOpacity, pulse);

        // Apply only to the Alpha channel so the green stays green
        tempColor.a = alpha;
        glowImage.color = tempColor;
    }
}