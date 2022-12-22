using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using MyGizmoTool;
using UnityEngine.UI;
using MyUtility;
using UnityEngine.Playables;

public class PlayerController : MonoBehaviour,ITakeDamage {

    GizmoDrawer gizmoDrawer = new GizmoDrawer();

    //移動パラメーター
    const float walkSpeed = 2;
    const float runSpeed = 6;
    const float gravity = -12;

    const float turnSmoothTime = 0.2f;
    float turnSmoothVelocity;
    Vector3 vec_turnSmoothVelocity;

    const float speedSmoothTime = 0.1f;
    float speedSmoothVelocity;
    Vector3 currentVelocity = new Vector3();
    Vector3 currentSmoothVelocity = new Vector3();
    float velocityY;

    public float squatPercent { get; private set; } = 0f;
    float squatDebuff = 0.1f;

    //音パラメーター
    public float noise_Foot { get; private set; } = 0f; //1メートル先からでも聞こえる音を１とする。
    public float noise_Gun { get; private set; } = 0f;//1メートル先からでも聞こえる音を１とする。
    public LayerMask groundLayer;


    //HP
    float HP = 1f;

    [Header("参照")]
    Transform cameraT;
    public Transform playerEnemyTargetT;
    public ThirdPersonCamera cameraCon { get; private set; }
    public CharacterController controller { get; private set; }
    public HitBoxController hitBoxController;
    public PlayerAnimation playerAnim;
    bool isHold = false;

    CopyPose copyPose = new CopyPose();

    [Header("UI関連")]
    public Image clossHair;
    public Image clossHair_Brock;
    public Text text_BulletAmount;


    public float healthPoint { get; private set; } = 1f;
    int handgunBulletAmount;
    int rifleBulletAmount;


    [Header("サウンド関連")]
    public AudioClip damageSound;
    public AudioClip gunManageSound;
    public AudioClip sound_MainGunPickUp;
    public AudioClip sound_MainGunTakeOff;
    public AudioClip sound_SubGunPickUp;
    public AudioClip sound_SubGunTakeOff;
    public List<AudioClip> footStepSoundList = new List<AudioClip>();
    public void FootStepUpdate() {
        if (footStepSoundList.Count != 0)
        AudioManager.instance.PlaySound(footStepSoundList[Random.Range(0, footStepSoundList.Count)], transform.position, 2 * (currentVelocity.magnitude / runSpeed) *Random.Range(0.8f,1.2f));
    }

    [Header("格闘アニメーション")]
    public List<CQCData> cqcDataList = new List<CQCData>();
    CQCData currentCQC = null;
    List<CQCData> cqcDataList_Temp = new List<CQCData>();
    EnemyController cqcTargetEneCon;
    float cqcAnimationStartTime = 0f;


    [HideInInspector]
    public GunManager currentGun;
    int index_currenGun = 0;

    #region State関連



    public State priviousState{ get; private set; } = State.Action_Event;
    public State currentState { get; private set; } = State.Action_Event;
    bool stateEnter = true;
    float stateEnterTime = 0f;
    float currentStateTime = 0f;
    float statePercent = 0f;
    bool stateFlag1 = false;
    bool stateFlag2 = false;
    Vector3 stateEnterPosition = Vector3.zero;
    Vector3 stateEnterForward = Vector3.zero;

    public void ChangeState(State newState) {
        priviousState = currentState;
        currentState = newState;
        stateEnterTime = Time.time;
        currentStateTime = 0f;
        stateEnter = true;
        statePercent = 0f;
        stateFlag1 = false;
        stateFlag2 = false;
        stateEnterPosition = transform.position;
        stateEnterForward = transform.forward;
        //Debug.Log($"{priviousState.ToString()}　⇒　{currentState.ToString()}");
    }

    #endregion

    [Header("特殊アクション用")]
    public List<HangEdge> hangEdgeList = new List<HangEdge>();
    public List<CoverEdge> coverEdgeList = new List<CoverEdge>();
    public HangEdge currentHangEdge { get; private set; }
    CoverEdge current_CoverEdge;
    Vector3 pos_cover_BackFrom_Aim;
    Vector3 pos_cover_To_Aim;
    bool isLeftCover;


    GameManager gm;

    // Start is called before the first frame update
    void Start()
    {
        gm = GameManager.instance;
        cameraT = Camera.main.transform;
        cameraCon = Camera.main.GetComponent<ThirdPersonCamera>();
        controller = GetComponent<CharacterController>();
        hitBoxController = GetComponent<HitBoxController>();
        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Janken, 0f, 0f);
        healthPoint = 1f;
        handgunBulletAmount = 1800;
        rifleBulletAmount = 3000;


        //playerAnim.SetState(PlayerAnimation.State.Normal);
        //playerAnim.SetState2(PlayerAnimation.State2.TakeOff);
        //playerAnim.SetLayerWaight(PlayerAnimation.Layer.Gun, 0, 0f);
        //playerAnim.SetLayerWaight(PlayerAnimation.Layer.GunHold, 0, 0f);
        //playerAnim.SetLayerWaight(PlayerAnimation.Layer.Hand, 0, 0f);
    }

    public enum State {
        Action_Janken,
        Action_Attack,
        Action_Event,
        Action_EventEnd,
        Action_Fall,
        Action_ExctuteBud,
        Normal_Stand,
        Normal_SitDown,
        Normal_StandUp,
        Normal_PickUp_Main,
        Normal_TakeOff_Main,
        Normal_PickUp_Sub,
        Normal_TakeOff_Sub,
        Normal_Jump,
        Normal_Cover_Left,
        Normal_Cover_Right,
        Crawling_Normal,
        Crawling_Aim,
        Crawling_Aim_Shoot,
        Crawling_Aim_Shoot_OutOfAmmo,
        Crawling_Aim_Reload,
        Aim_SetUp,
        Aim_SetUp_LowCover,
        Aim_Stand,
        Aim_SitDown,
        Aim_StandUp,
        Aim_Shoot,
        Aim_Shoot_OutOfAmmo,
        Aim_Reload,
        Aim_BackToCover,
        Dead,
    }

    void Update()
    {
        currentStateTime += Time.deltaTime;

        switch (currentState) {

            #region Action
            //ジャンケン
            case State.Action_Janken: {

                    if (stateEnter) {
                        
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Janken);
                        //playerAnim.SetState_Hand(PlayerAnimation.State_Hand.Janken);
                        //playerAnim.SetLayerWaight(PlayerAnimation.Layer.Gun_Sub, 1f, 0.1f);//手レイヤーを有効に

                        //音関連の処理
                        noise_Foot = 0f;
                        noise_Gun = 0f;

                        //カメラ処理
                        cameraCon.jankenCam.Priority = 20;

                        int randomValue = Random.Range(0, 3);

                        switch (randomValue) {

                            case 0:
                                playerAnim.SetState_Janken(PlayerAnimation.State_Janken.Guu);
                                break;

                            case 1:
                                playerAnim.SetState_Janken(PlayerAnimation.State_Janken.Choki);
                                break;

                            case 2:
                                playerAnim.SetState_Janken(PlayerAnimation.State_Janken.Paa);
                                break;
                        }
                    }

                    //0.7秒経過
                    if(currentStateTime >= 0.7f && stateFlag1 == false) {
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Janken, 1f, 0f);
                        stateFlag1 = true;
                    }

                    //5秒経過
                    if (currentStateTime >= 5f) {
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Janken, 0f, 0f);
                        ChangeState(State.Normal_Stand);
                        //カメラ処理
                        cameraCon.jankenCam.Priority = 2;
                        return;
                    }


                    return;
                }

            //近接攻撃
            case State.Action_Attack: {

                    if (stateEnter) {
                        playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Unarmd);
                        playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Unarmd);
                        playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Free);

                        //音関連の処理
                        noise_Foot = 0f;
                        noise_Gun = 0f;
                        //武器残弾表示
                        UpdateBulletAmountText();
                    }

                    //敵の方向を向く。
                    float moveTime = 0.3f;
                    if (currentStateTime <= moveTime) {
                        Vector3 targetDir = cqcTargetEneCon.transform.position - transform.position;
                        targetDir.y = 0f;
                        transform.forward = Vector3.Lerp(stateEnterForward, targetDir, currentStateTime / moveTime);
                    }


                    //アニメーションを実行待機
                    if (!stateFlag1) {
                        if (!playerAnim.IsTransition() && !cqcTargetEneCon.IsInTransition()) {
                            stateFlag1 = true;
                            playerAnim.SetState_Base(PlayerAnimation.State_Base.CQC);
                            cqcTargetEneCon.SetCQCAnimState();
                            cqcAnimationStartTime = currentStateTime;
                        }
                    }

                    //音声待機
                    if (!stateFlag2) {
                        if (currentStateTime > currentCQC.soundDelay + cqcAnimationStartTime) {
                            //サウンドディレイを経過
                            stateFlag2 = true;
                            AudioManager.instance.PlaySound(currentCQC.sound, transform.position, 4f);
                        }
                    }


                    if (currentStateTime >= currentCQC.playerStateTime) {
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    return;
                }

            
            //イベントに入る
            case State.Action_Event: {
                    if (stateEnter) {
                        playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Unarmd);
                        playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Unarmd);
                        playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Free);
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Normal);

                        playerAnim.SetMoveSpeed(0);
                        currentVelocity = Vector3.zero;
                        speedSmoothVelocity = 0f;
                        //武器残弾表示
                        text_BulletAmount.gameObject.SetActive(false);

                        //拳銃のサプレッサーを非表示にする
                        playerAnim.subGun.HideSuppressor();
                    }

                    return;
                }

            //イベント終了
            case State.Action_EventEnd: {
                    if (stateEnter) {
                        if (isHold) {
                            if(index_currenGun == 2 && playerAnim.subGun.suppressorPercent > 0) {
                                playerAnim.subGun.ShowSuppressor();
                            }
                        }

                        controller.enabled = true;
                    }

                    ChangeState(State.Normal_Stand);


                    return;
                }

            //落下
            case State.Action_Fall: {
                    if (stateEnter) {
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Fall);
                    }

                    NormalMove();

                    if (IsGrounded()) {
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    return;
                }
            #endregion

            #region Normal

            //通常状態
            case State.Normal_Stand: {

                    if (stateEnter) {

                        //クロスヘアを消す
                        clossHair.gameObject.SetActive(false);
                        clossHair_Brock.gameObject.SetActive(false);

                        //アニメーション設定
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Normal);
                        if(index_currenGun == 1) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Main);
                            playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Hold);
                        }
                        if(index_currenGun == 2) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Sub);
                            playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Hold);
                        }

                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 0f, 0.25f);


                        //武器残弾表示
                        UpdateBulletAmountText();

                        //カバー解除
                        current_CoverEdge = null;
                        currentHangEdge = null;

                        //通常カメラに戻す
                        cameraCon.aimCam.Priority = 5;
                        //カメラ高さ調整
                        if (squatPercent >= 0.5f) {
                            cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_squat_Nor);
                        }
                        else {
                            cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_stand_Nor);
                        }

                        //コントローラー形状制御
                        if(squatPercent >= 0.5f) {
                            controller.center = new Vector3(0, 0.3f, 0);
                            controller.radius = 0.15f;
                            controller.height = 0.5f;
                        }
                        else {
                            controller.center = new Vector3(0, 0.7f, 0);
                            controller.radius = 0.15f;
                            controller.height = 1.3f;
                        }

                    }

                    Vector3 targetDir;

                    //移動処理
                    targetDir = NormalMove();

                    if (squatPercent != 1f && squatPercent >= 0.5f) {
                        squatPercent = Mathf.Clamp01(squatPercent + Time.deltaTime / (turnSmoothTime / 2));
                        playerAnim.SetSquatPercent(squatPercent);

                    }

                    if(squatPercent != 0f && squatPercent < 0.5f) {
                        squatPercent = Mathf.Clamp01(squatPercent - Time.deltaTime / (turnSmoothTime / 2));
                        playerAnim.SetSquatPercent(squatPercent);
                    }


                    //近接戦闘ボタンが押された
                    if (Input.GetKeyDown(KeyCode.F)) {
                        TryAction_Attack();
                    }


                    //ジャンプボタンが押された
                    if (Input.GetKeyDown(KeyCode.Space)) {
                        currentHangEdge = null;
                        foreach (HangEdge hangEdge in hangEdgeList) {
                            if (hangEdge.canGrab) {
                                if(currentHangEdge != null) {
                                    if(currentHangEdge.distanceFromPlayer > hangEdge.distanceFromPlayer) {
                                        currentHangEdge = hangEdge;
                                    }
                                }
                                else {
                                    currentHangEdge = hangEdge;
                                }
                            }
                        }

                        if(currentHangEdge != null) {
                            ChangeState(State.Normal_Jump);
                            return;
                        }
                    }

                    //カバーアクション判定
                    if (!Input.GetKey(KeyCode.LeftShift)) {
                        //走っていない
                        foreach (CoverEdge coverEdge in coverEdgeList) {
                            if (coverEdge.isCoverring && Vector3.Angle(coverEdge.dir_Body, -targetDir) <= 90f) {
                                current_CoverEdge = coverEdge;

                                if (targetDir.magnitude != 0f && Vector3.Angle(targetDir, current_CoverEdge.dir_AB) < 90f) {
                                    //右方向に移動したがっている
                                    ChangeState(State.Normal_Cover_Right);
                                    return;
                                }
                                else {
                                    //左方向に移動したがっている
                                    ChangeState(State.Normal_Cover_Left);
                                    return;
                                }
                            }
                        }
                    }



                    //落下している
                    if (IsFalling()) {
                        ChangeState(State.Action_Fall);
                        return;
                    }

                    //しゃがみボタンが押された
                    if (Input.GetKeyDown(KeyCode.C)) {
                        //今立っている
                        if (squatPercent != 1) {
                            ChangeState(State.Normal_SitDown);
                            return;
                        }
                        //今しゃがんでいる
                        else {
                            ChangeState(State.Crawling_Normal);//匍匐に移動
                            return;
                        }
                    }

                    //立ち上がりボタンが押された
                    if (Input.GetKeyDown(KeyCode.X) && squatPercent != 0) {
                        ChangeState(State.Normal_StandUp);
                        return;
                    }

                    //メインウェポンに切り替え
                    if (Input.GetKeyDown(KeyCode.Alpha1) && index_currenGun != 1) {
                        index_currenGun = 1;
                        if (isHold) {
                            if (currentGun != playerAnim.mainGun) {
                                ChangeState(State.Normal_TakeOff_Sub);
                                return;
                            }

                        }
                        else {
                            ChangeState(State.Normal_PickUp_Main);
                            return;
                        }
                    }


                    //サブウェポンに切り替え
                    if (Input.GetKeyDown(KeyCode.Alpha2) && index_currenGun != 2) {
                        index_currenGun = 2;

                        if (isHold) {
                            if (currentGun != playerAnim.subGun) {
                                ChangeState(State.Normal_TakeOff_Main);
                                return;
                            }
                        }
                        else {
                            ChangeState(State.Normal_PickUp_Sub);
                            return;
                        }
                    }

                    //武器を構えようとしてる
                    if (Input.GetMouseButton(1) && currentStateTime >= 0.3f) {
                        if (isHold) {
                            ChangeState(State.Aim_SetUp);
                            return;
                        }
                    }

                    //手ぶらになろうとしている
                    if (Input.GetKeyDown(KeyCode.H) && isHold) {

                        if (index_currenGun == 1) {
                            index_currenGun = 0;
                            ChangeState(State.Normal_TakeOff_Main);
                            return;
                        }

                        if (index_currenGun == 2) {
                            index_currenGun = 0;
                            ChangeState(State.Normal_TakeOff_Sub);
                            return;
                        }
                    }

                    //リロードしようとしてる
                    if (Input.GetKeyDown(KeyCode.R) && currentStateTime >= 0.3f && CanReload()) {

                        ChangeState(State.Aim_Reload);
                        return;
                    }

                    //ジャンケンしようとしてる
                    if (Input.GetKeyDown(KeyCode.Alpha0) && !isHold) {
                        ChangeState(State.Action_Janken);
                        return;
                    }


                    return;
                }
            case State.Normal_PickUp_Main: {

                    if (stateEnter) {
                        isHold = true;
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Normal);
                        playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.PickUp_Main);
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 1, 0.1f);
                        playerAnim.ChangeMainWeapon(true);
                        currentGun = playerAnim.mainGun;
                        playerAnim.SetAimAngle(180f);

                    }

                    NormalMove();
                    statePercent += Time.deltaTime / playerAnim.gunOnOffRate;




                    //音声処理
                    if(currentStateTime > 0.1f && !stateFlag2) {
                        if(sound_MainGunPickUp != null)
                        AudioManager.instance.PlaySound(sound_MainGunPickUp, transform.position);
                        stateFlag2 = true;
                    }



                    if (statePercent >= 1) {
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 0, 0.1f);
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    return;

                }
            case State.Normal_TakeOff_Main: {

                    if (stateEnter) {
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Normal);
                        playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.TakeOff_Main);
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 1, 0.1f);

                    }

                    //cameraCon.CameraAngleUpdate();
                    NormalMove();
                    statePercent += Time.deltaTime / playerAnim.gunOnOffRate;


                    //音声処理
                    if (currentStateTime > 0.1f && !stateFlag2) {
                        if (sound_MainGunTakeOff != null)
                        AudioManager.instance.PlaySound(sound_MainGunTakeOff, transform.position);
                        stateFlag2 = true;
                    }

                    if (statePercent >= 1) {
                        currentGun = null;
                        if (index_currenGun == 2) {
                            ChangeState(State.Normal_PickUp_Sub);
                            return;
                        }

                        isHold = false;
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 0, 0.1f);
                        playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Free);
                        ChangeState(State.Normal_Stand);
                        return;
                    }
                    return;

                }
            case State.Normal_PickUp_Sub: {

                    if (stateEnter) {
                        isHold = true;
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Normal);
                        playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.PickUp_Sub);
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 1, 0.1f);
                        playerAnim.ChangeMainWeapon(false);
                        currentGun = playerAnim.subGun;
                        playerAnim.SetAimAngle(180f);
                    }

                    NormalMove();
                    statePercent += Time.deltaTime / playerAnim.gunOnOffRate;

                    

                    //ステートが半分経過
                    if (statePercent >= 0.25f && !stateFlag1) {
                        if(currentGun.suppressorPercent > 0) {
                            currentGun.ShowSuppressor();
                        }



                        stateFlag1 = true;
                    }

                    //音声処理
                    if (currentStateTime > 0.1f && !stateFlag2) {
                        if(sound_SubGunPickUp != null)
                        AudioManager.instance.PlaySound(sound_SubGunPickUp, transform.position);
                        stateFlag2 = true;
                    }


                    if (statePercent >= 1) {
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 0, 0.1f);
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    return;

                }
            case State.Normal_TakeOff_Sub: {

                    if (stateEnter) {
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Normal);
                        playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.TakeOff_Sub);
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 1, 0.1f);
                    }

                    NormalMove();
                    statePercent += Time.deltaTime / playerAnim.gunOnOffRate;

                    
                    if (statePercent >= 0.25f && !stateFlag1) {
                        currentGun.HideSuppressor();


                        stateFlag1 = true;
                    }

                    //音声処理
                    if (currentStateTime > 0.1f && !stateFlag2) {
                        if (sound_SubGunTakeOff != null)
                            AudioManager.instance.PlaySound(sound_SubGunTakeOff, transform.position);
                        stateFlag2 = true;
                    }


                    if (statePercent >= 1) {
                        currentGun = null;
                        if (index_currenGun == 1) {
                            ChangeState(State.Normal_PickUp_Main);
                            return;
                        }

                        isHold = false;
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 0, 0.1f);
                        playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Free);
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    return;
                }
            case State.Normal_SitDown: {

                    if (stateEnter) {
                        cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_squat_Nor);
                    }

                    Vector3 targetDir = new Vector3();

                    if (current_CoverEdge) {
                        targetDir = CoverMove();
                    }
                    else {
                        targetDir = NormalMove();
                    }

                    squatPercent += Time.deltaTime / 0.1f;
                    squatPercent = Mathf.Clamp01(squatPercent);
                    playerAnim.SetSquatPercent(squatPercent);


                    if (squatPercent >= 1) {
                        squatPercent = 1;

                        if (current_CoverEdge) {
                            //カバー中
                            if (targetDir.magnitude != 0f && Vector3.Angle(targetDir, current_CoverEdge.dir_AB) < 90f) {
                                //右方向に移動したがっている
                                ChangeState(State.Normal_Cover_Right);
                                return;
                            }
                            else {
                                //左方向に移動したがっている
                                ChangeState(State.Normal_Cover_Left);
                                return;
                            }
                        }
                        else {
                            ChangeState(State.Normal_Stand);
                        }

                        return;
                    }

                    return;
                }
            case State.Normal_StandUp: {

                    if (stateEnter) {
                        cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_stand_Nor);
                    }

                    Vector3 targetDir = new Vector3();

                    if (current_CoverEdge) {
                        targetDir = CoverMove();
                    }
                    else {
                        targetDir = NormalMove();
                    }

                    squatPercent -= Time.deltaTime / 0.1f;
                    squatPercent = Mathf.Clamp01(squatPercent);
                    playerAnim.SetSquatPercent(squatPercent);


                    if (squatPercent <= 0) {
                        squatPercent = 0;

                        if (current_CoverEdge) {
                            //カバー中
                            if (targetDir.magnitude != 0f && Vector3.Angle(targetDir, current_CoverEdge.dir_AB) < 90f) {
                                //右方向に移動したがっている
                                ChangeState(State.Normal_Cover_Right);
                                return;
                            }
                            else {
                                //左方向に移動したがっている
                                ChangeState(State.Normal_Cover_Left);
                                return;
                            }
                        }
                        else {
                            ChangeState(State.Normal_Stand);
                        }

                        return;
                    }

                    return;
                }
            case State.Normal_Jump: {
                    if (stateEnter) {
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Jump);
                        cameraCon.bodyCam.Priority = 20;
                    }

                    if (!stateFlag1) {
                        transform.forward = Vector3.Lerp(stateEnterForward, currentHangEdge.dir_Body, currentStateTime / 0.2f);

                        if(currentStateTime >= 0.2f) {
                            stateFlag1 = true;
                            playerAnim.SetMoveSpeed(0);
                            currentVelocity = Vector3.zero;
                            speedSmoothVelocity = 0f;
                        }
                    }

                    if(playerAnim.endAnimation) {
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    return;
                }
            case State.Normal_Cover_Left: {
                    if (stateEnter) {
                        //クロスヘアを消す
                        clossHair.gameObject.SetActive(false);
                        clossHair_Brock.gameObject.SetActive(false);

                        //アニメーション遷移
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Cover_Left);
                        isLeftCover = true;
                    }

                    //低いカバーだった、プレイヤーがしゃがんでない
                    if (current_CoverEdge.isLowCover && squatPercent != 1f) {
                        squatPercent = Mathf.Clamp01(squatPercent + Time.deltaTime / (turnSmoothTime / 2));
                        playerAnim.SetSquatPercent(squatPercent);
                        cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_squat_Nor);
                    }

                    Vector3 targetDir = CoverMove();

                    //外側に移動したがっている
                    if (targetDir.magnitude != 0f && Vector3.Angle(targetDir, -current_CoverEdge.dir_Body) > 130f) {
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    //走りたがってる
                    if (Input.GetKey(KeyCode.LeftShift)) {
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    //近接戦闘ボタンが押された
                    if (Input.GetKeyDown(KeyCode.F)) {
                        TryAction_Attack();
                    }

                    //右方向に移動したがっている
                    if (targetDir.magnitude != 0f && Vector3.Angle(targetDir, current_CoverEdge.dir_AB) < 90f) {
                        ChangeState(State.Normal_Cover_Right);
                        return;
                    }

                    //カバー線分の外にいる
                    if(current_CoverEdge.isOver_A || current_CoverEdge.isOver_B) {

                        //別のカバーに移ったか判定
                        foreach (CoverEdge coverEdge in coverEdgeList) {
                            if (coverEdge.isCoverring && Vector3.Angle(-coverEdge.dir_Body, targetDir) <= 90f) { 
                                current_CoverEdge = coverEdge;

                                if (targetDir.magnitude != 0f && Vector3.Angle(targetDir, current_CoverEdge.dir_AB) < 90f) {
                                    //右方向に移動したがっている
                                    ChangeState(State.Normal_Cover_Right);
                                    return;
                                }
                                else {
                                    //左方向に移動したがっている
                                    ChangeState(State.Normal_Cover_Left);
                                    return;
                                }

                            }
                        }


                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    //しゃがみボタンが押された
                    if (Input.GetKey(KeyCode.C) && squatPercent == 0) {
                        ChangeState(State.Normal_SitDown);
                        return;
                    }

                    //立ち上がりボタンが押された
                    if (Input.GetKey(KeyCode.X) && squatPercent == 1) {
                        ChangeState(State.Normal_StandUp);
                        return;
                    }

                    //ジャンプボタンが押された
                    if (Input.GetKeyDown(KeyCode.Space)) {
                        currentHangEdge = null;
                        foreach (HangEdge hangEdge in hangEdgeList) {
                            if (hangEdge.canGrab) {
                                if (currentHangEdge != null) {
                                    if (currentHangEdge.distanceFromPlayer > hangEdge.distanceFromPlayer) {
                                        currentHangEdge = hangEdge;
                                    }
                                }
                                else {
                                    currentHangEdge = hangEdge;
                                }
                            }
                        }

                        if (currentHangEdge != null) {
                            ChangeState(State.Normal_Jump);
                            return;
                        }
                    }


                    //メインウェポンに切り替え
                    if (Input.GetKeyDown(KeyCode.Alpha1) && index_currenGun != 1) {
                        index_currenGun = 1;
                        if (isHold) {
                            if (currentGun != playerAnim.mainGun) {
                                ChangeState(State.Normal_TakeOff_Sub);
                                return;
                            }

                        }
                        else {
                            ChangeState(State.Normal_PickUp_Main);
                            return;
                        }
                    }

                    //サブウェポンに切り替え
                    if (Input.GetKeyDown(KeyCode.Alpha2) && index_currenGun != 2) {
                        index_currenGun = 2;

                        if (isHold) {
                            if (currentGun != playerAnim.subGun) {
                                ChangeState(State.Normal_TakeOff_Main);
                                return;
                            }
                        }
                        else {
                            ChangeState(State.Normal_PickUp_Sub);
                            return;
                        }
                    }

                    //武器を構えようとしてる
                    if (Input.GetMouseButton(1) && currentStateTime >= 0.3f && isHold) {
                        //Debug.Log("武器を構えようとしている");
                        //左端にいる
                        if (Vector3.Distance(current_CoverEdge.posA.position, transform.position) <= 0.7f && !current_CoverEdge.isCorner_A) {
                            //Debug.Log("左端に居る");
                            //カバー方向を向いている
                            if (Vector3.Angle(cameraCon.transform.forward, -current_CoverEdge.dir_Body) < 90f) {
                                //Debug.Log("カバー方向を向いている");
                                pos_cover_BackFrom_Aim = current_CoverEdge.posA.position + (current_CoverEdge.dir_AB * 0.5f) + (current_CoverEdge.dir_Body * 0.5f);
                                pos_cover_To_Aim =       current_CoverEdge.posA.position - (current_CoverEdge.dir_AB * 0.5f) + (current_CoverEdge.dir_Body * 0.5f);
                                cameraCon.SwitchAimCameraSide(isLeftCover);
                            }
                            else {
                                //Debug.Log("カバー方向を向いていない");
                                current_CoverEdge = null;
                            }
                        }
                        //中腹にいる
                        else {
                            //カバー方向を向いている&&カバーが低い
                            if (Vector3.Angle(cameraCon.transform.forward, -current_CoverEdge.dir_Body) < 90f && current_CoverEdge.isLowCover) {
                                pos_cover_BackFrom_Aim = transform.position;
                                pos_cover_To_Aim = transform.position + (current_CoverEdge.dir_Body * 0.5f);
                                ChangeState(State.Aim_SetUp_LowCover);
                                return;
                            }
                            else {
                                current_CoverEdge = null;
                            }
                            current_CoverEdge = null;
                        }

                        ChangeState(State.Aim_SetUp);
                        return;
                    }

                    //リロードしようとしてる
                    if (Input.GetKeyDown(KeyCode.R) && currentStateTime >= 0.3f && CanReload()) {

                        if(index_currenGun == 1) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Reload_Main);
                        }

                        if(index_currenGun == 2) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Reload_Sub);
                        }

                        ChangeState(State.Aim_Reload);
                        return;
                    }



                    return;
                }
            case State.Normal_Cover_Right: {
                    if (stateEnter) {
                        //クロスヘアを消す
                        clossHair.gameObject.SetActive(false);
                        clossHair_Brock.gameObject.SetActive(false);

                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Cover_Right);
                        isLeftCover = false;
                    }

                    if (current_CoverEdge.isLowCover && squatPercent != 1f) {
                        squatPercent = Mathf.Clamp01(squatPercent + Time.deltaTime / (turnSmoothTime / 2));
                        playerAnim.SetSquatPercent(squatPercent);
                        cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_squat_Nor);
                    }


                    Vector3 targetDir = CoverMove();

                    //外側に移動したがっている
                    if (targetDir.magnitude != 0f && Vector3.Angle(targetDir, -current_CoverEdge.dir_Body) > 130f) {
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    //走りたがってる
                    if (Input.GetKey(KeyCode.LeftShift)) {
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    //近接戦闘ボタンが押された
                    if (Input.GetKeyDown(KeyCode.F)) {
                        TryAction_Attack();
                    }

                    //左方向に移動したがっている
                    if (targetDir.magnitude != 0f && Vector3.Angle(targetDir, current_CoverEdge.dir_AB) > 90f) {
                        ChangeState(State.Normal_Cover_Left);
                        return;
                    }

                    //カバー線分の外にいる
                    if (current_CoverEdge.isOver_A || current_CoverEdge.isOver_B) {

                        //カバーアクション判定
                        foreach (CoverEdge coverEdge in coverEdgeList) {
                            if (coverEdge.isCoverring && Vector3.Angle(-coverEdge.dir_Body, targetDir) <= 90f) { 
                                current_CoverEdge = coverEdge;

                                if (targetDir.magnitude != 0f && Vector3.Angle(targetDir, current_CoverEdge.dir_AB) < 90f) {
                                    //右方向に移動したがっている
                                    ChangeState(State.Normal_Cover_Right);
                                    return;
                                }
                                else {
                                    //左方向に移動したがっている
                                    ChangeState(State.Normal_Cover_Left);
                                    return;
                                }

                            }
                        }


                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    //しゃがみボタンが押された
                    if (Input.GetKey(KeyCode.C) && squatPercent == 0) {
                        ChangeState(State.Normal_SitDown);
                        return;
                    }

                    //立ち上がりボタンが押された
                    if (Input.GetKey(KeyCode.X) && squatPercent == 1) {
                        ChangeState(State.Normal_StandUp);
                        return;
                    }

                    //ジャンプボタンが押された
                    if (Input.GetKeyDown(KeyCode.Space)) {
                        currentHangEdge = null;
                        foreach (HangEdge hangEdge in hangEdgeList) {
                            if (hangEdge.canGrab) {
                                if (currentHangEdge != null) {
                                    if (currentHangEdge.distanceFromPlayer > hangEdge.distanceFromPlayer) {
                                        currentHangEdge = hangEdge;
                                    }
                                }
                                else {
                                    currentHangEdge = hangEdge;
                                }
                            }
                        }

                        if (currentHangEdge != null) {
                            ChangeState(State.Normal_Jump);
                            return;
                        }
                    }


                    //メインウェポンに切り替え
                    if (Input.GetKeyDown(KeyCode.Alpha1) && index_currenGun != 1) {
                        index_currenGun = 1;
                        if (isHold) {
                            if (currentGun != playerAnim.mainGun) {
                                ChangeState(State.Normal_TakeOff_Sub);
                                return;
                            }

                        }
                        else {
                            ChangeState(State.Normal_PickUp_Main);
                            return;
                        }
                    }


                    //サブウェポンに切り替え
                    if (Input.GetKeyDown(KeyCode.Alpha2) && index_currenGun != 2) {
                        index_currenGun = 2;

                        if (isHold) {
                            if (currentGun != playerAnim.subGun) {
                                ChangeState(State.Normal_TakeOff_Main);
                                return;
                            }
                        }
                        else {
                            ChangeState(State.Normal_PickUp_Sub);
                            return;
                        }
                    }

                    //武器を構えようとしてる
                    if (Input.GetMouseButton(1) && currentStateTime >= 0.3f && isHold) {

                        //左端にいる&&隅っこじゃない
                        if (Vector3.Distance(current_CoverEdge.posB.position, transform.position) <= 0.7f && !current_CoverEdge.isCorner_B) {
                            //カバー方向を向いている
                            if (Vector3.Angle(cameraCon.transform.forward, -current_CoverEdge.dir_Body) < 90f) {
                                pos_cover_BackFrom_Aim = current_CoverEdge.posB.position - (current_CoverEdge.dir_AB * 0.5f) + (current_CoverEdge.dir_Body * 0.5f);
                                pos_cover_To_Aim =       current_CoverEdge.posB.position + (current_CoverEdge.dir_AB * 0.5f) + (current_CoverEdge.dir_Body * 0.5f);
                                cameraCon.SwitchAimCameraSide(isLeftCover);
                            }
                            else {
                                current_CoverEdge = null;
                            }
                        }
                        //中腹にいる
                        else {
                            //カバー方向を向いている&&カバーが低い
                            if (Vector3.Angle(cameraCon.transform.forward, -current_CoverEdge.dir_Body) < 90f && current_CoverEdge.isLowCover) {
                                pos_cover_BackFrom_Aim = transform.position;
                                pos_cover_To_Aim = transform.position + (current_CoverEdge.dir_Body * 0.5f);
                                ChangeState(State.Aim_SetUp_LowCover);
                                return;
                            }
                            else {
                                current_CoverEdge = null;
                            }
                            current_CoverEdge = null;
                        }

                        ChangeState(State.Aim_SetUp);
                        return;
                    }

                    //リロードしようとしてる
                    if (Input.GetKeyDown(KeyCode.R) && currentStateTime >= 0.3f && CanReload()) {
                        if (index_currenGun == 1) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Reload_Main);
                        }

                        if (index_currenGun == 2) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Reload_Sub);
                        }
                        ChangeState(State.Aim_Reload);
                        return;
                    }

                    return;
                }

            #endregion

            #region 匍匐

            case State.Crawling_Normal: {
                    if (stateEnter) {
                        //クロスヘアを消す
                        clossHair.gameObject.SetActive(false);
                        clossHair_Brock.gameObject.SetActive(false);

                        //アニメーション設定
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 0, 0.25f);//銃レイヤーのウェイトを０に
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Crawl);
                        if (index_currenGun == 1) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Main_Crawl);
                        }
                        else 
                        if(index_currenGun == 2) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Sub_Crawl);
                        }

                        //通常カメラに戻す
                        cameraCon.aimCam.Priority = 5;

                        //武器残弾表示
                        UpdateBulletAmountText();

                        //カバー解除
                        current_CoverEdge = null;
                        currentHangEdge = null;

                        //カメラ高さ調整
                        cameraCon.SetCameraHeight(0.2f);

                        //コントローラー変形
                        controller.center = new Vector3(0, 0.15f, 0);
                        controller.radius = 0.1f;
                        controller.height = 0f;
                    }

                    //移動処理
                    CrawlMove();

                    //立ち上がりボタンが押された
                    if (Input.GetKeyDown(KeyCode.X) && currentStateTime >= 0.3f) {
                        squatPercent = 1f;
                        ChangeState(State.Normal_Stand);
                        return;
                    }


                    //武器を構えようとしてる
                    if (Input.GetMouseButton(1) && currentStateTime >= 0.3f && index_currenGun != 0) {
                        ChangeState(State.Crawling_Aim);
                        return;
                    }


                    return;
                }
            case State.Crawling_Aim: {
                    if (stateEnter) {
                        //アニメーション設定
                        if (index_currenGun == 1) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Main_Crawl);
                        }
                        else {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Sub_Crawl);
                        }
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 1, 0.05f);

                        //カメラ設定
                        cameraCon.aimCrawlCam.Priority = 20;

                        //クロスヘア表示
                        clossHair.gameObject.SetActive(true);

                        //武器残弾表示
                        UpdateBulletAmountText();
                    }

                    Crawl_AimMove();

                    //射線判定処理
                    Vector3 pos_BalletHit = currentGun.GetHitPos(cameraCon.lookTarget);
                    if (Vector3.Distance(pos_BalletHit, cameraCon.lookTarget.position) > 0.5f && Vector3.Distance(currentGun.muzzle.position, pos_BalletHit) <= 15.0f) {
                        clossHair_Brock.gameObject.SetActive(true);
                        clossHair_Brock.transform.position = Camera.main.WorldToScreenPoint(pos_BalletHit);
                    }
                    else {
                        clossHair_Brock.gameObject.SetActive(false);
                    }


                    //マウスを離した
                    if (!Input.GetMouseButton(1)) {
                        //カメラリセット
                        cameraCon.aimCrawlCam.Priority = 5;
                        //銃レイヤーリセット
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 0, 0.05f);
                        //匍匐に戻る
                        ChangeState(State.Crawling_Normal);
                        return;
                    }

                    //トリガーが引かれた
                    if (Input.GetMouseButton(0)) {

                        if (currentGun.bulletAmmount > 0) {
                            ChangeState(State.Crawling_Aim_Shoot);
                            return;
                        }
                        else {
                            ChangeState(State.Crawling_Aim_Shoot_OutOfAmmo);
                            return;
                        }
                    }

                    //リロードしようとしてる
                    if (Input.GetKeyDown(KeyCode.R) && CanReload()) {
                        ChangeState(State.Crawling_Aim_Reload);
                        return;
                    }


                    return;
                }
            case State.Crawling_Aim_Shoot: {

                    if (stateEnter) {
                        currentGun.Shoot(cameraCon.lookTarget);

                        if (index_currenGun == 1) {
                            playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Shoot);
                        }
                        else {
                            playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Shoot);
                        }

                        if (currentGun.isSuppress) {
                            noise_Gun = 5f;
                        }
                        else {
                            noise_Gun = 80f;
                        }

                        //武器残弾表示
                        UpdateBulletAmountText();
                    }

                    Crawl_AimMove();


                    statePercent += Time.deltaTime / currentGun.shootRate;

                    if (statePercent >= 1) {
                        if (index_currenGun == 1) {
                            playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Hold);
                        }
                        else {
                            playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Hold);
                        }
                        noise_Gun = 0f;
                        ChangeState(State.Crawling_Aim);
                        return;
                    }

                    return;
                }
            case State.Crawling_Aim_Shoot_OutOfAmmo: {
                    if (stateEnter) {
                        AudioManager.instance.PlaySound(currentGun.sfx_OutOfAmmo, currentGun.transform.position, 1);
                    }

                    Crawl_AimMove();


                    if (!Input.GetMouseButton(0)) {
                        //トリガーを離した
                        ChangeState(State.Crawling_Aim);
                        return;
                    }


                    return;
                }
            case State.Crawling_Aim_Reload: {

                    if (stateEnter) {

                        if (index_currenGun == 1) {
                            playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Reload);
                        }
                        else {
                            playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Reload);
                        }

                    }

                    if (!stateFlag1) {//音声処理
                        if (currentStateTime >= currentGun.sfxDelayTime_reload) {
                            if (currentGun.sfx_reload != null) {
                                AudioManager.instance.PlaySound(currentGun.sfx_reload, currentGun.transform.position, 1);
                            }
                            stateFlag1 = true;
                        }
                    }

                    //移動処理
                    Crawl_AimMove();

                    //リロードモーション完了率計算
                    statePercent += Time.deltaTime / currentGun.reloadRate;

                    //リロードモーション完了
                    if (statePercent >= 1f) {
                        //弾丸を込める
                        int require = currentGun.bulletCapa - currentGun.bulletAmmount;
                        int add = 0;
                        if (index_currenGun == 1) {
                            if (require > rifleBulletAmount) {
                                add = rifleBulletAmount;
                            }
                            else {
                                add = require;
                            }
                            rifleBulletAmount -= add;
                        }
                        else {
                            if (require > handgunBulletAmount) {
                                add = handgunBulletAmount;
                            }
                            else {
                                add = require;
                            }

                            handgunBulletAmount -= add;
                        }

                        currentGun.Reload(add);

                        ChangeState(State.Crawling_Aim);
                        return;


                    }
                    return;
                }
            #endregion

            #region Aim
            case State.Aim_SetUp: {

                    if (stateEnter) {
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Aim);

                        if (index_currenGun == 1) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Main);
                            playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Hold);
                        }
                        if (index_currenGun == 2) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Sub);
                            playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Hold);
                        }

                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 1, 0.1f);

                        cameraCon.aimCam.Priority = 20;

                        if (current_CoverEdge) {
                            controller.enabled = false;
                        }

                    }


                    playerAnim.SetAimAngle(cameraCon.vertical);

                    if (current_CoverEdge) {
                        transform.position = Vector3.Lerp(pos_cover_BackFrom_Aim, pos_cover_To_Aim, currentStateTime / (turnSmoothTime / 2));
                        float targetAngle = Mathf.Lerp(Mathf.Atan2(stateEnterForward.x, stateEnterForward.z) * Mathf.Rad2Deg, cameraCon.horizontal, currentStateTime / (turnSmoothTime / 2));
                        transform.eulerAngles = new Vector3(0f, targetAngle, 0f);
                    }
                    else {
                        AimMove();
                    }

                    if (currentStateTime >= turnSmoothTime / 2) {
                        controller.enabled = true;
                        ChangeState(State.Aim_Stand);
                        return;
                    }
                    return;
                }
            case State.Aim_SetUp_LowCover: {

                    if (stateEnter) {
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Aim);

                        if (index_currenGun == 1) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Main);
                            playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Hold);
                        }
                        if (index_currenGun == 2) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Sub);
                            playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Hold);
                        }

                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 1, 0.1f);

                        cameraCon.aimCam.Priority = 20;

                        cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_stand_Aim);
                    }

                    AimMove();

                    playerAnim.SetAimAngle(cameraCon.vertical);

                    transform.position = Vector3.Lerp(pos_cover_BackFrom_Aim, pos_cover_To_Aim, currentStateTime / (turnSmoothTime / 2));
                    squatPercent = Mathf.Clamp01(squatPercent - Time.deltaTime / (turnSmoothTime / 2));
                    playerAnim.SetSquatPercent(squatPercent);

                    if (currentStateTime >= turnSmoothTime / 2) {
                        ChangeState(State.Aim_Stand);
                        return;
                    }
                    return;
                }
            case State.Aim_Stand: {

                    if (stateEnter) {
                        //アニメーション設定
                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Aim);
                        if (index_currenGun == 1) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Main);
                        }

                        if (index_currenGun == 2) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Aim_Sub);
                        }

                        //クロスヘア表示
                        clossHair.gameObject.SetActive(true);

                        //武器残弾表示
                        UpdateBulletAmountText();
                    }

                    AimMove();

                    //上下角度計算

                    playerAnim.SetAimAngle(cameraCon.vertical);

                    //シャガミボタンが押された
                    if (Input.GetKey(KeyCode.C) && squatPercent == 0) {
                        ChangeState(State.Aim_SitDown);
                        return;
                    }

                    //立ち上がりボタンが押された
                    if (Input.GetKey(KeyCode.X)) {
                        ChangeState(State.Aim_StandUp);
                        return;
                    }

                    //肩越し視点反転ボタンが押された
                    if (Input.GetMouseButtonDown(2)) {
                        if (gunManageSound) {
                            AudioManager.instance.PlaySound(gunManageSound, transform.position);
                        }
                        cameraCon.SwitchAimCameraSide();
                    }

                    //射線判定処理
                    Vector3 pos_BalletHit = currentGun.GetHitPos(cameraCon.lookTarget);
                    if (Vector3.Distance(pos_BalletHit, cameraCon.lookTarget.position) > 0.5f && Vector3.Distance(currentGun.muzzle.position, pos_BalletHit) <= 15.0f) {
                        clossHair_Brock.gameObject.SetActive(true);
                        clossHair_Brock.transform.position = Camera.main.WorldToScreenPoint(pos_BalletHit);
                    }
                    else {
                        clossHair_Brock.gameObject.SetActive(false);
                    }

                    //マウスを離した
                    if (!Input.GetMouseButton(1)) {
                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 0, 0.1f);
                        cameraCon.aimCam.Priority = 5;

                        //カバーに戻る
                        if (current_CoverEdge && Vector3.Distance(transform.position,pos_cover_BackFrom_Aim)<1.5f ) {
                            ChangeState(State.Aim_BackToCover);
                            return;
                        }

                        //ノーマルに戻る
                        ChangeState(State.Normal_Stand);
                        return;
                    }

                    //トリガーが引かれた
                    if (Input.GetMouseButton(0)) {

                        if(currentGun.bulletAmmount > 0) {
                            ChangeState(State.Aim_Shoot);
                            return;
                        }
                        else {
                            ChangeState(State.Aim_Shoot_OutOfAmmo);
                            return;
                        }
                    }

                    //リロードしようとしてる
                    if (Input.GetKeyDown(KeyCode.R) && CanReload()) {
                        ChangeState(State.Aim_Reload);
                        return;
                    }

                    return;
                }
            case State.Aim_Shoot: {

                    if (stateEnter) {
                        currentGun.Shoot(cameraCon.lookTarget);

                        if (index_currenGun == 1) {
                            playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Shoot);
                        }
                        else {
                            playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Shoot);
                        }
                        
                        if (currentGun.isSuppress) {
                            noise_Gun = 5f;
                        }
                        else {
                            noise_Gun = 80f;
                        }

                        //武器残弾表示
                        UpdateBulletAmountText();
                    }

                    AimMove();

                    playerAnim.SetAimAngle(cameraCon.vertical);

                    statePercent += Time.deltaTime / currentGun.shootRate;

                    if (statePercent >= 1) {
                        if (index_currenGun == 1) {
                            playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Hold);
                        }
                        else {
                            playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Hold);
                        }
                        noise_Gun = 0f;
                        ChangeState(State.Aim_Stand);
                        return;
                    }

                    return;
                }
            case State.Aim_Shoot_OutOfAmmo: {
                    if (stateEnter) {
                        if (currentGun.sfx_OutOfAmmo) {
                            AudioManager.instance.PlaySound(currentGun.sfx_OutOfAmmo, currentGun.transform.position, 1);
                        }
                        
                    }

                    AimMove();

                    playerAnim.SetAimAngle(cameraCon.vertical);

                    if (!Input.GetMouseButton(0)) {
                        //トリガーを離した
                        ChangeState(State.Aim_Stand);
                        return;
                    }


                    return;
                }
            case State.Aim_SitDown: {

                    if (stateEnter) {
                        cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_squat_Aim);
                    }

                    AimMove();

                    playerAnim.SetAimAngle(cameraCon.vertical);
                    squatPercent += Time.deltaTime / 0.1f;
                    squatPercent = Mathf.Clamp01(squatPercent);
                    playerAnim.SetSquatPercent(squatPercent);

                    if (squatPercent >= 1) {
                        squatPercent = 1;
                        ChangeState(State.Aim_Stand);
                        return;
                    }

                    return;
                }
            case State.Aim_StandUp: {

                    if (stateEnter) {
                        cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_stand_Aim);
                    }

                    AimMove();

                    playerAnim.SetAimAngle(cameraCon.vertical);


                    squatPercent -= Time.deltaTime / 0.1f;
                    squatPercent = Mathf.Clamp01(squatPercent);
                    playerAnim.SetSquatPercent(squatPercent);

                    if (squatPercent <= 0) {
                        squatPercent = 0;
                        ChangeState(State.Aim_Stand);
                        return;
                    }

                    return;


                }
            case State.Aim_Reload: {

                    if (stateEnter) {
                        if(priviousState == State.Aim_Stand) {
                            playerAnim.SetState_Base(PlayerAnimation.State_Base.Aim);
                        }
                        else {
                            playerAnim.SetState_Base(PlayerAnimation.State_Base.Normal);
                        }

                        if (index_currenGun == 1) {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Reload_Main);
                            playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Reload);
                        }
                        else {
                            playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Reload_Sub);
                            playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Reload);
                        }

                        playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 1f, 0.25f);//銃取り扱いレイヤーを有効に
                    }

                    if (!stateFlag1) {//音声処理
                        if(currentStateTime >= currentGun.sfxDelayTime_reload) {
                            if(currentGun.sfx_reload != null) {
                                AudioManager.instance.PlaySound(currentGun.sfx_reload, currentGun.transform.position, 1);
                            }
                            stateFlag1 = true;
                        }
                    }

                    //移動処理
                    if (priviousState == State.Aim_Stand) {
                        AimMove();
                    }
                    else {
                        NormalMove();
                    }


                    //リロードモーション完了率計算
                    statePercent += Time.deltaTime / currentGun.reloadRate;

                    //リロードモーション完了
                    if (statePercent >= 1f) {
                        //弾丸を込める
                        int require = currentGun.bulletCapa - currentGun.bulletAmmount;
                        int add = 0;
                        if(index_currenGun == 1) {
                            if(require > rifleBulletAmount) {
                                add = rifleBulletAmount;
                            }
                            else {
                                add = require;
                            }
                            rifleBulletAmount -= add;
                        }
                        else {
                            if(require > handgunBulletAmount) {
                                add = handgunBulletAmount;
                            }
                            else {
                                add = require;
                            }

                            handgunBulletAmount -= add;
                        }

                        currentGun.Reload(add);

                        if (Input.GetMouseButton(1)) {
                            ChangeState(State.Aim_SetUp);
                            return;
                        }
                        else {
                            playerAnim.SetLayerWaight(PlayerAnimation.Layer.Base_Gun, 0f, 0.25f);//銃取り扱いレイヤーを有効に
                            ChangeState(State.Normal_Stand);
                            
                            return;
                        }


                    }
                    return;
                }

            case State.Aim_BackToCover: {
                    if (stateEnter) {
                        if (isLeftCover) {
                            playerAnim.SetState_Base(PlayerAnimation.State_Base.Cover_Left);
                        }
                        else {
                            playerAnim.SetState_Base(PlayerAnimation.State_Base.Cover_Right);
                        }

                        controller.enabled = false;
                    }

                    transform.position = Vector3.Lerp( stateEnterPosition, pos_cover_BackFrom_Aim, currentStateTime / (turnSmoothTime / 2));
                    float targetAngle = Mathf.Lerp(Mathf.Atan2(stateEnterForward.x, stateEnterForward.z) * Mathf.Rad2Deg, Mathf.Atan2(current_CoverEdge.dir_Body.x, current_CoverEdge.dir_Body.z) * Mathf.Rad2Deg, currentStateTime / turnSmoothTime);
                    transform.eulerAngles = new Vector3(0f, targetAngle, 0f);

                    if (current_CoverEdge.isLowCover && squatPercent != 1f) {
                        squatPercent = Mathf.Clamp01(squatPercent + Time.deltaTime / (turnSmoothTime / 2));
                        playerAnim.SetSquatPercent(squatPercent);
                        cameraCon.SetCameraHeight(ThirdPersonCamera.HEIGHT_squat_Nor);
                    }

                    //元の位置に戻った
                    if (currentStateTime >= turnSmoothTime / 2) {
                        controller.enabled = true;
                        if (isLeftCover){
                            ChangeState(State.Normal_Cover_Left);
                            return;
                        }
                        else {
                            ChangeState(State.Normal_Cover_Right);
                            return;
                        }
                    }
                    return;
                }

            #endregion

            case State.Dead: {
                    if (stateEnter) {
                        //playerAnim.SetLayerWaight(PlayerAnimation.Layer.Gun_Main, 0f, 0.1f);//銃取り扱いレイヤーを有効に
                        playerAnim.SetState_Gun_Main(PlayerAnimation.State_Gun.Unarmd);
                        playerAnim.SetState_Gun_Sub(PlayerAnimation.State_Gun.Unarmd);
                        playerAnim.SetState_GunBase(PlayerAnimation.State_GunBase.Free);

                        playerAnim.SetState_Base(PlayerAnimation.State_Base.Dead);
                        cameraCon.aimCam.Priority = 5;
                    }



                    return;
                }

        }

        
    }




    public void OnEndOfEvent() {
        ChangeState(State.Normal_Stand);
    }

    void LateUpdate() {

        if (currentStateTime != 0) {
            stateEnter = false;
        }

    }

    //近接戦闘アクション
    void TryAction_Attack() {
        EnemyController eneCon = GameManager.instance.enemyManager.GetNearestEnemyFromPos(transform.position, 1.5f);

        //近くに敵兵君がいる　＆　死んでない
        if (eneCon && eneCon.currentState != EnemyController.State.Dead_RagDoll) {
            cqcTargetEneCon = eneCon;

            //ランダムに技を選出
            if (cqcDataList_Temp.Count == 0) cqcDataList_Temp = new List<CQCData>(cqcDataList);
            int randomIndex = Random.Range(0, cqcDataList_Temp.Count);
            currentCQC = cqcDataList_Temp[randomIndex];
            cqcDataList_Temp.Remove(currentCQC);

            //プレイヤーと敵のアニメーターにクリップを登録
            playerAnim.SetCQCAnimation(currentCQC);
            cqcTargetEneCon.SetCQCAnimation(currentCQC);

            //プレイヤーと敵を近接戦闘ステートに移行させる。
            cqcTargetEneCon.ChangeState(EnemyController.State.Combat_GetGrab);
            ChangeState(State.Action_Attack);
            return;
        }
    }

    //接地判定
    bool IsGrounded() {
        return (velocityY > -0.2f);
    }

    //落下判定
    bool IsFalling() {
        return (velocityY <= -0.5f);
    }

    //移動処理
    Vector3 NormalMove() {

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"),0f, Input.GetAxisRaw("Vertical"));

        Vector3 inputDir = input.normalized;

        Vector3 targetDir = transform.forward;

        //入力あり
        if (input.magnitude != 0) {
            float targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
            transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);
            targetDir = Quaternion.Euler(0, cameraT.eulerAngles.y, 0) * inputDir;
        }
        //入力無し
        else {
            if(currentState != State.Aim_Reload) {
                Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime / 0.1f);
            }
        }

        bool running = false;
        
        if(currentState != State.Normal_PickUp_Main && 
           currentState != State.Normal_TakeOff_Main && 
           currentState != State.Aim_Reload &&
           currentState != State.Normal_PickUp_Sub &&
           currentState != State.Normal_TakeOff_Sub) {
            running = Input.GetKey(KeyCode.LeftShift);
        }

        float targetSpeed = ((running) ? runSpeed : walkSpeed) * inputDir.magnitude * (1f - squatDebuff * squatPercent);
        Vector3 targetVelocity = targetDir * targetSpeed;
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref currentSmoothVelocity, speedSmoothTime);

        //移動量を確定
        Vector3 velocity;

        //地面までの距離測定
        RaycastHit hit;
        float fallDistance = 0.2f;
        float skinWidth=0.1f;

        if(Physics.Raycast(transform.position+Vector3.up * skinWidth ,-Vector3.up,out hit, fallDistance + skinWidth, groundLayer)) {
            gizmoDrawer.AddRay(transform.position + Vector3.up * skinWidth, hit.point, Color.red);
            velocityY = -fallDistance;

            velocity = currentVelocity * Time.deltaTime + Vector3.up * velocityY;
        }
        else {
            //重力による移動量を加算
            velocityY += Time.deltaTime * gravity;
            velocity = (currentVelocity + Vector3.up * velocityY) * Time.deltaTime;
        }

        //移動量確定
        controller.Move(velocity);


        if (!controller.enabled) {
            //Debug.Log("非活性状態で呼ばれてるぞ！！" + currentState.ToString());
        }



        float targetAnimationSpeedPercent = controller.velocity.magnitude / (runSpeed * (1f - squatDebuff * squatPercent));//アニメーションを同期させるためにしゃがみデバフを再度かける。
        float currentAnimationSpeedPersent = playerAnim.GetMoveSpeed();

        if(Mathf.Abs(targetAnimationSpeedPercent - currentAnimationSpeedPersent) > 0.05f) {
            targetAnimationSpeedPercent = Mathf.Lerp(currentAnimationSpeedPersent, targetAnimationSpeedPercent, Time.deltaTime/0.1f);
        }

        playerAnim.SetMoveSpeed(targetAnimationSpeedPercent);

        if (controller.isGrounded) {
            velocityY = 0;
        }

        //音関連の処理
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        noise_Foot = 5f * (horizontalVelocity.magnitude / runSpeed);

        return targetDir;
    }

    //移動処理_匍匐
    void CrawlMove() {

        if (!controller.enabled) {
            //Debug.Log("非活性状態で呼ばれてるぞ！！" + currentState.ToString());
        }

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        Vector3 inputDir = Quaternion.Euler(0, cameraT.eulerAngles.y, 0) * input.normalized;

        gizmoDrawer.AddRay(transform.position + Vector3.up, transform.position + Vector3.up + inputDir, Color.cyan);

        //前進しているか後退しているか
        bool isForward = (Vector3.Angle(transform.forward, inputDir) <= 100f);

        //入力している
        if (input.magnitude != 0) {
            float targetRotation;
            //前進してる
            if (isForward) {
                targetRotation = Mathf.Atan2(input.normalized.x, input.normalized.z) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
               
            }
            //後退している
            else {
                targetRotation = Mathf.Atan2(-input.normalized.x, -input.normalized.z) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
            }
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime * 2f), transform.eulerAngles.z);
        }

        //移動速度を確定
        float targetSpeed = walkSpeed * 0.9f * inputDir.magnitude;
        if (isForward) {
            Vector3 targetVelocity = transform.forward * targetSpeed;
            currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref currentSmoothVelocity, speedSmoothTime);
        }
        else {
            Vector3 targetVelocity = -transform.forward * targetSpeed;
            currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref currentSmoothVelocity, speedSmoothTime);
        }


        
        Vector3 velocity;

        //地面までの距離測定
        RaycastHit hit;
        float fallDistance = 0.2f;
        float skinWidth = 0.1f;

        //地面と接触している
        if (Physics.Raycast(transform.position + transform.up * skinWidth, -transform.up, out hit, fallDistance + skinWidth, groundLayer)) {

            velocityY = -fallDistance;//これどういう意味？

            velocity = currentVelocity * Time.deltaTime + Vector3.up * velocityY;

            Vector3 targetBodyDir = Vector3.Cross(hit.normal, -transform.right);
            Vector3 targetBodyRightDir = Vector3.Cross(hit.normal, targetBodyDir);

            float targetXAngle = Vector3.Angle(Vector3.up, targetBodyDir) - 90f;
            targetXAngle = Mathf.Clamp(targetXAngle, -45, 45);

            float targetZAngle = -(Vector3.Angle(Vector3.up, targetBodyRightDir) - 90f);
            targetZAngle = Mathf.Clamp(targetZAngle, -45, 45);

            Quaternion targetRotation = Quaternion.Euler(targetXAngle, transform.eulerAngles.y, targetZAngle);

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime / 0.1f);

        }
        //地面と離れている
        else {
            //重力による移動量を加算
            velocityY += Time.deltaTime * gravity;
            velocity = (currentVelocity + Vector3.up * velocityY) * Time.deltaTime;
        }

        //移動量確定
        controller.Move(velocity);


        //アニメーション処理
        float targetAnimationSpeedPercent = controller.velocity.magnitude / ((isForward ? walkSpeed: - walkSpeed) * 0.9f);
        float currentAnimationSpeedPersent = playerAnim.GetMoveSpeed();

        if (Mathf.Abs(targetAnimationSpeedPercent - currentAnimationSpeedPersent) > 0.05f) {
            targetAnimationSpeedPercent = Mathf.Lerp(currentAnimationSpeedPersent, targetAnimationSpeedPercent, Time.deltaTime / 0.1f);
        }

        playerAnim.SetMoveSpeed(targetAnimationSpeedPercent);

        if (controller.isGrounded) {
            velocityY = 0;
        }

        //音関連の処理
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        noise_Foot = 2f * (horizontalVelocity.magnitude / runSpeed);

    }

    //カバー移動処理
    Vector3 CoverMove() {

        float targetRotation = Mathf.Atan2(current_CoverEdge.dir_Body.x, current_CoverEdge.dir_Body.z) * Mathf.Rad2Deg;
        transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);


        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"),0f, Input.GetAxisRaw("Vertical"));
        Vector3 inputDir = new Vector3();
        float targetMoveValue = 0f;
        if (input.magnitude != 0) {
            inputDir = Quaternion.Euler(0, cameraT.eulerAngles.y, 0) * input;
            targetMoveValue = Vector3.Dot(inputDir, current_CoverEdge.dir_AB);
        }

        float targetSpeed = (walkSpeed * Mathf.Abs(targetMoveValue) * (1f - squatDebuff * squatPercent));

        Vector3 targetVelocity = (targetMoveValue > 0 ? current_CoverEdge.dir_AB : -current_CoverEdge.dir_AB)  * targetSpeed;

        //壁に向かう移動量の計算
        Vector3 velocityToWall;
        if(Mathf.Abs(current_CoverEdge.distanceFromPlayer - 0.20f) > 0.01f) {
            velocityToWall = (- current_CoverEdge.dir_Body * (current_CoverEdge.distanceFromPlayer - 0.20f)).normalized * Time.deltaTime;
        }
        else {
            velocityToWall = - current_CoverEdge.dir_Body * (current_CoverEdge.distanceFromPlayer - 0.20f);
        }
        

        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref currentSmoothVelocity, speedSmoothTime);
        velocityY += Time.deltaTime * gravity;

        Vector3 velocity = currentVelocity + Vector3.up * velocityY;
        controller.Move(velocity * Time.deltaTime + velocityToWall);

        Vector3 finalVelocity = Quaternion.Euler(0, -cameraT.eulerAngles.y, 0) * currentVelocity / (runSpeed * (1f - squatDebuff * squatPercent));//アニメーションを同期させるためにしゃがみデバフを再度かける。

        playerAnim.SetMoveSpeed(finalVelocity.magnitude);

        if (controller.isGrounded) {
            velocityY = 0;
        }

        //音関連の処理
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        noise_Foot = 5f * (horizontalVelocity.magnitude / runSpeed);

        return inputDir;

    }

    //移動処理（構え）
    void AimMove() {
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, cameraCon.horizontal, transform.eulerAngles.z);

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        Vector3 targetDir = Vector3.forward;
        if (input.magnitude != 0) {
            targetDir = Quaternion.Euler(0, cameraT.eulerAngles.y, 0) * input;
        }

        float targetSpeed = (walkSpeed) * input.magnitude * (1f - squatDebuff * squatPercent);
        Vector3 targetVelocity = targetDir * targetSpeed;

        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref currentSmoothVelocity, speedSmoothTime);
        velocityY += Time.deltaTime * gravity;
        Vector3 velocity = currentVelocity + Vector3.up * velocityY;
        controller.Move(velocity * Time.deltaTime);

        Vector3 finalVelocity = Quaternion.Euler(0, -cameraT.eulerAngles.y, 0) * currentVelocity / (walkSpeed * (1f - squatDebuff * squatPercent));//アニメーションを同期させるためにしゃがみデバフを再度かける。

        playerAnim.SetAimMoveSpeed(finalVelocity.z, finalVelocity.x);

        if (controller.isGrounded) {
            velocityY = 0;
        }



    }

    //移動処理（匍匐構え）
    void Crawl_AimMove() {

        //カメラ方向
        Vector3 cameraDir = Quaternion.Euler(cameraCon.vertical, cameraCon.horizontal, 0) * Vector3.forward;
        float lookDir_Z = Vector3.Dot(transform.forward, cameraDir);
        float lookDir_X = Vector3.Dot(transform.right, cameraDir);

        //カメラ水平回転ベクトル
        Vector3 cameraDir_Horizontal = new Vector3(lookDir_X, 0f, lookDir_Z).normalized;
        playerAnim.SetCrawlAim_PoseDir(cameraDir_Horizontal.z, cameraDir_Horizontal.x);

        //カメラ上下回転角度
        float verticalAngle = Vector3.Angle(transform.up, cameraDir);
        playerAnim.SetAimAngle(verticalAngle-90f);

    }


    //ダメージ処理
    public void TakeDamage(float damage, Vector3 hitPos, Vector3 hitDirection, BodyPart part) {

        if (currentState != State.Dead) {
            HP -= damage;

            HP = Mathf.Clamp01(HP);

            if (HP <= 0) {
                ChangeState(State.Dead);
            }
        }
    }

    //治癒処理
    public void Heal(float healPoint) {
        healthPoint += healPoint;

        healthPoint = Mathf.Clamp01(healthPoint);
    }

    //リロード可否判定処理
    bool CanReload() {

        if (!isHold) {
            return false;
        }

        if(currentGun.bulletAmmount >= currentGun.bulletCapa) {
            return false;
        }
        
        if(index_currenGun == 1 && rifleBulletAmount >= 1) {
            return true;
        }

        if(index_currenGun == 2 && handgunBulletAmount >= 1) {
            return true;
        }

        return false;
    }

    //リロード処理
    public void AddBullet(int addAmount , bool sub) {
        if (sub) {
            handgunBulletAmount += addAmount;

        }
        else {
            rifleBulletAmount += addAmount;
        }
    }

    //残弾数
    public int BulletAmount(bool sub) {
        return sub ? handgunBulletAmount : rifleBulletAmount;
    }

    //弾数表示
    void UpdateBulletAmountText() {
        //武器残弾数表示
        if (index_currenGun != 0) {
            text_BulletAmount.gameObject.SetActive(true);

            switch (index_currenGun) {

                case 1: {//メインウェポン
                        text_BulletAmount.text = $"{currentGun.bulletAmmount:00}/{rifleBulletAmount:000}";
                        break;
                    }


                case 2: {//サブウェポン
                        text_BulletAmount.text = $"{currentGun.bulletAmmount:00}/{handgunBulletAmount:000}";
                        break;
                    }

            }

        }
        else {
            text_BulletAmount.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos() {

        gizmoDrawer.Execute();

    }
}

namespace MyGizmoTool {
    public class GizmoDrawer {

        List<RayForGizmo> rayForGizmoList = new List<RayForGizmo>();

        class RayForGizmo {
            public Color color;
            public Vector3 startPos;
            public Vector3 endPos;

            public RayForGizmo(Vector3 _startPos, Vector3 _endPos, Color _color) {
                startPos = _startPos;
                endPos = _endPos;
                color = _color;
            }
        }

        public void AddRay(Vector3 _startPos, Vector3 _endPos, Color _color) {
            rayForGizmoList.Add(new RayForGizmo(_startPos, _endPos, _color));
        }

        public void Execute() {
            foreach (RayForGizmo ray in rayForGizmoList) {
                Gizmos.color = ray.color;
                Gizmos.DrawLine(ray.startPos, ray.endPos);
            }

            rayForGizmoList = new List<RayForGizmo>();
        }

    }

}

namespace MyUtility {

    public class CopyPose {

        public void CopyWorldLerp(Transform target,Transform From, Transform To, float percent,bool late = false) {

            if (late) {
                copyTransformInfoList.Add(new CopyTransformInfo(target, From, To, percent, Type.CopyWorldLerp));
                return;
            }


            target.position = Vector3.Lerp(From.position, To.position, percent);
            target.rotation = Quaternion.Lerp(From.rotation, To.rotation, percent);
        }

        public void CopyWorld(Transform target, Transform origin, bool late = false) {

            if (late) {
                copyTransformInfoList.Add(new CopyTransformInfo(target, null, origin, 0, Type.CopyWorld));
                return;
            }



            target.position = origin.position;
            target.rotation = origin.rotation;
        }

        public void CopyWorldPos(Transform target, Transform origin, bool late = false) {

            if (late) {
                copyTransformInfoList.Add(new CopyTransformInfo(target, null, origin, 0, Type.CopyWorldPos));
                return;
            }

            target.position = origin.position;
        }

        public void CopyWorldRor(Transform target, Transform origin, bool late = false) {

            if (late) {
                copyTransformInfoList.Add(new CopyTransformInfo(target, null, origin, 0, Type.CopyWorldRor));
                return;
            }

            target.eulerAngles = origin.eulerAngles;
        }

        public void CopyWorldRorLerp(Transform target, Transform From, Transform To, float percent, bool late = false) {

            if (late) {
                copyTransformInfoList.Add(new CopyTransformInfo(target, From, To, 0, Type.CopyWorldRorLerp));
                return;
            }

            target.eulerAngles = Vector3.Lerp(From.eulerAngles,To.eulerAngles, percent);
        }

        public void CopyPoseAll(Transform target, Transform origin, bool late = false) {

            if (late) {
                copyTransformInfoList.Add(new CopyTransformInfo(target, null, origin, 0, Type.CopyPoseAll));
                return;
            }


            target.localRotation = origin.localRotation;
            target.localPosition = origin.localPosition;
            if (origin.childCount != 0) {
                foreach (Transform childOrigin in origin) {
                    Transform childTarget = target.Find(childOrigin.name);
                    if (childTarget != null) {
                        CopyPoseAll(childTarget, childOrigin);
                    }
                    else {
                        continue;
                    }
                }
            }
            else {
                return;
            }
        }

        public void CopyPoseOne(Transform target, Transform origin, bool late = false) {
            if (late) {
                copyTransformInfoList.Add(new CopyTransformInfo(target, null, origin, 0, Type.CopyPoseOne));
                return;
            }

            target.localRotation = origin.localRotation;
            target.localPosition = origin.localPosition;
        }

        public void CopyPoseOneLerp(Transform target, Transform From, Transform To, float percent, bool late = false) {
            if (late) {
                copyTransformInfoList.Add(new CopyTransformInfo(target, From, To, percent, Type.CopyPoseOneLerp));
                return;
            }

            target.localRotation = Quaternion.Lerp(From.localRotation, To.localRotation, percent);
            target.localPosition = Vector3.Lerp(From.localPosition, To.localPosition, percent);
        }

        public void CopyPoseAllLerp(Transform target,Transform From, Transform To, float percent, bool late = false) {

            if (late) {
                copyTransformInfoList.Add(new CopyTransformInfo(target, From, To, percent, Type.CopyPoseAllLerp));
                return;
            }


            target.localRotation = Quaternion.Lerp(From.localRotation, To.localRotation, percent);
            target.localPosition = Vector3.Lerp(From.localPosition, To.localPosition, percent);

            if (To.childCount != 0 && From.childCount != 0) {
                foreach (Transform childTo in To) {
                    Transform childTarget = target.Find(childTo.name);
                    Transform childFrom = From.Find(childTo.name);
                    if (childTarget != null && childFrom != null) {
                        CopyPoseAllLerp(childTarget, childFrom, childTo, percent);
                    }
                    else {
                        continue;
                    }
                }
            }
            else {
                return;
            }
        }

        List<CopyTransformInfo> copyTransformInfoList = new List<CopyTransformInfo>();

        public enum Type {
            CopyWorld,
            CopyWorldLerp,
            CopyWorldPos,
            CopyWorldRor,
            CopyWorldRorLerp,
            CopyPoseAll,
            CopyPoseOne,
            CopyPoseAllLerp,
            CopyPoseOneLerp
        }

        public class CopyTransformInfo {
            public Transform target;
            public Transform From;
            public Transform To;
            public float percent;
            public CopyPose.Type type = MyUtility.CopyPose.Type.CopyWorld;

            public CopyTransformInfo(Transform _target, Transform _From, Transform _To, float _percent,CopyPose.Type _type) {
                target = _target;
                From = _From;
                To = _To;
                percent = _percent;
                type = _type;
            }



        }

        public void Excute() {
            foreach (CopyTransformInfo info in copyTransformInfoList) {
                switch (info.type) {

                    case Type.CopyWorld: {
                            CopyWorld(info.target, info.To);
                            break;
                        }

                    case Type.CopyWorldLerp: {
                            CopyWorldLerp(info.target, info.From, info.To, info.percent);
                            break;
                        }

                    case Type.CopyWorldPos: {
                            CopyWorldPos(info.target, info.To);
                            break;
                        }

                    case Type.CopyWorldRor: {
                            CopyWorldRor(info.target, info.To);
                            break;
                        }

                    case Type.CopyWorldRorLerp: {
                            CopyWorldRorLerp(info.target, info.From, info.To, info.percent);
                            break;
                        }

                    case Type.CopyPoseAll: {
                            CopyPoseAll(info.target, info.To);
                            break;
                        }

                    case Type.CopyPoseOne: {
                            CopyPoseOne(info.target, info.To);
                            break;
                        }

                    case Type.CopyPoseAllLerp: {
                            CopyPoseAllLerp(info.target, info.From, info.To, info.percent);
                            break;
                        }

                    case Type.CopyPoseOneLerp: {
                            CopyPoseOneLerp(info.target, info.From, info.To, info.percent);
                            break;
                        }

                }
            }

            copyTransformInfoList = new List<CopyTransformInfo>();
        }
    }

    [System.Serializable]
    public struct SoundDelay {
        public AudioClip clip;
        public float delayTime;
    }

    public class SoundChain {
        List<SoundDelay> soundList;
        int soundIndex;
        float nextSoundTime;
        Transform soundPos;


        public SoundChain(List<SoundDelay> _soundList,Transform _soundPos) {
            soundList = _soundList;
            soundPos = _soundPos;
        }

        public void Reset() {
            soundIndex = 0;
            nextSoundTime = Time.time + soundList[soundIndex].delayTime;
        }

        public void Update() {
            if (nextSoundTime <= Time.time && soundIndex < soundList.Count) {
                AudioManager.instance.PlaySound(soundList[soundIndex].clip, soundPos.position);
                soundIndex++;
                if (soundIndex < soundList.Count) {
                    nextSoundTime += soundList[soundIndex].delayTime;
                }
            }
        }
    }

    public class LayerSmoothDamp {
        public int layerIndex;
        public float currentWeight;
        public float targetWeight;
        public float smoothTime;
        public float currentVelocity;

        public void Update() {
            if (currentWeight == targetWeight) {
                return;
            }

            currentWeight = Mathf.SmoothDamp(currentWeight, targetWeight, ref currentVelocity, smoothTime);
        }

        public LayerSmoothDamp(int index) {
            layerIndex = index;
            currentWeight = 1;
            targetWeight = 1;
            smoothTime = 0.3f;
        }

        public void ChangeWeight(float _weight, float _smoothTime) {
            targetWeight = _weight;
            smoothTime = _smoothTime;
        }
    }


}



