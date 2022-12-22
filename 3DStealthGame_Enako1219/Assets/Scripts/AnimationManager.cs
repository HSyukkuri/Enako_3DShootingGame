using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>> {
	public AnimationClipOverrides(int capacity) : base(capacity) { }

	public AnimationClip this[string name] {
		get { return this.Find(x => x.Key.name.Equals(name)).Value; }
		set {
			int index = this.FindIndex(x => x.Key.name.Equals(name));
			if (index != -1)
				this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
		}
	}
}


public class AnimationManager:MonoBehaviour {

	string[] overRideClipName;

	int currentClipIndex = 0;

	public AnimationClip clip_Stand;
	public AnimationClip clip_Walk;
	public AnimationClip clip_Run;



	private AnimatorOverrideController animatorOverrideController;
	private AnimationClipOverrides clipOverrides;
	private Animator anim;

	void Start() {
		overRideClipName = new string[2];
		overRideClipName[0] = "Clip0";
		overRideClipName[1] = "Clip1";

		anim = GetComponent<Animator>();
		animatorOverrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);
		anim.runtimeAnimatorController = animatorOverrideController;

		clipOverrides = new AnimationClipOverrides(animatorOverrideController.overridesCount);
		animatorOverrideController.GetOverrides(clipOverrides);
	}

    public void Update() {
		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			StartCoroutine(ChangeClip(clip_Stand));
		}

        if (Input.GetKeyDown(KeyCode.Alpha2)) {
			StartCoroutine(ChangeClip(clip_Walk));
		}

		if (Input.GetKeyDown(KeyCode.Alpha3)) {
			StartCoroutine(ChangeClip(clip_Run));
		}

	}

    public IEnumerator ChangeClip(AnimationClip clip) {
		// ステートをキャッシュ
		AnimatorStateInfo[] stateInfo = new AnimatorStateInfo[anim.layerCount];
		for (int i = 0; i < anim.layerCount; i++) {
			stateInfo[i] = anim.GetCurrentAnimatorStateInfo(i);
		}

		// AnimationClipを差し替えて、強制的にアップデート
		// ステートがリセットされる

		currentClipIndex = 1 - currentClipIndex;
		clipOverrides[currentClipIndex] = new KeyValuePair<AnimationClip, AnimationClip>(clipOverrides[currentClipIndex].Key, clip);
		animatorOverrideController.ApplyOverrides(clipOverrides);

		anim.Update(0f);
		// ステートを戻す
		for (int i = 0; i < anim.layerCount; i++) {
			anim.Play(stateInfo[i].fullPathHash, i, stateInfo[i].normalizedTime);
		}

		yield return null;

		anim.SetInteger("ClipIndex",currentClipIndex);
		Debug.Log("Clop0:" + animatorOverrideController.animationClips[0].name);
		Debug.Log("Clop1:" + animatorOverrideController.animationClips[1].name);

	}
}