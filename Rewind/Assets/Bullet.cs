using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    Vector2 startingPos;
    float posNorm;
    private Vector2 movDir;
    Vector3 targetPosition;
    Transform targetTransform;
    float speed;
    const float skinWidth = 0.015f;
    RaycastOrigins raycastOrigins;
    [SerializeField] BoxCollider2D hitbox2D;
    [SerializeField] LayerMask collisionMask;

    public void SetTarget(float speed, Vector3 targetPosition, Transform targetTransform = null) {
        this.targetPosition = targetPosition;
        this.targetTransform = targetTransform;
        this.speed = speed;
        this.startingPos = transform.position;
        this.posNorm = 0;
        Vector3 movDir = (targetPosition - transform.position);
        this.targetPosition += movDir * 6f;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCollisionInfo();

        posNorm += speed;

        if (targetTransform != null) {
            transform.position = Vector2.Lerp(startingPos, targetTransform.position, posNorm);
        }
        else if (targetPosition != null) {
            transform.position = Vector2.Lerp(startingPos, targetPosition, posNorm);
        }
    }

    private void UpdateCollisionInfo() {
        UpdateRaycastOrigins();

        RaycastHit2D hitUp = Physics2D.Raycast(raycastOrigins.topLeft + new Vector2(0f, skinWidth * 2f), Vector2.right, (hitbox2D.bounds.size.x - skinWidth * 2f), collisionMask);
        RaycastHit2D hitDown = Physics2D.Raycast(raycastOrigins.bottomLeft - new Vector2(0f, skinWidth * 2f), Vector2.right, (hitbox2D.bounds.size.x - skinWidth * 2f), collisionMask);
        RaycastHit2D hitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft - new Vector2(skinWidth * 2, 0f), Vector2.up, (hitbox2D.bounds.size.y - skinWidth * 2f), collisionMask);
        RaycastHit2D hitRight = Physics2D.Raycast(raycastOrigins.bottomRight + new Vector2(skinWidth * 2f, 0f), Vector2.up, (hitbox2D.bounds.size.y - skinWidth * 2f), collisionMask);

        CheckForPlayer(hitUp, hitDown, hitLeft, hitRight);
    }

    private void CheckForPlayer(params RaycastHit2D[] hits) {
        System.Collections.Generic.List<Collectable> collectables = new System.Collections.Generic.List<Collectable>();
        bool hit = false;
        for (int i = 0; i < hits.Length; i++) {
            if (!hit) {
                if (hits[i]) {
                    hit = true;
                    PlayerController player = hits[i].transform.GetComponent<PlayerController>();
                    Debug.Log(player.IsAlive);
                    if (player.IsAlive) {
                        player.PlayerHit();
                    }
                }
            }
        }

        if (hit)
            Destroy(this.gameObject);
    }

    void UpdateRaycastOrigins() {
        Bounds bounds = hitbox2D.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

}
