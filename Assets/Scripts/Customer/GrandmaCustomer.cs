using UnityEngine;
using System.Collections;

public class GrandmaCustomer : CustomerController
{
    [Header("Toad-Specific Settings")] [SerializeField]
    private float toadPatienceLevel = 5.0f;

    [SerializeField] private float toadOrderDelay = 1.0f;
    [SerializeField] private string[] toadPreferredFoods = { "Burger", "Fries" };

    // Override abstract properties
    public override float PatienceLevel => toadPatienceLevel;
    public override string[] PreferredFoods => toadPreferredFoods;
    public override float OrderDelay => toadOrderDelay;

    protected override void Awake()
    {
        base.Awake();

        // CUSTOMIZE: Set timing for Toad customer
        fallbackWalkInDuration = 2.0f;
        servicePointDelay = 1.0f;

        Debug.Log($"ToadCustomer initialized with OrderDelay: {OrderDelay}s, ServicePointDelay: {servicePointDelay}s");
    }

    public override void OnOrderGenerated()
    {
        base.OnOrderGenerated();
        Debug.Log("🐸 Toad customer: *croaks* What's for dinner?");
    }

    // SIMPLIFIED: Only happy reactions (orders always completed correctly)
    protected override void PlayHappyReaction()
    {
        Debug.Log("🐸✨ Toad customer: *happy croaking* RIBBIT! Perfect meal! *bounces with joy*");
        base.PlayHappyReaction();
    }

    // SIMPLIFIED: Only sad reactions (orders expired)
    protected override void PlaySadReaction()
    {
        Debug.Log("🐸💢 Toad customer: *VERY angry croaking* I've been waiting too long! RIBBIT RIBBIT!");
        base.PlaySadReaction();
    }

    // SIMPLIFIED: Happy walk out message
    protected override IEnumerator HappyWalkOut()
    {
        Debug.Log("🐸🎉 Toad: *happy hopping away* RIBBIT! I'm so satisfied! Thank you!");
        yield return StartCoroutine(base.HappyWalkOut());
    }

    // SIMPLIFIED: Sad walk out message  
    protected override IEnumerator SadWalkOut()
    {
        Debug.Log("🐸😤 Toad: *grumpy hopping away* Ribbit... this place is terrible!");
        yield return StartCoroutine(base.SadWalkOut());
    }

    public override void PlayWalkInAnimation()
    {
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 0); // 0 = Walking In
            animator.SetBool("IsHappy", false);
            Debug.Log("🐸 Toad: Playing walk in animation");
        }
    }
}