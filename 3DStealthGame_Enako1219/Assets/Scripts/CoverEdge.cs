using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGizmoTool;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class CoverEdge : MonoBehaviour {


    public Transform posA;
    public bool isCorner_A = false;
    public Transform posB;
    public bool isCorner_B = false;
    public bool isLowCover = false;

    PlayerController playerCon;

    public float distanceFromPlayer { get; private set; }
    public bool isCoverring { get; private set; }
    public bool isOver_A { get; private set; }
    public bool isOver_B { get; private set; }
    public Vector3 dir_AB { get; private set; }
    public Vector3 dir_AB_H { get; private set; }//A����B�܂ł̐����x�N�g��
    public float len_AB_H { get; private set; }//A����B�܂ł̐��������̋����@H�̓z���]���^����H
    public Vector3 dir_Body { get; private set; }
    public Vector3 pos_Stand { get; private set; }
    public Vector3 vec_AB { get; private set; }

    GizmoDrawer gizmoDrawer = new GizmoDrawer();


    private void Start() {
        playerCon = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        vec_AB = posB.position - posA.position;
        dir_AB = vec_AB.normalized;
        dir_Body = Vector3.Cross(Vector3.up, dir_AB).normalized;
        dir_AB_H = Vector3.Cross(Vector3.up, dir_Body);
        len_AB_H = Vector3.Dot(dir_AB_H, vec_AB);


    }

    private void Update() {


        if (Vector3.Distance(this.transform.position, playerCon.transform.position) > 10f) {
            if (playerCon.coverEdgeList.Contains(this)) {
                playerCon.coverEdgeList.Remove(this);
            }
            return;
        }
        else {
            //�v���C���[�̈ʒu����ŒZ�n�_������o���B
            Vector3 vA_Player = playerCon.transform.position - posA.position;//A��Player�ԃx�N�g��
            distanceFromPlayer = Vector3.Dot(dir_Body, vA_Player);

            //�J�o�[�̗��ɂ���
            if (distanceFromPlayer < 0) {
                isCoverring = false;
                return;
            }

            float len_A_Stand_H = Vector3.Dot(dir_AB_H, vA_Player);//A����CoverPos�܂ł̐�����������
            float len_StandPos = vec_AB.magnitude * (len_A_Stand_H / len_AB_H);//A����CoverPos�܂ł̋���

            if (len_StandPos < 0) {
                isOver_A = true;
                isOver_B = false;
            }
            else if (len_StandPos > vec_AB.magnitude) {
                isOver_A = false;
                isOver_B = true;
            }
            else {
                isOver_A = false;
                isOver_B = false;
            }


            len_StandPos = Mathf.Clamp(len_StandPos, 0, vec_AB.magnitude);//A����GrabPos�܂ł̒��������̃g���~���O

            Vector3 vA_StandPos = dir_AB * len_StandPos;
            pos_Stand = posA.position + vA_StandPos;


            //�������Ȃ�������
            if (Vector3.Distance(pos_Stand, playerCon.transform.position) >= 0.5f || isOver_A || isOver_B) {
                isCoverring = false;
                return;
            }

            isCoverring = true;

            if (!playerCon.coverEdgeList.Contains(this)) {
                playerCon.coverEdgeList.Add(this);
            }

        }




    }






    private void OnDrawGizmos() {

        gizmoDrawer.Execute();

        vec_AB = posB.position - posA.position;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(posA.position, posA.position + (vec_AB * 0.5f));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(posB.position, posB.position + (-vec_AB * 0.5f));


    }

}