using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;


public class GameManager : MonoBehaviour {
    #region �V���O���g��
    public static GameManager instance;
    private void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else {
            Destroy(this);
        }
    }
    #endregion





    float mouseSensitivity = 2;

    [Header("UI")]

    //�V�[��
    public SceneObject titleScene;
    public SceneObject playScene;

    public TitleUI ui_title { get; private set; }
    public UI_System ui_system { get; private set; }

    public EnemisManager enemyManager{ get; private set; }
    PlayerController plaerCon;

    public List<PlayableDirector> playableDirectorsList;

    EventManager eventManager;

    public FlagObject startFlag;

    bool isLoading = false;
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        isLoading = false;
    }

    public enum State {
        Title,
        Playing,
        Event,
        GameOver,
        Load,
    }
    public State priviousState { get; private set; }
    public State currentState { get; private set; }
    float stateTime = 0f;
    bool stateEnter = false;

    void ChangeState(State newState) {
        priviousState = currentState;
        currentState = newState;
        stateTime = 0f;
        stateEnter = true;
        //Debug.Log($"GM{priviousState}��{currentState}");
    }

    public void Start() {
        SceneManager.sceneLoaded += OnSceneLoaded;

        ChangeState(State.Title);
    }

    public void Update() {
        stateTime += Time.deltaTime;

        switch (currentState) {

            //�^�C�g���X�e�[�g
            case State.Title: {
                    if (stateEnter) {
                        SceneManager.LoadScene(titleScene);
                        Cursor.lockState = CursorLockMode.Confined;
                        Cursor.visible = true;
                        
                    }

                    if (isLoading) {
                        return;
                    }

                    if(ui_title == null) {
                        ui_title = FindObjectOfType<TitleUI>();
                        if(ui_title == null) {
                            return;
                        }
                    }
                    
                    if (ui_title.pb_Start) {
                        SetMouseSencitivity(ui_title.slider_Mouse.value);
                        AudioManager.instance.masterVolumePercent = ui_title.slider_BGM.value;
                        ChangeState(State.Load);
                        return;
                    }

                    return;
                }
            
            //���[�h�X�e�[�g
            case State.Load: {
                    if (stateEnter) {
                        isLoading = true;
                        SceneManager.LoadScene(playScene);
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                        AudioManager.instance.Initialize();
                    }

                    //���[�h�I�������B
                    if (!isLoading) {
                        ui_system = FindObjectOfType<UI_System>();
                        ui_system.HideRestartButton();
                        enemyManager = FindObjectOfType<EnemisManager>();
                        eventManager = FindObjectOfType<EventManager>();
                        eventManager.playableDirector.Play();

                        //�}�E�X���x�󂯓n��
                        FindObjectOfType<ThirdPersonCamera>().SetMouseSensitivity(mouseSensitivity);

                        Instantiate(startFlag);
                        ChangeState(State.Playing);
                        return;
                    }

                    return;
                }

            //�Q�[���i���C���j�X�e�[�g
            case State.Playing: {
                    if (stateEnter) {
                        plaerCon = FindObjectOfType<PlayerController>();
                    }


                    //�v���C���[������
                    if (plaerCon.currentState == PlayerController.State.Dead) {
                        ChangeState(State.GameOver);
                        return;
                    }

                    //�C�x���g����������
                    if (eventManager.playableDirector.state == PlayState.Playing) {
                        ChangeState(State.Event);
                        return;
                    }

                    return;
                }

            //�C�x���g�X�e�[�g
            case State.Event: {
                    if (stateEnter) {
                        plaerCon.ChangeState(PlayerController.State.Action_Event);
                    }

                    //�C�x���g���I�����
                    if (eventManager.playableDirector.state != PlayState.Playing) {
                        plaerCon.ChangeState(PlayerController.State.Action_EventEnd);
                        ChangeState(State.Playing);
                        return;
                    }

                    return;
                }

            case State.GameOver: {
                    if (stateEnter) {
                        ui_system.ShowRestartButton();
                        Cursor.lockState = CursorLockMode.Confined;
                        Cursor.visible = true;
                    }

                    //���g���C�{�^���������ꂽ�B
                    if ( ui_system.index == 0) {
                        ui_system.gameObject.SetActive(false);
                        ChangeState(State.Load);
                        return;
                    }



                    return;
                }
        }


    }

    public void LateUpdate() {
        if(stateTime != 0f) {
            stateEnter = false;
        }
    }

    public void SetEvent(PlayableAsset playableAsset) {
        eventManager.playableDirector.Play(playableAsset);
        plaerCon.controller.enabled = false;
    }

    public void Ending() {
        ChangeState(State.GameOver);
    }

    public void SetMouseSencitivity(float sencitivity) {
        mouseSensitivity = sencitivity;
    }

}


