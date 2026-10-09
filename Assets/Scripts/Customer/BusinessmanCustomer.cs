using UnityEngine;

/// Shop customer ($700): tips triple, but his order stays open only 75 percent as long.
public class BusinessmanCustomer : CustomerController
{
    [Header("Businessman settings")]
    [SerializeField] private float patienceLevel = 5.0f;
    [SerializeField] private float orderDelay = 1.0f;
    [SerializeField] private string[] preferredFoods = { "Coffee", "Bread" };

    public override float PatienceLevel => patienceLevel;
    public override string[] PreferredFoods => preferredFoods;
    public override float OrderDelay => orderDelay;
    public override float TipMultiplier => 3f;
    public override float OrderTimeMultiplier => 0.75f;

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
