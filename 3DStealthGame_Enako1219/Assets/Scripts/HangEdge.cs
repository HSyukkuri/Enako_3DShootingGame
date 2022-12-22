using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGizmoTool;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class HangEdge : MonoBehaviour {


    public Transform posA;
    public Transform posB;

    PlayerController playerCon;

    public float distanceFromPlayer { get; private set; }
    public bool canGrab { get; private set; }
    public Vector3 dir_AB { get; private set; }
    public Vector3 dir_AB_H { get; private set; }//AからBまでの水平方向
    public float len_AB_H { get; private set; }//AからBまでの水平方向の距離　HはホリゾンタルのH
    public Vector3 dir_Body { get; private set; }
    public Vector3 pos_Grab { get; private set; }
    public Vector3 pos_Body { get; private set; }
    public Vector3 pos_Fall { get; private set; }
    public Vector3 vec_AB { get; private set; }

    GizmoDrawer gizmoDrawer = new GizmoDrawer();


    private void Start() {
        playerCon = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        vec_AB = posB.position - posA.position;
        dir_AB = vec_AB.normalized;
        dir_Body = Vector3.Cross(dir_AB, Vector3.up).normalized;
        dir_AB_H = Vector3.Cross(Vector3.up, dir_Body);
        len_AB_H = Vector3.Dot(dir_AB_H, vec_AB);

        playerCon.hangEdgeList.Add(this);

    }

    private void Update() {

        if (Vector3.Distance(this.transform.position, playerCon.transform.position) > 10f) {
            if (playerCon.hangEdgeList.Contains(this)) {
                playerCon.hangEdgeList.Remove(this);
            }
            return;
        }
        else {
            //プレイヤーの位置から最短地点を割り出す。
            Vector3 vA_Player = playerCon.transform.position - posA.position;//A→Player間ベクトル
            distanceFromPlayer = Vector3.Dot(-dir_Body, vA_Player);
            if (distanceFromPlayer < 0) {
                canGrab = false;
                return;
            }
            float len_A_Stand_H = Vector3.Dot(dir_AB_H, vA_Player);//AからGrabPosまでの水平方向距離
            float len_StandPos = vec_AB.magnitude * (len_A_Stand_H / len_AB_H);//AからGrabPosまでの直線距離
            //len_StandPos = Mathf.Clamp(len_StandPos, 0, vec_AB.magnitude);//AからGrabPosまでの直線距離のトリミング
            if (len_StandPos < 0 || len_StandPos > vec_AB.magnitude) {
                //線分の外にいる
                canGrab = false;
                return;
            }


            Vector3 vA_StandPos = dir_AB * len_StandPos;
            pos_Grab = posA.position + vA_StandPos;
            pos_Body = pos_Grab - Vector3.up - dir_Body;

            //遠すぎないか判定
            if (Vector3.Distance(pos_Body, playerCon.transform.position) >= 1f) {
                canGrab = false;
                return;
            }

            //着地地点を探す
            RaycastHit hit;
            if (Physics.Raycast(pos_Grab +Vector3.up + dir_Body*0.5f, -Vector3.up, out hit, 2.5f , playerCon.groundLayer)) {
                pos_Fall = hit.point;
            }
            else {
                pos_Fall = pos_Grab + dir_Body - Vector3.up * 1.5f;
            }


            canGrab = true;

            if (!playerCon.hangEdgeList.Contains(this)) {
                playerCon.hangEdgeList.Add(this);
            }

        }





    }



    //public void PlayTimeline() {

    //    TimelineAsset timelineAsset = director_Window.playableAsset as TimelineAsset;
    //    if (timelineAsset == null) {
    //        Debug.Log("タイムラインアセットがnull");
    //        return;
    //    }

    //    AnimationTrack animTrack = timelineAsset.GetOutputTrack(1) as AnimationTrack;
    //    if (animTrack == null) {
    //        Debug.Log("アニメーショントラックがnull");
    //        return;
    //    }

    //    animTrack.position = pos_Stand;
    //    animTrack.rotation = Quaternion.LookRotation(dir_Body, Vector3.up);

    //    director_Window.Play();
    //}




    private void OnDrawGizmos() {

        gizmoDrawer.Execute();

        vec_AB = posB.position - posA.position;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(posA.position, posA.position + (vec_AB * 0.5f));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(posB.position, posB.position + (-vec_AB * 0.5f));


    }

}
