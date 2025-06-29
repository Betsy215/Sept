using UnityEngine;
using System.Collections;
using DefaultNamespace;

public abstract class CustomerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform[] movementWaypoints; // 0: spawn, 1: service, 2: exit
    
    [Header("Animation Settings")]
    public string walkInAnimationState = "Sad_Walking";
    public string waitingAnimationState = "Sad_Walking"; // Can be idle later
    public string perfectReactionState = "Perfect_Order";
    public string happyWalkOutState = "happy_walking";
    public string sadWalkOutState = "Sad_Walking";
    
    // Core components
    protected Animator animator;
    protected bool isMoving;
    protected Vector3 targetPosition;
    protected CustomerManager customerManager;
    
    // Current state tracking
    protected bool hasReachedServicePoint = false;
    protected bool isWaitingForOrder = false;
    
    // Abstract properties for variants to override
    public abstract float PatienceLevel { get; }
    public abstract string[] PreferredFoods { get; }
    public abstract float MovementSpeed { get; }
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
        // Start the customer lifecycle
        StartCustomerLifecycle();
    }
    
    protected virtual void Update()
    {
        HandleMovement();
    }
    
    #region Customer Lifecycle
    
    public virtual void StartCustomerLifecycle()
    {
        // Begin by walking in
        PlayWalkInAnimation();
        MoveToServicePoint();
    }
    
    public virtual void OnReachedServicePoint()
    {
        hasReachedServicePoint = true;
        isWaitingForOrder = true;
        
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
        // Customer can react to seeing the order (future: show thought bubble, etc.)
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
        
        // Stay sad, walk out disappointed
        StartCoroutine(DelayedWalkOut(false));
    }
    
    protected virtual IEnumerator DelayedWalkOut(bool happy)
    {
        // Wait a moment for reaction animation to play
        yield return new WaitForSeconds(1.0f);
        
        PlayWalkOutAnimation(happy);
        MoveToExitPoint();
    }
    
    #endregion
    
    #region Animation Control
    
    public virtual void PlayWalkInAnimation()
    {
        if (animator != null)
        {
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
                animator.Play(perfectReactionState);
                Debug.Log($"{gameObject.name} playing perfect reaction: {perfectReactionState}");
            }
            else
            {
                // Stay in current sad animation or play a specific disappointed reaction
                Debug.Log($"{gameObject.name} staying sad for wrong order");
            }
        }
    }
    
    public virtual void PlayWalkOutAnimation(bool happy)
    {
        if (animator != null)
        {
            string animationState = happy ? happyWalkOutState : sadWalkOutState;
            animator.Play(animationState);
            Debug.Log($"{gameObject.name} playing walk out animation: {animationState}");
        }
    }
    
    #endregion
    
    #region Movement System
    
    public virtual void MoveToServicePoint()
    {
        if (movementWaypoints != null && movementWaypoints.Length > 1)
        {
            MoveToPosition(movementWaypoints[1].position); // Service point
        }
    }
    
    public virtual void MoveToExitPoint()
    {
        if (movementWaypoints != null && movementWaypoints.Length > 2)
        {
            MoveToPosition(movementWaypoints[2].position); // Exit point
        }
    }
    
    public virtual void MoveToPosition(Vector3 target)
    {
        targetPosition = target;
        isMoving = true;
    }
    
    protected virtual void HandleMovement()
    {
        if (!isMoving) return;
        
        // Move towards target position
        float step = MovementSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
        
        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isMoving = false;
            OnReachedTarget();
        }
    }
    
    protected virtual void OnReachedTarget()
    {
        // Check which waypoint we reached
        if (movementWaypoints != null && movementWaypoints.Length > 1)
        {
            // Service point
            if (Vector3.Distance(transform.position, movementWaypoints[1].position) < 0.1f && !hasReachedServicePoint)
            {
                OnReachedServicePoint();
            }
            // Exit point
            else if (movementWaypoints.Length > 2 && Vector3.Distance(transform.position, movementWaypoints[2].position) < 0.1f)
            {
                OnReachedExit();
            }
        }
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
    
    #endregion
    
    #region Public Getters
    
    public bool IsWaitingForOrder()
    {
        return isWaitingForOrder;
    }
    
    public bool HasReachedServicePoint()
    {
        return hasReachedServicePoint;
    }
    
    #endregion
}