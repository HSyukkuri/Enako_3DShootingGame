using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUtility;


public class EnemyAnimation : MonoBehaviour
{

    public GunManager gun;

    Animator anim;
    protected AnimatorOverrideController overrideCon;
    protected AnimationClipOverrides clipOverrides;

    public Transform ragdollParent;
    Collider[] childrenCollider;
    Rigidbody[] childrenRigidBody;
    public EnemyController enemyController;

    bool isLastStepLeft;

    public enum Layer {
        Base = 0,
        Gun = 1,
    }

    public enum State {
        Stand = 0,
        Walk = 1,
        TargetLost = 2,
        GaveUp = 3,
        GetGrab = 4,
        Damage_ARM_R = 5,
        Damage_ARM_L = 6,
        Damage_LEG_R = 7,
        Damage_LEG_L = 8,
        Damage_Chest = 9,
        Dance = 10,
        Jump = 11,
    }



    List<LayerSmoothDamp> layerSmoothDamps = new List<LayerSmoothDamp>();

    void Start() {
        anim = GetComponent<Animator>();
        overrideCon = new AnimatorOverrideController(anim.runtimeAnimatorController);
        anim.runtimeAnimatorController = overrideCon;

        SetAimAngle(0);

        for (int i = 0; i < anim.layerCount; i++) {
            layerSmoothDamps.Add(new LayerSmoothDamp(i));
        }


        childrenCollider = ragdollParent.GetComponentsInChildren<Collider>();
        childrenRigidBody = ragdollParent.GetComponentsInChildren<Rigidbody>();

        RagdollActive(false);
    }

    void ChangeClip(AnimationClip clip, string overrideClipName) {
        overrideCon[overrideClipName] = clip;
    }

    public void SetCQCAnimation(CQCData data) {
        ChangeClip(data.clip, "AnimCQCâÒÇµèRÇË");
    }

    public void SetState(State state) {
        anim.SetInteger("ID", (int)state);
    }

    public void SetAimAngle(float cameraAngle) {
        anim.SetFloat("angle", (cameraAngle + 90) % 360f);
    }

    public bool IsInTransition() {
        return anim.IsInTransition(0);
    }

    void Update() {
        foreach(LayerSmoothDamp lsd in layerSmoothDamps) {
            lsd.Update();
            anim.SetLayerWeight(lsd.layerIndex, lsd.currentWeight);
        }
    }

    public void SetMoveSpeed(float percent) {
        anim.SetFloat("speedPercent", percent);
    }

    public void SetMoveSpeed(float z, float x) {
        anim.SetFloat("moveZ", z);
        anim.SetFloat("moveX", x);
    }

    public void SetLayerWaight(Layer layer, float weight, float smoothTime) {
        layerSmoothDamps[(int)layer].ChangeWeight(weight, smoothTime);
    }

    public void RagdollActive(bool active) {
        foreach(var collider in childrenCollider) {
            collider.enabled = active;
        }

        foreach(var rigidbody in childrenRigidBody) {
            rigidbody.detectCollisions = active;
            rigidbody.isKinematic = !active;
        }

        anim.enabled = !active;

    }

    public void ExcuteFootStep(int isLeft) {
        if (isLastStepLeft) {
            if (isLeft == 1) {
                return;
            }
            else {
                isLastStepLeft = false;
                enemyController.FootStepUpdate();
            }
        }
        else {
            if (isLeft != 1) {
                return;
            }
            else {
                isLastStepLeft = true;
                enemyController.FootStepUpdate();
            }
        }

    }
}
