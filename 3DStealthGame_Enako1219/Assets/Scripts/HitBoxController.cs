using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUtility;

//���̃N���X�͕K���R���g���[���[�X�N���v�g�Ɠ����I�u�W�F�N�g�ɃA�^�b�`���Ȃ���΂Ȃ�Ȃ��B
public class HitBoxController : MonoBehaviour
{

    public Transform originalRoot;
    public Transform hitboxRoot;

    CopyPose copyPose = new CopyPose();

    ITakeDamage pearent;

    [Header("�����蔻��")]
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

    [ContextMenu("HITBOX�擾")]
    void GetHitBox() {
        list_HitBox = GetComponentsInChildren<HitBox>();
        pearent = GetComponent<ITakeDamage>();

        foreach (HitBox hitBox in list_HitBox) {
            hitBox.pearent = pearent;
        }
    }
}
