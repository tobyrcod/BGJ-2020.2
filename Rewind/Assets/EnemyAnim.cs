using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnim : MonoBehaviour
{
    private bool isFiring;
    [SerializeField] Animator anim;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform bulletSpawn;
    private GameManager gm;

    private void Start() {
        isFiring = false;
        anim.SetBool("isFiring", isFiring);
        Invoke("Fire", 0.2f);
        gm = FindObjectOfType<GameManager>();
    }

    private void Update() {
        RotateToFaceDir();
    }

    private void RotateToFaceDir() {
        int faceDir = (int)Mathf.Sign(transform.position.x - gm.GetActivePlayerPosition().x) * -1;
        int scale = 1;
        if (transform.localScale.x != scale * faceDir) {
            Vector2 newScale = Vector2.Lerp(transform.localScale, new Vector2(scale * faceDir, scale), 0.1f);
            transform.localScale = newScale;
        }
    }

    private void Fire() {
        isFiring = true;
        anim.SetBool("isFiring", isFiring);

        Bullet bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity, this.transform).GetComponent<Bullet>();
        bullet.SetTarget(0.0025f, gm.GetActivePlayerPosition());
        Invoke("ToggleFiring", 0.5f);
        Invoke("Destroy", 4f);
    }

    private void ToggleFiring() {
        isFiring = !isFiring;
    }

    private void Destroy() {
        Destroy(this.gameObject);
    }
}
