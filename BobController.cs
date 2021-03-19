using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.XR;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
// ReSharper disable Unity.InefficientPropertyAccess

[System.Serializable]
public struct StepByValue
{
    public int distance;
    public float reward;
    [HideInInspector]
    public float heat;

    public StepByValue(int myDistance, float myReward, float myHeat)
    {
        this.distance = myDistance;
        this.reward = myReward;
        this.heat = myHeat;
        return;
    }
}

public class BobController : Agent
{

    [Header("AI Settings")] 
    public GameObject targetEnemy; // This is the target
    // targetEnemy.transform.position returns the vector3 location of the target vector3 = (x,y,z)
    public float jumpThreshold = 1.0f; // The value that the AI has to pass to jump.
    public StepByValue[] stepsToReward; // If finished in these many steps we will give extra rweard

    [HideInInspector] public TextMeshPro rewardValue = null;
    [HideInInspector] public TextMeshPro episodesValue = null;
    [HideInInspector] public TextMeshPro stepValue = null;

    private float overallReward = 0;
    private float overallSteps = 0;
    

    [HideInInspector] public EnvironmentManager parentScene;
    
    [Header("Movement Settings")] 
    public bool playerControlled = false;
    public float movementSpeed;
    public float jumpHeight;
    public float turnSpeed;
    private bool canJump = true;
    private BoxCollider2D _boxCollider2D;
    
    
    private bool _isDead = false; // Weather the characters healths is equal to 0 or not
    private bool _isMoveing = false; // Is bob moving
    private bool _isGrounded = true; // Is bob touching the ground (used for jumps so that he can't jump again until he is landed)
    
    private Vector3[] _defaultRotations;
    
    private Rigidbody2D _rigidbody2D;
    private Animator _animator;
    
    [Header("Health Settings")]
    public float maxHealth = 100.0f;
    public float health = 100.0f;
    
    private ParticleSystem _particleSystem;
    public GameObject healthBarPivot;


    [Header("Weapon Settings")] 
    public GameObject hand;
    [HideInInspector] public GameObject heldWeapon;
    
    [Header("Barricade Settings")]
    public GameObject barricadeMesh;
    public GameObject barricadeUI;
    public float barricadeCd = 5;
    private int _numOfBarricades = 3; // Number of barricades the player has
    private bool _canPlaceBarricade = true;

    public float extraReward = 0.0f;

    // Start is called before the first frame update
    private void Awake()
    {
        _particleSystem = this.GetComponentInChildren<ParticleSystem>();
        _rigidbody2D = this.GetComponent<Rigidbody2D>();
        _animator = this.GetComponentInChildren<Animator>();
        _boxCollider2D = this.GetComponent<BoxCollider2D>();
        _particleSystem.Stop();
        InitChildRotation();

    }

    private void Start()
    {
        UpdateHealth();
    }
    
    //ML AGENTS --------------------------------------------


    public override void OnEpisodeBegin() // This is being called in every episode
    {
        // Reset the player
        extraReward = 0;
        if(parentScene != null)
            parentScene.SetupScene();
        this.transform.localPosition = new Vector3(-5.5f, 0, 0);
        UpdateStats();
    }
    
    public override void OnActionReceived(float[] vectorAction) // On every tick vector action recieves a random value between -1 and 1
    {
        moveAgent(vectorAction[0]); // We use the first random variable for movement
        jumpAgent(vectorAction[1]); // We use the second random variable for the jumping input
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition); // Pass the player position -- passes 3 values (x,y,z)
        sensor.AddObservation(targetEnemy.transform.localPosition); // Pass the target position -- passes 3 values (x,y,z)
        sensor.AddObservation(Vector3.Distance(targetEnemy.transform.localPosition, transform.localPosition)); // Passes the distance to the target (float)
        sensor.AddObservation((this.transform.position - targetEnemy.transform.position).normalized); // Passes the direction to the target (x,y,z)
        sensor.AddObservation(_isGrounded); // We pass the grounded state to inform the ai that our agent is on air (jumping or falling)
    }

    public override void Heuristic(float[] actionsOut) // For manual movement [DEBUGING PURPOSES]
    {
        actionsOut[0] = Input.GetAxisRaw("Horizontal"); // If the right arrow is pressed the value is 1 if the left one is pressed the value is -1
        if (Input.GetButton("Jump")) // If space button is pressed set the value for actionsOut[1] to the jumpThreshold (which handles the jumping inputs)
        {
            actionsOut[1] = jumpThreshold;
        }
        else
        {
            actionsOut[1] = 0;
        } 
        
    }

    public void GivePoints()
    {
        AddReward(1.0f /*+ extraReward*/);
        for (int i = 0; i < stepsToReward.Length; i++)
        {
            if (StepCount <= stepsToReward[i].distance)
            {
                AddReward(stepsToReward[i].reward);
            }
        }
        UpdateStats();
        EndEpisode();
        parentScene.WinColor();
    }
    
    public void RemovePoints()
    {
        SetReward(-0.5f);
        UpdateStats();
        EndEpisode();
        parentScene.LooseColor();
    }
    
    
    // -----------------------------------------------------
    
    //Agent ACTIONS ----------------------------------------

    void moveAgent(float movement) // This function is used to move the agent
    {
        if (movement == 0)
        {
            _isMoveing = false;
            return;
        }

        float rotation = movement > 0 ? 0 : 180; // Sets the rotation value based on the movement - means left + means right 
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, rotation, transform.eulerAngles.z); // Sets the rotation
        transform.Translate(Vector2.right * (movementSpeed * Time.deltaTime)); // Moves the actor
        KeepChildRotation(); // Keeps the rotation of the UI Objects
        _animator.SetBool("Walk", true); // Sets the animation values for walking
        _isMoveing = true; 
    }

    void jumpAgent(float movement) // This function is used to make the agent jump
    {
        if (checkGround() && movement >= jumpThreshold)
        {
            Debug.Log("JUMMP");
            canJump = false;
            _isGrounded = false;
            _rigidbody2D.AddForce(Vector2.up * jumpHeight, ForceMode2D.Impulse);
            _animator.SetBool("Jump", true);
            
        }
    }
    
    // -----------------------------------------------------

    public void UpdateStats()
    {
        overallReward += GetCumulativeReward();
        overallSteps += StepCount;
        rewardValue.text = $"{overallReward.ToString("F2")}";
        episodesValue.text = $"{CompletedEpisodes}";
        stepValue.text = $"{overallSteps}";
    }
    private void OnCollisionEnter2D(Collision2D other) 
    {
        if (other.gameObject.CompareTag("Ground")) // Called when the player is touching the ground
        {
            //_isGrounded = true;
            _animator.SetBool("Jump", false); // Sets the animation values for jumping
        }

        if (other.gameObject == targetEnemy) // Called when the player touches the target
        {
            GivePoints();
            /*AddReward(5f); // Give reward to the agent
            parentScene.WinColor(); // Set the indicator color to green (for debug)
            EndEpisode(); // End the episode to start a new one based on the findings from this one 
            Destroy(gameObject);*/
        }

        if (other.gameObject.CompareTag("StopBox")) // Called when the player touches the map borders (which is two invisible boxes on the corner of the scenes)
        {
            RemovePoints();
            /*SetReward(-1f); // Punish the agent
            parentScene.LooseColor(); // Set the indicator color to red (for debug)
            EndEpisode(); // End the episode to start a new one based on the findings from this one 
            Destroy(gameObject);*/
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Gripable")) // Get called when the player touches either sword or gun
        {
            PickUpObject(other.gameObject);
        }
    }

    void PickUpObject(GameObject pickedObject) // Handles the picking up of the object
    {
        var gripScript = pickedObject.GetComponent<GripableObject>();
        if (gripScript)
        {
            if (!heldWeapon)
            {
                pickedObject.transform.position = hand.transform.position;
                pickedObject.transform.rotation = hand.transform.rotation;

                pickedObject.transform.SetParent(hand.gameObject.transform);
                gripScript.isGrabbed = true;
                gripScript.OwnerPlayer = this.gameObject;
                heldWeapon = pickedObject;
            }
        }
    }
    
    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground")) // Gets called when the agent stops touching the ground
        {
            //_isGrounded = false;
            //_animator.SetBool("Jump", true);
        }
    }
    
    public void ApplyDamage(float damage) // Gets called when the agent gets hit (recieves damage)
    {
        health -= damage;
        if (health <= 0)
        {
            _isDead = true;
        }
        UpdateHealth();
    }

    public void timedOut() // Gets called by the EnvironmentManager when the time for generation is passed
    {
        RemovePoints();
        /*SetReward(-2f);
        parentScene.LooseColor();
        EndEpisode();
        Destroy(gameObject);*/
    }
    
    bool checkGround()
    {
        int layerMask = (LayerMask.GetMask("Ground"));
        float extraHeight = 0.05f;
        RaycastHit2D raycastHit2D = Physics2D.Raycast(_boxCollider2D.bounds.center, Vector2.down, _boxCollider2D.bounds.extents.y + extraHeight,layerMask);
        if (raycastHit2D.collider != null)
        {
            Debug.DrawRay(_boxCollider2D.bounds.center,Vector2.down *(_boxCollider2D.bounds.extents.y + extraHeight), Color.green);
        }
        else
        {
            Debug.DrawRay(_boxCollider2D.bounds.center,Vector2.down *(_boxCollider2D.bounds.extents.y + extraHeight), Color.red);
        }

        return raycastHit2D.collider != null;
    }
    
    void UpdateHealth() // Updates the health UI
    {
        healthBarPivot.transform.localScale = new Vector3(Mathf.Max(0,health / maxHealth),1,1);
    }

    void MoveHand(bool up) // Used to move the hand up and down
    {
        var turn = hand.transform.forward;
        turn = new Vector3(Mathf.Abs(hand.transform.forward.x), Mathf.Abs(hand.transform.forward.y), Mathf.Abs(hand.transform.forward.z));
        if (up)
        {
            turn *= turnSpeed;
        }
        else
        {
            turn *= turnSpeed * -1;
        }

        hand.transform.Rotate(turn * Time.deltaTime);
    }

    void InitChildRotation() // Inits the rotation array
    {
        _defaultRotations = new Vector3[this.transform.childCount];
        var index = 0;
        foreach (Transform child in transform)
        {
            if (child.gameObject.CompareTag("KeepRotation"))
            {
                _defaultRotations[index] = child.eulerAngles;
                index++;
            }
        }
    }
    void KeepChildRotation() // Keeps the rotation of the child objects
    {
        var index = 0;
        foreach (Transform child in transform)
        {
            if(child == null)
                continue;

            if (_defaultRotations.Length <= index)
            {
                break;
            }
            
            if (child.gameObject.CompareTag("KeepRotation"))
            {
                child.eulerAngles = _defaultRotations[index];
                index++;
            }
        }
    }

    
    // Update is called once per frame
    void Update()
    {

        checkGround();
        if (!_isDead)
        {
            if (_isMoveing)
            {
                if (!_particleSystem.isPlaying)
                {
                    _particleSystem.Play();
                }
            }
            else
            {
                if (_particleSystem.isPlaying)
                {
                    _particleSystem.Stop();
                }
            }

            if (Input.GetButtonDown("Submit") && playerControlled)
            {
                int layerMask = ~(LayerMask.GetMask("Ground"));
                var startPos = new Vector2(hand.transform.position.x, hand.transform.position.y) + (new Vector2(transform.right.x,transform.right.y) * 1f);
                var targetPos = startPos + (Vector2.down * 5f);
                Debug.DrawLine(startPos,targetPos,Color.green,1,false);
                RaycastHit2D hit = Physics2D.Raycast(startPos, Vector2.down,Mathf.Infinity, layerMask);
                if (hit.transform.gameObject.CompareTag("Ground") && _canPlaceBarricade && _numOfBarricades > 0)
                {
                    Instantiate(barricadeMesh, hit.point, Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, 0)));
                    _canPlaceBarricade = false;
                    _numOfBarricades -= 1;
                    UpdateBarricadeUI();
                    StartCoroutine(BarricadeTimer());
                }
                
            }
            
            if (Input.GetButtonUp("Horizontal") && playerControlled)
            {
                _isMoveing = false;
                _animator.SetBool("Walk", false);
            }
            
            if (Input.GetButton("Horizontal") && playerControlled)
            {
                if (Input.GetAxis("Horizontal") > 0)
                {
                    transform.Translate(Vector2.right * (movementSpeed * Time.deltaTime));
                    transform.eulerAngles = new Vector3(transform.eulerAngles.x, 0,
                        transform.eulerAngles.z);
                    KeepChildRotation();
                    
                }
                else if (Input.GetAxisRaw("Horizontal") < 0)
                {
                    transform.Translate(Vector2.right * (movementSpeed * Time.deltaTime));
                    transform.eulerAngles = new Vector3(transform.eulerAngles.x, 180,
                        transform.eulerAngles.z);
                    KeepChildRotation();
                }
                
                _animator.SetBool("Walk", true);
                _isMoveing = true;
            }

            if (Input.GetButton("Vertical") && playerControlled)
            {
                if (Input.GetAxis("Vertical") > 0)
                {
                    MoveHand(true);
                    Debug.Log("TURNIING");
                }
                if(Input.GetAxis("Vertical") < 0)
                {
                    MoveHand(false);
                }
            }
        }
    }
    
    void UpdateBarricadeUI() // Updates the barricade UI whenever one is used
    {
        for (int i = 0; i < barricadeUI.transform.childCount; i++)
        {
            var renderer = barricadeUI.transform.GetChild(i).GetComponent<SpriteRenderer>();
            renderer.enabled = i < _numOfBarricades;
        }
    }
    
    IEnumerator BarricadeTimer() // Function that handles the timer for the barricade
    {
        yield return new WaitForSeconds(barricadeCd);
        _canPlaceBarricade = true;
    }
}
