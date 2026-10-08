using UnityEngine;
using System.Collections;

public class Bread : MonoBehaviour
{
    // Sprites in ascending order: index 0 = empty, last index = full
    [SerializeField] private Sprite[] breadSprites;
    [SerializeField] [Min(1)] private int countPerSpriteChange = 2;

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
        refillableItem?.RefillToFull();
    }

    private void OnCountChanged(int currentCount, int maxCount)
    {
        UpdateSprite(currentCount);
    }

    private void UpdateSprite(int count)
    {
        var consumed = refillableItem.GetMaxCount() - count;
        var index = breadSprites.Length - 1 - consumed / countPerSpriteChange;
        index = Mathf.Clamp(index, 0, breadSprites.Length - 1);
        Debug.Log($"[Bread] count={count}, maxCount={refillableItem.GetMaxCount()}, consumed={consumed}, arrayLength={breadSprites.Length}, index={index}, sprite={breadSprites[index]?.name ?? "NULL"}");
        if (breadSprites[index] != null)
            spriteRenderer.sprite = breadSprites[index];
    }
}