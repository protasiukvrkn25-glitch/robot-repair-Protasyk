using UnityEngine;

public class SpriteFlashFeedback : MonoBehaviour
{
    private SpriteRenderer[] sprites;
    private Color[] originalColors;
    private float remaining;
    private float duration;
    private Color flashColor;

    private void Awake()
    {
        SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>(true);
        var visibleSprites = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (SpriteRenderer sprite in allSprites)
            if (sprite != null && !sprite.name.StartsWith("HealthBar")) visibleSprites.Add(sprite);
        sprites = visibleSprites.ToArray();
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) originalColors[i] = sprites[i].color;
    }

    public void Play(Color color, float seconds = 0.14f)
    {
        if (sprites == null || sprites.Length == 0) Awake();
        flashColor = color;
        duration = Mathf.Max(0.01f, seconds);
        remaining = duration;
        Apply(0.8f);
    }

    private void Update()
    {
        if (remaining <= 0f) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) sprites[i].color = originalColors[i];
            return;
        }
        float pulse = Mathf.Clamp01(remaining / duration);
        Apply(0.2f + pulse * 0.65f);
    }

    private void Apply(float strength)
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = Color.Lerp(originalColors[i], flashColor, strength);
    }
}
