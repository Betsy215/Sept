using UnityEngine;
using System.Collections;

public class ToadCustomer : CustomerController
{
    [Header("Toad-Specific Settings")]
    [SerializeField] private float toadPatienceLevel = 5.0f;
    [SerializeField] private float toadOrderDelay = 1.0f;
    [SerializeField] private string[] toadPreferredFoods = { "Burger", "Fries" };
    
    // Override abstract properties
    public override float PatienceLevel => toadPatienceLevel;
    public override string[] PreferredFoods => toadPreferredFoods;
    public override float OrderDelay => toadOrderDelay;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Set Toad-specific animation state names to match your existing clips
        walkInAnimationState = "Sad_Toad_Walking";
        waitingAnimationState = "Sad_Toad_Walking";
        perfectReactionState = "Perfect_Order_Toad";
        happyWalkOutState = "happy_toad_walking";
        sadWalkOutState = "Sad_Toad_Walking";
        
        // CUSTOMIZE: Set timing for Toad customer
        fallbackWalkInDuration = 2.0f; // If animation detection fails
        servicePointDelay = 1.0f; // Extra delay after reaching service point
        
        // CUSTOMIZE: Set sad walk-out behavior
        sadWalkDistance = 5.0f; // Distance to walk right when sad
        sadWalkDuration = 3.0f; // Time to walk to the right
        
        Debug.Log($"ToadCustomer initialized with OrderDelay: {OrderDelay}s, ServicePointDelay: {servicePointDelay}s");
    }
    
    public override void OnOrderGenerated()
    {
        base.OnOrderGenerated();
        Debug.Log("Toad customer: *croaks* What's for dinner?");
        
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
            Debug.Log("🐸✨ Toad customer: *happy croaking* RIBBIT! Perfect meal! *bounces with joy*");
        }
        else
        {
            Debug.Log("🐸😠 Toad customer: *angry croak* This isn't what I ordered! I'm leaving! RIBBIT!");
            Debug.Log("🐸➡️ Toad will now walk to the RIGHT in disappointment...");
        }
        
        base.OnOrderServed(perfect);
    }
    
    /// <summary>
    /// ENHANCED: Better reaction for expired orders
    /// </summary>
    public override void OnOrderExpired()
    {
        Debug.Log("🐸💢 Toad customer: *VERY angry croaking* I've been waiting too long! RIBBIT RIBBIT!");
        Debug.Log("🐸➡️ Toad is storming off to the RIGHT!");
        
        base.OnOrderExpired();
    }
    
    /// <summary>
    /// ENHANCED: Toad-specific sad walk out behavior
    /// </summary>
    protected override IEnumerator SadWalkOutToRight()
    {
        Debug.Log("🐸😤 Toad: *angry/disappointed croaking* RIBBIT! This is NOT what I wanted!");
        Debug.Log("🐸➡️ Toad is walking away to the RIGHT in disappointment...");
        
        // Call base implementation which handles the movement
        yield return StartCoroutine(base.SadWalkOutToRight());
    }
    
    /// <summary>
    /// ENHANCED: Toad-specific sad walk animation with better messaging
    /// </summary>
    protected override void PlaySadWalkOutAnimation()
    {
        Debug.Log("🐸😞 Toad: Playing SAD walk animation - walking to the right!");
        Debug.Log("🚶‍♂️➡️ Animation: " + sadWalkOutState + " (moving from middle to right)");
        
        // Call base implementation
        base.PlaySadWalkOutAnimation();
    }
    
    /// <summary>
    /// ENHANCED: Different walk out messages based on direction and mood
    /// </summary>
    public override void PlayWalkOutAnimation(bool happy)
    {
        if (happy)
        {
            Debug.Log("🐸🎉 Toad: *happy hopping away* RIBBIT! I'm so satisfied! Thank you!");
            // Happy customers can exit in original direction (left, fade, etc.)
        }
        else
        {
            Debug.Log("🐸😤 Toad: *grumpy hopping to the RIGHT* Ribbit... this place is terrible!");
            // Sad customers will use the new right-walking behavior
        }
        
        base.PlayWalkOutAnimation(happy);
    }
    
    public override void PlayWalkInAnimation()
    {
        if (animator != null)
        {
            // Set parameters for sad walking (walking in)
            animator.SetInteger("CustomerState", 0); // 0 = Walking In
            animator.SetBool("IsHappy", false);
            animator.Play(walkInAnimationState);
            Debug.Log($"Toad: Playing walk in animation - {walkInAnimationState}");
        }
    }
    
    public override void PlayOrderReaction(bool perfect)
    {
        if (animator != null)
        {
            if (perfect)
            {
                // Set parameters for perfect reaction
                animator.SetBool("IsHappy", true);
                animator.SetTrigger("TriggerReaction");
                Debug.Log("Toad: *happy croaking* Ribbit! Perfect meal! Playing perfect reaction");
            }
            else
            {
                // Set parameters for disappointed reaction (stays sad)
                animator.SetBool("IsHappy", false);
                animator.SetTrigger("TriggerReaction");
                Debug.Log("Toad: *disappointed croak* This isn't what I ordered... Staying sad");
            }
        }
    }
}