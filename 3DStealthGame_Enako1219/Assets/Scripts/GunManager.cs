using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUtility;

public class GunManager : MonoBehaviour {

    public Transform muzzle;
    public Transform HandRig;

    public Projectile projectile;
    public float shootRate;
    public float reloadRate = 1;
    public float muzzleVelocity = 35;
    public float accuracy = 0f;

    public GameObject flashHolder;
    public Sprite[] flashSprites;
    public SpriteRenderer[] spriteRenderers;
    float flashTime = 0.1f;

    public AudioClip sfx_reload;
    public float sfxDelayTime_reload = 0f;

    public AudioClip sfx_shoot;
    public AudioClip sfx_shootSmall;
    public AudioClip sfx_OutOfAmmo;

    public GameObject suppressorObj;
    public bool isSuppress { get; private set; } = true;//消音オン
    public float suppressorPercent = 1f;
    public float suppressorSpeed = 15f;

    public int bulletCapa = 10;

    public int bulletAmmount { get; private set; } = 10;

    [Header("アニメーションクリップAim")]
    public AnimationClip clip_Aim000;
    public AnimationClip clip_Aim090;
    public AnimationClip clip_Aim180;
    [Header("アニメーションクリップShoot")]
    public AnimationClip clip_Shoot000;
    public AnimationClip clip_Shoot090;
    public AnimationClip clip_Shoot180;
    [Header("アニメーションクリップHand")]
    public AnimationClip clip_Hand;
    [Header("アニメーションクリップReload")]
    public AnimationClip clip_Reload;

    void Start() {
        flashHolder.SetActive(false);

        if (shootRate <= 0) {
            shootRate = 0.1f;
        }

        if (flashTime > shootRate) {
            flashTime = shootRate - 0.01f;
        }

        ActivateFlash();
    }


    public void Shoot(Transform targetPos) {

        bulletAmmount -= 1;

        float errorLength = Random.Range(0, accuracy);

        float angle = Mathf.Atan2(errorLength, 100) * Mathf.Rad2Deg;

        float randomRotate = Random.Range(0f, 360f);

        muzzle.transform.LookAt(targetPos);
        muzzle.transform.Rotate(new Vector3(0, 0, randomRotate), Space.Self);
        muzzle.transform.Rotate(new Vector3(0, angle, 0), Space.Self);
        Projectile newProjectile = Instantiate(projectile, muzzle.position, muzzle.rotation);
        newProjectile.SetSpeed(muzzleVelocity);
        ActivateFlash();

        if (isSuppress) {
            if (sfx_shootSmall) {
                AudioManager.instance.PlaySound(sfx_shootSmall, transform.position);
            }

            suppressorPercent -= (1 / suppressorSpeed);
            if(suppressorPercent <= 0f) {
                suppressorPercent = 0f;
                isSuppress = false;
                HideSuppressor();
            }
            
        }
        else {
            if (sfx_shoot) {
                AudioManager.instance.PlaySound(sfx_shoot, transform.position);
            }
            
        }
    }

    public Vector3 GetHitPos(Transform targetPos) {

        RaycastHit hit;
        Vector3 originPos = muzzle.transform.position;

        Vector3 velocity = (targetPos.transform.position - muzzle.transform.position);

        Ray ray = new Ray(originPos, velocity);

        if (Physics.Raycast(ray, out hit, velocity.magnitude, projectile.collisionMask, QueryTriggerInteraction.Collide)) {
            return hit.point;
        }
        else {
            return targetPos.position;
        }

    }

    void ActivateFlash() {
        flashHolder.SetActive(true);

        int flashSpriteIndex = Random.Range(0, flashSprites.Length);
        for (int i = 0; i < spriteRenderers.Length; i++) {
            spriteRenderers[i].sprite = flashSprites[flashSpriteIndex];
        }

        Invoke("DeactivateFlash", flashTime);
    }

    void DeactivateFlash() {
        flashHolder.SetActive(false);
    }

    public void Reload(int ammount) {
        bulletAmmount += ammount; 

        if(bulletAmmount > bulletCapa) {
            Debug.LogError("そんなにリロードできねえええぞ！！");
        }
    }

    public void ShowSuppressor() {
        if (suppressorObj != null) {
            suppressorObj.SetActive(true);
            isSuppress = true;
        }
    }

    public void HideSuppressor() {
        //Debug.Log("サプレッサー隠す");
        if (suppressorObj != null) {
            suppressorObj.SetActive(false);
            isSuppress = false;
        }
    }
}
