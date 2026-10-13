using UnityEngine;
using System.Collections;

/// Plate of heart cookies (12 October 2026): the same count-to-sprite rule as Choux, 3 cookies on a plate.
public class Cookies : MonoBehaviour
{
    [SerializeField] private Sprite[] cookiesSprites = new Sprite[4]; // index 0 = empty plate, 3 = three cookies

    private RefillableItem refillableItem;
    private SpriteRenderer spriteRenderer;

    // Subscribe in OnEnable/OnDisable (not a Start coroutine) so the subscription survives the
    // object being toggled off and on by LevelManager / KitchenFoodGate.
    private void OnEnable()
    {
        if (refillableItem == null) refillableItem = GetComponent<RefillableItem>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (refillableItem == null) return;

        refillableItem.OnCountChanged -= OnCountChanged; // never double-subscribe
        refillableItem.OnCountChanged += OnCountChanged;
        StartCoroutine(RefreshSpriteNextFrame());
    }

    private void OnDisable()
    {
        if (refillableItem != null)
            refillableItem.OnCountChanged -= OnCountChanged;
    }

    // RefillableItem gets its counts from RefillSystem during Start, so read them a frame later.
    private IEnumerator RefreshSpriteNextFrame()
    {
        yield return null;
        UpdateSprite(refillableItem.GetCurrentCount());
    }

    public void RefillToFull()
    {
        Debug.Log($"[Cookies] count={refillableItem.GetCurrentCount()}, max={refillableItem.GetMaxCount()}");
        refillableItem?.RefillToFull();
    }

    private void OnCountChanged(int currentCount, int maxCount)
    {
        Debug.Log($"[Cookies] OnCountChanged received: {currentCount}/{maxCount}");
        UpdateSprite(currentCount);
    }

    private void UpdateSprite(int count)
    {
        var index = Mathf.Clamp(count, 0, cookiesSprites.Length - 1);
        if (cookiesSprites[index] != null)
            spriteRenderer.sprite = cookiesSprites[index];
    }
}