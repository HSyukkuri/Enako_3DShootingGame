using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WindowEdge : MonoBehaviour
{
    public EnemyController targetEnemyController { get; private set; }
    public OffMeshLink offmeshLink { get; private set; }

    public void Start() {
        offmeshLink = GetComponent<OffMeshLink>();
    }

    public void SetTarget(EnemyController enemyController) {
        if(targetEnemyController != null) {
            Debug.LogError("���łɃZ�b�g����Ă�I�I");
            return;
        }
        targetEnemyController = enemyController;
        offmeshLink.activated = false;
    }

    public void ReleaseTarget(EnemyController enemyController) {
        if(targetEnemyController == enemyController) {
            targetEnemyController = null;
            offmeshLink.activated = true;
        }
        else {
            Debug.LogError("���O�͂��������Z�b�g����Ă˂��I�I�I�I�I");
        }
    }

}
