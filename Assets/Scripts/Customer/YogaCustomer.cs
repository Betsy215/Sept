using UnityEngine;

/// Shop customer ($500): patient, her order stays open 50 percent longer.
public class YogaCustomer : CustomerController
{
    [Header("Yoga settings")]
    [SerializeField] private float patienceLevel = 5.0f;
    [SerializeField] private float orderDelay = 1.0f;
    [SerializeField] private string[] preferredFoods = { "Coffee", "Bread" };

    public override float PatienceLevel => patienceLevel;
    public override string[] PreferredFoods => preferredFoods;
    public override float OrderDelay => orderDelay;
    public override float OrderTimeMultiplier => 1.5f;

    protected override void Awake()
    {
        base.Awake();
        fallbackWalkInDuration = 2.0f;
        servicePointDelay = 1.0f;
    }

    public override void PlayWalkInAnimation()
    {
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 0);
            animator.SetBool("IsHappy", false);
        }
    }
}
