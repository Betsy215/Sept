using UnityEngine;
using System.Collections;

public class Bread : MonoBehaviour
{
    // Sprites in ascending order: index 0 = empty, last index = full
    [SerializeField] private Sprite[] breadSprites;
    [SerializeField] [Min(1)] private int countPerSpriteChange = 2;

    private RefillableItem refillableItem;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        refillableItem = GetComponent<RefillableItem>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        yield return null;
        refillableItem.OnCountChanged += OnCountChanged;
        UpdateSprite(refillableItem.GetCurrentCount());
    }

    private void OnDestroy()
    {
        if (refillableItem != null)
            refillableItem.OnCountChanged -= OnCountChanged;
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