using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    #region variables
    const float skinWidth = 0.015f;
    const float minRayLength = skinWidth * 2;
    const float maxDrag = 10;
    [SerializeField] float gravityScale = -9.8f;

    [Space]

    bool wallSlidingDown = true;
    bool dashing = false;
    bool canDash = false;
    bool canMove = true;
    bool hasFullControl = true;

    [Space]

    [Header("Animation Variables")]
    [SerializeField] bool isWallSliding = false;
    [SerializeField] bool isGabbingWall = false;
    [SerializeField] bool isIdle = false;
    [SerializeField] bool isWalking = false;
    [SerializeField] bool isJumping = false;
    [SerializeField] bool isFalling = false;

    public bool IsWallSliding { get => isWallSliding; set => isWallSliding = value; }
    public bool IsGrabbingWall { get => isGabbingWall; set => isGabbingWall = value; }
    public bool IsIdle { get => isIdle; set => isIdle = value; }
    public bool IsWalking { get => isWalking; set => isWalking = value; }
    public bool IsJumping { get => isJumping; set => isJumping = value; }
    public bool IsFalling { get => isFalling; set => isFalling = value; }

    [Space]

    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float lowJumpMultiplier = 2f;

    [Space]

    [SerializeField] float speed = 10f;
    [SerializeField] float stopFrames = 3f;
    [SerializeField] float coyoteFrames = 6f;

    [Space]

    [SerializeField] float dashForce = 50f;
    [SerializeField] float dashTime = 0.3f;
    [SerializeField] float dashDrag = 1f;
    [SerializeField] float dashGravity = 0f;
    [SerializeField] float disableControlFrameDash = 12f;

    [Space]

    [SerializeField] float slideDownSpeedMax = 5f;
    [SerializeField] float slideUpSpeedMax = 2.5f;
    [SerializeField] float slideHoldSpeedMax = 2f;
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
    [SerializeField] float disableControlFrameWallJump = 12f;
    [SerializeField] float velXThreshold = 0.05f;
    [SerializeField] [Range(0, 10)] float dragScale;
    [SerializeField] public Vector2 velocity;
    public Vector2 _velocity { get; private set; }

    [Space]

    [SerializeField] float horizontalRayCount = 4f;
    [SerializeField] float verticalRayCount = 4f;
    float horizontalRaySpacing, verticalRaySpacing;

    BoxCollider2D hitbox2D;
    RaycastOrigins raycastOrigins;

    [Space] 

    [SerializeField] CollisionInfo collisions;
    [SerializeField] LayerMask collisionMask;

    #endregion

    private void OnValidate() {
        if (hitbox2D == null)
            hitbox2D = GetComponent<BoxCollider2D>();
    }

    private void Start() {
        UpdateRaycastOrigins();
        CalculateRaySpacing();

        collisions = new CollisionInfo(coyoteFrames);
    }

    private void FixedUpdate() {
        UpdateCollisionInfo();

        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        int rawX = (int)Input.GetAxisRaw("Horizontal");
        int rawY = (int)Input.GetAxisRaw("Vertical");
        float wallDirX = collisions.right ? 1 : -1;


        Vector2 input = new Vector2(x, y);
        Vector2Int rawInput = new Vector2Int(rawX, rawY);
        Walk(input, rawInput);

        wallSlidingDown = false;

        isWallSliding = false;
        isGabbingWall = false;
        isIdle = false;
        isWalking = false;
        isJumping = false;
        isFalling = false;

        if (collisions.OnGround) {
            if (Mathf.Abs(velocity.x) < velXThreshold && rawX == 0) {
                isIdle = true;
            }
            else {
                isWalking = true;
            }
        }
        else {
            if (!collisions.OnWall) {
                if (velocity.y > 0) {
                    isJumping = true;
                }
                else {
                    isFalling = true;
                }
            }
        }

        if (collisions.TouchingCeiling) {
            velocity.y = 0;
        }

        if (collisions.OnGround) {
            velocity.y = 0;
            canDash = true;
        }
        else {

            if (!dashing) {
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

            if (collisions.OnWall) {

                if (!IsGrabbingWall) {
                    //We are sliding on a wall;
                    WallSlide(input.x, wallDirX);
                }              
            }
        }

        if (collisions.OnWall) {

            if (Input.GetKey(KeyCode.LeftShift)) {
                WallGrab(input.y);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (wallSlidingDown) {
                WallJump(rawX, wallDirX);
            }
            else if (collisions.OnCoyoteGround()) {
                Jump(standingJump);
            }           
        }

        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            if (canDash) {
                if (!collisions.OnGround && !collisions.OnWall) {
                    if (rawX != 0 || rawY != 0) {
                        Dash(rawX, rawY);
                    }
                }
            }
        }

        _velocity = velocity;

        Move();
    }

    private void Dash(int rawX, int rawY) {
        Debug.Log("Dash");
        dashing = true;
        canDash = false;

        velocity = Vector2.zero;
        Vector2 dir = new Vector2(rawX, rawY);
        velocity += dir.normalized * dashForce;

        StopCoroutine("DisableFullControl");
        StartCoroutine(DisableFullControl(Time.fixedDeltaTime * disableControlFrameDash));

        StopCoroutine("DashWait");
        StartCoroutine(DashWait(dashTime));

        StopCoroutine("LerpDragOverTime");
        StartCoroutine(LerpDragOverTime(dragScale, dragScale, dashTime));
    }

    IEnumerator DashWait(float time) {
        float _gravity = gravityScale;
        gravityScale = dashGravity;
        dragScale = dashDrag;

        yield return new WaitForSeconds(time);

        gravityScale = _gravity;
        dashing = false;
        velocity = Vector2.zero;
    }

    IEnumerator LerpDragOverTime(float currentDrag, float dragToLose, float duration) {
        float counter = 0;
        float startDrag = currentDrag;
        float endDrag = currentDrag - dragToLose;
        while (counter < duration) {
            counter += Time.deltaTime;
            float newDrag = Mathf.Lerp(startDrag, endDrag, counter / duration);
            dragScale = newDrag;
            yield return null;
        }
    }

    IEnumerator LerpXVelOverTime(float currentSpeed, float speedToLose, float duration) {
        float counter = 0;
        float startSpeed = currentSpeed;
        float endSpeed = currentSpeed - speedToLose;
        while (counter < duration) {
            counter += Time.deltaTime;
            float newSpeed = Mathf.Lerp(startSpeed, endSpeed, counter / duration);
            velocity.x = newSpeed;
            yield return null;
        }
    }

    private void WallGrab(float moveDir) {
        velocity.y = 0;
        IsGrabbingWall = true;
        IsWallSliding = false;
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

        IsWallSliding = true;
        //jumping = false;
        //falling = false;

        if (velocity.y < 0) {
            wallSlidingDown = true;

            float maxSpeed = (xInput != 0 && xInput == wallDirX) ? slideHoldSpeedMax : slideDownSpeedMax;

            if (velocity.y < -maxSpeed) {
                velocity.y = -maxSpeed;
            }
        }
        else {

            if (velocity.y > slideUpSpeedMax) {
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

    private void Walk(Vector2 input, Vector2Int rawInput) {

        if (!canMove)
            return;

        if (IsGrabbingWall)
            return;

        if (hasFullControl) {
            if (rawInput.x != 0) {
                velocity = new Vector2(input.x * speed, velocity.y);
            }
            else {

                if (collisions.OnGround) {
                    StopCoroutine("LerpXVelOverTime");
                    StartCoroutine(LerpXVelOverTime(velocity.x, velocity.x, stopFrames * Time.fixedDeltaTime));
                }
                else {

                    velocity = Vector2.Lerp(velocity, new Vector2(0, velocity.y), (speed - velocity.x + 1) / noInputLerpSpeed * Time.fixedDeltaTime);

                    if (Mathf.Abs(velocity.x) < velXThreshold)
                        velocity.x = 0;
                }
            }

            if (rawInput.y == -1) {
                StopCoroutine("LerpXVelOverTime");
                StartCoroutine(LerpXVelOverTime(velocity.x, velocity.x, stopFrames * Time.fixedDeltaTime));
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
        StartCoroutine(DisableFullControl(Time.fixedDeltaTime * disableControlFrameWallJump));
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

        collisions.Update(hitUp, hitDown, hitLeft, hitRight);
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
    private class CollisionInfo {
        public bool up { get; private set; }
        public bool down { get; private set; }
        public bool left { get; private set; }
        public bool right { get; private set; }
        public bool OnGround { get { return down; } }
        private float coyoteFrames;
        private float coyoteCutoff;
        private bool canCoyoteJump;

        public CollisionInfo(float coyoteFrames) {
            this.coyoteFrames = coyoteFrames;
        }

        public bool TouchingCeiling { get { return up; } }
        public bool OnWall { get { return left || right; } }

        public void Update(bool up, bool down, bool left, bool right) {
            this.up = up;
            this.left = left;
            this.right = right;

            if (down && !this.down)
                canCoyoteJump = true;

            if (this.down && !down)
                coyoteCutoff = Time.time + Time.fixedDeltaTime * coyoteFrames;

            this.down = down;
        }

        public bool OnCoyoteGround() {
            if (canCoyoteJump && (OnGround || Time.time < coyoteCutoff)) {
                canCoyoteJump = false;
                return true;
            }
            return false;
        }
    }

    [Serializable]
    private struct JumpInfo {
        public Vector2 direction;
        public float force;
    }
}
