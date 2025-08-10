using UnityEngine;
using System.Collections;

public class GirlCustomer : CustomerController
{
    [Header("Girl-Specific Settings")]
    [SerializeField] private float girlPatienceLevel = 5.0f;
    [SerializeField] private float girlOrderDelay = 1.0f;
    [SerializeField] private string[] girlPreferredFoods = { "Burger", "Fries" };
    
    // Override abstract properties
    public override float PatienceLevel => girlPatienceLevel;
    public override string[] PreferredFoods => girlPreferredFoods;
    public override float OrderDelay => girlOrderDelay;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Set Girl-specific animation state names to match your existing clips
        walkInAnimationState = "Sad_Girl_Walking";
        waitingAnimationState = "Sad_Girl_Walking";
        perfectReactionState = "Perfect_Order_Girl";
        happyWalkOutState = "happy_girl_walking";
        sadWalkOutState = "Sad_Girl_Walking";
        
        // CUSTOMIZE: Set timing for Girl customer
        fallbackWalkInDuration = 2.0f; // If animation detection fails
        servicePointDelay = 1.0f; // Extra delay after reaching service point
        
        // CUSTOMIZE: Set sad walk-out behavior
        sadWalkDistance = 5.0f; // Distance to walk right when sad
        sadWalkDuration = 3.0f; // Time to walk to the right
        
        Debug.Log($"GirlCustomer initialized with OrderDelay: {OrderDelay}s, ServicePointDelay: {servicePointDelay}s");
    }
    
    public override void OnOrderGenerated()
    {
        base.OnOrderGenerated();
        Debug.Log("Girl customer: *excited* Ooh, what delicious food do you have?");
        
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
            Debug.Log("👧✨ Girl customer: *happy giggle* Perfect! This looks amazing! *claps hands*");
        }
        else
        {
            Debug.Log("👧😠 Girl customer: *disappointed sigh* This isn't what I ordered! I'm leaving!");
            Debug.Log("👧➡️ Girl will now walk to the RIGHT in disappointment...");
        }
        
        base.OnOrderServed(perfect);
    }
    
    /// <summary>
    /// ENHANCED: Better reaction for expired orders
    /// </summary>
    public override void OnOrderExpired()
    {
        Debug.Log("👧💢 Girl customer: *upset* I've been waiting way too long! This is terrible service!");
        Debug.Log("👧➡️ Girl is storming off to the RIGHT!");
        
        base.OnOrderExpired();
    }
    
    /// <summary>
    /// ENHANCED: Girl-specific sad walk out behavior
    /// </summary>
    protected override IEnumerator SadWalkOutToRight()
    {
        Debug.Log("👧😤 Girl: *frustrated* This is NOT what I wanted! I'm never coming back!");
        Debug.Log("👧➡️ Girl is walking away to the RIGHT in disappointment...");
        
        // Call base implementation which handles the movement
        yield return StartCoroutine(base.SadWalkOutToRight());
    }
    
    /// <summary>
    /// ENHANCED: Girl-specific sad walk animation with better messaging
    /// </summary>
    protected override void PlaySadWalkOutAnimation()
    {
        Debug.Log("👧😞 Girl: Playing SAD walk animation - walking to the right!");
        Debug.Log("🚶‍♀️➡️ Animation: " + sadWalkOutState + " (moving from middle to right)");
        
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
            Debug.Log("👧🎉 Girl: *happy skipping* This was wonderful! Thank you so much!");
            // Happy customers can exit in original direction (left, fade, etc.)
        }
        else
        {
            Debug.Log("👧😤 Girl: *angrily walking to the RIGHT* This place has terrible service!");
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
            Debug.Log($"Girl: Playing walk in animation - {walkInAnimationState}");
        }
    }
    
    public override void PlayOrderReaction(bool perfect)
    {
        if (animator != null)
        {
            if (perfect)
            {
                SetSpriteState(true);
                // Set parameters for perfect reaction
                animator.SetBool("IsHappy", true);
                animator.SetTrigger("TriggerReaction");
                Debug.Log("Girl: *happy smile* Perfect! This looks delicious! Playing perfect reaction");
            }
            else
            {
                SetSpriteState(false);
                // Set parameters for disappointed reaction (stays sad)
                animator.SetBool("IsHappy", false);
                animator.SetTrigger("TriggerReaction");
                Debug.Log("Girl: *disappointed frown* This isn't what I ordered... Staying sad");
            }
        }
    }
}