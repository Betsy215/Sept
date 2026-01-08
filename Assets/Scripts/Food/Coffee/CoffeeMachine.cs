using UnityEngine;

public class CoffeeMachine : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private BeanContainer beanContainer;

    [SerializeField] private RefillableItem coffeeRefillableItem;
    [SerializeField] private Animator animator;

    [Header("Settings")] [SerializeField] private int cupsPerBrew = 1;

    private bool isBrewing = false;

    private void Start()
    {
        // Auto-find components if not assigned
        if (beanContainer == null)
            beanContainer = GetComponentInChildren<BeanContainer>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnMouseUpAsButton()
    {
        if (CanBrew()) StartBrewing();
    }

    private bool CanBrew()
    {
        if (isBrewing) return false;
        if (beanContainer == null || coffeeRefillableItem == null) return false;

        return beanContainer.HasEnoughBeans(cupsPerBrew) &&
               coffeeRefillableItem.HasSpace(cupsPerBrew);
    }

    private void StartBrewing()
    {
        isBrewing = true;

        // Consume beans immediately
        beanContainer.ConsumeBeans(cupsPerBrew);

        // Trigger animation
        if (animator != null)
            animator.SetTrigger("StartBrewing");

        Debug.Log("Brewing started...");
    }

    // Called by animation event when brewing completes
    public void OnBrewingComplete()
    {
        // Add coffee using updated RefillableItem method
        if (coffeeRefillableItem != null) coffeeRefillableItem.IncreaseCount(cupsPerBrew);

        isBrewing = false;
        Debug.Log("Brewing complete!");
    }
}