using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] Animator anim;
    [SerializeField] AnimInfo Body;

    [Space]

    [SerializeField] float scale;
    int faceDir = 1;

    private void Update() {

        anim.SetBool("WallSliding", player.IsWallSliding);
        anim.SetBool("GrabbingWall", player.IsGrabbingWall);
        anim.SetBool("Idle", player.IsIdle);
        anim.SetBool("Walking", player.IsWalking);
        anim.SetBool("Jumping", player.IsJumping);
        anim.SetBool("Falling", player.IsFalling);

        Body.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Sin(Time.time * Body.RotationSpeed) * Body.RotationRange);

        if (player.velocity.x != 0) {
            faceDir = Math.Sign(player.velocity.x);
        }
        else if (player._velocity.x != 0) {
            faceDir = Math.Sign(player._velocity.x);
        }

        Vector2 scaleV = Vector2.Lerp(transform.localScale, new Vector2(scale * faceDir, scale), 0.2f);
        transform.localScale = scaleV;
    }

    IEnumerator LerpXScaleOverTime(float currentXScale, float endXScale, float duration) {
        float counter = 0;
        float startScale = currentXScale;
        while (counter < duration) {
            counter += Time.deltaTime;
            float newScale = Mathf.Lerp(startScale, endXScale, counter / duration);
            transform.localScale = new Vector2(faceDir * newScale, scale);
            yield return null;
        }
    }

    [Serializable]
    struct AnimInfo {
        public Transform transform;
        public float RotationSpeed, RotationRange;
    }
}
