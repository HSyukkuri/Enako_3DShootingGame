using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public LayerMask collisionMask;
    
    float speed = 10;
    float damage = 1;
    float lifetime = 3;

    MeshRenderer meshRenderer;

    float currentDis = 0f;

    public void SetSpeed(float newSpeed) {
        speed = newSpeed;
    }

    public void SetDamage(float newDamage) {
        damage = newDamage;
    }

    private void Start() {
        meshRenderer = GetComponent<MeshRenderer>();
        Destroy(gameObject, lifetime);
    }

    private void Update() {
        float moveDistance = speed * Time.deltaTime;
        currentDis += moveDistance;
        CheckCollisions(moveDistance);
        transform.Translate(Vector3.forward * Time.deltaTime * speed);

        if (!meshRenderer.enabled && currentDis >= 2f) {
            meshRenderer.enabled = true;
        }

    }

    void CheckCollisions(float moveDistance) {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray: ray, hitInfo: out hit, maxDistance: moveDistance, layerMask: collisionMask)) {
            
            OnHitObject(hit);
        }
    }

    void OnHitObject(RaycastHit hit) {
        GameObject.Destroy(gameObject);
        if (hit.collider != null){
            ITakeDamage hitBox = hit.collider.GetComponent<ITakeDamage>();
            if (hitBox != null) {
                hitBox.TakeDamage(damage, hit.point, hit.normal, BodyPart.Head);
            }
        }


    }
}
