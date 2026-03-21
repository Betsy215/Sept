using UnityEngine;
using System.Collections;

public class KidCustomer : CustomerController
{
    [Header("Girl-Specific Settings")] [SerializeField]
    private float girlPatienceLevel = 5.0f;

    [SerializeField] private float girlOrderDelay = 1.0f;
    [SerializeField] private string[] girlPreferredFoods = { "Burger", "Fries" };

    // Override abstract properties
    public override float PatienceLevel => girlPatienceLevel;
    public override string[] PreferredFoods => girlPreferredFoods;
    public override float OrderDelay => girlOrderDelay;
    public override int MinOrderItems => 4; 
    protected override void Awake()
    {
        base.Awake();

        // CUSTOMIZE: Set timing for Girl customer
        fallbackWalkInDuration = 2.0f;
        servicePointDelay = 1.0f;

        Debug.Log($"GirlCustomer initialized with OrderDelay: {OrderDelay}s, ServicePointDelay: {servicePointDelay}s");
    }

    public override void OnOrderGenerated()
    {
        base.OnOrderGenerated();
        Debug.Log("👧 Girl customer: *excited* Ooh, what delicious food do you have?");
    }

    // SIMPLIFIED: Only happy reactions (orders always completed correctly)
    protected override void PlayHappyReaction()
    {
        Debug.Log("👧✨ Girl customer: *happy giggle* Perfect! This looks amazing! *claps hands*");
        base.PlayHappyReaction();
    }

    // SIMPLIFIED: Only sad reactions (orders expired)
    protected override void PlaySadReaction()
    {
        Debug.Log("👧💢 Girl customer: *upset* I've been waiting way too long! This is terrible service!");
        base.PlaySadReaction();
    }

    // SIMPLIFIED: Happy walk out message
    protected override IEnumerator HappyWalkOut()
    {
        Debug.Log("👧🎉 Girl: *happy skipping* This was wonderful! Thank you so much!");
        yield return StartCoroutine(base.HappyWalkOut());
    }

    // SIMPLIFIED: Sad walk out message
    protected override IEnumerator SadWalkOut()
    {
        Debug.Log("👧😤 Girl: *frustrated* I'm never coming back! This place has terrible service!");
        yield return StartCoroutine(base.SadWalkOut());
    }

    public override void PlayWalkInAnimation()
    {
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 0); // 0 = Walking In
            animator.SetBool("IsHappy", false);
            Debug.Log("👧 Girl: Playing walk in animation");
        }
    }
}