using UnityEngine;
using System.Collections;

public abstract class CustomerController : MonoBehaviour
{
    [Header("Animation Settings")]
    public string walkInAnimationState = "Sad_Toad_Walking";
    public string waitingAnimationState = "Sad_Toad_Walking"; 
    public string perfectReactionState = "Perfect_Order_Toad";
    public string happyWalkOutState = "happy_toad_walking";
    public string sadWalkOutState = "Sad_Toad_Walking";
    
    // Core components
    protected Animator animator;
    protected CustomerManager customerManager;
    
    // Current state tracking
    protected bool hasReachedServicePoint = false;
    protected bool isWaitingForOrder = false;
    protected bool isWalkingIn = false;
    protected bool isWalkingOut = false;
    
    // Abstract properties for variants to override
    public abstract float PatienceLevel { get; }
    public abstract string[] PreferredFoods { get; }
    public abstract float OrderDelay { get; }
    
    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        customerManager = FindObjectOfType<CustomerManager>();
        
        if (animator == null)
        {
            Debug.LogError($"No Animator component found on {gameObject.name}");
        }
    }
    
    protected virtual void Start()
    {
        StartCustomerLifecycle();
    }
    
    public virtual void StartCustomerLifecycle()
    {
        Debug.Log($"{gameObject.name} starting customer lifecycle");
        isWalkingIn = true;
        PlayWalkInAnimation();
        
        // Use animation events or coroutine to detect when walk-in animation completes
        StartCoroutine(WaitForWalkInComplete());
    }
    
    protected virtual IEnumerator WaitForWalkInComplete()
    {
        // Wait for the walk-in animation to complete
        // This timing should match your animation length
        float walkInDuration = GetAnimationLength(walkInAnimationState);
        yield return new WaitForSeconds(walkInDuration);
        
        OnReachedServicePoint();
    }
    
    protected virtual float GetAnimationLength(string animationName)
    {
        if (animator == null) return 1f;
        
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animationName)
            {
                return clip.length;
            }
        }
        return 1f; // Default fallback
    }
    
    public virtual void OnReachedServicePoint()
    {
        hasReachedServicePoint = true;
        isWaitingForOrder = true;
        isWalkingIn = false;
        
        // Notify customer manager to start order delay
        if (customerManager != null)
        {
            customerManager.OnCustomerReachedService(this);
        }
        
        Debug.Log($"{gameObject.name} reached service point. Order delay: {OrderDelay}s");
    }
    
    public virtual void OnOrderGenerated()
    {
        Debug.Log($"{gameObject.name} sees the order and starts waiting");
    }
    
    public virtual void OnOrderServed(bool perfect)
    {
        isWaitingForOrder = false;
        
        if (perfect)
        {
            Debug.Log($"{gameObject.name} is happy with perfect order!");
            PlayOrderReaction(true);
            StartCoroutine(DelayedWalkOut(true));
        }
        else
        {
            Debug.Log($"{gameObject.name} is disappointed with wrong order");
            PlayOrderReaction(false);
            StartCoroutine(DelayedWalkOut(false));
        }
    }
    
    public virtual void OnOrderExpired()
    {
        isWaitingForOrder = false;
        Debug.Log($"{gameObject.name} is frustrated - order expired!");
        
        StartCoroutine(DelayedWalkOut(false));
    }
    
    protected virtual IEnumerator DelayedWalkOut(bool happy)
    {
        // Wait a moment for reaction animation to play
        if (happy)
        {
            float reactionDuration = GetAnimationLength(perfectReactionState);
            yield return new WaitForSeconds(reactionDuration);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }
        
        PlayWalkOutAnimation(happy);
        StartCoroutine(WaitForWalkOutComplete());
    }
    
    protected virtual IEnumerator WaitForWalkOutComplete()
    {
        isWalkingOut = true;
        
        // Wait for walk-out animation to complete
        string walkOutAnim = animator.GetBool("IsHappy") ? happyWalkOutState : sadWalkOutState;
        float walkOutDuration = GetAnimationLength(walkOutAnim);
        yield return new WaitForSeconds(walkOutDuration);
        
        OnReachedExit();
    }
    
    protected virtual void OnReachedExit()
    {
        Debug.Log($"{gameObject.name} has left the scene");
        
        // Notify customer manager that this customer is done
        if (customerManager != null)
        {
            customerManager.OnCustomerExited(this);
        }
        
        // Destroy this customer
        Destroy(gameObject);
    }
    
    public virtual void PlayWalkInAnimation()
    {
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 0);
            animator.SetBool("IsHappy", false);
            animator.Play(walkInAnimationState);
            Debug.Log($"{gameObject.name} playing walk in animation: {walkInAnimationState}");
        }
    }
    
    public virtual void PlayOrderReaction(bool perfect)
    {
        if (animator != null)
        {
            if (perfect)
            {
                animator.SetBool("IsHappy", true);
                animator.SetTrigger("TriggerReaction");
                Debug.Log($"{gameObject.name} playing perfect reaction: {perfectReactionState}");
            }
            else
            {
                animator.SetBool("IsHappy", false);
                animator.SetTrigger("TriggerReaction");
                Debug.Log($"{gameObject.name} staying sad for wrong order");
            }
        }
    }
    
    public virtual void PlayWalkOutAnimation(bool happy)
    {
        if (animator != null)
        {
            if (happy)
            {
                animator.SetInteger("CustomerState", 2);
                animator.SetBool("IsHappy", true);
                animator.Play(happyWalkOutState);
                Debug.Log($"{gameObject.name} playing happy walk out: {happyWalkOutState}");
            }
            else
            {
                animator.SetInteger("CustomerState", 1);
                animator.SetBool("IsHappy", false);
                animator.Play(sadWalkOutState);
                Debug.Log($"{gameObject.name} playing sad walk out: {sadWalkOutState}");
            }
        }
    }
    
    public bool IsWaitingForOrder()
    {
        return isWaitingForOrder;
    }
    
    public bool HasReachedServicePoint()
    {
        return hasReachedServicePoint;
    }
    
    public bool IsWalkingIn()
    {
        return isWalkingIn;
    }
    
    public bool IsWalkingOut()
    {
        return isWalkingOut;
    }
}