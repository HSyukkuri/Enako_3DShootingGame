using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUtility;

//このクラスは必ずコントローラースクリプトと同じオブジェクトにアタッチしなければならない。
public class HitBoxController : MonoBehaviour
{

    public Transform originalRoot;
    public Transform hitboxRoot;

    CopyPose copyPose = new CopyPose();

    ITakeDamage pearent;

    [Header("当たり判定")]
    public HitBox[] list_HitBox;

    // Update is called once per frame
    void Start()
    {
        GetHitBox();
    }

    private void Update() {
        copyPose.CopyPoseAll(hitboxRoot, originalRoot);
        hitboxRoot.position = originalRoot.position;
    }

    [ContextMenu("HITBOX取得")]
    void GetHitBox() {
        list_HitBox = GetComponentsInChildren<HitBox>();
        pearent = GetComponent<ITakeDamage>();

        foreach (HitBox hitBox in list_HitBox) {
            hitBox.pearent = pearent;
        }
    }
}
