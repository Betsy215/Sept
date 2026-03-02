using UnityEngine;

/// <summary>
/// Attach to an order display prefab to randomize its sprite when instantiated.
/// </summary>
public class RandomSprite : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;

    private void Start()
    {
        if (sprites == null || sprites.Length == 0) return;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
    }
}