using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MeshDissolve : MonoBehaviour {
	


	List<Material> skinMaterials = new List<Material>();
	public List<Transform> targetTransforms = new List<Transform>();


	public void Start() {

		foreach(Renderer renderer in GetComponentsInChildren<Renderer>()) {
			foreach(Material material in renderer.materials) {
				skinMaterials.Add(material);
            }
		}

		
	}



    public void Update() {
		float distance = 1;
		float minDistance = 0.2f;
		float maxDistance = 0.4f;

		foreach (Transform targetTranform in targetTransforms) {
			float currentDistance = Vector3.Distance(Camera.main.transform.position, targetTranform.position);
			if (distance > currentDistance) {
				distance = currentDistance;
            }

			if(distance <= minDistance) {
				break;
            }
        }
			


		float value;

		if(distance >= maxDistance) {
			value = 0f;
        }else
		if(distance > minDistance) {
			value = (maxDistance - distance) / (maxDistance - minDistance);
        }
        else {
			value = 1f;
        }


		foreach(Material material in skinMaterials) {
			material.SetFloat("_Dissolve", value);
		}
    }
}