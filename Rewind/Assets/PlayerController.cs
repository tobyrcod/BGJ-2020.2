using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour {

    const float skinWidth = 0.015f;
    const float minRayLength = skinWidth * 2;
    const float maxDrag = 10;
    [SerializeField] float gravityScale = -9.8f;

    [Space]

    bool wallGrab = false;
    bool wallSliding = false;
    bool wallSlidingDown = true;
    bool canMove = true;
    bool hasFullControl = true;

    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float lowJumpMultiplier = 2f;

    [Space]

    [SerializeField] float speed = 10f;

    [Space]

    [SerializeField] float slideDownSpeedMax = 5f;
    [SerializeField] float slideUpSpeedMax = 2.5f;
    [SerializeField] float wallStickTime = 0.25f;
    float timeToWallUnstick;

    [Space]

    [SerializeField] JumpInfo standingJump;
    [SerializeField] JumpInfo wallJumpClimb;
    [SerializeField] JumpInfo wallJumpOff;
    [SerializeField] JumpInfo wallLeap;

    [Space]

    [SerializeField] float wallLerpSpeed = 10f;
    [SerializeField] float noInputLerpSpeed = 3f;
    [SerializeField] float frameDelay = 10f;
    [SerializeField] float velXThreshold = 0.05f;
    [SerializeField] [Range(0, 10)] float dragScale;
    [SerializeField] private Vector2 velocity;

    [Space]

    [SerializeField] float horizontalRayCount = 4f;
    [SerializeField] float verticalRayCount = 4f;
    float horizontalRaySpacing, verticalRaySpacing;

    BoxCollider2D hitbox2D;
    RaycastOrigins raycastOrigins;

    [Space] 

    [SerializeField] CollisionInfo collisions;
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
        int rawX = (int)Input.GetAxisRaw("Horizontal");
        int rawY = (int)Input.GetAxisRaw("Vertical");
        float wallDirX = collisions.right ? 1 : -1;

        Vector2 input = new Vector2(x, y);
        Walk(input, rawX);

        wallSliding = false;
        wallSlidingDown = false;
        wallGrab = false;

        if (collisions.OnGround) {
            velocity.y = 0;
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

            if (collisions.OnWall) {

                if (Input.GetKey(KeyCode.LeftShift)) {
                    WallGrab(input.y);
                }

                if (!wallGrab) {
                    //We are sliding on a wall;
                    WallSlide(input.x, wallDirX);
                }              
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (wallSlidingDown) {
                WallJump(rawX, wallDirX);
            }

            if (collisions.OnGround) {
                Jump(standingJump);
            }
        }

        Move();
    }

    private void WallGrab(float moveDir) {
        velocity.y = 0;
        wallGrab = true;
        velocity.y = moveDir * speed;
    }

    private void Move() {

        if (dragScale != 0) 
            velocity = ApplyDrag(velocity);
        
        float deltaX = MoveHorizontally(velocity.x * Time.fixedDeltaTime);
        float deltaY = MoveVertically(velocity.y * Time.fixedDeltaTime);

        transform.Translate(deltaX, deltaY, 0f);
    }

    private Vector2 ApplyDrag(Vector2 velocity) {
        return velocity *= 1 - dragScale / maxDrag;
    }

    private void WallSlide(float xInput, float wallDirX) {

        if (!canMove)
            return;

        wallSliding = true;

        if (velocity.y < 0) {
            wallSlidingDown = true;

            if (velocity.y < -slideDownSpeedMax) {
                Debug.Log("Slow Down Speed");
                velocity.y = -slideDownSpeedMax;
            }
        }
        else {

            if (velocity.y > slideUpSpeedMax) {
                Debug.Log("Slow Up Speed");
                velocity.y = slideUpSpeedMax;
            }
        }

        if (timeToWallUnstick > 0) {

            velocity.x = 0;

            if (xInput != 0 && xInput != wallDirX) {
                timeToWallUnstick -= Time.fixedDeltaTime;
            }
            else {
                timeToWallUnstick = wallStickTime;
            }
        }
        else {
            timeToWallUnstick = wallStickTime;
        }
    }

    private void Walk(Vector2 input, int rawX) {

        if (!canMove)
            return;

        if (wallGrab)
            return;

        if (hasFullControl) {
            if (rawX != 0) {
                velocity = new Vector2(input.x * speed, velocity.y);
            }
            else {         
                velocity = Vector2.Lerp(velocity, new Vector2(0, velocity.y),  (speed - velocity.x + 1) / 7 * Time.fixedDeltaTime);

                if (Mathf.Abs(velocity.x) < velXThreshold)
                    velocity.x = 0;
            }
        }
        else {

            velocity = Vector2.Lerp(velocity, new Vector2(input.x * speed, velocity.y), wallLerpSpeed * Time.fixedDeltaTime);

            if (Mathf.Abs(velocity.x) < velXThreshold)
                velocity.x = 0;
        }
    }

    private void Jump(JumpInfo jumpInfo) {
        velocity += jumpInfo.direction.normalized * jumpInfo.force;
    }

    private void WallJump(float xInput, float wallDirX) {

        JumpInfo jumpInfo;

        if (xInput == wallDirX) {
            jumpInfo.direction.x = -wallDirX * wallJumpClimb.direction.x;
            jumpInfo.direction.y = wallJumpClimb.direction.y;
            jumpInfo.force = wallJumpClimb.force;
        }
        else if (xInput == 0) {
            jumpInfo.direction.x = -wallDirX * wallJumpOff.direction.x;
            jumpInfo.direction.y = wallJumpOff.direction.y;
            jumpInfo.force = wallJumpOff.force;
        }
        else {
            jumpInfo.direction.x = -wallDirX * wallLeap.direction.x;
            jumpInfo.direction.y = wallLeap.direction.y;
            jumpInfo.force = wallLeap.force;
        }

        StopCoroutine("DisableFullControl");
        Jump(jumpInfo);
        StartCoroutine(DisableFullControl(Time.fixedDeltaTime * frameDelay));
    }

    IEnumerator DisableInput(float time) {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    IEnumerator DisableFullControl(float time) {
        hasFullControl = false;
        yield return new WaitForSeconds(time);
        hasFullControl = true;
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
            float directionX = Math.Sign(moveDistance);
            float rayLength = Mathf.Abs(moveDistance) + skinWidth;

            for (int i = 0; i < horizontalRayCount; i++) {
                Vector2 rayOrigin = (directionX == 1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
                rayOrigin += i * Vector2.up * horizontalRaySpacing;
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

    [Serializable]
    private struct JumpInfo {
        public Vector2 direction;
        public float force;
    }
}
