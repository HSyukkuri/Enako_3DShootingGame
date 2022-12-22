using System.Collections.Generic;
using UnityEngine;
using MyGizmoTool;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, ITakeDamage
    {
    public bool debug = false;

    //�̗�
    float HP = 1f;

    //�ړ�
    const float interval_navMeshUpdate = 0.1f;
    float timer_navMeshUpdate;
    float turnSmoothVelocity;
    public Vector3 eventOffsetTargetPos;
    public Vector3 eventOffsetTargetDir;
    Vector3 targetPos;


    //���E
    const float dis_eye = 40f;
    const float ang_eye = 160f;
    const float detectLag = 0.5f;
    public float percent_Detect { get; private set; } = 0f;
    const float squatPenalty = 0.5f;
    bool detectPlayer = false;

    //�퓬
    const float shootDistance = 30f;

    //�x�����[�h
    Vector3 pos_look = Vector3.zero;
    Vector3 dir_look = Vector3.zero;
    float timer_LookDirectionUpdate;
    const float interval_lookDirectionUpdate = 3;

    //���C���[
    public LayerMask targetLayer;
    public LayerMask visibleLayer;
    public LayerMask friendLayer;

    //��
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

    [Header("����")]
    public List<AudioClip> footStepSoundList = new List<AudioClip>();
    public void FootStepUpdate() {
        AudioManager.instance.PlaySound(footStepSoundList[Random.Range(0, footStepSoundList.Count)], transform.position, 2);
    }

    //�Q��
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


    //�X�e�[�g�֘A
    public State priviousState { get; private set; } = State.Patrol_Walk;
    public State currentState { get; private set; } = State.Patrol_Walk;
    float currentStateTime = 0f;
    Vector3 stateEnterPosition = Vector3.zero;
    Vector3 stateEnterForward = Vector3.zero;
    bool stateEnter = false;
    bool stateFlag1 = false;

    //�A�g
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
            Debug.Log($"{priviousState.ToString()}�@�ˁ@{currentState.ToString()}");
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
        Patrol_DetectShootSound1,//�����~�܂�
        Patrol_DetectShootSound2,//�e�����I�i�e�������������������Ȃ���j
        Patrol_CheckKnownPos,
        Patrol_BeAttacked,
        Patrol_GetSurpriseAttack,//�U�����󂯂��I

        Conver_WaitComversationBuddy,//��b�v��
        Conver_Talk,//�b��
        Conver_Listen,//����

        Combat_Encount,
        Combat_Chace,
        Combat_Shoot,
        Combat_ReachKnownPos1,//����̈ʒu�܂ňړ�
        Combat_ReachKnownPos2,//����̍Ō�Ɍ����ʒu�܂ňړ�
        Combat_Aim,
        Combat_TargetLost,//�u�G�������������v
        Combat_GetGrab,//�ߐڍU�����󂯂�
        Combat_GetDamage,//�_���[�W������

        Alert_Walk,
        Alert_Stop,
        Alart_GiveUp,
        Alart_DetectDeadBody,//���̂�������
        Alart_HeadingToDeadBody,//���̂Ɍ�����
        Alart_CheckDeadBody,//���̂𒲂ׂ�

        Alart_RequestBuddy,//�T���ɒ��Ԃ��Ă�
        Alart_WaitBuddy,�@//���Ԃ�҂�
        Alart_ApproveBuddy,//�T���˗��ɗ�������
        Alart_FollowBuddy,//���ԂɒǏ]����


        Dead_RagDoll,//���O�h�[��

        System_Initialize,//������
        System_Jump
    }

    private void Update() {



        currentStateTime += Time.deltaTime;

        //�M�Y������
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

            //������
            case State.System_Initialize: {

                    //�G���}�l�[�W�����Q��
                    if(em == null) {
                        em = GameManager.instance.enemyManager;
                        if(em == null) {
                            //Debug.LogError("�G���}�l�[�W�����Q�Ƃł��Ȃ������I");
                            return;
                        }
                    }

                    //�G���}�l�[�W�������������Ȃ�ҋ@
                    if(em.currentState == EnemisManager.State.Initialize) {
                        return;
                    }

                    //���g��G���}�l�[�W���ɓo�^
                    em.enemyList.Add(this);

                    //�C���W�P�[�^�[��A�g
                    indicator = em.GetIndicator();
                    indicator.SetEnemyController(this);

                    //���[�_�[�|�C���^��A�g
                    raderPointer = em.GetRaderPointer();
                    raderPointer.SetEnemyController(this);

                    //�^�[�Q�b�g�ړ��n�_��ݒ�
                    targetPos = RandomPosition();

                    //Walk�X�e�[�g�Ɉڍs
                    ChangeState(State.Patrol_Walk);
                    return;
                }
            //����щz��
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

            //�p�g���[��
            case State.Patrol_Walk: {

                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//�e���C���[�𖳌���
                        enemyAnim.SetState(EnemyAnimation.State.Walk);

                        if(Vector3.Distance(transform.position,targetPos) <= 0.5f) {
                            targetPos = RandomPosition();
                        }
                        navMesh.SetDestination(targetPos);

                        timer_voice = Time.time + 8f + Random.Range(-3f, 3f);
                    }

                    //��]����
                    Rotate(transform.position + navMesh.velocity, 1f);

                    //�r�̃A�j���[�V����
                    FootAnimation();

                    //�v���C���[���F����
                    UpdateDetectPercent(1);

                    //��������
                    MakeVoice(EnemyVoice.Patrol, 8, false,3);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //����щz�����n�܂���
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    

                    //�ڕW�n�_�ɓ���
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 0.5f) {
                        ChangeState(State.Patrol_Stop);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f && currentStateTime >= 0.3f) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�v���C���[�̋C�z���@�m
                    if (percent_Detect >= 0.3f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_SmallSensation);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //���Ԃ̎��̂𔭌�
                    if (DetectDeadBody()) {
                        voiceSource.Stop();
                        ChangeState(State.Alart_DetectDeadBody);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�x���Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    return;
                }
            //�p�g���[����~
            case State.Patrol_Stop: {

                    if (stateEnter) {

                    }

                    //��]����
                    Rotate(transform.position + transform.forward, 0.2f);

                    //�r�A�j���[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(1);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);



                    //�O�b�o��
                    if (currentStateTime >= 3f) {
                        ChangeState(State.Patrol_Walk);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�v���C���[�̋C�z���@�m
                    if (percent_Detect >= 0.3f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_SmallSensation);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�x���Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    return;
                }
            //�p�g���[�����ɉ�������̋C�z���@�m
            case State.Patrol_SmallSensation: {
                    if (stateEnter) {
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                        navMesh.SetDestination(transform.position);
                        targetPos = player.transform.position;
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //��]����
                    Rotate(targetPos, 1f);

                    //�r�A�j���[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(1);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //3�b�o��
                    if (currentStateTime >= 3) {
                        ChangeState(State.Patrol_CheckKnownPos);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�x���Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    return;
                }
            //��������̋C�z���@�m���Ă���߂Â�
            case State.Patrol_CheckKnownPos: {
                    if (stateEnter) {
                        navMesh.SetDestination(targetPos);
                        MakeVoice(EnemyVoice.Sus, 0, true);
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //��]����
                    Rotate(targetPos, 0.2f);

                    //�����A�j���[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);


                    //�G�𔭌�
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�G�𔭌������n�_�ɓ���
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 1f) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�x���Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    //����щz�����n�܂���
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    return;
                }
            //�e���𕷂��ė����~�܂�
            case State.Patrol_DetectShootSound1: {
                    
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//�e���C���[�𖳌���
                        enemyAnim.SetState(EnemyAnimation.State.TargetLost);
                        navMesh.SetDestination(transform.position);
                        targetPos = player.playerEnemyTargetT.position;
                    }

                    //�r�A�j���[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //1�b�o��
                    if (currentStateTime >= 1f) {
                        ChangeState(State.Patrol_DetectShootSound2);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }


                    return;
                }
            //�e�����I�i�e�������������������Ȃ���j
            case State.Patrol_DetectShootSound2: {

                    if (stateEnter) {
                        MakeVoice(EnemyVoice.DetectShoot,0,true);
                    }

                    //��]����
                    Rotate(targetPos, 2);

                    //���F����
                    UpdateDetectPercent(2);

                    //�r�A�j���[�V����
                    FootAnimation();

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //3�b�o��
                    if (currentStateTime >= 3f) {
                        em.Notify(targetPos, EnemisManager.NotifyType.DetectPlayerActivity);
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }


                    return;
                }
            //��P���󂯂ĘT������
            case State.Patrol_GetSurpriseAttack: {
                    if (stateEnter) {
                        enemyAnim.SetState(EnemyAnimation.State.TargetLost);
                        navMesh.SetDestination(transform.position);
                        targetPos = player.playerEnemyTargetT.position;
                        MakeVoice(EnemyVoice.GetSurpriseAttack,0,true);
                        em.Notify(targetPos, EnemisManager.NotifyType.VisualContact);
                    }

                    //��]����
                    Rotate(targetPos, 2);

                    //�r�A�j���[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //4�b�o��
                    if (currentStateTime >= 4f) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    return;
                }
            #endregion

            #region Convar
            //��b�����҂��Ă���
            case State.Conver_WaitComversationBuddy: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//�e���C���[�𖳌���
                        enemyAnim.SetState(EnemyAnimation.State.Stand);
                    }

                    //�r�A�j���[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(1);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //buddy�\�����󗝂��ꂽ�Bbuddy��10���[�g���t�߂܂ŋ߂Â��Ă����B
                    if (buddy != null && Vector3.Distance(buddy.transform.position, transform.position) <= 10f) {
                        ChangeState(State.Conver_Talk);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }

            //��b�i����̘b�𕷂��j
            case State.Conver_Listen: {
                    if (stateEnter) {

                    }

                    //�o�f�B�[������
                    if(buddy != null) {
                        //�i�r���b�V������
                        UpdateNavMesh(buddy.transform.position, 1.5f, 10f, false);

                        //��]����
                        if (CanSeeFriend(buddy) >= 0.7f) {
                            Rotate(buddy.eyeT.position, 0.2f);
                        }
                        else {
                            Rotate(transform.position + navMesh.velocity, 0.2f);
                        }
                    }
                        

                    //�r�A�j���[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(1);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //��b���I�����
                    if (buddy == null) {
                        ChangeState(State.Patrol_Walk);
                        return;
                    }

                    //���肪�u���ɓ]���Ă�
                    if (buddy.currentState == State.Conver_Listen) {
                        ChangeState(State.Conver_Talk);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }


                    return;
                }
            #endregion

            #region Combat
            //�G�Ƒ���
            case State.Combat_Encount: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//�e���C���[��L����
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                        navMesh.SetDestination(transform.position);//�~�܂�
                        detectPlayer = true;
                    }

                    //��]����
                    Rotate(player.transform.position, 0.2f);

                    //�������[�V��������
                    FootAnimation();

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(true);

                    //�`�F�C�X�X�e�[�g�Ɉڍs
                    if (currentStateTime >= 0.5f) {
                        MakeVoice(EnemyVoice.Encount, 2, true);
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    return;
                }
            //�K���ȑ_���ʒu�܂œG�ɋ߂Â�
            case State.Combat_Chace: {

                    if (stateEnter) {
                        em.Notify(player.playerEnemyTargetT.position, EnemisManager.NotifyType.VisualContact);
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//�e���C���[��L����
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                        buddy = null;
                        indicator.CloseIndicator();
                    }

                    //�i�r���b�V������
                    UpdateNavMesh(player.playerEnemyTargetT.position, 0f,shootDistance, false);

                    //��]����
                    Rotate(player.transform.position, 0.2f);

                    //�������[�V��������
                    FootAnimation();

                    //��������
                    MakeVoice(EnemyVoice.Attack,4,false);

                    //�v���C���[��������
                    if (PlayerExposePercent() <= 0f) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�G�ɏ\���ڋ߂���
                    if (!navMesh.pathPending && navMesh.remainingDistance <= shootDistance) {
                        navMesh.SetDestination(transform.position);
                        ChangeState(State.Combat_Aim);
                        return;
                    }

                    //����щz�����n�܂���
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }


                    return;
                }
            //���Ă�͈͂ɂ���B�G�����F�ł��Ă���B
            case State.Combat_Aim: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//�e���C���[��L����
                    }

                    //�ړ�����
                    UpdateNavMesh(player.playerEnemyTargetT.position, 5f, shootDistance, false);

                    //�e�̊p�x�ݒ�
                    UpdateGunAngle(player.playerEnemyTargetT.position);

                    //���̃��[�V��������
                    FootAnimation();

                    //��]����
                    Rotate(player.transform.position, 0.2f);

                    //��������
                    MakeVoice(EnemyVoice.Attack, 4, false);

                    //�����̒�������
                    float distance = Vector3.Distance(transform.position, player.transform.position);

                    //�G�����E����O���B
                    if (PlayerExposePercent() <= 0f && distance > 1.5f) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    //�G����������
                    if (distance > shootDistance) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    ChangeState(State.Combat_Shoot);
                    return;
                }
            //����
            case State.Combat_Shoot: {

                    if (stateEnter) {
                        gun.Shoot(player.playerEnemyTargetT);
                    }

                    //�ړ�����
                    UpdateNavMesh(player.playerEnemyTargetT.position, 5f, shootDistance, false);

                    //��]����
                    Rotate(player.transform.position, 0.2f);

                    //�e�̊p�x�ݒ�
                    UpdateGunAngle(player.playerEnemyTargetT.position);

                    //���̃��[�V��������
                    FootAnimation();

                    //��������
                    MakeVoice(EnemyVoice.Attack, 4, false);

                    //�ˌ��I������
                    if (currentStateTime >= gun.shootRate) {
                        ChangeState(State.Combat_Aim);
                        return;
                    }

                    return;
                }
            //�v���C���[���������Ă���1�b�ԃv���C���[�ɂ܂������i��
            case State.Combat_ReachKnownPos1: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//�e���C���[��L����
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //�i�r���b�V������
                    UpdateNavMesh(player.transform.position, 0f, 0.5f, true);

                    //�e�̊p�x�ݒ�
                    UpdateGunAngle(player.playerEnemyTargetT.position);

                    //��]����
                    Rotate(player.transform.position, 0.2f);

                    //�����A�j���[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(10);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //�G���ēx����
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    //1�b�o��
                    if (currentStateTime >= 1f) {
                        ChangeState(State.Combat_ReachKnownPos2);
                        return;
                    }

                    //����щz�����n�܂���
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }


                    return;

                }
            //�Ō�ɔ��������ꏊ�ֈړ�
            case State.Combat_ReachKnownPos2: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//�e���C���[��L����
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //�i�r���b�V������
                    UpdateNavMesh(em.pos_LastKnown, 0f, 1f, true);

                    //��]����
                    Rotate(em.pos_LastKnown, 0.2f);

                    //�e�̊p�x�ݒ�
                    UpdateGunAngle(em.pos_LastKnown);

                    //�����A�j���[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(10);

                    //����
                    MakeVoice(EnemyVoice.LostSight, 4, false);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //�G���ēx����
                    if (percent_Detect >= 1f) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    //�Ō�ɓG�𔭌������n�_�ɓ����������N�����Ȃ�����
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 1f) {
                        ChangeState(State.Combat_TargetLost);
                        return;
                    }

                    //���Ԃ��Ō�ɓG�𔭌������n�_�ɓ����������N�����Ȃ�����
                    if(em.currentState == EnemisManager.State.Alart) {
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                    }

                    //����щz�����n�܂���
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    return;
                }
            //�^�[�Q�b�g����������
            case State.Combat_TargetLost: {

                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//�e���C���[�𖳌���
                        enemyAnim.SetState(EnemyAnimation.State.TargetLost);
                        navMesh.SetDestination(transform.position);
                        percent_Detect = 0f;
                    }

                    //��
                    if(!voiceExcuted && currentStateTime >= 1f) {
                        MakeVoice(EnemyVoice.LostSight,0,true);
                        voiceExcuted = true;
                    }

                    //�������[�V����
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //5�b�o��
                    if (currentStateTime >= 5) {
                        enemyAnim.SetAimAngle(0);
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        em.Notify(em.pos_LastKnown, EnemisManager.NotifyType.TargetLost);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        ChangeState(State.Combat_Chace);
                        return;
                    }

                    return;
                }
            //�g�Z���󂯂�
            case State.Combat_GetGrab: {

                    if (stateEnter) {
                        navMesh.isStopped = true;
                        navMesh.enabled = false;
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.5f);

                        //�^�[�Q�b�g����
                        cqcTargetDir = player.transform.position - transform.position;
                        cqcTargetDir.y = 0f;
                        cqcTargetDir.Normalize();

                        //�^�[�Q�b�g�ʒu
                        cqcTargetPos = player.transform.position - cqcTargetDir;
                    }

                    //�ʒu�ƕ����C��
                    float moveTime = 0.3f;
                    if (currentStateTime <= moveTime) {
                        transform.position = Vector3.Lerp(stateEnterPosition, cqcTargetPos, currentStateTime / moveTime);
                        transform.forward = Vector3.Lerp(stateEnterForward, cqcTargetDir, currentStateTime / moveTime);
                    }

                    //0.3f�o�߁��ʒu�ƕ��������S�ɌŒ�
                    if(currentStateTime >= moveTime && !stateFlag1) {
                        stateFlag1 = true;
                        transform.position = cqcTargetPos;
                        transform.forward = cqcTargetDir;
                    }

                    //���O�h�[�����̎���
                    if (currentStateTime >= currentCQC.ragdollTime + cqcAnimatonStartTime) {
                        ChangeState(State.Dead_RagDoll);
                        return;
                    }


                    return;
                }
            //�_���[�W���󂯂�
            case State.Combat_GetDamage: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.2f);
                        navMesh.isStopped = true;
                    }

                    //0.2�b�o�߁E�̗̓[��
                    if (HP <= 0f && currentStateTime >= 0.2f) {
                        ChangeState(State.Dead_RagDoll);
                        return;
                    }

                    //1�b�o��
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
            //���̂𔭌��B�����~�܂�
            case State.Alart_DetectDeadBody:{
                    if (stateEnter) {
                        voiceSource.Stop();
                        navMesh.SetDestination(transform.position);
                        targetPos = buddy.eyeT.position;
                    }

                    //��]����
                    Rotate(buddy.transform.position, 1f);

                    //���̃��[�V��������
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(1);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f && currentStateTime >= 0.3f) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //3�b�o��
                    if (currentStateTime >= 3f) {
                        ChangeState(State.Alart_HeadingToDeadBody);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
            }
            //���̂Ɍ������Đi��
            case State.Alart_HeadingToDeadBody: {
                    if (stateEnter) {
                        navMesh.SetDestination(buddy.eyeT.position);
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//�e���C���[��L����
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                        MakeVoice(EnemyVoice.DetectDeadBody,0, true);
                    }

                    //��]����
                    Rotate(buddy.eyeT.position, 1f);

                    //�e�̊p�x����
                    UpdateGunAngle(buddy.eyeT.position);

                    //���̃A�j���[�V�����X�V
                    FootAnimation();

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //���̂̌��ɓ�������
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 1f) {
                        ChangeState(State.Alart_CheckDeadBody);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f && currentStateTime >= 0.3f) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�v���C���[�̋C�z���@�m
                    if (percent_Detect >= 0.3f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_SmallSensation);
                        return;
                    }

                    //����щz�����n�܂���
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
            }
            //���̂̑O�ŗ����~�܂�A���̂𒲂ׂ�
            case State.Alart_CheckDeadBody: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//�e���C���[�𖳌���
                        enemyAnim.SetState(EnemyAnimation.State.TargetLost);
                        MakeVoice(EnemyVoice.CheckDeadBody, 0, true);
                    }

                    //�e�̊p�x����
                    UpdateGunAngle(buddy.eyeT.position);

                    //��]����
                    Rotate(buddy.eyeT.position, 1f);

                    //���̃A�j���[�V����
                    FootAnimation();

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //3�b�o��
                    if (currentStateTime >= 3) {
                        em.Notify(buddy.eyeT.position, EnemisManager.NotifyType.DetectPlayerActivity);
                        targetPos = RandomPosition_Alart();
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }
            //�x�����Ȃ������
            case State.Alert_Walk: {
                    if (stateEnter) {
                        UpdateGunAngle(eyeT.position + transform.forward);//�����𐅕���
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//�e���C���[��L����
                        enemyAnim.SetState(EnemyAnimation.State.Walk);//�������[�V�����J�n

                        if (Vector3.Distance(transform.position, targetPos) <= 0.5f) {
                            targetPos = RandomPosition_Alart();
                        }
                        navMesh.SetDestination(targetPos);
                    }

                    GD.AddRay(targetPos, targetPos + Vector3.up * 2, Color.green);

                    //�����_�������X�V����
                    UpdateRandomDirection();

                    //��]����
                    RotateDirection(dir_look, 0.2f);

                    //���̃��[�V��������
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //��������
                    MakeVoice(EnemyVoice.Alart, 4, false);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //�ڕW�n�_�ɓ���
                    if (!navMesh.pathPending && navMesh.remainingDistance <= 0.5f) {
                        ChangeState(State.Alert_Stop);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //�x���Ԑ����Ԑ؂�
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //����щz�����n�܂���
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }
            //�x�����Ȃ���~�܂�
            case State.Alert_Stop: {

                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//�e���C���[��L����
                        enemyAnim.SetState(EnemyAnimation.State.Stand);
                    }


                    //�����_�������X�V����
                    UpdateRandomDirection();

                    //��]����
                    RotateDirection(dir_look, 0.2f);

                    //���̃|�[�Y
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    //�x���Ԑ�����10�b�o�߁A�܂��o�f�B�[��g��ł��Ȃ��A�o�f�B�\�\���\
                    if (em.currentStateTime >= 20f && em.buddyRequest == null && buddy == null) {
                        em.buddyRequest = this;
                        ChangeState(State.Alart_RequestBuddy);
                        return;
                    }

                    //�����ȊO�Ńo�f�B�[�\�������Ă���G��������B�������󗝂ł���B
                    if (em.buddyRequest != null && em.buddyRequest != this && em.buddyRequest.currentState == State.Alart_WaitBuddy && em.buddyRequest.buddy == null && buddy == null) {
                        buddy = em.buddyRequest;
                        buddy.buddy = this;
                        ChangeState(State.Alart_ApproveBuddy);
                        return;
                    }

                    //4�b�o��
                    if (currentStateTime >= 4) {
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�x���Ԑ����Ԑ؂�
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }


                    return;
                }
            //�o�f�B�[�v��
            case State.Alart_RequestBuddy: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                        MakeVoice(EnemyVoice.RequestBuddy, 0f, true);
                    }

                    //�����_�������X�V����
                    UpdateRandomDirection();

                    //��]����
                    RotateDirection(dir_look, 0.2f);

                    //���̃|�[�Y
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //5�b�o��
                    if (currentStateTime >= 5f) {
                        ChangeState(State.Alart_WaitBuddy);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�x���Ԑ����Ԑ؂�
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }
            //�o�f�B�\�҂�
            case State.Alart_WaitBuddy: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                    }

                    //�����_�������X�V����
                    UpdateRandomDirection();

                    //��]����
                    RotateDirection(dir_look, 0.2f);

                    //���̃|�[�Y
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //buddy�\�����󗝂��ꂽ�Bbuddy��2���[�g���t�߂܂ŋ߂Â��Ă����B
                    if (buddy != null && Vector3.Distance(buddy.transform.position,transform.position) <= 2f ) {
                        ChangeState(State.Alert_Walk);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�x���Ԑ����Ԑ؂�
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }


                    return;
                }
            //�o�f�B�[�Ɍ������Đi��
            case State.Alart_ApproveBuddy: {
                    if (stateEnter) {
                        navMesh.SetDestination(transform.position);
                        MakeVoice(EnemyVoice.ApproveBuddy, 0f, true);
                    }

                    //�����_�������X�V����
                    UpdateRandomDirection();

                    //��]����
                    RotateDirection(dir_look, 0.2f);

                    //���̃|�[�Y
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //5�b�o��
                    if (currentStateTime >= 5f) {
                        ChangeState(State.Alart_FollowBuddy);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�x���Ԑ����Ԑ؂�
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //����щz�����n�܂���
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }



                    return;
                }
            //�o�f�B�[�ɂ��Ă���
            case State.Alart_FollowBuddy: {
                    if (stateEnter) {
                        UpdateGunAngle(eyeT.position + transform.forward);//�����𐅕���
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 1f, 0.3f);//�e���C���[��L����
                        enemyAnim.SetState(EnemyAnimation.State.Walk);
                    }

                    //�i�r���b�V������
                    UpdateNavMesh(buddy.transform.position,1.5f,2f, false);

                    //�����_�������X�V����
                    UpdateRandomDirection();

                    //��]����
                    RotateDirection(dir_look, 0.2f);

                    //���̃|�[�Y
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(2);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //5�b�o��
                    if (currentStateTime >= 5f) {
                        ChangeState(State.Alart_FollowBuddy);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�x���Ԑ����Ԑ؂�
                    if (em.currentState == EnemisManager.State.Patrol) {
                        ChangeState(State.Alart_GiveUp);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //����щz�����n�܂���
                    if (CheckWindowEdge()) {
                        ChangeState(State.System_Jump);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;
                }
            //�{������߂�
            case State.Alart_GiveUp: {
                    if (stateEnter) {
                        enemyAnim.SetLayerWaight(EnemyAnimation.Layer.Gun, 0f, 0.3f);//�e���C���[�𖳌���
                        enemyAnim.SetState(EnemyAnimation.State.GaveUp);
                        navMesh.SetDestination(transform.position);
                        percent_Detect = 0f;
                        detectPlayer = false;
                        buddy = null;
                    }


                    //���̓���
                    FootAnimation();

                    //���F����
                    UpdateDetectPercent(1);

                    //�C���W�P�[�^�[
                    indicator.UpdateIndicator(false);

                    //�������o��
                    if (currentStateTime >= 1.0f && !voiceExcuted) {
                        voiceExcuted = true;
                        MakeVoice(EnemyVoice.Alart_GaveUp, 0, true);
                    }

                    //4�b�o��
                    if (currentStateTime >= 4) {
                        ChangeState(State.Patrol_Walk);
                        return;
                    }

                    //�v���C���[�𔭌�
                    if (percent_Detect >= 1f || PlayerFootStepExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Combat_Encount);
                        return;
                    }

                    //�퓬�Ԑ��Ɉڍs
                    if (em.currentState == EnemisManager.State.Combat) {
                        ChangeState(State.Combat_ReachKnownPos1);
                        return;
                    }

                    //�e�������m
                    if (PlayerShootSoundExposePercent() >= 1) {
                        voiceSource.Stop();
                        ChangeState(State.Patrol_DetectShootSound1);
                        return;
                    }

                    return;

                }
            #endregion

            //���O�h�[��
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
            //if (debug) Debug.Log("�X�V");

            if (distance > max) {
                //if (debug) Debug.Log("�^�[�Q�b�g���痣�ꂷ��:" + distance);
                navMesh.SetDestination(targetPos);

                return;
            }
            else
            if(distance <= max && distance >= min) {
                //if (debug) Debug.Log("�K���ȋ���:"+distance);
                navMesh.SetDestination(transform.position);
            }
            else {
                //if (debug) Debug.Log("�߂���:" + distance);
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

    //60��60�}�X�̃����_���Ȉʒu����
    Vector3 RandomPosition() {
        return new Vector3(Random.Range(-60f, 60f), 0, Random.Range(-60f, 60f));
    }

    //���S�Ƃ��̔��a�������Ƀ����_���Ȉʒu����
    Vector3 RandomPosition(Vector3 pos_center, float radius) {
        //float distance = Random.Range(1f, radius);
        return pos_center + Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward * radius;
    }

    //�T���͈͂������_���ɍ���
    Vector3 RandomPosition_Alart() {
        return new Vector3(Random.Range(em.alartArea_rec_A.x, em.alartArea_rec_B.x), 0, Random.Range(em.alartArea_rec_A.z, em.alartArea_rec_B.z));
    }


    //����̈ʒu������
    void Rotate(Vector3 target, float time) {

        //�����v�Z
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        direction = direction.normalized;

        GD.AddRay(transform.position, transform.position + direction  *2, Color.red);

        float targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, time);

    }�@

    //����̕������������ʒu�ł͂Ȃ�����
    void RotateDirection(Vector3 direction, float time) {

        float targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, time);

    } 

    //�e�̊p�x�A�b�v�f�[�g
    void UpdateGunAngle(Vector3 targetPos) {
        float angle = Vector3.Angle(Vector3.up, (targetPos - eyeT.position));
        enemyAnim.SetAimAngle(angle - 90);
    }

    //�r�̃A�j���[�V�����̃A�b�v�f�[�g
    void FootAnimation() {
        Vector3 finalVelocity = Quaternion.Euler(0, -transform.eulerAngles.y, 0) * navMesh.velocity / navMesh.speed;
        enemyAnim.SetMoveSpeed(finalVelocity.z, finalVelocity.x);
    }

    //�v���C���[�̎��F�x�A�b�v�f�[�g
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
    }//�v���C���[�̎��E��L���v�Z

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

    }//���Ԃ������Ă��邩����

    float PlayerFootStepExposePercent() {
        float distance = Vector3.Distance(eyeT.position, player.transform.position);
        return player.noise_Foot / distance;
    }//�v���C���[�̑������m��

    float PlayerShootSoundExposePercent() {
        float distance = Vector3.Distance(eyeT.position, player.transform.position);
        return player.noise_Gun / distance;
    }//�v���C���[�̏e�����m��

    bool CheckWindowEdge() {
        if (navMesh.isOnOffMeshLink) {
            //���ɓ���
            navMesh.isStopped = true;
            currentWindowEdge = navMesh.currentOffMeshLinkData.offMeshLink.GetComponent<WindowEdge>();
            eventOffsetTargetPos = navMesh.currentOffMeshLinkData.startPos;
            eventOffsetTargetDir = navMesh.currentOffMeshLinkData.endPos - eventOffsetTargetPos;
            if (currentWindowEdge.targetEnemyController == null) {
                currentWindowEdge.SetTarget(this);
                navMesh.Warp(transform.position);//����ňʒu�𐮂���
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
