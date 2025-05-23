using System.Collections;
using UnityEngine;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Horizontal Movement Settings")]
        [SerializeField] private float walkSpeed = 1;

        [Space(5)] [Header("Dash Settings")] 
        [SerializeField] private GameObject dashEffect;
        [SerializeField] private float dashSpeed;
        [SerializeField] private float dashTime;
        [SerializeField] private float dashCooldown;
        private bool _dashed; // This value is to track if the player dashed in the air
        [Space(5)]
    
        [Header("Vertical Movement Settings")]
        [SerializeField] private float jumpForce = 45;
        private float _jumpBufferCounter;
        [SerializeField] private float jumpBufferFrames;
        private int _airJumpCounter;
        [SerializeField] private int maxAirJumps;
        [Space(5)]
    
        // Coyote time allows player to jump shortly after leaving a platform
        // Great for platformers, like running off a ledge
        private float _coyoteTimeCounter;
        [SerializeField] private float coyoteTime;
    
        [Header("Ground Check Settings")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckY = 0.2f;
        [SerializeField] private float groundCheckX = 0.5f;
        [SerializeField] private LayerMask whatIsGround;
        [Space(5)] 
        
        [Header("Attack Settings")] 
        private bool _attack = false;
        [SerializeField] private float timeBetweenAttacks;
        [SerializeField] private float timeSinceAttack;
    
        // Private variables
        private Rigidbody2D _rb;
        private float _xAxis;
        private Animator _anim;
        private PlayerStateList _playerState;
        private bool _canDash = true;
        private float _gravity;
    
        // Instance is the player instance reference
        // Used in follow camera script
        public static PlayerController Instance;

        // What is the Awake() function doing?
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            _anim = GetComponent<Animator>();
            _playerState = GetComponent<PlayerStateList>();
            _gravity = _rb.gravityScale;
        }

        // Update is called once per frame
        private void Update()
        {
            GetInputs();
            UpdateJumpVariables();
            // If the player is dashing, wait until no longer dashing to be able to use other movements
            if (_playerState.dashing) return;
            Flip();
            Move();
            Jump();
            StartDash();
            Attack();
        }

        private void GetInputs()
        {
            _xAxis = Input.GetAxisRaw("Horizontal");
            _attack = Input.GetMouseButtonDown(0);
        }

        // Action for flipping the character if moving in a new direction
        private void Flip()
        {
            if (_xAxis < 0)
            {
                transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
            }
            else if (_xAxis > 0)
            {
                transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
            }
        }
    
        private void Move()
        {
            _rb.linearVelocity = new Vector2(_xAxis * walkSpeed, _rb.linearVelocity.y);
            // This is setting the boolean for Walking animation
            _anim.SetBool("Walking", _rb.linearVelocity != Vector2.zero && Grounded());
        }

        private void StartDash()
        {
            if (Input.GetButtonDown("Dash") && _canDash && !_dashed)
            {
                StartCoroutine(Dash());
                _dashed = true;
            }

            if (Grounded())
            {
                _dashed = false;
            }
        }
        
        IEnumerator Dash()
        {
            // Set the necessary variables to dash
            _canDash = false;
            _playerState.dashing = true;
            _anim.SetBool("Dashing", true);
            _rb.gravityScale = 0;
            _rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0);
            // Create dash effect if the player is grounded
            if (Grounded())
            {
                // Creates dash effect as a child object
                Instantiate(dashEffect, transform);
            }
            // Wait for the full dash and then reset variables
            yield return new WaitForSeconds(dashTime);
            _rb.gravityScale = _gravity;
            _playerState.dashing = false;
            // After the dash is completed, go through cooldown before allowing dash again
            yield return new WaitForSeconds(dashCooldown);
            _canDash = true;
            
        }

        private void Attack()
        {
            timeSinceAttack += Time.deltaTime;
            // Prevents attack from being spammed, wait for the cooldown
            if (_attack && timeSinceAttack >= timeBetweenAttacks)
            {
                timeSinceAttack = 0;
                _anim.SetTrigger("Attacking");
                    
            }
            
        }
        
        // Why is this not private? Referenced from unity somewhere?
        public bool Grounded()
        {
        
            // Returns true if player is touching the Ground layer or if on the edge of ground layer
            return (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround) ||
                    Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down,
                        groundCheckY, whatIsGround) ||
                    Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down,
                        groundCheckY, whatIsGround));

        }

        private void Jump()
        {
            if (Input.GetButtonUp("Jump") && _rb.linearVelocity.y > 0)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * 0.4f);
                _playerState.jumping = false;
            }

            // Using this if statement for the jump buffer
            if (!_playerState.jumping)
            {
                // If not using coyote time, then can replace time counter with check for if the character is grounded
                if (_jumpBufferCounter > 0 && _coyoteTimeCounter > 0)
                {
                    _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce);
                    _playerState.jumping = true;
                }
                else if (!Grounded() && maxAirJumps > _airJumpCounter && Input.GetButtonDown("Jump"))
                {
                    _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce);
                    _playerState.jumping = true;
                    _airJumpCounter++;
                }
            }
        
            // Set animation boolean for jumping when not on the ground
            _anim.SetBool("Jumping", !Grounded());
        }

        private void UpdateJumpVariables()
        {
            if (Grounded())
            {
                _playerState.jumping = false;
                _coyoteTimeCounter = coyoteTime;
                _airJumpCounter = 0;

            } else if (_coyoteTimeCounter > 0)
            {
                // By decreasing by Time.deltaTime ever frame, it will decrease counter by 1 every second
                _coyoteTimeCounter -= Time.deltaTime;
            }
            if (Input.GetButtonDown("Jump")) {
                _jumpBufferCounter = jumpBufferFrames;
            }
            else if (_jumpBufferCounter > 0)
            {
                _jumpBufferCounter -= Time.deltaTime * 10;

            }
        }
    
    }
}
