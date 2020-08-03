using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour {

    const float skinWidth = 0.015f;
    const float minRayLength = skinWidth * 2;
    const float gravityScale = -5f;

    bool wallGrab = false;
    bool wallJumped = false;
    bool canMove = true;

    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float lowJumpMultiplier = 2f;

    [Space]

    [SerializeField] float jumpForce = 4f;
    [SerializeField] float speed = 7f;
    [SerializeField] float slideSpeed = 1f;
    private Vector2 velocity;

    [SerializeField] float horizontalRayCount = 4f, verticalRayCount = 4f;
    float horizontalRaySpacing, verticalRaySpacing;

    BoxCollider2D hitbox2D;
    RaycastOrigins raycastOrigins;
    [SerializeField] CollisionInfo collisions;
    
    [Space]

    [SerializeField] LayerMask collisionMask;

    private void OnValidate() {
        if (hitbox2D == null)
            hitbox2D = GetComponent<BoxCollider2D>();
    }

    private void Start() {
        UpdateRaycastOrigins();
        CalculateRaySpacing();
        collisions = new CollisionInfo();
    }

    private void FixedUpdate() {
        UpdateCollisionInfo();

        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        Vector2 dir = new Vector2(x, y);
        Walk(dir);

        if (collisions.OnGround) {
            velocity.y = 0f;
            wallJumped = false;
        }
        else {
            //Apply Gravity
            velocity.y += gravityScale * Time.fixedDeltaTime;

            //Better Jump Logic
            if (velocity.y < 0) {
                velocity.y += gravityScale * Time.fixedDeltaTime * (fallMultiplier - 1);
            }
            else if (velocity.y > 0 && !Input.GetKey(KeyCode.Space)) {
                velocity.y += gravityScale * Time.fixedDeltaTime * (lowJumpMultiplier - 1);
            }
        }

        if (collisions.OnWall && !collisions.OnGround) {
            //We are sliding on a wall;
            WallSlide();
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (collisions.OnGround) {
                Jump(Vector2.up);
            }
            else if (collisions.OnWall) {
                WallJump();
            }
        }

        wallGrab = collisions.OnWall && Input.GetKey(KeyCode.LeftShift);
        if (wallGrab) {
            velocity.y = y * speed;
        }

        Move();
    }

    private void Move() {
        float deltaX = MoveHorizontally(velocity.x * Time.fixedDeltaTime);
        float deltaY = MoveVertically(velocity.y * Time.fixedDeltaTime);

        transform.Translate(deltaX, deltaY, 0f);
    }

    private void WallSlide() {
        if (!canMove)
            return;

        int velDir = Math.Sign(velocity.x);
        int wallDir = (collisions.right) ? 1 : -1;
        bool pushingOnWall = velDir == wallDir;
        float push = pushingOnWall ? 0 : velocity.x;

        velocity = new Vector2(push, -slideSpeed);
    }

    private void Walk(Vector2 dir) {

        if (!canMove)
            return;

        if (!wallJumped) {
            velocity = new Vector2(dir.x * speed, velocity.y);
        }
        else {
            velocity = Vector2.Lerp(velocity, new Vector2(dir.x * speed, velocity.y), 1f * Time.fixedDeltaTime);
        }
    }

    private void Jump(Vector2 direction) {
        velocity.y = 0f;
        velocity += direction * jumpForce;
    }

    private void WallJump() {
        //StopCoroutine(DisableMovement(0f));
        //StartCoroutine(DisableMovement(0.1f));

        Vector2 wallDir = (collisions.right) ? Vector2.left : Vector2.right;
        Jump(Vector2.up / 1.5f + wallDir / 1.5f);

        wallJumped = true;
    }

    IEnumerator DisableMovement(float time) {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    private void UpdateCollisionInfo() {
        UpdateRaycastOrigins();

        RaycastHit2D hitUp = Physics2D.Raycast(raycastOrigins.topLeft + new Vector2(0f, skinWidth * 2f), Vector2.right, (hitbox2D.bounds.size.y - skinWidth * 2f), collisionMask);
        RaycastHit2D hitDown = Physics2D.Raycast(raycastOrigins.bottomLeft - new Vector2(0f, skinWidth * 2f), Vector2.right, (hitbox2D.bounds.size.y - skinWidth * 2f), collisionMask);
        RaycastHit2D hitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft - new Vector2(skinWidth * 2, 0f), Vector2.up, (hitbox2D.bounds.size.x - skinWidth * 2f), collisionMask);
        RaycastHit2D hitRight = Physics2D.Raycast(raycastOrigins.bottomRight + new Vector2(skinWidth * 2f, 0f), Vector2.up, (hitbox2D.bounds.size.x - skinWidth * 2f), collisionMask);

        Debug.DrawRay(raycastOrigins.topLeft + new Vector2(0f, skinWidth * 2f), Vector2.right * (hitbox2D.bounds.size.y - skinWidth * 2f), Color.blue);
        Debug.DrawRay(raycastOrigins.bottomLeft - new Vector2(0f, skinWidth * 2f), Vector2.right * (hitbox2D.bounds.size.y - skinWidth * 2f), Color.blue);
        Debug.DrawRay(raycastOrigins.bottomLeft - new Vector2(skinWidth * 2, 0f), Vector2.up * (hitbox2D.bounds.size.x - skinWidth * 2f), Color.blue);
        Debug.DrawRay(raycastOrigins.bottomRight + new Vector2(skinWidth * 2f, 0f), Vector2.up * (hitbox2D.bounds.size.x - skinWidth * 2f), Color.blue);

        collisions.down = hitDown;
        collisions.up = hitUp;
        collisions.left = hitLeft;
        collisions.right = hitRight;
    }

    private float MoveHorizontally(float moveDistance) {
        if (moveDistance != 0) {
            float directionX = Mathf.Sign(moveDistance);
            float rayLength = Mathf.Abs(moveDistance) + skinWidth;


            for (int i = 0; i < horizontalRayCount; i++) {
                Vector2 rayOrigin = (directionX == 1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
                rayOrigin += i * Vector2.up * verticalRaySpacing;
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
                Debug.DrawRay(rayOrigin, Vector2.right * rayLength * directionX, Color.red);

                if (hit) {
                    moveDistance = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;
                }
            }
            return moveDistance;
        }

        return 0f;
    }

    private float MoveVertically(float moveDistance) {
        if (moveDistance != 0) {
            float directionY = Mathf.Sign(moveDistance);
            float rayLength = Mathf.Abs(moveDistance) + skinWidth;

            for (int i = 0; i < verticalRayCount; i++) {
                Vector2 rayOrigin = (directionY == 1) ? raycastOrigins.topLeft : raycastOrigins.bottomLeft;
                rayOrigin += i * Vector2.right * verticalRaySpacing;
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
                Debug.DrawRay(rayOrigin, Vector2.up * rayLength * directionY, Color.red);

                if (hit) {
                    moveDistance = (hit.distance - skinWidth) * directionY;
                    rayLength = hit.distance;
                }
            }

            return moveDistance;
        }

        return 0f;
    }

    void UpdateRaycastOrigins() {
        Bounds bounds = hitbox2D.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void CalculateRaySpacing() {
        Bounds bounds = hitbox2D.bounds;
        bounds.Expand(skinWidth * -2);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    private struct RaycastOrigins {
        public Vector2 bottomLeft, bottomRight, topLeft, topRight;
    }

    [Serializable]
    private struct CollisionInfo {
        public bool up, down, left, right;
        public bool OnGround { get { return down; } }
        public bool OnWall { get { return left || right; } }
    }
}
