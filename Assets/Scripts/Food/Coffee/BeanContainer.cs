using UnityEngine;

public class BeanContainer : MonoBehaviour
{
    [Header("Bean Sprites")] [SerializeField]
    private Sprite[] beanLevelSprites = new Sprite[7]; // 0-6 levels

    [Header("References")] [SerializeField]
    private SpriteRenderer spriteRenderer;

    private int currentBeanLevel = 6; // Start full

    private void Start()
    {
        // Auto-find sprite renderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateDisplay();
    }

    private void OnMouseUpAsButton()
    {
        // Refill beans to full
        currentBeanLevel = 6;
        UpdateDisplay();
        Debug.Log("Beans refilled!");
    }

    public bool HasEnoughBeans(int amount)
    {
        return currentBeanLevel >= amount;
    }

    public void ConsumeBeans(int amount)
    {
        currentBeanLevel = Mathf.Max(0, currentBeanLevel - amount);
        UpdateDisplay();
        Debug.Log($"Consumed {amount} beans. Level: {currentBeanLevel}");
    }

    private void UpdateDisplay()
    {
        if (spriteRenderer != null && beanLevelSprites != null &&
            currentBeanLevel < beanLevelSprites.Length)
            spriteRenderer.sprite = beanLevelSprites[currentBeanLevel];
    }
}