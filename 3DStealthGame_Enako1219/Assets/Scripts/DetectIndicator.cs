using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetectIndicator : MonoBehaviour {
    [SerializeField] Image image;

    EnemyController enemyCon;
    PlayerController playerCon;
    Camera camera;

    public void SetEnemyController(EnemyController enemyController) {
        enemyCon = enemyController;
        playerCon = FindObjectOfType<PlayerController>();
        camera = Camera.main;
    }

    public void UpdateIndicator(bool detect) {

        if (!enemyCon || !camera || !playerCon) {
            return;
        }

        Vector3 cameraPos = new Vector3(camera.transform.position.x, camera.transform.position.z, 0f);
        Vector3 playerPos = new Vector3(playerCon.transform.position.x, playerCon.transform.position.z, 0f);
        Vector3 enemyPos = new Vector3(enemyCon.transform.position.x, enemyCon.transform.position.z, 0f);
        Vector3 direction = Quaternion.Euler(0, 0, camera.transform.eulerAngles.y) * (enemyPos - cameraPos).normalized;
 
        transform.rotation = Quaternion.FromToRotation(Vector3.down, direction);


        if (detect) {
            image.color = new Color(1, 0, 0, 1);
        }
        else {
            image.color = new Color(1, 1, 1, enemyCon.percent_Detect);

        }


    }

    public void CloseIndicator() {
        image.color = new Color(1, 1, 1, 0);
    }




}
