using UnityEngine;

public class ToadCustomer : CustomerController
{
    [Header("Toad-Specific Settings")]
    [SerializeField] private float toadPatienceLevel = 5.0f;
    [SerializeField] private float toadMovementSpeed = 2.0f;
    [SerializeField] private float toadOrderDelay = 1.0f;
    [SerializeField] private string[] toadPreferredFoods = { "Burger", "Fries" };
    
    public override float PatienceLevel => toadPatienceLevel;
    public override string[] PreferredFoods => toadPreferredFoods;
    public override float MovementSpeed => toadMovementSpeed;
    public override float OrderDelay => toadOrderDelay;
    
    protected override void Awake()
    {
        base.Awake();
        
        walkInAnimationState = "Sad_Toad_Walking";
        waitingAnimationState = "Sad_Toad_Walking";
        perfectReactionState = "Perfect_Order_Toad";
        happyWalkOutState = "happy_toad_walking";
        sadWalkOutState = "Sad_Toad_Walking";
        
        Debug.Log($"ToadCustomer initialized with OrderDelay: {OrderDelay}s, MovementSpeed: {MovementSpeed}");
    }
    
    public override void PlayWalkInAnimation()
    {
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 0);
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
                animator.SetBool("IsHappy", true);
                animator.SetTrigger("TriggerReaction");
                Debug.Log("Toad: *happy croaking* Ribbit! Perfect meal! Playing perfect reaction");
            }
            else
            {
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
                animator.SetInteger("CustomerState", 2);
                animator.SetBool("IsHappy", true);
                animator.Play(happyWalkOutState);
                Debug.Log($"Toad: Playing happy walk out - {happyWalkOutState}");
            }
            else
            {
                animator.SetInteger("CustomerState", 1);
                animator.SetBool("IsHappy", false);
                animator.Play(sadWalkOutState);
                Debug.Log($"Toad: Playing sad walk out - {sadWalkOutState}");
            }
        }
    }
    
    public override void OnOrderGenerated()
    {
        base.OnOrderGenerated();
        Debug.Log("Toad customer: *croaks* What's for dinner?");
        
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 0);
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
}