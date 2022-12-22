
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MyUtility;

[ExecuteInEditMode]
public class OnionSkin : MonoBehaviour {
    public AnimationWindow animWindow;

    public Transform targetRoot;
    public Transform copyModel;

    public List<Transform> copyModelList = new List<Transform>();

    AnimationClip clip;

    CopyPose cp = new CopyPose();

    [Range(1, 25)]
    public int framePace = 1;

    [Range(1, 25)]
    public int visibleFrameCount = 2;

    [Range(0f,0.8f)]
    public float alpha = 0.5f;

    int frame = 0;

    public bool isTimeLine = false;

    void Awake() {
        for (int i = copyModelList.Count - 1; i >= 0; i--) {

            if (copyModelList[i].gameObject != null) {
                DestroyImmediate(copyModelList[i].gameObject);
                Debug.Log("�v�f���폜" + i);
            }


        }

        copyModelList = new List<Transform>();
    }

    void Update() {

        if(animWindow != null && clip != null && copyModelList.Count != 0) {

            if (animWindow.frame != frame && !animWindow.playing) {
                Initialize();
                frame = animWindow.frame;
            }
        }



    }


    [ContextMenu("������")]
    private void Initialize() {

        if (!isTimeLine) {
            //�^�C�����C������Ȃ�
            if (animWindow == null) {
                animWindow = (AnimationWindow)EditorWindow.GetWindow(typeof(UnityEditor.AnimationWindow));
            }

            if (animWindow != null) {
                Debug.Log("�������J�n");

                copyModel.gameObject.SetActive(true);

                Debug.Log("�v�f�̐��𑵂���");
                if (visibleFrameCount * 2 < copyModelList.Count) {
                    for (int i = copyModelList.Count - 1; i >= visibleFrameCount * 2; i--) {
                        DestroyImmediate(copyModelList[i].gameObject);
                        copyModelList.Remove(copyModelList[i]);
                    }
                }
                else
                if (visibleFrameCount * 2 > copyModelList.Count) {

                    int addCount = visibleFrameCount * 2 - copyModelList.Count;

                    for (int i = 0; i < addCount; i++) {
                        Transform copy = Instantiate(copyModel).transform;
                        copy.SetParent(transform);
                        copy.transform.position = copyModel.transform.position;
                        copy.transform.rotation = copyModel.transform.rotation;
                        copyModelList.Add(copy);
                    }
                }


                clip = animWindow.animationClip;
                float frameRate = clip.frameRate;
                float length = clip.length;
                int currentFrame = animWindow.frame;
                Debug.Log($"�N���b�v���擾�@���[�g:{frameRate:0.00} ����:{length:0.00} ���݂̃t���[��:{currentFrame}");
                Debug.Log("������for���ɓ���");

                for (int i = 1; i <= visibleFrameCount; i++) {
                    Debug.Log("for���ɓ�����");
                    if (animWindow == null) {
                        Debug.Log("animWindow���������I�I");
                    }
                    else {
                        Debug.Log("animWindow�͎Q�Əo���Ă�");
                    }
                    animWindow.frame = currentFrame + i * framePace;
                    Debug.Log("�t���[�����ړ�");
                    Transform copy = copyModelList[visibleFrameCount + i - 1];
                    Debug.Log("�Ώۂ̃R�s�[�I�u�W�F�N�g���擾");

                    if (animWindow.frame > frameRate * length) {
                        Debug.Log("�����t���[���I�[�o�[�Ȃ��\��");
                        copy.gameObject.SetActive(false);
                        continue;
                    }

                    copy.gameObject.SetActive(true);
                    Transform root = copy.Find(targetRoot.name);
                    cp.CopyPoseAll(root, targetRoot);
                    cp.CopyWorld(root, targetRoot);
                    Debug.Log("�ʒu�����ƃ|�[�Y�R�s�[����");

                    SkinnedMeshRenderer[] meshRenderers = copy.GetComponentsInChildren<SkinnedMeshRenderer>();
                    Debug.Log("SMR�擾�F���b�V����:"+meshRenderers.Length);

                    foreach (SkinnedMeshRenderer mr in meshRenderers) {
                        Color color = Color.green;
                        color.a = alpha / i;
                        mr.sharedMaterial.color = color;
                        Debug.Log("�O���i�΁j�\��");
                    }
                    
                }

                for (int i = 1; i <= visibleFrameCount; i++) {
                    animWindow.frame = currentFrame - i * framePace;
                    Transform copy = copyModelList[visibleFrameCount - i];

                    if (animWindow.frame < 0) {
                        copy.gameObject.SetActive(false);
                        continue;
                    }

                    copy.gameObject.SetActive(true);
                    Transform root = copy.Find(targetRoot.name);
                    cp.CopyPoseAll(root, targetRoot);
                    cp.CopyWorld(root, targetRoot);

                    SkinnedMeshRenderer[] meshRenderers = copy.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer mr in meshRenderers) {
                        Color color = Color.red;
                        color.a = alpha / i;
                        mr.sharedMaterial.color = color;
                        Debug.Log("����i�ԁj�\��");
                    }

                }


                animWindow.frame = currentFrame;
                copyModel.gameObject.SetActive(false);
            }
        }
        else {
            if (animWindow == null) {
                animWindow = (AnimationWindow)EditorWindow.GetWindow(typeof(UnityEditor.AnimationWindow));
            }

            if (animWindow != null) {
                Debug.Log("�������J�n");

                copyModel.gameObject.SetActive(true);

                Debug.Log("�v�f�̐��𑵂���");
                if (visibleFrameCount * 2 < copyModelList.Count) {
                    for (int i = copyModelList.Count - 1; i >= visibleFrameCount * 2; i--) {
                        DestroyImmediate(copyModelList[i].gameObject);
                        copyModelList.Remove(copyModelList[i]);
                    }
                }
                else
                if (visibleFrameCount * 2 > copyModelList.Count) {

                    int addCount = visibleFrameCount * 2 - copyModelList.Count;

                    for (int i = 0; i < addCount; i++) {
                        Transform copy = Instantiate(copyModel).transform;
                        copy.SetParent(transform);
                        copy.transform.position = copyModel.transform.position;
                        copy.transform.rotation = copyModel.transform.rotation;
                        copyModelList.Add(copy);
                    }
                }


                clip = animWindow.animationClip;
                float frameRate = clip.frameRate;
                float length = clip.length;
                int currentFrame = animWindow.frame;
                Debug.Log($"�N���b�v���擾�@���[�g:{frameRate:0.00} ����:{length:0.00} ���݂̃t���[��:{currentFrame}");

                for (int i = 0; i < copyModelList.Count; i++) {
                    int copyModelsFrame = (int)((frameRate * length)/copyModelList.Count)*i;
                    Debug.Log("���f���̑Ώۃt���[��:" + copyModelsFrame);

                    if(copyModelsFrame == currentFrame) {
                        Transform copy = copyModelList[i];
                        Transform root = copy.Find(targetRoot.name);
                        cp.CopyPoseAll(root, targetRoot);
                        cp.CopyWorld(root, targetRoot);

                        SkinnedMeshRenderer[] meshRenderers = copy.GetComponentsInChildren<SkinnedMeshRenderer>();
                        foreach (SkinnedMeshRenderer mr in meshRenderers) {
                            Color color = Color.green;
                            color.a = alpha / i;
                            mr.material.color = color;
                            Debug.Log("�O���i�΁j�\��");
                        }
                    }
                }

                copyModel.gameObject.SetActive(false);
            }
        }




    }
}

#endif