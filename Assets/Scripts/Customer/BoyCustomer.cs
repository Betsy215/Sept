using UnityEngine;
using System.Collections;

public class BoyCustomer : CustomerController
{
    [Header("Boy-Specific Settings")]
    [SerializeField] private float boyPatienceLevel = 5.0f;
    [SerializeField] private float boyOrderDelay = 1.0f;
    [SerializeField] private string[] boyPreferredFoods = { "Burger", "Fries" };
    
    // Override abstract properties
    public override float PatienceLevel => boyPatienceLevel;
    public override string[] PreferredFoods => boyPreferredFoods;
    public override float OrderDelay => boyOrderDelay;
    
    protected override void Awake()
    {
        base.Awake();
        
        // CUSTOMIZE: Set timing for Boy customer
        fallbackWalkInDuration = 2.0f;
        servicePointDelay = 1.0f;
        
        Debug.Log($"BoyCustomer initialized with OrderDelay: {OrderDelay}s, ServicePointDelay: {servicePointDelay}s");
    }
    
    public override void OnOrderGenerated()
    {
        base.OnOrderGenerated();
        Debug.Log("👦 Boy customer: *excited* Hey! What food do you have?");
    }
    
    // SIMPLIFIED: Only happy reactions (orders always completed correctly)
    protected override void PlayHappyReaction()
    {
        Debug.Log("👦✨ Boy customer: *happy shout* AWESOME! This looks amazing! *jumps with excitement*");
        base.PlayHappyReaction();
    }
    
    // SIMPLIFIED: Only sad reactions (orders expired)
    protected override void PlaySadReaction()
    {
        Debug.Log("👦💢 Boy customer: *angry* I've been waiting forever! This is terrible service!");
        base.PlaySadReaction();
    }
    
    // SIMPLIFIED: Happy walk out message
    protected override IEnumerator HappyWalkOut()
    {
        Debug.Log("👦🎉 Boy: *excited jumping* This was the best meal ever! Thank you!");
        yield return StartCoroutine(base.HappyWalkOut());
    }
    
    // SIMPLIFIED: Sad walk out message
    protected override IEnumerator SadWalkOut()
    {
        Debug.Log("👦😤 Boy: *stomping away angrily* This place stinks! I'm outta here!");
        yield return StartCoroutine(base.SadWalkOut());
    }
    
    public override void PlayWalkInAnimation()
    {
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 0); // 0 = Walking In
            animator.SetBool("IsHappy", false);
            Debug.Log("👦 Boy: Playing walk in animation");
        }
    }
}