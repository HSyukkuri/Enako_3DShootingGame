using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class EventManager : MonoBehaviour {
    public PlayableDirector playableDirector { get; private set; }

    void Start() {
        playableDirector = GetComponent<PlayableDirector>();
    }

    public void StartNormalBGM(AudioClip clip){
        AudioManager.instance.SetNormalBGMType(clip);
        AudioManager.instance.StartNormalBGM(0);
    }

}
