using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUtility;
using MyGizmoTool;

public class EnemisManager : MonoBehaviour
{

    public EnemyController buddyRequest;
    public GameObject indicatorPrefab;
    public GameObject raderPointerPrefab;
    GameObject canvas;
    GameObject rader;

    PlayerController player;
    public List<EnemyController> enemyList { get; private set; } = new List<EnemyController>();
    public List<EnemyController> deadEnemyList { get; private set; } = new List<EnemyController>();
    //�x���p�����[�^�[
    public Vector3 pos_LastKnown { get; private set; } = Vector3.zero;

    public const float TIME_ALART = 120f;
    public const float RADIAS_ALART_MAX = 60f;
    public float currentAlart_AreaPercent { get; private set; }
    public Vector3 alartArea_rec_A { get; private set; }
    public Vector3 alartArea_rec_B { get; private set; }

    GizmoDrawer gizmoDrower = new GizmoDrawer();

    void Initialize() {
        enemyList = new List<EnemyController>();
        deadEnemyList = new List<EnemyController>();

        player = FindObjectOfType<PlayerController>();

        canvas = FindObjectOfType<Canvas>().gameObject;

        rader = GameObject.FindGameObjectWithTag("Rader");

    }


    //�G�����̏��
    bool stateEnter = false;
    public float currentStateTime { get; private set; } = 0f;
    public State currentState { get; private set; } = State.Initialize;

    NotifyType currentNortifyType = NotifyType.None;
    public enum NotifyType {
        None,
        VisualContact,
        DetectPlayerActivity,
        TargetLost,
    }

    void ChangeState(State newState) {
        //Debug.Log("�����X�e�[�g�F" + currentState.ToString() + "��" + newState.ToString());
        currentState = newState;
        currentNortifyType = NotifyType.None;
        stateEnter = true;
        currentStateTime = 0f;

    }
    public enum State {
        Initialize,
        Patrol,
        Combat,
        Alart,
    }

    public void Update() {
        currentStateTime += Time.deltaTime;

        //�Q�[���I�[�o�[
        if(currentState != State.Initialize && GameManager.instance.currentState == GameManager.State.GameOver) {
            ChangeState(State.Initialize);
            return;
        }

        switch (currentState) {
            case State.Initialize:{
                    //�Q�[�����J�n���ꂽ
                    if(GameManager.instance.currentState == GameManager.State.Playing) {
                        Initialize();
                        ChangeState(State.Patrol);
                    }

                    return;
            }

            case State.Patrol: {

                    //�G���N���v���C���[�𔭌�
                    if (currentNortifyType == NotifyType.VisualContact) {
                        buddyRequest = null;
                        ChangeState(State.Combat);
                        return;
                    }

                    //�G���N���s�R�ȓ_�𔭌�
                    if (currentNortifyType == NotifyType.DetectPlayerActivity) {
                        buddyRequest = null;
                        ChangeState(State.Alart);
                        return;
                    }
                    return;
                }

            case State.Combat: {

                    //�G����������
                    if (currentNortifyType == NotifyType.TargetLost) {
                        buddyRequest = null;
                        ChangeState(State.Alart);
                        return;
                    }

                    return;
                }

            case State.Alart: {

                    //�T���͈̓A�b�v�f�[�g
                    currentAlart_AreaPercent = currentStateTime / 120f;

                    float lenght_x_plus = RADIAS_ALART_MAX - pos_LastKnown.x;
                    float lenght_x_minus = RADIAS_ALART_MAX + pos_LastKnown.x;
                    float lenght_z_plus = RADIAS_ALART_MAX - pos_LastKnown.z;
                    float lenght_z_minus = RADIAS_ALART_MAX + pos_LastKnown.z;

                    alartArea_rec_A = new Vector3(pos_LastKnown.x + lenght_x_plus * currentAlart_AreaPercent,
                                                  0f,
                                                  pos_LastKnown.z + lenght_z_plus * currentAlart_AreaPercent);

                    alartArea_rec_B = new Vector3(pos_LastKnown.x - lenght_x_minus * currentAlart_AreaPercent,
                                                  0f,
                                                  pos_LastKnown.z - lenght_z_minus * currentAlart_AreaPercent);


                    gizmoDrower.AddRay(alartArea_rec_A, alartArea_rec_A + Vector3.up * 5, Color.red);
                    gizmoDrower.AddRay(alartArea_rec_B, alartArea_rec_B + Vector3.up * 5, Color.blue);
                    gizmoDrower.AddRay(alartArea_rec_A + Vector3.up * 5, pos_LastKnown + Vector3.up * 5, Color.red);
                    gizmoDrower.AddRay(alartArea_rec_B + Vector3.up * 5, pos_LastKnown + Vector3.up * 5, Color.blue);

                    //�T���ł��؂�
                    if (currentStateTime >= TIME_ALART) {
                        buddyRequest = null;
                        ChangeState(State.Patrol);
                        return;
                    }

                    //�G�𔭌�
                    if (currentNortifyType == NotifyType.VisualContact) {
                        buddyRequest = null;
                        ChangeState(State.Combat);
                        return;
                    }

                    return;
                }
        }


    }

    public void OnDrawGizmos() {
        gizmoDrower.Execute();
    }

    public void LateUpdate() {
        if (currentStateTime != 0f) {
            stateEnter = false;
        }
    }

    public DetectIndicator GetIndicator() {

        if(canvas == null) {
            canvas = FindObjectOfType<Canvas>().gameObject;
        }

        GameObject newIndicator = Instantiate(indicatorPrefab);
        newIndicator.transform.SetParent(canvas.transform, false);

        return newIndicator.GetComponent<DetectIndicator>();
    }

    public EnemyRaderPointer GetRaderPointer() {
        if(rader == null) {
            rader = GameObject.FindGameObjectWithTag("Rader");
        }

        GameObject newPointer = Instantiate(raderPointerPrefab);
        newPointer.transform.SetParent(rader.transform, false);

        return newPointer.GetComponent<EnemyRaderPointer>();
    }

    public void SetEnemyDead(EnemyController enemy) {
        enemyList.Remove(enemy);
        deadEnemyList.Add(enemy);
    }

    //�߂��̓G�����擾
    public EnemyController GetNearestEnemyFromPos(Vector3 position, float maxDistance) {
        EnemyController target = null;
        float shortestDistance = maxDistance;

        foreach (EnemyController eneCon in enemyList) {
            float distancefromThis = Vector3.Distance(position, eneCon.transform.position);

            if (shortestDistance >= distancefromThis) {
                target = eneCon;
                shortestDistance = distancefromThis;
            }
        }

        return target;
    }



    public EnemyController GetNearestRecognizeEnemyFromEnemy(EnemyController target) {

        EnemyController enemy = null;

        float maxDetectPercent = 0f;

        foreach (EnemyController eneCon in enemyList) {
            if (eneCon == target) {
                continue;
            }

            float detectPercent = eneCon.CanSeeFriend(target);
            if (detectPercent > maxDetectPercent) {
                enemy = eneCon;
                maxDetectPercent = detectPercent;
            }

        }

        return enemy;
    }

    public void Notify(Vector3 lastKnownPos, NotifyType type) {
        pos_LastKnown = lastKnownPos;
        currentNortifyType = type;
    }
}
