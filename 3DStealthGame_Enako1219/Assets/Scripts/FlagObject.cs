using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class FlagObject : MonoBehaviour
{

    public GameObject nextFlag;
    public PlayableAsset playableAsset;


    [TextArea(2,50)]
    public string achievementText;


    public bool isEnd = false;

    bool isEnter = false;

    public bool hasTargetObject = false;

    private void Start() {
        //目標行動テキストを表示
      　GameManager.instance.ui_system.UpdateAchievementText(achievementText);

        //イベントアニメーションの発生位置を設定
        if (playableAsset != null) {
            TimelineAsset timelineAsset = playableAsset as TimelineAsset;
            foreach (var track in timelineAsset.GetOutputTracks()) {
                if (track.GetType() == typeof(AnimationTrack)) {
                    AnimationTrack animTrack = track as AnimationTrack;
                    animTrack.position = transform.position;
                    animTrack.rotation = transform.rotation;
                }
            }
        }

        //レーダー上にポインターを乗せる必要あり。
        if (hasTargetObject) {
            TargetPointer pointer = FindObjectOfType<TargetPointer>();
            if (pointer != null) {
                pointer.SetTargetTransform(this.transform);
            }
        }
        //ポインター不要
        else {
            TargetPointer pointer = FindObjectOfType<TargetPointer>();
            if (pointer != null) {
                pointer.ResetTarget();
            }
        }

    }

    private void OnTriggerEnter(Collider other) {
        if(other.GetComponent<PlayerController>()!= null) {
            isEnter = true;
        }
    }

    private void Update() {

        if (isEnter) {
            if(nextFlag != null) {
                Instantiate(nextFlag);
                GameManager.instance.SetEvent(playableAsset);
            }

            if (isEnd) {
                GameManager.instance.Ending();
            }

            Destroy(this.gameObject);
        }

    }


    private void LateUpdate() {
        isEnter = false;
    }


}
