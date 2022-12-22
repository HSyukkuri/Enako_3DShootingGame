using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetPointer : MonoBehaviour
{
    [SerializeField] RawImage image;
    Transform target;
    PlayerController playerCon;
    Camera camera;


    public void SetTargetTransform(Transform _target) {
        target = _target;
        playerCon = FindObjectOfType<PlayerController>();
        camera = Camera.main;
    }

    public void ResetTarget() {
        target = null;
    }

    public void Update() {

        if (!target || !camera || !playerCon) {
            return;
        }

        if (target == null) {
            if (image.enabled != false) {
                image.enabled = false;
            }
            return;
        }
        else {
            if (image.enabled != true) {
                image.enabled = true;
            }
        }


        Vector3 playerPos = new Vector3(playerCon.transform.position.x, playerCon.transform.position.z, 0f);
        Vector3 targetPos = new Vector3(target.position.x, target.position.z, 0f);

        Vector3 pointerPos = Quaternion.Euler(0, 0, camera.transform.eulerAngles.y) * (targetPos - playerPos);

        float radius = 30f;

        if (pointerPos.magnitude > radius) {
            pointerPos = pointerPos.normalized * radius;
            image.color = new Color(image.color.r, image.color.g, image.color.b, 0.5f);
        }
        else {
            image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
        }

        transform.localPosition = pointerPos * 50 / radius;
    }
}
