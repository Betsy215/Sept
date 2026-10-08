using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class Choux : MonoBehaviour
{
    [FormerlySerializedAs("breadSprites")] [SerializeField]
    private Sprite[] chouxSprites = new Sprite[5]; // index 0 = empty, 4 = full

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
        Debug.Log($"[Choux] count={refillableItem.GetCurrentCount()}, max={refillableItem.GetMaxCount()}");
        refillableItem?.RefillToFull();
    }

    private void OnCountChanged(int currentCount, int maxCount)
    {
        Debug.Log($"[Choux] OnCountChanged received: {currentCount}/{maxCount}");
        UpdateSprite(currentCount);
    }

    private void UpdateSprite(int count)
    {
        var index = Mathf.Clamp(count, 0, chouxSprites.Length - 1);
        if (chouxSprites[index] != null)
            spriteRenderer.sprite = chouxSprites[index];
    }
}