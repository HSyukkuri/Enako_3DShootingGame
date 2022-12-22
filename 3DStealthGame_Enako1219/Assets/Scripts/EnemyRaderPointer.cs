using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyRaderPointer : MonoBehaviour
{
    [SerializeField] RawImage image;

    EnemyController enemyCon;
    PlayerController playerCon;
    Camera camera;

    public void SetEnemyController(EnemyController enemyController) {
        enemyCon = enemyController;
        playerCon = FindObjectOfType<PlayerController>();
        camera = Camera.main;
    }

    public void Update() {

        if(!enemyCon || !camera || !playerCon) {
            return;
        }

        if(enemyCon.currentState == EnemyController.State.Dead_RagDoll) {
            if (image.color.a != 0) {
                image.color = new Color(1, 1, 1, 0);
            }
            return;
        }

        float enemyAngle = enemyCon.transform.eulerAngles.y;
        float cameraAngle = camera.transform.eulerAngles.y;

        Vector3 playerPos = new Vector3(playerCon.transform.position.x, playerCon.transform.position.z, 0f);
        Vector3 enemyPos =  new Vector3(enemyCon.transform.position.x, enemyCon.transform.position.z, 0f);

        Vector3 pointerPos = Quaternion.Euler(0,0, camera.transform.eulerAngles.y) * (enemyPos - playerPos);

        float radius = 30f;

        if(pointerPos.magnitude > radius) {
            image.enabled = false;
        }
        else {
            image.enabled = true;
        }

        transform.localEulerAngles = new Vector3(0, 0, -enemyAngle + cameraAngle + 180);

        transform.localPosition = pointerPos * 50/radius;
    }
}
