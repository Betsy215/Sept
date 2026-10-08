using UnityEngine;

public class BeanContainer : MonoBehaviour
{
    [Header("Bean Sprites")] [SerializeField]
    private Sprite[] beanLevelSprites = new Sprite[7]; // 0-6 levels

    [Header("References")] [SerializeField]
    private SpriteRenderer spriteRenderer;

    private const int MaxBeanLevel = 6;
    private int currentBeanLevel = MaxBeanLevel;

    private void Start()
    {
        // Auto-find sprite renderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateDisplay();
    }

    public void RefillToFull()
    {
        // Refill beans to full
        currentBeanLevel = MaxBeanLevel;
        UpdateDisplay();
        Debug.Log("Beans refilled!");
    }

    public bool HasEnoughBeans(int amount)
    {
        return currentBeanLevel >= amount;
    }

    public void ConsumeBeans(int amount)
    {
        if (amount <= 0) return;
        currentBeanLevel = Mathf.Clamp(currentBeanLevel - amount, 0, MaxBeanLevel);
        UpdateDisplay();
        Debug.Log($"Consumed {amount} beans. Level: {currentBeanLevel}");
    }

    private void UpdateDisplay()
    {
        if (spriteRenderer != null)
        {
            if (currentBeanLevel <= 0)
            {
                // No beans - hide sprite completely
                spriteRenderer.sprite = null;
                // OR: spriteRenderer.enabled = false;
            }
            else if (beanLevelSprites != null && beanLevelSprites.Length > 0)
            {
                // Show corresponding bean level sprite (array index = currentBeanLevel - 1).
                // The scene wires 6 sprites; clamp so a shorter array never leaves a stale sprite.
                var index = Mathf.Clamp(currentBeanLevel, 1, beanLevelSprites.Length) - 1;
                spriteRenderer.sprite = beanLevelSprites[index];
                spriteRenderer.enabled = true;
            }
        }
    }
}