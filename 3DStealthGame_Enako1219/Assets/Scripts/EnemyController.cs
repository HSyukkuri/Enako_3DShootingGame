using System.Collections.Generic;
using UnityEngine;
using MyGizmoTool;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, ITakeDamage
    {
    public bool debug = false;

    //体力
    float HP = 1f;

    //移動
    const float interval_navMeshUpdate = 0.1f;
    float timer_navMeshUpdate;
    float turnSmoothVelocity;
    public Vector3 eventOffsetTargetPos;
    public Vector3 eventOffsetTargetDir;
    Vector3 targetPos;


    //視界
    const float dis_eye = 40f;
    const float ang_eye = 160f;
    const float detectLag = 0.5f;
    public float percent_Detect { get; private set; } = 0f;
    const float squatPenalty = 0.5f;
    bool detectPlayer = false;

    //戦闘
    const float shootDistance = 30f;

    //警戒モード
    Vector3 pos_look = Vector3.zero;
    Vector3 dir_look = Vector3.zero;
    float timer_LookDirectionUpdate;
    const float interval_lookDirectionUpdate = 3;

    //レイヤー
    public LayerMask targetLayer;
    public LayerMask visibleLayer;
    public LayerMask friendLayer;

    //声
    public AudioSource voiceSource;
    public AudioSource hitSource;
    public AudioClip hitSound;
    float timer_voice = 0f;
    bool voiceExcuted = false;
    public enum EnemyVoice {
        Patrol,
        Sus,
        Encount,
        Attack,
        LostSight,
        GaveUp,
        Alart,
        Alart_GaveUp,
        DetectShoot,
        GetSurpriseAttack,
        DetectDeadBody,
        CheckDeadBody,
        RequestBuddy,
        ApproveBuddy,

    }
    [System.Serializable]
    public class VoiceList {
        public EnemyVoice voiceType;
        public List<AudioClip> voices = new List<AudioClip>();
    }
    public List<VoiceList> voiceCorrection = new List<VoiceList>();

    [Header("足音")]
    public List<AudioClip> footStepSoundList = new List<AudioClip>();
    public void FootStepUpdate() {
        AudioManager.instance.PlaySound(footStepSoundList[Random.Range(0, footStepSoundList.Count)], transform.position, 2);
    }

    //参照
    NavMeshAgent navMesh;
    public EnemyAnimation enemyAnim;
    public GunManager gun;
    public Transform eyeT;
    GizmoDrawer GD;
    HitBoxController hitBoxCon;
    DetectIndicator indicator;
    EnemyRaderPointer raderPointer;
    GameManager gm;
    EnemisManager em;
    PlayerController player;


    //ステート関連
    public State priviousState { get; private set; } = State.Patrol_Walk;
    public State currentState { get; private set; } = State.Patrol_Walk;
    float currentStateTime = 0f;
    Vector3 stateEnterPosition = Vector3.zero;
    Vector3 stateEnterForward = Vector3.zero;
    bool stateEnter = false;
    bool stateFlag1 = false;

    //連携
    public EnemyController buddy = null;
    bool isMainBuddy = false;

    CQCData currentCQC;
    float cqcAnimatonStartTime;
    Vector3 cqcTargetPos;
    Vector3 cqcTargetDir;
    WindowEdge currentWindowEdge;
    

    public void ChangeState(State newState) {
        priviousState = currentState;
        currentState = newState;
        currentStateTime = 0f;
        stateEnter = true;
        voiceExcuted = false;

        stateEnterPosition = transform.position;
        stateEnterForward = transform.forward;

        stateFlag1 = false;

        if(indicator != null) {
            indicator.CloseIndicator();
        }

        

        if (debug) {
            Debug.Log($"{priviousState.ToString()}　⇒　{currentState.ToString()}");
        }

    }


    private void Start() {
        timer_navMeshUpdate = Time.time;
        gm = GameManager.instance;

        player = FindObjectOfType<PlayerController>();
        navMesh = GetComponent<NavMeshAgent>();
        navMesh.updateRotation = false;
        gun = enemyAnim.gun;

        GD = new GizmoDrawer();
        hitBoxCon = GetComponent<HitBoxController>();

        ChangeState(State.System_Initialize);
    }

    public enum State {
        Patrol_Walk,
        Patrol_Stop,
        Patrol_SmallSensation,
        Patrol_DetectShootSound1,//立ち止まる
        Patrol_DetectShootSound2,//銃声だ！（銃声が鳴った方向を向きながら）
        Patrol_CheckKnownPos,
        Patrol_BeAttacked,
        Patrol_GetSurpriseAttack,//攻撃を受けた！

        Conver_WaitComversationBuddy,//会話要請
        Conver_Talk,//話す
        Conver_Listen,//聴く

        Combat_Encount,
        Combat_Chace,
        Combat_Shoot,
        Combat_ReachKnownPos1,//相手の位置まで移動
        Combat_ReachKnownPos2,//相手の最後に見た位置まで移動
        Combat_Aim,
        Combat_TargetLost,//「敵を見失ったぞ」
        Combat_GetGrab,//近接攻撃を受ける
        Combat_GetDamage,//ダメージをおう

        Alert_Walk,
        Alert_Stop,
        Alart_GiveUp,
        Alart_DetectDeadBody,//死体を見つけた
        Alart_HeadingToDeadBody,//死体に向かう
        Alart_CheckDeadBody,//死体を調べる

        Alart_RequestBuddy,//探索に仲間を呼ぶ
        Alart_WaitBuddy,　//仲間を待つ
        Alart_ApproveBuddy,//探索依頼に了承する
        Alart_FollowBuddy,//仲間に追従する


        Dead_RagDoll,//ラグドール

        System_Initialize,//初期化
        System_Jump
    }

    private void Update() {



        currentStateTime += Time.deltaTime;

        //ギズモ処理
        //Color gizmoColor = Color.Lerp(Color.yellow, Color.red, PlayerExposePercent());
        //Vector3 viewDirA = Quaternion.Euler(0, -ang_eye / 2, 0) * eyeT.forward;
        //Vector3 viewDirB = Quaternion.Euler(0, ang_eye / 2, 0) * eyeT.forward;
        //GD.AddRay(eyeT.position, eyeT.position + eyeT.forward * dis_eye, gizmoColor);
        //GD.AddRay(eyeT.position, eyeT.position + viewDirA * dis_eye, gizmoColor);
        //GD.AddRay(eyeT.position, eyeT.position + viewDirB * dis_eye, gizmoColor);

        if (Input.GetKeyDown(KeyCode.K)) {
            ChangeState(State.Dead_RagDoll);
            return;
        }




        switch (currentState) {

            //初期化
            case State.System_Initialize: {

                    //敵兵マネージャを参照
                    if(em == null) {
                        em = GameManager.instance.enemyManager;
                        if(em == null) {
                            //Debug.LogError("敵兵マネージャを参照できなかった！");
                            return;
                        }
                    }

                    //敵兵マネージャが初期化中なら待機
                    if(em.currentState == EnemisManager.State.Initialize) {
                        return;
                    }

                    //自身を敵兵マネージャに登録
                    em.enemyList.Add(this);

                    //インジケーターを連携
                    indicator = em.GetIndicator();
                    indicator.SetEnemyController(this);

                    //レーダーポインタを連携
                    raderPointer = em.GetRaderPointer();
                    raderPointer.SetEnemyController(this);

                    //ターゲット移動地点を設定
                    targetPos = RandomPosition();

                    //Walkステートに移行
                    ChangeState(State.Patrol_Walk);
                    return;
                }
            //窓飛び越え
            case State.System_Jump: {
                    if (stateEnter) {
                        enemyAnim.SetState(EnemyAnimation.State.Jump);
                        navMesh.isStopped = true;
                        navMesh.enabled = false;
                    }

                    if(currentStateTime <= 0.5f) {
                        transform.position = Vector3.Lerp(stateEnterPosition, eventOffsetTargetPos, currentStateTime / 0.5f);
                        transform.forward = Vector3.Lerp(stateEnterForward, eventOffsetTargetDir, currentStateTime / 0.5f);
                    }


                    if(currentStateTime >= 6f) {
                        navMesh.enabled = true;
                        navMesh.isStopped = false;
                        currentWindowEdge.ReleaseTarget(this);
                        currentWindowEdge = null;
                        ChangeState(priviousState);
                    }

                    return;
                }

            #region Patrol

            //パトロール
            case State.Patrol_Walk: {

                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//銃レイヤーを無効に
                        enemyAnim.SetState(EnemyAnimation.State.Walk);

                        if(Vector3.Distance(transform.position,targetPos) <= 0.5f) {
                            targetPos = RandomPosition();
                        }
                        navMesh.SetDestination(targetPos);

                        timer_voice = Time.time + 8f + Random.Range(-3f, 3f);
                    }

                    //回転処理
                    Rotate(transform.position + navMesh.velocity, 1f);

                    //脚のアニメーション
                    FootAnimation();

                    //プレイヤー視認判定
                    UpdateDetectPercent(1);

                    //音声処理
                    MakeVoice(EnemyVoice.Patrol, 8, false,3);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //窓飛び越えが始まった
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    

                    //目標地点に到着
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 0.5f) {
                        ChangeState(State.Patrol_Stop);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f && currentStateTime >= 0.3f) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //プレイヤーの気配を察知
                    if (percent_Detect >= 0.3f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_SmallSensation);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //仲間の死体を発見
                    if (DetectDeadBody()) {
                        voiceSource.Stop();
                        ChangeState(State.Alart_DetectDeadBody);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //警戒態勢に移行
                    if (em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    return;
                }
            //パトロール停止
            case State.Patrol_Stop: {

                    if (stateEnter) {

                    }

                    //回転処理
                    Rotate(transform.position + transform.forward, 0.2f);

                    //脚アニメーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(1);

                    //インジケーター
                    indicator.UpdateIndicator(false);



                    //三秒経過
                    if (currentStateTime >= 3f) {
                        ChangeState(State.Patrol_Walk);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //プレイヤーの気配を察知
                    if (percent_Detect >= 0.3f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_SmallSensation);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //警戒態勢に移行
                    if (em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    return;
                }
            //パトロール中に何かしらの気配を察知
            case State.Patrol_SmallSensation: {
                    if (stateEnter) {
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                        navMesh.SetDestination(transform.position);
                        targetPos = player.transform.position;
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //回転処理
                    Rotate(targetPos, 1f);

                    //脚アニメーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(1);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //3秒経過
                    if (currentStateTime >= 3) {
                        ChangeState(State.Patrol_CheckKnownPos);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //警戒態勢に移行
                    if (em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    return;
                }
            //何かしらの気配を察知してから近づく
            case State.Patrol_CheckKnownPos: {
                    if (stateEnter) {
                        navMesh.SetDestination(targetPos);
                        MakeVoice(EnemyVoice.Sus, 0, true);
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //回転処理
                    Rotate(targetPos, 0.2f);

                    //歩きアニメーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //インジケーター
                    indicator.UpdateIndicator(false);


                    //敵を発見
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //敵を発見した地点に到着
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 1f) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //警戒態勢に移行
                    if (em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    //窓飛び越えが始まった
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    return;
                }
            //銃声を聞いて立ち止まる
            case State.Patrol_DetectShootSound1: {
                    
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//銃レイヤーを無効に
                        enemyAnim.SetState(EnemyAnimation.State.TargetLost);
                        navMesh.SetDestination(transform.position);
                        targetPos = player.playerEnemyTargetT.position;
                    }

                    //脚アニメーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //1秒経過
                    if (currentStateTime >= 1f) {
                        ChangeState(State.Patrol_DetectShootSound2);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }


                    return;
                }
            //銃声だ！（銃声が鳴った方向を向きながら）
            case State.Patrol_DetectShootSound2: {

                    if (stateEnter) {
                        MakeVoice(EnemyVoice.DetectShoot,0,true);
                    }

                    //回転処理
                    Rotate(targetPos, 2);

                    //視認判定
                    UpdateDetectPercent(2);

                    //脚アニメーション
                    FootAnimation();

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //3秒経過
                    if (currentStateTime >= 3f) {
                        em.Notify(targetPos, EnemisManager.NotifyType.DetectPlayerActivity);
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }


                    return;
                }
            //奇襲を受けて狼狽える
            case State.Patrol_GetSurpriseAttack: {
                    if (stateEnter) {
                        enemyAnim.SetState(EnemyAnimation.State.TargetLost);
                        navMesh.SetDestination(transform.position);
                        targetPos = player.playerEnemyTargetT.position;
                        MakeVoice(EnemyVoice.GetSurpriseAttack,0,true);
                        em.Notify(targetPos, EnemisManager.NotifyType.VisualContact);
                    }

                    //回転処理
                    Rotate(targetPos, 2);

                    //脚アニメーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //4秒経過
                    if (currentStateTime >= 4f) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    return;
                }
            #endregion

            #region Convar
            //会話相手を待っている
            case State.Conver_WaitComversationBuddy: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//銃レイヤーを無効に
                        enemyAnim.SetState(EnemyAnimation.State.Stand);
                    }

                    //脚アニメーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(1);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //buddy申請が受理された。buddyが10メートル付近まで近づいてきた。
                    if (buddy != null && Vector3.Distance(buddy.transform.position, transform.position) <= 10f) {
                        ChangeState(State.Conver_Talk);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }

            //会話（相手の話を聞く）
            case State.Conver_Listen: {
                    if (stateEnter) {

                    }

                    //バディーがいる
                    if(buddy != null) {
                        //ナビメッシュ処理
                        UpdateNavMesh(buddy.transform.position, 1.5f, 10f, false);

                        //回転処理
                        if (CanSeeFriend(buddy) >= 0.7f) {
                            Rotate(buddy.eyeT.position, 0.2f);
                        }
                        else {
                            Rotate(transform.position + navMesh.velocity, 0.2f);
                        }
                    }
                        

                    //脚アニメーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(1);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //会話が終わった
                    if (buddy == null) {
                        ChangeState(State.Patrol_Walk);
                        return;
                    }

                    //相手が訊きに転じてる
                    if (buddy.currentState == State.Conver_Listen) {
                        ChangeState(State.Conver_Talk);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }


                    return;
                }
            #endregion

            #region Combat
            //敵と遭遇
            case State.Combat_Encount: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//銃レイヤーを有効に
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                        navMesh.SetDestination(transform.position);//止まる
                        detectPlayer = true;
                    }

                    //回転処理
                    Rotate(player.transform.position, 0.2f);

                    //歩きモーション処理
                    FootAnimation();

                    //インジケーター
                    indicator.UpdateIndicator(true);

                    //チェイスステートに移行
                    if (currentStateTime >= 0.5f) {
                        MakeVoice(EnemyVoice.Encount, 2, true);
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    return;
                }
            //適正な狙撃位置まで敵に近づく
            case State.Combat_Chace: {

                    if (stateEnter) {
                        em.Notify(player.playerEnemyTargetT.position, EnemisManager.NotifyType.VisualContact);
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//銃レイヤーを有効に
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                        buddy = null;
                        indicator.CloseIndicator();
                    }

                    //ナビメッシュ処理
                    UpdateNavMesh(player.playerEnemyTargetT.position, 0f,shootDistance, false);

                    //回転処理
                    Rotate(player.transform.position, 0.2f);

                    //歩きモーション処理
                    FootAnimation();

                    //音声処理
                    MakeVoice(EnemyVoice.Attack,4,false);

                    //プレイヤーを見失う
                    if (PlayerExposePercent() <= 0f) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //敵に十分接近した
                    if (!navMesh.pathPending && navMesh.remainingDistance <= shootDistance) {
                        navMesh.SetDestination(transform.position);
                        ChangeState(State.Combat_Aim);
                        return;
                    }

                    //窓飛び越えが始まった
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }


                    return;
                }
            //撃てる範囲にいる。敵も視認できている。
            case State.Combat_Aim: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//銃レイヤーを有効に
                    }

                    //移動処理
                    UpdateNavMesh(player.playerEnemyTargetT.position, 5f, shootDistance, false);

                    //銃の角度設定
                    UpdateGunAngle(player.playerEnemyTargetT.position);

                    //足のモーション処理
                    FootAnimation();

                    //回転処理
                    Rotate(player.transform.position, 0.2f);

                    //音声処理
                    MakeVoice(EnemyVoice.Attack, 4, false);

                    //距離の調整処理
                    float distance = Vector3.Distance(transform.position, player.transform.position);

                    //敵が視界から外れる。
                    if (PlayerExposePercent() <= 0f && distance > 1.5f) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    //敵が遠すぎる
                    if (distance > shootDistance) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    ChangeState(State.Combat_Shoot);
                    return;
                }
            //撃つ
            case State.Combat_Shoot: {

                    if (stateEnter) {
                        gun.Shoot(player.playerEnemyTargetT);
                    }

                    //移動処理
                    UpdateNavMesh(player.playerEnemyTargetT.position, 5f, shootDistance, false);

                    //回転処理
                    Rotate(player.transform.position, 0.2f);

                    //銃の角度設定
                    UpdateGunAngle(player.playerEnemyTargetT.position);

                    //足のモーション処理
                    FootAnimation();

                    //音声処理
                    MakeVoice(EnemyVoice.Attack, 4, false);

                    //射撃終了判定
                    if (currentStateTime >= gun.shootRate) {
                        ChangeState(State.Combat_Aim);
                        return;
                    }

                    return;
                }
            //プレイヤーを見失ってから1秒間プレイヤーにまっすぐ進む
            case State.Combat_ReachKnownPos1: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//銃レイヤーを有効に
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //ナビメッシュ処理
                    UpdateNavMesh(player.transform.position, 0f, 0.5f, true);

                    //銃の角度設定
                    UpdateGunAngle(player.playerEnemyTargetT.position);

                    //回転処理
                    Rotate(player.transform.position, 0.2f);

                    //歩きアニメーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(10);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //敵を再度発見
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    //1秒経過
                    if (currentStateTime >= 1f) {
                        ChangeState(State.Combat_ReachKnownPos2);
                        return;
                    }

                    //窓飛び越えが始まった
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }


                    return;

                }
            //最後に発見した場所へ移動
            case State.Combat_ReachKnownPos2: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//銃レイヤーを有効に
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //ナビメッシュ処理
                    UpdateNavMesh(em.pos_LastKnown, 0f, 1f, true);

                    //回転処理
                    Rotate(em.pos_LastKnown, 0.2f);

                    //銃の角度設定
                    UpdateGunAngle(em.pos_LastKnown);

                    //歩きアニメーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(10);

                    //音声
                    MakeVoice(EnemyVoice.LostSight, 4, false);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //敵を再度発見
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    //最後に敵を発見した地点に到着したが誰もいなかった
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 1f) {
                        ChangeState(State.Combat_TargetLost);
                        return;
                    }

                    //仲間が最後に敵を発見した地点に到着したが誰もいなかった
                    if(em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                    }

                    //窓飛び越えが始まった
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    return;
                }
            //ターゲットを見失った
            case State.Combat_TargetLost: {

                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//銃レイヤーを無効に
                        enemyAnim.SetState(EnemyAnimation.State.TargetLost);
                        navMesh.SetDestination(transform.position);
                        percent_Detect = 0f;
                    }

                    //声
                    if(!voiceExcuted && currentStateTime >= 1f) {
                        MakeVoice(EnemyVoice.LostSight,0,true);
                        voiceExcuted = true;
                    }

                    //歩きモーション
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //5秒経過
                    if (currentStateTime >= 5) {
                        enemyAnim.SetAimAngle(0);
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        em.Notify(em.pos_LastKnown, EnemisManager.NotifyType.TargetLost);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    return;
                }
            //組技を受ける
            case State.Combat_GetGrab: {

                    if (stateEnter) {
                        navMesh.isStopped = true;
                        navMesh.enabled = false;
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.5f);

                        //ターゲット方向
                        cqcTargetDir = player.transform.position - transform.position;
                        cqcTargetDir.y = 0f;
                        cqcTargetDir.Normalize();

                        //ターゲット位置
                        cqcTargetPos = player.transform.position - cqcTargetDir;
                    }

                    //位置と方向修正
                    float moveTime = 0.3f;
                    if (currentStateTime <= moveTime) {
                        transform.position = Vector3.Lerp(stateEnterPosition, cqcTargetPos, currentStateTime / moveTime);
                        transform.forward = Vector3.Lerp(stateEnterForward, cqcTargetDir, currentStateTime / moveTime);
                    }

                    //0.3f経過→位置と方向を完全に固定
                    if(currentStateTime >= moveTime && !stateFlag1) {
                        stateFlag1 = true;
                        transform.position = cqcTargetPos;
                        transform.forward = cqcTargetDir;
                    }

                    //ラグドール化の時間
                    if (currentStateTime >= currentCQC.ragdollTime + cqcAnimatonStartTime) {
                        ChangeState(State.Dead_RagDoll);
                        return;
                    }


                    return;
                }
            //ダメージを受ける
            case State.Combat_GetDamage: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.2f);
                        navMesh.isStopped = true;
                    }

                    //0.2秒経過・体力ゼロ
                    if (HP <= 0f && currentStateTime >= 0.2f) {
                        ChangeState(State.Dead_RagDoll);
                        return;
                    }

                    //1秒経過
                    if(currentStateTime >= 1) {
                        navMesh.isStopped = false;
                        if (detectPlayer){
                            ChangeState(State.Combat_Chace);
                            return;
                        }
                        else {
                            ChangeState(State.Patrol_GetSurpriseAttack);
                            return;
                        }
                        
                    }

                    return;
                }
            #endregion

            #region Alert
            //死体を発見。立ち止まる
            case State.Alart_DetectDeadBody:{
                    if (stateEnter) {
                        voiceSource.Stop();
                        navMesh.SetDestination(transform.position);
                        targetPos = buddy.eyeT.position;
                    }

                    //回転処理
                    Rotate(buddy.transform.position, 1f);

                    //足のモーション処理
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(1);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //プレイヤーを発見
                    if (percent_Detect >= 1f && currentStateTime >= 0.3f) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //3秒経過
                    if (currentStateTime >= 3f) {
                        ChangeState(State.Alart_HeadingToDeadBody);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
            }
            //死体に向かって進む
            case State.Alart_HeadingToDeadBody: {
                    if (stateEnter) {
                        navMesh.SetDestination(buddy.eyeT.position);
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//銃レイヤーを有効に
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                        MakeVoice(EnemyVoice.DetectDeadBody,0, true);
                    }

                    //回転処理
                    Rotate(buddy.eyeT.position, 1f);

                    //銃の角度処理
                    UpdateGunAngle(buddy.eyeT.position);

                    //足のアニメーション更新
                    FootAnimation();

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //死体の元に到着した
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 1f) {
                        ChangeState(State.Alart_CheckDeadBody);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f && currentStateTime >= 0.3f) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //プレイヤーの気配を察知
                    if (percent_Detect >= 0.3f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_SmallSensation);
                        return;
                    }

                    //窓飛び越えが始まった
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
            }
            //死体の前で立ち止まり、死体を調べる
            case State.Alart_CheckDeadBody: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//銃レイヤーを無効に
                        enemyAnim.SetState(EnemyAnimation.State.TargetLost);
                        MakeVoice(EnemyVoice.CheckDeadBody, 0, true);
                    }

                    //銃の角度処理
                    UpdateGunAngle(buddy.eyeT.position);

                    //回転処理
                    Rotate(buddy.eyeT.position, 1f);

                    //足のアニメーション
                    FootAnimation();

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //3秒経過
                    if (currentStateTime >= 3) {
                        em.Notify(buddy.eyeT.position, EnemisManager.NotifyType.DetectPlayerActivity);
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }
            //警戒しながら歩く
            case State.Alert_Walk: {
                    if (stateEnter) {
                        UpdateGunAngle(eyeT.position + transform.forward);//視線を水平に
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//銃レイヤーを有効に
                        enemyAnim.SetState(EnemyAnimation.State.Walk);//歩きモーション開始

                        if (Vector3.Distance(transform.position, targetPos) <= 0.5f) {
                            targetPos = RandomPosition_Alart();
                        }
                        navMesh.SetDestination(targetPos);
                    }

                    GD.AddRay(targetPos, targetPos + Vector3.up * 2, Color.green);

                    //ランダム方向更新処理
                    UpdateRandomDirection();

                    //回転処理
                    RotateDirection(dir_look, 0.2f);

                    //足のモーション処理
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //音声処理
                    MakeVoice(EnemyVoice.Alart, 4, false);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //目標地点に到着
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 0.5f) {
                        ChangeState(State.Alert_Stop);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //警戒態勢時間切れ
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //窓飛び越えが始まった
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }
            //警戒しながら止まる
            case State.Alert_Stop: {

                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//銃レイヤーを有効に
                        enemyAnim.SetState(EnemyAnimation.State.Stand);
                    }


                    //ランダム方向更新処理
                    UpdateRandomDirection();

                    //回転処理
                    RotateDirection(dir_look, 0.2f);

                    //足のポーズ
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //警戒態勢から10秒経過、まだバディーを組んでいない、バディ―申請可能
                    if (em.currentStateTime >= 20f && em.buddyRequest == null && buddy == null) {
                        em.buddyRequest = this;
                        ChangeState(State.Alart_RequestBuddy);
                        return;
                    }

                    //自分以外でバディー申請をしている敵兵がいる。自分が受理できる。
                    if (em.buddyRequest != null && em.buddyRequest != this && em.buddyRequest.currentState == State.Alart_WaitBuddy && em.buddyRequest.buddy == null && buddy == null) {
                        buddy = em.buddyRequest;
                        buddy.buddy = this;
                        ChangeState(State.Alart_ApproveBuddy);
                        return;
                    }

                    //4秒経過
                    if (currentStateTime >= 4) {
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //警戒態勢時間切れ
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }


                    return;
                }
            //バディー要請
            case State.Alart_RequestBuddy: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                        MakeVoice(EnemyVoice.RequestBuddy, 0f, true);
                    }

                    //ランダム方向更新処理
                    UpdateRandomDirection();

                    //回転処理
                    RotateDirection(dir_look, 0.2f);

                    //足のポーズ
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //5秒経過
                    if (currentStateTime >= 5f) {
                        ChangeState(State.Alart_WaitBuddy);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //警戒態勢時間切れ
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }
            //バディ―待ち
            case State.Alart_WaitBuddy: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                    }

                    //ランダム方向更新処理
                    UpdateRandomDirection();

                    //回転処理
                    RotateDirection(dir_look, 0.2f);

                    //足のポーズ
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //buddy申請が受理された。buddyが2メートル付近まで近づいてきた。
                    if (buddy != null && Vector3.Distance(buddy.transform.position,transform.position) <= 2f ) {
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //警戒態勢時間切れ
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }


                    return;
                }
            //バディーに向かって進む
            case State.Alart_ApproveBuddy: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                        MakeVoice(EnemyVoice.ApproveBuddy, 0f, true);
                    }

                    //ランダム方向更新処理
                    UpdateRandomDirection();

                    //回転処理
                    RotateDirection(dir_look, 0.2f);

                    //足のポーズ
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //5秒経過
                    if (currentStateTime >= 5f) {
                        ChangeState(State.Alart_FollowBuddy);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //警戒態勢時間切れ
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //窓飛び越えが始まった
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }



                    return;
                }
            //バディーについていく
            case State.Alart_FollowBuddy: {
                    if (stateEnter) {
                        UpdateGunAngle(eyeT.position + transform.forward);//視線を水平に
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//銃レイヤーを有効に
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //ナビメッシュ処理
                    UpdateNavMesh(buddy.transform.position,1.5f,2f, false);

                    //ランダム方向更新処理
                    UpdateRandomDirection();

                    //回転処理
                    RotateDirection(dir_look, 0.2f);

                    //足のポーズ
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(2);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //5秒経過
                    if (currentStateTime >= 5f) {
                        ChangeState(State.Alart_FollowBuddy);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //警戒態勢時間切れ
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //窓飛び越えが始まった
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }
            //捜索を諦める
            case State.Alart_GiveUp: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//銃レイヤーを無効に
                        enemyAnim.SetState(EnemyAnimation.State.GaveUp);
                        navMesh.SetDestination(transform.position);
                        percent_Detect = 0f;
                        detectPlayer = false;
                        buddy = null;
                    }


                    //足の動き
                    FootAnimation();

                    //視認判定
                    UpdateDetectPercent(1);

                    //インジケーター
                    indicator.UpdateIndicator(false);

                    //声を一回出す
                    if (currentStateTime >= 1.0f && !voiceExcuted) {
                        voiceExcuted = true;
                        MakeVoice(EnemyVoice.Alart_GaveUp, 0, true);
                    }

                    //4秒経過
                    if (currentStateTime >= 4) {
                        ChangeState(State.Patrol_Walk);
                        return;
                    }

                    //プレイヤーを発見
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //戦闘態勢に移行
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //銃声を感知
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;

                }
            #endregion

            //ラグドール
            case State.Dead_RagDoll: {
                    if (stateEnter) {
                        if(currentWindowEdge != null) {
                            currentWindowEdge.ReleaseTarget(this);
                            currentWindowEdge = null;
                        }

                        enemyAnim.RagdollActive(true);
                        StopVoice();
                        em.SetEnemyDead(this);
                        indicator.CloseIndicator();
                    }

                    return;
                }

        }




    }

    private void LateUpdate() {

        if(currentStateTime != 0f) {
            stateEnter = false;
        }

    }

    public void SetCQCAnimState() {
        enemyAnim.SetState(EnemyAnimation.State.GetGrab);
        cqcAnimatonStartTime = currentStateTime;
    }

    public void SetCQCAnimation(CQCData data) {
        enemyAnim.SetCQCAnimation(data);
        currentCQC = data;
    }

    public bool IsInTransition() {
        return enemyAnim.IsInTransition();
    }

    public void TakeDamage(float damage, Vector3 hitPos, Vector3 hitDirection , BodyPart part) {

        if(HP < 0) {
            return;
        }

        hitSource.clip = hitSound;
        hitSource.Play();

        HP -= damage;
        switch (part) {
            case BodyPart.Chest:
            case BodyPart.Hip:
            case BodyPart.Head: {
                    enemyAnim.SetState(EnemyAnimation.State.Damage_Chest);
                    break;
                }

            case BodyPart.ArmUpper_L:
            case BodyPart.ArmLower_L: {
                    enemyAnim.SetState(EnemyAnimation.State.Damage_ARM_L);
                    break;
                }

            case BodyPart.ArmUpper_R:
            case BodyPart.ArmLower_R: {
                    enemyAnim.SetState(EnemyAnimation.State.Damage_ARM_R);
                    break;
                }

            case BodyPart.LegUpper_L:
            case BodyPart.LegLower_L: {
                    enemyAnim.SetState(EnemyAnimation.State.Damage_LEG_L);
                    break;
                }

            case BodyPart.LegUpper_R:
            case BodyPart.LegLower_R: {
                    enemyAnim.SetState(EnemyAnimation.State.Damage_LEG_R);
                    break;
                }
        }

        ChangeState(State.Combat_GetDamage);
        return;
    }

    void MakeVoice(EnemyVoice enemyVoice, float timer,bool immediately,float randomRange = 0f) {

        if (timer_voice <= Time.time || immediately) {

            VoiceList list = voiceCorrection.Find(a => a.voiceType == enemyVoice);

            if (list != null) {
                voiceSource.clip = list.voices[Random.Range(0, list.voices.Count)];
                voiceSource.Play();
            }

            timer_voice = Time.time + timer + Random.Range(-randomRange,randomRange);
        }
    }

    void StopVoice() {
        voiceSource.clip = null;
        voiceSource.Stop();
    }

    bool DetectDeadBody() {
        if(em.deadEnemyList.Count == 0) {
            return false;
        }

        foreach(EnemyController eneCon in em.deadEnemyList) {

            if(CanSeeFriend(eneCon) >= 0.7f) {
                buddy = eneCon;
                return true;
            }

        }


        return false;
    }


    void UpdateNavMesh(Vector3 targetPos,float min,float max, bool immediatly) {

        float distance = Vector3.Distance(transform.position, targetPos);

        if (Time.time >= timer_navMeshUpdate || immediatly) {
            //if (debug) Debug.Log("更新");

            if (distance > max) {
                //if (debug) Debug.Log("ターゲットから離れすぎ:" + distance);
                navMesh.SetDestination(targetPos);

                return;
            }
            else
            if(distance <= max && distance >= min) {
                //if (debug) Debug.Log("適正な距離:"+distance);
                navMesh.SetDestination(transform.position);
            }
            else {
                //if (debug) Debug.Log("近すぎ:" + distance);
                Vector3 direction = transform.position - targetPos;
                direction = new Vector3(direction.x, 0, direction.z).normalized;
                navMesh.SetDestination(transform.position + direction);
            }

            timer_navMeshUpdate = Time.time + interval_navMeshUpdate;
        }


    }

    void UpdateRandomDirection() {
        if (timer_LookDirectionUpdate <= Time.time) {
            timer_LookDirectionUpdate = Time.time + interval_lookDirectionUpdate;

            //Vector3 lookPos;
            //if (Random.Range(0, 2) == 1) {
            //    lookPos = transform.position + transform.right * 2;
            //}
            //else {
            //    lookPos = transform.position - transform.right * 2;
            //}

            //pos_look = RandomPosition();

            dir_look = transform.position - RandomPosition();
        }
    }

    //60＊60マスのランダムな位置生成
    Vector3 RandomPosition() {
        return new Vector3(Random.Range(-60f, 60f), 0, Random.Range(-60f, 60f));
    }

    //中心とその半径を引数にランダムな位置生成
    Vector3 RandomPosition(Vector3 pos_center, float radius) {
        //float distance = Random.Range(1f, radius);
        return pos_center + Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward * radius;
    }

    //探索範囲をランダムに策定
    Vector3 RandomPosition_Alart() {
        return new Vector3(Random.Range(em.alartArea_rec_A.x, em.alartArea_rec_B.x), 0, Random.Range(em.alartArea_rec_A.z, em.alartArea_rec_B.z));
    }


    //特定の位置を向く
    void Rotate(Vector3 target, float time) {

        //方向計算
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        direction = direction.normalized;

        GD.AddRay(transform.position, transform.position + direction  *2, Color.red);

        float targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, time);

    }　

    //特定の方向を向く※位置ではなく方向
    void RotateDirection(Vector3 direction, float time) {

        float targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, time);

    } 

    //銃の角度アップデート
    void UpdateGunAngle(Vector3 targetPos) {
        float angle = Vector3.Angle(Vector3.up, (targetPos - eyeT.position));
        enemyAnim.SetAimAngle(angle - 90);
    }

    //脚のアニメーションのアップデート
    void FootAnimation() {
        Vector3 finalVelocity = Quaternion.Euler(0, -transform.eulerAngles.y, 0) * navMesh.velocity / navMesh.speed;
        enemyAnim.SetMoveSpeed(finalVelocity.z, finalVelocity.x);
    }

    //プレイヤーの視認度アップデート
    void UpdateDetectPercent(float speed) {

        switch (currentState) {

            default: {
                    float exposePer = PlayerExposePercent();

                    if (exposePer != 0f) {
                        percent_Detect += exposePer * (Time.deltaTime / detectLag) * speed;

                    }
                    else {
                        percent_Detect -= Time.deltaTime;
                    }

                    percent_Detect = Mathf.Clamp01(percent_Detect);

                    return;
                }

        }

    }

    float PlayerExposePercent() {

        float detectPercent = 0f;

        foreach(HitBox hitBox in player.hitBoxController.list_HitBox) {
            float distance = Vector3.Distance(eyeT.position, hitBox.transform.position);
            if (distance < dis_eye) {
                Vector3 dirToHitBox = (hitBox.transform.position - eyeT.position).normalized;
                float angleBetweenEnemyAndHitBox = Vector3.Angle(eyeT.forward, dirToHitBox);
                if (angleBetweenEnemyAndHitBox <= ang_eye / 2) {
                    RaycastHit hit;
                    Ray ray = new Ray(eyeT.position, dirToHitBox);
                    if (Physics.Raycast(ray, out hit, dis_eye,visibleLayer)) {
                        if ((targetLayer.value & (1 << hit.collider.transform.gameObject.layer)) > 0) {
                            detectPercent += hitBox.detectIncrease * (1 - distance / dis_eye) * ((1 - squatPenalty) + squatPenalty * (1 - player.squatPercent)) * (1 - angleBetweenEnemyAndHitBox/(ang_eye/2));
                            //GD.AddRay(eyeT.position, hit.point, Color.green);
                        }
                    }
                }
                else {
                    //GD.AddRay(eyeT.position, hitBox.transform.position, Color.yellow);
                }

            }
            else {
                //GD.AddRay(eyeT.position, hitBox.transform.position, Color.red);
            }


        }
        return Mathf.Clamp01(detectPercent);
    }//プレイヤーの視界占有率計算

    public float CanSeeFriend(EnemyController friendCon) {

        float detectPercent = 0f;

        foreach(HitBox hitBox in friendCon.hitBoxCon.list_HitBox) {
            float distance = Vector3.Distance(eyeT.position, hitBox.transform.position);
            if (distance < dis_eye) {
                Vector3 dirHitBox = (hitBox.transform.position - eyeT.position).normalized;
                float angleBetweenEnemyAndHitBox = Vector3.Angle(eyeT.forward, dirHitBox);
                if (angleBetweenEnemyAndHitBox <= ang_eye / 2) {
                    RaycastHit hit;
                    Ray ray = new Ray(eyeT.position, dirHitBox);
                    if (Physics.Raycast(ray, out hit, dis_eye, visibleLayer)) {
                        if ((friendLayer.value & (1 << hit.collider.transform.gameObject.layer)) > 0) {
                            detectPercent += hitBox.detectIncrease * (1 - distance / dis_eye) * ((1 - squatPenalty) + squatPenalty * (1 - player.squatPercent)) * (1 - angleBetweenEnemyAndHitBox / (ang_eye / 2));
                            GD.AddRay(eyeT.position, hit.point, Color.green);
                        }
                    }
                }
            }
            else {
                return detectPercent;
            }
        }

        return detectPercent;

    }//仲間が見えているか判定

    float PlayerFootStepExposePercent() {
        float distance = Vector3.Distance(eyeT.position, player.transform.position);
        return player.noise_Foot / distance;
    }//プレイヤーの足音検知率

    float PlayerShootSoundExposePercent() {
        float distance = Vector3.Distance(eyeT.position, player.transform.position);
        return player.noise_Gun / distance;
    }//プレイヤーの銃声検知率

    bool CheckWindowEdge() {
        if (navMesh.isOnOffMeshLink) {
            //窓に到着
            navMesh.isStopped = true;
            currentWindowEdge = navMesh.currentOffMeshLinkData.offMeshLink.GetComponent<WindowEdge>();
            eventOffsetTargetPos = navMesh.currentOffMeshLinkData.startPos;
            eventOffsetTargetDir = navMesh.currentOffMeshLinkData.endPos - eventOffsetTargetPos;
            if (currentWindowEdge.targetEnemyController == null) {
                currentWindowEdge.SetTarget(this);
                navMesh.Warp(transform.position);//これで位置を整える
                return true;
            }
            else {
                navMesh.SetDestination(targetPos);
                return false;
            }
        }

        return false;
    }


    private void OnDrawGizmos() {
        if(GD != null) {
            GD.Execute();
        }
    }



}
