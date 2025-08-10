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
    [Tooltip("Pause before customer starts walking out after order completion")]
    public float walkOutPauseDelay = 0.3f;
    [Tooltip("Timeout for animation state detection (safety fallback)")]
    public float animationDetectionTimeout = 5.0f;
    
    [Header("Walk-Out Settings")]
    [Tooltip("Distance sad customers walk to the right")]
    public float sadWalkDistance = 5.0f;
    [Tooltip("Duration for sad customers to walk to the right")]
    public float sadWalkDuration = 3.0f;
    
    [Header("Sprite Management")]
    [Tooltip("SpriteRenderer for sad/default state")]
    public SpriteRenderer sadSpriteRenderer;
    [Tooltip("SpriteRenderer for happy state")]  
    public SpriteRenderer happySpriteRenderer;
    
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
    
    public virtual void SetSpriteState(bool isHappy)
    {
        Debug.Log($"{gameObject.name}: SetSpriteState called with isHappy={isHappy}");
        Debug.Log($"BEFORE: Sad={sadSpriteRenderer?.enabled}, Happy={happySpriteRenderer?.enabled}");
    
        if (sadSpriteRenderer != null)
            sadSpriteRenderer.enabled = !isHappy;
        else
            Debug.LogError($"{gameObject.name}: sadSpriteRenderer is NULL!");
        
        if (happySpriteRenderer != null)
            happySpriteRenderer.enabled = isHappy;
        else
            Debug.LogError($"{gameObject.name}: happySpriteRenderer is NULL!");
        
        Debug.Log($"AFTER: Sad={sadSpriteRenderer?.enabled}, Happy={happySpriteRenderer?.enabled}");
        Debug.Log($"{gameObject.name}: Sprite state set to {(isHappy ? "Happy" : "Sad")}");
    }
    
    protected virtual void Awake()
    {
        Debug.Log($"{gameObject.name}: Starting Awake()");
    
        animator = GetComponent<Animator>();
        customerManager = FindObjectOfType<CustomerManager>();
    
        Debug.Log($"{gameObject.name}: Animator found: {animator != null}");
        Debug.Log($"{gameObject.name}: CustomerManager found: {customerManager != null}");
    
        // Auto-find sprite renderers if not assigned
        if (sadSpriteRenderer == null)
        {
            sadSpriteRenderer = GetComponent<SpriteRenderer>(); // Main sprite renderer
            Debug.Log($"{gameObject.name}: Sad sprite renderer found: {sadSpriteRenderer != null}");
        }
        
        if (happySpriteRenderer == null)
        {
            // Look for sprite renderer on child named "HappySprite"
            Transform happyChild = transform.Find("HappySprite");
            Debug.Log($"{gameObject.name}: HappySprite child found: {happyChild != null}");
        
            if (happyChild != null)
            {
                happySpriteRenderer = happyChild.GetComponent<SpriteRenderer>();
                Debug.Log($"{gameObject.name}: Happy sprite renderer found: {happySpriteRenderer != null}");
            }
        }
    
        Debug.Log($"{gameObject.name}: About to call SetSpriteState(false)");
    
        // Start in sad state
        SetSpriteState(false);
    
        Debug.Log($"{gameObject.name}: SetSpriteState(false) completed");
    
        if (animator == null)
        {
            Debug.LogError($"No Animator component found on {gameObject.name}");
        }
    
        Debug.Log($"{gameObject.name}: Awake() completed successfully");
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
            float timeout = animationDetectionTimeout; // Safety timeout
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
    
    // ENHANCED: OnOrderExpired method with reaction first
    public virtual void OnOrderExpired()
    {
        isWaitingForOrder = false;
        Debug.Log($"🔥 {gameObject.name} is frustrated - order expired!");
        Debug.Log($"🔥 Starting sad walk out sequence for expired order...");
        
        // CRITICAL: Play disappointed reaction FIRST before walking out
        PlayOrderReaction(false);
        
        // Then start walk out after a brief delay to show the reaction
        StartCoroutine(DelayedWalkOutAfterReaction(false));
    }
    
    // NEW: Delayed walk out after showing reaction
    protected virtual IEnumerator DelayedWalkOutAfterReaction(bool happy)
    {
        // Brief pause to show the disappointment reaction
        yield return new WaitForSeconds(1.0f);
        
        // Now start the walk out
        yield return StartCoroutine(DelayedWalkOut(happy));
    }
    
    // ENHANCED: DelayedWalkOut method with better debugging
    protected virtual IEnumerator DelayedWalkOut(bool happy)
    {
        Debug.Log($"🚶 {gameObject.name}: Starting walk out sequence - Happy: {happy}");
        
        if (happy)
        {
            Debug.Log($"😊 {gameObject.name}: Customer is HAPPY - playing happy walk out");
        }
        else
        {
            Debug.Log($"😞 {gameObject.name}: Customer is SAD/DISAPPOINTED - playing sad walk out");
        }
        
        // Brief pause before walking out 
        yield return new WaitForSeconds(walkOutPauseDelay);
        
        if (happy)
        {
            // Happy customers: normal walk out (could be any direction)
            PlayWalkOutAnimation(true);
            yield return StartCoroutine(WaitForWalkOutComplete());
        }
        else
        {
            // SAD/DISAPPOINTED customers: walk from middle to right
            Debug.Log($"🔄 {gameObject.name}: Customer is sad - starting SadWalkOutToRight coroutine");
            yield return StartCoroutine(SadWalkOutToRight());
        }
    }
    
    // ENHANCED: SadWalkOutToRight with detailed debugging
    protected virtual IEnumerator SadWalkOutToRight()
    {
        Debug.Log($"💔 {gameObject.name}: STARTING SadWalkOutToRight coroutine");
        
        isWalkingOut = true;
        
        // CRITICAL: Play sad walking animation BEFORE starting movement
        Debug.Log($"🎬 {gameObject.name}: Playing sad walking animation...");
        PlaySadWalkOutAnimation();
        
        // Get current position (should be at service point - middle of screen)
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + new Vector3(sadWalkDistance, 0f, 0f); // Move right
        
        Debug.Log($"📍 {gameObject.name}: Starting position: {startPosition}");
        Debug.Log($"📍 {gameObject.name}: Target position: {endPosition}");
        Debug.Log($"⏱️ {gameObject.name}: Walk duration: {sadWalkDuration}s");
        
        // Walk to the right over time
        float elapsed = 0f;
        
        while (elapsed < sadWalkDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / sadWalkDuration;
            
            // Move smoothly from start to end position
            Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, progress);
            transform.position = currentPos;
            
            // Debug every 0.5 seconds
            if (elapsed % 0.5f < Time.deltaTime)
            {
                Debug.Log($"🚶‍♂️ {gameObject.name}: Walking... Progress: {progress:F2}, Position: {currentPos}");
            }
            
            yield return null;
        }
        
        // Ensure final position
        transform.position = endPosition;
        
        Debug.Log($"✅ {gameObject.name}: Reached final exit position: {endPosition}");
        Debug.Log($"🗑️ {gameObject.name}: Removing sad customer from scene");
        
        // Customer has exited
        OnReachedExit();
    }
    
    // ENHANCED: PlaySadWalkOutAnimation with detailed debugging
    protected virtual void PlaySadWalkOutAnimation()
    {
        Debug.Log($"🎭 {gameObject.name}: PlaySadWalkOutAnimation called");
        
        if (animator != null)
        {
            Debug.Log($"🎬 {gameObject.name}: Animator found - setting up sad walk animation");
            
            // CRITICAL: Set sprite state to sad BEFORE animation
            SetSpriteState(false);
            Debug.Log($"😞 {gameObject.name}: Set sprite to SAD state");
            
            // Set parameters for sad walking out
            animator.SetInteger("CustomerState", 1); // 1 = Walking Out Sad  
            animator.SetBool("IsHappy", false);
            
            Debug.Log($"🎛️ {gameObject.name}: Set CustomerState=1, IsHappy=false");
            Debug.Log($"🎬 {gameObject.name}: Playing animation: {sadWalkOutState}");
            
            animator.Play(sadWalkOutState);
            
            // VERIFY: Check if animation is actually playing
            StartCoroutine(VerifyAnimationPlaying());
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name}: NO ANIMATOR FOUND! Cannot play sad walk animation!");
        }
    }
    
    // NEW: Verify that the animation is actually playing
    protected virtual IEnumerator VerifyAnimationPlaying()
    {
        yield return new WaitForSeconds(0.1f); // Wait a frame for animation to start
        
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"🔍 {gameObject.name}: Current animation state: {stateInfo.fullPathHash}");
            Debug.Log($"🔍 {gameObject.name}: Animation playing: {stateInfo.IsName(sadWalkOutState)}");
            Debug.Log($"🔍 {gameObject.name}: Animation length: {stateInfo.length}s");
            Debug.Log($"🔍 {gameObject.name}: CustomerState parameter: {animator.GetInteger("CustomerState")}");
            Debug.Log($"🔍 {gameObject.name}: IsHappy parameter: {animator.GetBool("IsHappy")}");
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
    
    // ENHANCED: PlayOrderReaction for better disappointment display
    public virtual void PlayOrderReaction(bool perfect)
    {
        if (animator != null)
        {
            if (perfect)
            {
                Debug.Log($"😊 {gameObject.name}: Playing PERFECT order reaction");
                // SWITCH TO HAPPY SPRITE FIRST, BEFORE ANIMATION
                SetSpriteState(true);
            
                // Small delay to ensure sprite swap completes
                StartCoroutine(DelayedPerfectAnimation());
            }
            else
            {
                Debug.Log($"😞 {gameObject.name}: Playing DISAPPOINTED order reaction");
                // For wrong orders: Keep sad sprite and show disappointment
                SetSpriteState(false);
                animator.SetBool("IsHappy", false);
                animator.SetTrigger("TriggerReaction");
                Debug.Log($"💔 {gameObject.name}: Triggered disappointment reaction");
            }
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name}: No animator for order reaction!");
        }
    }

    /// <summary>
    /// Play perfect animation after sprite swap
    /// </summary>
    private IEnumerator DelayedPerfectAnimation()
    {
        // Wait one frame to ensure sprite swap is complete
        yield return null;
    
        // Now play the animation on the happy sprite
        animator.SetBool("IsHappy", true);
        animator.SetTrigger("TriggerReaction");
        Debug.Log($"{gameObject.name} playing perfect reaction: {perfectReactionState}");
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
                // Switch to happy sprite immediately for happy walk out
                SetSpriteState(true);
            
                // Play happy walk animation (it will animate the sad sprite, but it's hidden)
                animator.SetInteger("CustomerState", 2); // 2 = Walking Out Happy
                animator.SetBool("IsHappy", true);
                animator.Play(happyWalkOutState);
                Debug.Log($"{gameObject.name} playing happy walk out: {happyWalkOutState}");
            }
            else
            {
                // Keep sad sprite for sad walk out
                SetSpriteState(false);
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
    
    [ContextMenu("Test Order Expired Animation")]
    public void TestOrderExpiredAnimation()
    {
        if (Application.isPlaying)
        {
            Debug.Log($"🧪 Testing order expired animation for {gameObject.name}");
            OnOrderExpired();
        }
        else
        {
            Debug.LogWarning("Can only test animations in Play Mode!");
        }
    }
}