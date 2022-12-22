using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkindMeshComverter : MonoBehaviour
{
	public SkinnedMeshRenderer targetMesh;
	public Texture2D outFitAlphaMap;


	[ContextMenu("Convert to regular mesh")]
	public void ChangeOuftit() {

		SkinnedMeshRenderer BodyMesh = GetComponent<SkinnedMeshRenderer>();

		SkinnedMeshRenderer newMesh = Instantiate<SkinnedMeshRenderer>(targetMesh);
		newMesh.transform.SetParent(gameObject.transform);
		newMesh.bones = BodyMesh.bones;
		newMesh.rootBone = BodyMesh.rootBone;

		Material material = BodyMesh.GetComponent<Renderer>().material;
		material.SetTexture("AlphaMap", outFitAlphaMap);
	}

}
