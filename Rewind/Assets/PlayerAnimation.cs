using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] Animator anim;

    [Header("Body Parts")]
    [SerializeField] AnimInfo[] PlayerAliveAnimInfo;
    [SerializeField] AnimInfo[] PlayerDeadAnimInfo;

    [Space]

    [SerializeField] float scale;
    int faceDir = 1;
    bool _IsAlive = true;

    private void Update() {

        anim.SetBool("WallSliding", player.IsWallSliding);
        anim.SetBool("GrabbingWall", player.IsGrabbingWall);
        anim.SetBool("Idle", player.IsIdle);
        anim.SetBool("Walking", player.IsWalking);
        anim.SetBool("Jumping", player.IsJumping);
        anim.SetBool("Falling", player.IsFalling);
        anim.SetBool("Alive", player.IsAlive);

        if (player.IsAlive) {
            anim.speed = 1f;

            foreach (AnimInfo bodyPart in PlayerAliveAnimInfo) {
                if (bodyPart.transform != null) {
                    bodyPart.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Sin(Time.time * bodyPart.RotationSpeed) * bodyPart.RotationRange);
                }

                if (!_IsAlive) {
                    if (bodyPart.spriteRenderer != null) {
                        bodyPart.spriteRenderer.color = bodyPart.color;
                        if (bodyPart.sprite != null) {
                            bodyPart.spriteRenderer.sprite = bodyPart.sprite;
                        }
                    }
                }
            }

            if (player.IsWallSliding || player.IsGrabbingWall) {
                faceDir = -player.WallDirX;
            }
            else {
                if (player.velocity.x != 0) {
                    faceDir = Math.Sign(player.velocity.x);
                }
                else if (player._velocity.x != 0) {
                    faceDir = Math.Sign(player._velocity.x);
                }
            }

            if (transform.localScale.x != scale * faceDir) {
                Vector2 newScale = Vector2.Lerp(transform.localScale, new Vector2(scale * faceDir, scale), 0.2f);
                transform.localScale = newScale;
            }
        }
        else {
            anim.speed = 0f;

            foreach (AnimInfo bodyPart in PlayerDeadAnimInfo) {
                if (bodyPart.transform != null) {
                    bodyPart.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Sin(Time.time * bodyPart.RotationSpeed) * bodyPart.RotationRange);
                }

                if (_IsAlive) {
                    if (bodyPart.spriteRenderer != null) {
                        bodyPart.spriteRenderer.color = bodyPart.color;
                        if (bodyPart.sprite != null) {
                            bodyPart.spriteRenderer.sprite = bodyPart.sprite;
                        }
                    }
                }
            }
        }

        _IsAlive = player.IsAlive;
    }

    [Serializable]
    struct AnimInfo {
        public Transform transform;
        public float RotationSpeed, RotationRange;

        [Space]

        public SpriteRenderer spriteRenderer;
        public Color color;
        public Sprite sprite;
    }
}
