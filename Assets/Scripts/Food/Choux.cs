using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class Choux : MonoBehaviour
{
    [FormerlySerializedAs("breadSprites")] [SerializeField]
    private Sprite[] chouxSprites = new Sprite[5]; // index 0 = empty, 4 = full

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
        yield return null; // wait for RefillableItem to initialize
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