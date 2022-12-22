using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using MyUtility;

public class PlayerAnimation : MonoBehaviour {


    Animator anim;
    protected AnimatorOverrideController overrideCon;

    [Header("メインウェポン")]
    public GunManager mainGun;

    [Header("サブウェポン")]
    public GunManager subGun;

    [Header("リグ/ホルスター")]
    public  MultiParentConstraint mainGunHolder;
    public MultiParentConstraint subGunHolder;

    [Header("リグ/左手")]
    public MultiParentConstraint L_HandRig;

    [Header("ジャンケン")]
    public AnimationClip guu;
    public AnimationClip choki;
    public AnimationClip paa;

    [Header("アンプルブッサしなどの特殊アクション")]
    public AnimationClip Anim_ExcuteBud;

    public float gunOnOffRate { get; private set; } = 0.75f;


    bool changed = false;


    PlayerController playerController;

    public enum Layer {
        Gun_Main = 0,
        Gun_Sub = 1,
        Base = 2,
        Base_Gun = 3,//銃を取り扱うレイヤー
        Janken = 4,
    }

    public enum State_Base {
        Normal = 0,
        Aim=1,
        CQC = 2,
        Jump = 3,
        Fall = 4,
        Cover_Left = 5,
        Cover_Right = 6,
        Crawl = 7,
        Dead = 8,
        Janken = 9,
    }

    public enum State_GunBase {
        Free = 0,

        PickUp_Main = 1,
        Aim_Main = 2,
        Aim_Main_Crawl = 3,
        Reload_Main = 4,
        TakeOff_Main = 5,

        PickUp_Sub = 6,
        Aim_Sub = 7,
        Aim_Sub_Crawl = 8,
        Reload_Sub = 9,
        TakeOff_Sub = 10,
    }

    public enum State_Gun {
        Unarmd = 0,
        Hold = 1,
        Shoot = 2,
        Reload = 3,
    }

    public enum State_Janken {
        Guu = 0,
        Choki = 1,
        Paa = 2,
    }



    List<LayerSmoothDamp> layerSmoothDamps = new List<LayerSmoothDamp>();

    bool isLeapingOffset = false;
    float currentLerpTime;
    float lerpTime;
    Vector3 offsetFrom;
    Vector3 offsetTo;

    public bool endAnimation { get; private set; } = false;


    void Start() {
        anim = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        overrideCon = new AnimatorOverrideController(anim.runtimeAnimatorController);
        anim.runtimeAnimatorController = overrideCon;


        //銃関連アニメーションの初期化
        anim.SetFloat("gunOnOffRate", 1 / gunOnOffRate);

        for (int i = 0; i < anim.layerCount; i++) {
            layerSmoothDamps.Add(new LayerSmoothDamp(i));
        }

        ChangeMainWeapon(true);
    }

    private void Update() {
        foreach (LayerSmoothDamp lsd in layerSmoothDamps) {
            lsd.Update();
            anim.SetLayerWeight(lsd.layerIndex, lsd.currentWeight);
        }

        if (isLeapingOffset) {
            currentLerpTime += Time.deltaTime;
            playerController.transform.position = Vector3.Lerp(offsetFrom, offsetTo, currentLerpTime / lerpTime);
            if(currentLerpTime >= lerpTime) {
                playerController.transform.position = offsetTo;
                isLeapingOffset = false;
            }
        }


    }

    void ChangeClip(AnimationClip clip,string overrideClipName) {
        //Debug.Log("Player_ChangeClip" + clip.name);
        overrideCon[overrideClipName] = clip;
    }

    public bool IsTransition() {
        return anim.IsInTransition(0);
    }

    //近接戦闘アクション
    public void SetCQCAnimation(CQCData data) {
        ChangeClip(data.clip, "AnimCQC回し蹴り");
    }

    //薬剤投与アクション
    public void SetAnimation_ExcuteBud() {
        ChangeClip(Anim_ExcuteBud, "AnimCQC回し蹴り");
    }


    public void ChangeMainWeapon(bool main) {

        //if (main) {
        //    anim.SetFloat("shootRate", 1 / mainGun.shootRate);
        //    anim.SetFloat("reloadRate", 1 / mainGun.reloadRate);
        //    var source = L_HandRig.data.sourceObjects;
        //    source.SetWeight(0, 1);
        //    source.SetWeight(1, 0);
        //    L_HandRig.data.sourceObjects = source;

        //    ChangeClip(Anim_Gun0_Stop, "Anim_Gun0_Stop");
        //    ChangeClip(Anim_Gun0_Stop_Squat, "Anim_Gun0_Stop_Squat");
        //    ChangeClip(Anim_Gun1_Walk, "Anim_Gun1_Walk");
        //    ChangeClip(Anim_Gun1_Walk_Squat, "Anim_Gun1_Walk_Squat");
        //    ChangeClip(Anim_Gun2_Run, "Anim_Gun2_Run");
        //    ChangeClip(Anim_Gun2_Run_Squat, "Anim_Gun2_Run_Squat");

        //    ChangeClip(Anim_Gun3_Cover_Stop_Left,　      "Anim_Gun3_Cover_Stop_Left");
        //    ChangeClip(Anim_Gun3_Cover_Stop_Right,       "Anim_Gun3_Cover_Stop_Right");
        //    ChangeClip(Anim_Gun3_Cover_Walk_Left, "Anim_Gun3_Cover_Walk_Left");
        //    ChangeClip(Anim_Gun3_Cover_Walk_Right, "Anim_Gun3_Cover_Walk_Right");
        //    ChangeClip(Anim_Gun3_Cover_Stop_Left_Squat, "Anim_Gun3_Cover_Stop_Left_Squat");
        //    ChangeClip(Anim_Gun3_Cover_Stop_Right_Squat, "Anim_Gun3_Cover_Stop_Right_Squat");
        //    ChangeClip(Anim_Gun3_Cover_Walk_Left_Squat, "Anim_Gun3_Cover_Walk_Left_Squat");
        //    ChangeClip(Anim_Gun3_Cover_Walk_Right_Squat, "Anim_Gun3_Cover_Walk_Right_Squat");

        //    //銃のアニメーション切替
        //    //ハンド
        //    ChangeClip(mainGun.clip_Hand, "Anim_Gun_Aim_Stand_HandGrip");
        //    //リロード
        //    ChangeClip(mainGun.clip_Reload, "Anim_Gun_Reload");
        //    //エイム
        //    ChangeClip(mainGun.clip_Aim000, "Anim_Gun_Aim_Stand_000");
        //    ChangeClip(mainGun.clip_Aim090, "Anim_Gun_Aim_Stand_090");
        //    ChangeClip(mainGun.clip_Aim180, "Anim_Gun_Aim_Stand_180");
        //    //シュート
        //    ChangeClip(mainGun.clip_Shoot000, "Anim_Gun_Shoot_000");
        //    ChangeClip(mainGun.clip_Shoot090, "Anim_Gun_Shoot_090");
        //    ChangeClip(mainGun.clip_Shoot180, "Anim_Gun_Shoot_180");

        //}
        //else {
        //    anim.SetFloat("shootRate", 1 / subGun.shootRate);
        //    anim.SetFloat("reloadRate", 1 / subGun.reloadRate);
        //    var source = L_HandRig.data.sourceObjects;
        //    source.SetWeight(0, 0);
        //    source.SetWeight(1, 1);
        //    L_HandRig.data.sourceObjects = source;

        //    ChangeClip(Anim_HandGun0_Stop, "Anim_Gun0_Stop");
        //    ChangeClip(Anim_HandGun0_Stop_Squat, "Anim_Gun0_Stop_Squat");
        //    ChangeClip(Anim_HandGun1_Walk, "Anim_Gun1_Walk");
        //    ChangeClip(Anim_HandGun1_Walk_Squat, "Anim_Gun1_Walk_Squat");
        //    ChangeClip(Anim_HandGun2_Run, "Anim_Gun2_Run");
        //    ChangeClip(Anim_HandGun2_Run_Squat, "Anim_Gun2_Run_Squat");

        //    ChangeClip(Anim_HandGun3_Cover_Stop_Left,        "Anim_Gun3_Cover_Stop_Left");
        //    ChangeClip(Anim_HandGun3_Cover_Stop_Right,       "Anim_Gun3_Cover_Stop_Right");
        //    ChangeClip(Anim_HandGun3_Cover_Walk_Left,        "Anim_Gun3_Cover_Walk_Left");
        //    ChangeClip(Anim_HandGun3_Cover_Walk_Right,       "Anim_Gun3_Cover_Walk_Right");
        //    ChangeClip(Anim_HandGun3_Cover_Stop_Left_Squat,  "Anim_Gun3_Cover_Stop_Left_Squat");
        //    ChangeClip(Anim_HandGun3_Cover_Stop_Right_Squat, "Anim_Gun3_Cover_Stop_Right_Squat");
        //    ChangeClip(Anim_HandGun3_Cover_Walk_Left_Squat,  "Anim_Gun3_Cover_Walk_Left_Squat");
        //    ChangeClip(Anim_HandGun3_Cover_Walk_Right_Squat, "Anim_Gun3_Cover_Walk_Right_Squat");


        //    //銃のアニメーション切替
        //    //ハンド
        //    ChangeClip(subGun.clip_Hand, "Anim_Gun_Aim_Stand_HandGrip");
        //    //リロード
        //    ChangeClip(subGun.clip_Reload, "Anim_Gun_Reload");
        //    //エイム
        //    ChangeClip(subGun.clip_Aim000, "Anim_Gun_Aim_Stand_000");
        //    ChangeClip(subGun.clip_Aim090, "Anim_Gun_Aim_Stand_090");
        //    ChangeClip(subGun.clip_Aim180, "Anim_Gun_Aim_Stand_180");
        //    //シュート
        //    ChangeClip(subGun.clip_Shoot000, "Anim_Gun_Shoot_000");
        //    ChangeClip(subGun.clip_Shoot090, "Anim_Gun_Shoot_090");
        //    ChangeClip(subGun.clip_Shoot180, "Anim_Gun_Shoot_180");
        //}
    }



    void LateUpdate() {
        if (changed) {
            changed = false;
        }
    }

    public void SetMoveSpeed(float percent) {

        anim.SetFloat("speedPercent", percent);
    }

    public float GetMoveSpeed() {
        return anim.GetFloat("speedPercent");
    }

    public void SetAimMoveSpeed(float z, float x) {
        anim.SetFloat("moveZ", z);
        anim.SetFloat("moveX", x);
    }

    public void SetCrawlAim_PoseDir(float z,float x) {
        anim.SetFloat("forwardZ", z);
        anim.SetFloat("forwardX", x);
    }

    public void SetSquatPercent(float percent) {

        anim.SetFloat("Squat", percent);
    }



    public void SetState_Base(State_Base state) {
        anim.SetInteger("ID_BASE", (int)state);
        endAnimation = false;
    }

    public void SetState_GunBase(State_GunBase state) {
        anim.SetInteger("ID_GUNBASE", (int)state);
    }

    public void SetState_Gun_Main(State_Gun state) {
        anim.SetInteger("ID_MAINGUN", (int)state);
    }

    public void SetState_Gun_Sub(State_Gun state) {
        anim.SetInteger("ID_SUBGUN", (int)state);
    }

    public void SetState_Janken(State_Janken state) {
        anim.SetInteger("ID_JANKEN", (int)state);
    }

    public void SetAimAngle(float cameraAngle) {
        float currentAngle = GetAimAngle();
        float targetAngle = cameraAngle;
        if (Mathf.Abs(targetAngle - currentAngle) > 10f) {
            targetAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime / 0.1f);
        }

        anim.SetFloat("angle", (targetAngle + 90) % 360f);
    }

    public float GetAimAngle() {
        return (anim.GetFloat("angle") - 90) % 360f;
    }

    public void SetLayerWaight(Layer layer, float weight, float smoothTime) {
        layerSmoothDamps[(int)layer].ChangeWeight(weight, smoothTime);
    }

    public void SetLayerWaightOn_GunBase(float smoothTime) {
        layerSmoothDamps[(int)Layer.Base_Gun].ChangeWeight(1, smoothTime);
    }

    public void SetLayerWaightOff_GunBase(float smoothTime) {
        layerSmoothDamps[(int)Layer.Base_Gun].ChangeWeight(0, smoothTime);
    }



    public void ExcuteFootStep(AnimationEvent avt) {
        if(avt.animatorClipInfo.weight > 0.8f) {
            playerController.FootStepUpdate();
        }
    }

    public void LarpPositionStart(AnimationEvent amv) {
        isLeapingOffset = true;
        currentLerpTime = 0f;
        lerpTime = amv.floatParameter;
        offsetFrom = playerController.transform.position;
        offsetTo = playerController.currentHangEdge.pos_Body;
    }

    public void LarpPositonFall(AnimationEvent amv) {
        isLeapingOffset = true;
        currentLerpTime = 0f;
        lerpTime = amv.floatParameter;
        offsetFrom = transform.position;
        offsetTo = playerController.currentHangEdge.pos_Fall;
    }

    public void OnController() {
        playerController.controller.enabled = true;
    }

    public void OffController() {
        playerController.controller.enabled = false;
    }

    public void EndAnimation() {
        endAnimation = true;
    }

    public void CameraReset() {
        playerController.cameraCon.bodyCam.Priority = 5;
    }

}
