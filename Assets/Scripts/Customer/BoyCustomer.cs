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
        
        // Set Boy-specific animation state names
        // You can reuse existing animations or create new ones
        walkInAnimationState = "Sad_Boy_Walking";      // Create this or reuse "Sad_Toad_Walking"
        waitingAnimationState = "Sad_Boy_Walking";     // Create this or reuse "Sad_Toad_Walking"
        perfectReactionState = "Perfect_Order_Boy";    // Create this or reuse "Perfect_Order_Toad"
        happyWalkOutState = "happy_boy_walking";       // Create this or reuse "happy_toad_walking"
        sadWalkOutState = "Sad_Boy_Walking";           // Create this or reuse "Sad_Toad_Walking"
        
        // CUSTOMIZE: Set timing for Boy customer
        fallbackWalkInDuration = 2.0f; // If animation detection fails
        servicePointDelay = 1.0f; // Extra delay after reaching service point
        
        // CUSTOMIZE: Set sad walk-out behavior
        sadWalkDistance = 5.0f; // Distance to walk right when sad
        sadWalkDuration = 3.0f; // Time to walk to the right
        
        Debug.Log($"BoyCustomer initialized with OrderDelay: {OrderDelay}s, ServicePointDelay: {servicePointDelay}s");
    }
    
    public override void OnOrderGenerated()
    {
        base.OnOrderGenerated();
        Debug.Log("Boy customer: *excited* Hey! What food do you have?");
        
        // Set waiting state parameters
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 0); // Still in waiting state
        }
    }
    
    /// <summary>
    /// ENHANCED: Better reaction messages for orders
    /// </summary>
    public override void OnOrderServed(bool perfect)
    {
        if (perfect)
        {
            Debug.Log("👦✨ Boy customer: *happy shout* AWESOME! This looks amazing! *jumps with excitement*");
        }
        else
        {
            Debug.Log("👦😠 Boy customer: *disappointed* This isn't what I ordered! I'm outta here!");
            Debug.Log("👦➡️ Boy will now walk to the RIGHT in disappointment...");
        }
        
        base.OnOrderServed(perfect);
    }
    
    /// <summary>
    /// ENHANCED: Better reaction for expired orders
    /// </summary>
    public override void OnOrderExpired()
    {
        Debug.Log("👦💢 Boy customer: *angry* I've been waiting forever! This is terrible service!");
        Debug.Log("👦➡️ Boy is storming off to the RIGHT!");
        
        base.OnOrderExpired();
    }
    
    /// <summary>
    /// ENHANCED: Boy-specific sad walk out behavior
    /// </summary>
    protected override IEnumerator SadWalkOutToRight()
    {
        Debug.Log("👦😤 Boy: *angry stomping* This place stinks!");
        
        // Use base implementation but with boy-specific debug messages
        yield return base.SadWalkOutToRight();
        
        Debug.Log("👦➡️ Boy has left the area in disappointment!");
    }
}