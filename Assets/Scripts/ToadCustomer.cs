using UnityEngine;

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
    
    public override void OnOrderServed(bool perfect)
    {
        if (perfect)
        {
            Debug.Log("Toad customer: *happy croaking* Ribbit! Perfect meal!");
        }
        else
        {
            Debug.Log("Toad customer: *disappointed croak* This isn't what I ordered...");
        }
        
        base.OnOrderServed(perfect);
    }
    
    public override void OnOrderExpired()
    {
        Debug.Log("Toad customer: *angry croaking* I've been waiting too long! Ribbit!");
        base.OnOrderExpired();
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
    
    public override void PlayWalkOutAnimation(bool happy)
    {
        if (animator != null)
        {
            if (happy)
            {
                // Set parameters for happy walking out
                animator.SetInteger("CustomerState", 2); // 2 = Walking Out Happy
                animator.SetBool("IsHappy", true);
                animator.Play(happyWalkOutState);
                Debug.Log($"Toad: Playing happy walk out - {happyWalkOutState}");
            }
            else
            {
                // Set parameters for sad walking out
                animator.SetInteger("CustomerState", 1); // 1 = Walking Out Sad
                animator.SetBool("IsHappy", false);
                animator.Play(sadWalkOutState);
                Debug.Log($"Toad: Playing sad walk out - {sadWalkOutState}");
            }
        }
    }
}