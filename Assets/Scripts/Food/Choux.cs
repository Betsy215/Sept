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
        // The long platter (owner, 16 November 2026) is wider and lower in its picture than the old board: the box that
        // takes the tap and keeps other food off follows it (was 10 x 8 at (0, -1.3)).
        var box = GetComponent<BoxCollider>();
        if (box != null) { box.size = new Vector3(15.6f, 4.6f, 0f); box.center = new Vector3(0f, -3.2f, 0f); }
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