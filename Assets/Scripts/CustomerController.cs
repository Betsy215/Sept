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
    
    [Header("Timing Settings")]
    [Tooltip("Fallback duration if animation length detection fails")]
    public float fallbackWalkInDuration = 2.0f;
    [Tooltip("Additional delay after reaching service point before order generation")]
    public float servicePointDelay = 1.0f;
    
    [Header("Walk-Out Settings")]
    [Tooltip("Distance sad customers walk to the right")]
    public float sadWalkDistance = 5.0f;
    [Tooltip("Duration for sad customers to walk to the right")]
    public float sadWalkDuration = 3.0f;
    
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
        
        // FIXED: Use proper animation state detection instead of timing
        StartCoroutine(WaitForWalkInAnimationComplete());
    }
    
    /// <summary>
    /// FIXED: Properly detect when walk-in animation completes using animation state monitoring
    /// </summary>
    protected virtual IEnumerator WaitForWalkInAnimationComplete()
    {
        if (animator == null)
        {
            Debug.LogWarning($"{gameObject.name}: No animator found, using fallback timing");
            yield return new WaitForSeconds(fallbackWalkInDuration);
            OnReachedServicePoint();
            yield break;
        }
        
        // Wait one frame for animation to start
        yield return null;
        
        // Method 1: Wait for animation state to finish (most reliable)
        bool useStateMonitoring = true;
        
        if (useStateMonitoring)
        {
            // Wait for the walk-in animation state to start
            float timeout = 5.0f; // Safety timeout
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                
                // Check if we're in the walk-in animation
                if (stateInfo.IsName(walkInAnimationState))
                {
                    Debug.Log($"{gameObject.name}: Walk-in animation detected, waiting for completion...");
                    
                    // Now wait for the animation to complete
                    while (stateInfo.normalizedTime < 1.0f)
                    {
                        yield return null;
                        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                        
                        // Safety check - if we're no longer in the walk-in state, break
                        if (!stateInfo.IsName(walkInAnimationState))
                            break;
                    }
                    
                    Debug.Log($"{gameObject.name}: Walk-in animation completed");
                    break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogWarning($"{gameObject.name}: Animation detection timed out, proceeding anyway");
            }
        }
        else
        {
            // Method 2: Fallback to duration-based timing
            float walkInDuration = GetAnimationLength(walkInAnimationState);
            if (walkInDuration <= 0)
                walkInDuration = fallbackWalkInDuration;
                
            Debug.Log($"{gameObject.name}: Using duration-based timing: {walkInDuration}s");
            yield return new WaitForSeconds(walkInDuration);
        }
        
        // Animation finished, customer has reached service point
        OnReachedServicePoint();
    }
    
    /// <summary>
    /// Fallback method to get animation length from clips
    /// </summary>
    protected virtual float GetAnimationLength(string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) 
            return fallbackWalkInDuration;
        
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animationName || clip.name.Contains(animationName))
            {
                Debug.Log($"{gameObject.name}: Found animation '{clip.name}' with length {clip.length}s");
                return clip.length;
            }
        }
        
        Debug.LogWarning($"{gameObject.name}: Animation '{animationName}' not found, using fallback duration");
        return fallbackWalkInDuration;
    }
    
    public virtual void OnReachedServicePoint()
    {
        hasReachedServicePoint = true;
        isWaitingForOrder = true;
        isWalkingIn = false;
        
        Debug.Log($"{gameObject.name} reached service point. Starting {servicePointDelay}s delay before order generation");
        
        // FIXED: Add configurable delay at service point before order generation
        StartCoroutine(ServicePointDelay());
    }
    
    /// <summary>
    /// NEW: Configurable delay after reaching service point before order generation
    /// </summary>
    protected virtual IEnumerator ServicePointDelay()
    {
        // Wait the specified delay at service point
        yield return new WaitForSeconds(servicePointDelay);
        
        // Now notify customer manager to start order delay
        if (customerManager != null)
        {
            customerManager.OnCustomerReachedService(this);
        }
        
        Debug.Log($"{gameObject.name} service point delay complete. Order delay: {OrderDelay}s");
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
    
    /// <summary>
    /// MODIFIED: Different walk-out behavior based on customer satisfaction
    /// </summary>
    protected virtual IEnumerator DelayedWalkOut(bool happy)
    {
        Debug.Log($"{gameObject.name}: Starting walk out sequence - Happy: {happy}");
        
        // Brief pause before walking out 
        yield return new WaitForSeconds(0.3f);
        
        if (happy)
        {
            // Happy customers: normal walk out (could be any direction)
            PlayWalkOutAnimation(true);
            yield return StartCoroutine(WaitForWalkOutComplete());
        }
        else
        {
            // SAD/DISAPPOINTED customers: walk from middle to right
            Debug.Log($"{gameObject.name}: Customer is sad - walking from current position to right");
            yield return StartCoroutine(SadWalkOutToRight());
        }
    }
    
    /// <summary>
    /// NEW: Sad customers walk from middle (current position) to the right
    /// </summary>
    protected virtual IEnumerator SadWalkOutToRight()
    {
        isWalkingOut = true;
        
        // Play sad walking animation
        PlaySadWalkOutAnimation();
        
        // Get current position (should be at service point - middle of screen)
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + new Vector3(sadWalkDistance, 0f, 0f); // Move right
        
        // Walk to the right over time
        float elapsed = 0f;
        
        Debug.Log($"{gameObject.name}: Walking from {startPosition} to {endPosition} over {sadWalkDuration}s");
        
        while (elapsed < sadWalkDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / sadWalkDuration;
            
            // Move smoothly from start to end position
            transform.position = Vector3.Lerp(startPosition, endPosition, progress);
            
            yield return null;
        }
        
        // Ensure final position
        transform.position = endPosition;
        
        Debug.Log($"{gameObject.name}: Reached exit position, removing customer");
        
        // Customer has exited
        OnReachedExit();
    }
    
    /// <summary>
    /// NEW: Play sad walking animation specifically for walk-out
    /// </summary>
    protected virtual void PlaySadWalkOutAnimation()
    {
        if (animator != null)
        {
            // Set parameters for sad walking out
            animator.SetInteger("CustomerState", 1); // 1 = Walking Out Sad  
            animator.SetBool("IsHappy", false);
            animator.Play(sadWalkOutState);
            
            Debug.Log($"{gameObject.name}: Playing sad walk out animation - {sadWalkOutState}");
        }
    }
    
    /// <summary>
    /// MODIFIED: Wait for walk out complete (for happy customers)
    /// </summary>
    protected virtual IEnumerator WaitForWalkOutComplete()
    {
        isWalkingOut = true;
        
        // Wait for walk-out animation to complete (for happy customers)
        string walkOutAnim = happyWalkOutState;
        float walkOutDuration = GetAnimationLength(walkOutAnim);
        if (walkOutDuration <= 0) walkOutDuration = 2.0f;
        
        Debug.Log($"{gameObject.name}: Waiting {walkOutDuration}s for happy walk out to complete");
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
    
    /// <summary>
    /// MODIFIED: Happy walk out (can keep original behavior or modify)
    /// </summary>
    public virtual void PlayWalkOutAnimation(bool happy)
    {
        if (animator != null)
        {
            if (happy)
            {
                // Happy walk out - could walk to left, right, or fade out
                animator.SetInteger("CustomerState", 2); // 2 = Walking Out Happy
                animator.SetBool("IsHappy", true);
                animator.Play(happyWalkOutState);
                Debug.Log($"{gameObject.name} playing happy walk out: {happyWalkOutState}");
            }
            else
            {
                // This is now handled by PlaySadWalkOutAnimation()
                PlaySadWalkOutAnimation();
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
    
    // Debug methods for testing
    [ContextMenu("Test Perfect Order Animation")]
    public void TestPerfectOrderAnimation()
    {
        if (Application.isPlaying)
        {
            Debug.Log($"🧪 Testing perfect order animation for {gameObject.name}");
            OnOrderServed(true);
        }
        else
        {
            Debug.LogWarning("Can only test animations in Play Mode!");
        }
    }
    
    [ContextMenu("Test Wrong Order Animation")]
    public void TestWrongOrderAnimation()
    {
        if (Application.isPlaying)
        {
            Debug.Log($"🧪 Testing wrong order animation for {gameObject.name}");
            OnOrderServed(false);
        }
        else
        {
            Debug.LogWarning("Can only test animations in Play Mode!");
        }
    }
}