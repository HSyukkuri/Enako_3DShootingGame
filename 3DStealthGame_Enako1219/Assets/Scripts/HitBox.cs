using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour,ITakeDamage,IVisible
{

    public float damageIncrease;
    public float detectIncrease;

    public ITakeDamage pearent;

    public BodyPart part;

    public void TakeDamage(float damage, Vector3 hitPos, Vector3 hitDirection, BodyPart _part) {

        if(pearent == null) {
            return;
        }

        pearent.TakeDamage(damage * damageIncrease, hitPos, hitDirection, part);
    }

    public void Detected() {

    }
}
