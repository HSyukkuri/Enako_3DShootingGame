using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    public float masterVolumePercent = 1;
    public float sfxVolumePercent = 1;
    public float systemVoluePercent = 1;
    public float musicVolumePercent = 1;

    AudioSource[] musicSources;
    int activeMusicSourceIndex;

    public static AudioManager instance;

    GameManager gm;


    [System.Serializable]
    public class BGM {
        public AudioClip clip;
        public float volumeAdjust = 1f;
    }

    AudioClip normalBGMClip;
    AudioClip battleBGMClip;

    BGM currentBGM;

    public List<BGM> BGMList = new List<BGM>();

    private void Awake() {
        if (instance != null) {
            Destroy(this);
        }
        else {
            instance = this;
            DontDestroyOnLoad(this.gameObject);

            //BGM
            musicSources = new AudioSource[2];
            for (int i = 0; i < 2; i++) {
                GameObject newMusicSource = new GameObject("Music source " + (i + 1));
                musicSources[i] = newMusicSource.AddComponent<AudioSource>();
                newMusicSource.transform.parent = transform;
            }

        }

    }

    private void Start() {

        Initialize();
    }

    public void Initialize() {
        currentBGM = BGMList[0];
        normalBGMClip = BGMList[0].clip;
        battleBGMClip = BGMList[1].clip;

        gm = GameManager.instance;

        musicSources[0].volume = musicVolumePercent * masterVolumePercent;
        musicSources[1].volume = musicVolumePercent * masterVolumePercent;

        musicSources[0].Stop();
        musicSources[1].Stop();
    }


    public void SetVol_Master(float percent) {

        masterVolumePercent = percent;

        musicSources[0].volume = musicVolumePercent * masterVolumePercent;
        musicSources[1].volume = musicVolumePercent * masterVolumePercent;


    }

    public void SetVol_SFX(float percent) {
        sfxVolumePercent = percent;
    }

    public void SetVol_Music(float percent) {

        musicVolumePercent = percent;

        musicSources[0].volume = musicVolumePercent * masterVolumePercent;
        musicSources[1].volume = musicVolumePercent * masterVolumePercent;

    }

    public void SetVol_System(float percent) {

        systemVoluePercent = percent;

    }


    public void PlayMusic(AudioClip clip, float fadeDuration = 1) {
        BGM newBGM = BGMList.Find(i => i.clip == clip);

        activeMusicSourceIndex = 1 - activeMusicSourceIndex;
        musicSources[activeMusicSourceIndex].clip = newBGM.clip;
        musicSources[activeMusicSourceIndex].loop = true;
        musicSources[activeMusicSourceIndex].Play();

        StartCoroutine(AnimateMusicCrossfade(newBGM,fadeDuration));
    }



    public void SetNormalBGMType(AudioClip clip) {
        normalBGMClip = clip;
    }

    public void StartNormalBGM(float duration = 1f) {
        PlayMusic(normalBGMClip,duration);
    }

    public void PlaySound(AudioClip clip, Vector3 pos, float volumePercent = 1f) {
        AudioSource.PlayClipAtPoint(clip, pos, sfxVolumePercent * masterVolumePercent * volumePercent);
    }

    public void PlaySystemSound(AudioClip clip) {
        AudioSource.PlayClipAtPoint(clip, transform.position, systemVoluePercent * masterVolumePercent);
    }

    IEnumerator AnimateMusicCrossfade(BGM newBGM,float duration) {
        float percent = 0;

        while(percent < 1) {
            percent += Time.deltaTime * 1 / duration;
            musicSources[activeMusicSourceIndex].volume = Mathf.Lerp(0, musicVolumePercent * masterVolumePercent * newBGM.volumeAdjust , percent);
            musicSources[1 - activeMusicSourceIndex].volume = Mathf.Lerp(musicVolumePercent * masterVolumePercent* currentBGM.volumeAdjust, 0, percent);
            yield return null;
        }

        musicSources[1 - activeMusicSourceIndex].clip = null;
        currentBGM = newBGM;
    }

}
