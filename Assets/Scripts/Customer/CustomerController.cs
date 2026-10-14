using UnityEngine;
using System.Collections;

public abstract class CustomerController : MonoBehaviour
{
    [Header("Timing Settings")] public float fallbackWalkInDuration = 2.0f;
    public float servicePointDelay = 1.0f;
    public float walkOutPauseDelay = 0.3f;

    [Header("Sprite Management")] public SpriteRenderer sadSpriteRenderer;
    public SpriteRenderer happySpriteRenderer;

    // Core components
    protected Animator animator;
    protected CustomerManager customerManager;

    // Current state tracking
    protected bool hasReachedServicePoint = false;
    protected bool isWaitingForOrder = false;
    protected bool isWalkingIn = false;
    protected bool isWalkingOut = false;

    // CRITICAL: Prevent multiple walk-out attempts
    protected bool hasProcessedOrder = false;

    /// Which counter slot this customer stands in (0 right, 1 left), set by CustomerManager (12 October 2026)
    [System.NonSerialized] public int Slot;

    [Header("Order Sounds")] public AudioClip perfectOrderSound; // plays when order completed with time bonus
    public AudioClip orderDoneSound;

    // Abstract properties for variants to override
    public abstract float PatienceLevel { get; }
    public abstract string[] PreferredFoods { get; }
    public abstract float OrderDelay { get; }

    public virtual float TipMultiplier => 1.0f; // 1.0 = normal tip
    public virtual int MinOrderItems => 1;
    /// Items added on top of the level's minimum order size (Chef: 1). Clamped to the level maximum.
    public virtual int ExtraOrderItems => 0;
    /// Multiplies the level's order time for this customer (Yoga lady 1.5, Businessman 0.75).
    public virtual float OrderTimeMultiplier => 1f;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        customerManager = FindObjectOfType<CustomerManager>();

        // Auto-find sprite renderers if not assigned
        if (sadSpriteRenderer == null)
            sadSpriteRenderer = GetComponent<SpriteRenderer>();

        if (happySpriteRenderer == null)
        {
            var happyChild = transform.Find("HappySprite");
            if (happyChild != null)
                happySpriteRenderer = happyChild.GetComponent<SpriteRenderer>();
        }

        // Start in sad state
        SetSpriteState(false);
    }

    public virtual void SetSpriteState(bool isHappy)
    {
        if (sadSpriteRenderer != null)
            sadSpriteRenderer.enabled = !isHappy;

        if (happySpriteRenderer != null)
            happySpriteRenderer.enabled = isHappy;
    }

    // SIMPLIFIED: Only called with perfect = true (orders are always completed correctly)
    public virtual void OnOrderServed(bool perfect)
    {
        if (hasProcessedOrder)
        {
            Debug.Log($"{gameObject.name}: Order already processed, ignoring duplicate call");
            return;
        }

        if (perfect)
        {
            var sound = perfectOrderSound ?? AudioManager.Instance.defaultPerfectOrderSound;
            if (sound != null) AudioManager.Instance.PlaySFX(sound);
        }
        else
        {
            var sound = orderDoneSound ?? AudioManager.Instance.defaultOrderDoneSound;
            if (sound != null) AudioManager.Instance.PlaySFX(sound);
        }


        hasProcessedOrder = true;
        isWaitingForOrder = false;

        // In your system, this should always be true
        Debug.Log($"{gameObject.name}: Order completed perfectly - happy walk out");
        PlayHappyReaction();
        StartCoroutine(HappyWalkOut());
    }

    // SIMPLIFIED: Order expired = sad walk out (only other outcome)
    public virtual void OnOrderExpired()
    {
        if (hasProcessedOrder)
        {
            Debug.Log($"{gameObject.name}: Order already processed, ignoring expiration");
            return;
        }

        hasProcessedOrder = true;
        isWaitingForOrder = false;

        Debug.Log($"{gameObject.name}: Order expired - sad walk out");
        PlaySadReaction();
        StartCoroutine(DestroyAfterAnimation());
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(2.0f); // Adjust this to your animation length
        OnReachedExit();
    }

    // CLEANED: Happy reaction using only Animator Controller parameters
    protected virtual void PlayHappyReaction()
    {
        Debug.Log($"{gameObject.name}: Playing happy reaction");
        SetSpriteState(true);

        if (animator != null)
        {
            animator.SetBool("IsHappy", true);
            animator.SetTrigger("TriggerReaction");
        }
    }

    // CLEANED: Sad reaction using only Animator Controller parameters
    protected virtual void PlaySadReaction()
    {
        Debug.Log($"{gameObject.name}: Playing sad reaction");
        SetSpriteState(false);

        if (animator != null)
        {
            animator.SetBool("IsHappy", false);
            animator.SetTrigger("TriggerReaction");
        }
    }

    // CLEANED: Happy walk out - single path, no conflicts
    protected virtual IEnumerator HappyWalkOut()
    {
        if (isWalkingOut)
        {
            Debug.Log($"{gameObject.name}: Already walking out, ignoring duplicate");
            yield break;
        }

        isWalkingOut = true;

        // Brief pause to show reaction
        yield return new WaitForSeconds(walkOutPauseDelay);

        Debug.Log($"{gameObject.name}: Playing happy walk out");
        SetSpriteState(true);

        // Use Animator Controller parameters only
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 2); // 2 = Walking Out Happy
            animator.SetBool("IsHappy", true);
        }

        // Walk until off the right edge of the screen (a left-slot customer needs longer than the old fixed 2 s)
        yield return WalkOffScreen(2.0f, 6.0f);

        // Customer exits
        OnReachedExit();
    }

    /// Waits at least minSeconds, then until the sprite has left the screen to the right, at most maxSeconds
    protected IEnumerator WalkOffScreen(float minSeconds, float maxSeconds)
    {
        var t = 0f;
        var cam = Camera.main;
        var handedOver = false;
        var walkSpeed = 2.5f;                     // world units per second once the clip has played through
        var prevX = transform.position.x;
        while (t < maxSeconds)
        {
            t += Time.deltaTime;
            var right = cam != null ? cam.transform.position.x + cam.orthographicSize * cam.aspect : 3f;
            if (t >= minSeconds && transform.position.x > right + 1.2f) yield break;
            if (!handedOver && animator != null && animator.enabled)
            {
                // The walk clip loops and moves the sprite by a fixed distance, so a customer who is not yet off screen
                // when it ends would snap back and walk again (owner, 13 October: "the happy walk out repeats"). After one
                // play-through the code walks the customer the rest of the way, at the clip's own pace.
                var speedNow = (transform.position.x - prevX) / Mathf.Max(Time.deltaTime, 1e-4f);
                if (speedNow > 0.3f) walkSpeed = Mathf.Lerp(walkSpeed, speedNow, 0.2f);
                var st = animator.GetCurrentAnimatorStateInfo(0);
                if (t > 0.2f && st.normalizedTime >= 0.97f)
                {
                    animator.enabled = false;     // keeps the last pose and position
                    handedOver = true;
                }
            }
            else if (handedOver)
            {
                transform.position += Vector3.right * walkSpeed * Time.deltaTime;
            }
            prevX = transform.position.x;
            yield return null;
        }
    }

    // CLEANED: Sad walk out - single path, no conflicts
    protected virtual IEnumerator SadWalkOut()
    {
        if (isWalkingOut)
        {
            Debug.Log($"{gameObject.name}: Already walking out, ignoring duplicate");
            yield break;
        }

        isWalkingOut = true;

        Debug.Log($"🔴 Starting sad walkout - Time.timeScale = {Time.timeScale}");

        // Brief pause to show reaction
        Debug.Log($"🔴 Waiting {walkOutPauseDelay}s for reaction pause...");
        yield return new WaitForSeconds(walkOutPauseDelay);
        Debug.Log($"🔴 Reaction pause complete");

        Debug.Log($"{gameObject.name}: Playing sad walk out");
        SetSpriteState(false);

        // Use Animator Controller parameters only
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 1);
            animator.SetBool("IsHappy", false);
            Debug.Log($"🔴 Set CustomerState=1, IsHappy=false");
        }

        // Wait for animation to complete
        Debug.Log($"🔴 Waiting 2.0s for animation to complete...");
        var startTime = Time.time;
        yield return new WaitForSeconds(2.0f); // Fallback duration
        var endTime = Time.time;
        Debug.Log($"🔴 Animation wait complete - actual time elapsed: {endTime - startTime}s");

        // Customer exits
        Debug.Log($"🔴 Calling OnReachedExit()");
        OnReachedExit();
    }

    protected virtual void OnReachedExit()
    {
        Debug.Log($"{gameObject.name} has left the scene");

        if (customerManager != null) customerManager.OnCustomerExited(this);

        Destroy(gameObject);
    }

    // Other required methods for walk-in behavior
    public virtual void OnOrderGenerated()
    {
        Debug.Log($"{gameObject.name} sees the order and starts waiting");
        isWaitingForOrder = true;
    }

    public virtual void PlayWalkInAnimation()
    {
        if (animator != null)
        {
            animator.SetInteger("CustomerState", 0); // 0 = Walking In
            animator.SetBool("IsHappy", false);
            Debug.Log($"{gameObject.name}: Playing walk in animation");
        }
    }

    // Animation Event Handlers - called by animation clips
    // These methods handle animation events that might be embedded in any animation clip

    public virtual void OnReachedServicePoint()
    {
        Debug.Log($"{gameObject.name}: Reached service point via animation event");
        hasReachedServicePoint = true;

        // Notify customer manager
        if (customerManager != null) customerManager.OnCustomerReachedService(this);
    }

    public virtual void OnWalkInComplete()
    {
        Debug.Log($"{gameObject.name}: Walk in animation completed via animation event");
        isWalkingIn = false;
    }

    public virtual void OnWalkOutComplete()
    {
        Debug.Log($"{gameObject.name}: Walk out animation completed via animation event");
        isWalkingOut = false;
        OnReachedExit();
    }

    public virtual void OnReactionComplete()
    {
        Debug.Log($"{gameObject.name}: Reaction animation completed via animation event");
        // Reaction is complete, customer can proceed to next state
    }

    public virtual void OnAnimationMidpoint()
    {
        Debug.Log($"{gameObject.name}: Animation midpoint reached via animation event");
        // Generic midpoint event that some animations might use
    }

    public virtual void OnCustomerReady()
    {
        Debug.Log($"{gameObject.name}: Customer ready via animation event");
        // Generic ready state event
    }

    // Helper methods for state checking
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