using UnityEngine;

/// Shop customer ($300): orders one item more than the day's minimum, so each visit pays more.
public class ChefCustomer : CustomerController
{
    [Header("Chef settings")]
    [SerializeField] private float patienceLevel = 5.0f;
    [SerializeField] private float orderDelay = 1.0f;
    [SerializeField] private string[] preferredFoods = { "Coffee", "Bread" };

    public override float PatienceLevel => patienceLevel;
    public override string[] PreferredFoods => preferredFoods;
    public override float OrderDelay => orderDelay;
    public override int ExtraOrderItems => 1;

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
