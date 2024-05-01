using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    // SerializeField allows you to modify variables in Unity interface
    
    //Declared so we can manipulate these components on the Player
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Player Movement")]
    private float moveInput;
    private bool facingRight = true;
    [SerializeField] public float speed;
    [SerializeField] public float jumpSpeed;
    [SerializeField] private AudioClip miniJumpSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Jumping")]
    private bool isGrounded;
    public Transform groundCheck;
    public float checkRadius;
    public LayerMask whatIsGround;
    public int jumpCount;
    [SerializeField] private AudioClip jumpSound;

    [Header("Dashing")]
    [SerializeField] public float dashDistance = 15f;
    [SerializeField] public float dashTime;
    bool isDashing;
    [SerializeField] private AudioClip dashSound;

    [Header("Other Scrips Access")]
    private Trap trapScript;
    private GravitySwap gravityScript;
    

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        trapScript = FindObjectOfType<Trap>();
        gravityScript = FindObjectOfType<GravitySwap>();
    }

    // Update is called once per frame
    private void Update()
    {
        //Boolean checks if Ground Check object (set at Player's feet) is touching the ground 
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        if (!isDashing)
        {
            moveInput = Input.GetAxisRaw("Horizontal");
            rb.velocity = new Vector2(moveInput*speed, rb.velocity.y);
            anim.SetBool("run", moveInput !=0);
            anim.SetBool("grounded", isGrounded);
        }

        //maybe come back to this - get miniJumpSound to play at regular intervals when the character moves side to side on the ground. See coroutine below.
        // if (rb.velocity.x !=0 && isGrounded)
        // {
        //     StartCoroutine(RunCoroutine());
        // }

        //Flips character around when direction chances
        if (facingRight == false && moveInput > 0) 
        {
            Flip();
        } else if (facingRight == true && moveInput < 0) 
        {
            Flip();
        }

        //Jump Logic, jump button changes if gravity is flipped
        if (gravityScript.upsideDown && Input.GetKeyDown(KeyCode.DownArrow) && jumpCount == 0 && isGrounded)
        {
            Jump();
            SoundManager.instance.PlaySound(jumpSound);
            anim.SetTrigger("jump");
            jumpCount = 0;
        } else if (!gravityScript.upsideDown && Input.GetKeyDown(KeyCode.UpArrow) && jumpCount == 0 && isGrounded)
        {
            Jump();
            SoundManager.instance.PlaySound(jumpSound);
            anim.SetTrigger("jump");
            jumpCount = 0;
        }

        //Dashing Logic, see coroutine below as well
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Dash();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Quit();
        }
    }
    
    //Collision Detection. Dying/Respawning for traps, next and previous levels for doors.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Trap") && trapScript != null)
        {
            GetComponent<Collider2D>().enabled = false;
            StartCoroutine(DieAndRespawn());
        }
        else if (other.CompareTag("NextLevel"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else if (other.CompareTag("PreviousLevel"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
    }

    private void Jump()
    {
        if (gravityScript.upsideDown)
        {
            rb.velocity = new Vector2(rb.velocity.x, -(jumpSpeed));
            jumpCount += 1;
        }
        else 
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            jumpCount += 1;
        }
    }

    private void Flip() 
    {
        facingRight = !facingRight;
        Vector3 Scaler = transform.localScale;
        Scaler.x *= -1;
        transform.localScale = Scaler;
    }

    private void Dash()
    {
        isDashing = true;
        SoundManager.instance.PlaySound(dashSound);
        anim.SetBool("dash", isDashing);
        StartCoroutine(DashCoroutine());
    }

    private void MiniJump()
    {
        if (gravityScript.upsideDown) 
        {
            SoundManager.instance.PlaySound(miniJumpSound);
            rb.velocity = new Vector2(rb.velocity.x, -(jumpSpeed/2));
        }
        else 
        {
            SoundManager.instance.PlaySound(miniJumpSound);
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed/2);
        }
    }

    // IEnumerator RunCoroutine()
    // {
    //     SoundManager.instance.PlaySound(miniJumpSound);
    //     yield return new WaitForSeconds(1.1f);
    // }

    IEnumerator DashCoroutine()
    {
        if (facingRight)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(new Vector2(dashDistance, 0f), ForceMode2D.Impulse);
        }
        if (!facingRight)
        {
            rb.velocity = new Vector2(-(rb.velocity.x), 0f);
            rb.AddForce(new Vector2(-(dashDistance + rb.velocity.x), 0f), ForceMode2D.Impulse);
        }
        float gravity = rb.gravityScale;
        rb.gravityScale = 0;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        anim.SetBool("dash", isDashing);
        rb.gravityScale = gravity;
    }

    //interesting bug to return to - if player is mid-death and tries to swap gravity they will turn upside down, interrupt the animation, but not fall upwards. Player still dies and respawns.
    //solution would have the player be unable to do literally anything once they hit a trap, including manipulating gravity. freezing the position is good, but freezing the entire keyboard would be better.
    IEnumerator DieAndRespawn()
    {
        MiniJump();
        yield return new WaitForSeconds(0.1f);
        rb.constraints = RigidbodyConstraints2D.FreezePosition;
        anim.SetTrigger("die");
        yield return new WaitForSeconds(0.7f);
        SoundManager.instance.PlaySound(deathSound); 
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(trapScript.Respawn);

        //ensures gravity/player rotation returns to normal in case character is upside down when they die
        Physics2D.gravity = new Vector2(0f,-9.81f);
        transform.eulerAngles = new Vector3(0,0,0);
    }

    private void Quit()
    {
        SceneManager.LoadScene(0);
    }
}
