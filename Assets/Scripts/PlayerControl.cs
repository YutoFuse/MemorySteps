using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    public float speed = 7.0f;
    public bool canMove = true; // 移動可能ならtrue
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;


    void Start() {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update() {
        if (!canMove) {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 入力に基づいて移動方向を設定
        Vector2 movement = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) {
            movement.y += 1;
        }
        if (Input.GetKey(KeyCode.S)) {
            movement.y -= 1;
        }
        if (Input.GetKey(KeyCode.A)) {
            movement.x -= 1;
        }
        if (Input.GetKey(KeyCode.D)) {
            movement.x += 1;
        }
        movement = movement.normalized * speed;
        
        rb.linearVelocity = movement;
        bool isMoving = movement != Vector2.zero;
        anim.SetBool("isMoving", isMoving);

        if (movement.x > 0) {
            spriteRenderer.flipX = false; // 右向き
        } else if (movement.x < 0) {
            spriteRenderer.flipX = true; // 左向き
        }
    }

    public void DisableMovement() {
        canMove = false;
    }

    public void EnableMovement() {
        canMove = true;
    }
}