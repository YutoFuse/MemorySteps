using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    private float speed = 7.0f;
    private Rigidbody2D rb;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) {
            Debug.LogError("Rigidbody2D component not found on the player object.");
        }
    }

    void Update() {
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
        movement = movement.normalized * speed; // ベクトルの正規化

        rb.linearVelocity = movement;
    }
}
