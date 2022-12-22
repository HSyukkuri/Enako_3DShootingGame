#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MyUtility;
using System.Linq;

[ExecuteInEditMode]
public class MirrorCopy : MonoBehaviour {

    //アニメーションのプロパティ
    [System.Serializable]
    class AnimationProperty {
        public List<Keyframe> posX = new List<Keyframe>();
        public List<Keyframe> posY = new List<Keyframe>();
        public List<Keyframe> posZ = new List<Keyframe>();

        public List<Keyframe> rotX = new List<Keyframe>();
        public List<Keyframe> rotY = new List<Keyframe>();
        public List<Keyframe> rotZ = new List<Keyframe>();
        public List<Keyframe> rotW = new List<Keyframe>();

        public Keyframe[] GetMirrorPosX() {
            Keyframe[] keyframes = posX.ToArray();
            for (int i = 0; i < keyframes.Length; i++) {
                Keyframe keyframe = keyframes[i];
                keyframe.value *= -1f;
                keyframe.inTangent *= -1f;
                keyframe.outTangent *= -1f;
                keyframes[i] = keyframe;
            }
            return keyframes;
        }

        public Quaternion GetMirrorQuaternion(int index) {
            return new Quaternion(
                rotX[index].value,
                rotY[index].value * -1f,
                rotZ[index].value * -1f,
                rotW[index].value
                ) ;
        }

        public Keyframe[] GetMirrorRotX() {
            Keyframe[] keyframes = rotX.ToArray();
            for (int i = 0; i < keyframes.Length; i++) {
                Keyframe keyframe = keyframes[i];
                keyframe.value = GetMirrorQuaternion(i).x;
                keyframes[i] = keyframe;
            }
            return keyframes;
        }

        public Keyframe[] GetMirrorRotX_Euler() {
            Keyframe[] keyframes = rotX.ToArray();
            return keyframes;
        }

        public Keyframe[] GetMirrorRotY() {
            Keyframe[] keyframes = rotY.ToArray();
            for (int i = 0; i < keyframes.Length; i++) {
                Keyframe keyframe = keyframes[i];
                keyframe.value = GetMirrorQuaternion(i).y;
                keyframe.inTangent *= -1f;
                keyframe.outTangent *= -1f;
                keyframes[i] = keyframe;
            }
            return keyframes;
        }

        public Keyframe[] GetMirrorRotY_Euler() {
            Keyframe[] keyframes = rotY.ToArray();
            for (int i = 0; i < keyframes.Length; i++) {
                Keyframe keyframe = keyframes[i];
                keyframe.value *= -1f;
                keyframe.inTangent *= -1f;
                keyframe.outTangent *= -1f;
                keyframes[i] = keyframe;
            }

            return keyframes;
        }

        public Keyframe[] GetMirrorRotZ() {
            Keyframe[] keyframes = rotZ.ToArray();
            for (int i = 0; i < keyframes.Length; i++) {
                Keyframe keyframe = keyframes[i];
                keyframe.value = GetMirrorQuaternion(i).z;
                keyframe.inTangent *= -1f;
                keyframe.outTangent *= -1f;
                keyframes[i] = keyframe;
            }
            return keyframes;
        }

        public Keyframe[] GetMirrorRotZ_Euler() {
            Keyframe[] keyframes = rotZ.ToArray();
            for (int i = 0; i < keyframes.Length; i++) {
                Keyframe keyframe = keyframes[i];
                keyframe.value *= -1f;
                keyframe.inTangent *= -1f;
                keyframe.outTangent *= -1f;
                keyframes[i] = keyframe;
            }

            return keyframes;
        }

        public Keyframe[] GetMirrorRotW() {
            Keyframe[] keyframes = rotW.ToArray();
            for (int i = 0; i < keyframes.Length; i++) {
                Keyframe keyframe = keyframes[i];
                keyframe.value = GetMirrorQuaternion(i).w;
                keyframe.inTangent *= -1f;
                keyframe.outTangent *= -1f;
                keyframes[i] = keyframe;
            }
            return keyframes;
        }


    }

    //ミラーのセット
    [System.Serializable]
    public class MirrorSet {
        public Transform left;
        public Transform right;
    }

    
    public AnimationWindow animWindow;

    public bool Double = false;

    public List<MirrorSet> MirrorSetList = new List<MirrorSet>();

    AnimationClip clip;

    int frame = 0;



    void Update() {


    }


    [ContextMenu("反転")]
    private void Mirror() {
        animWindow = (AnimationWindow)EditorWindow.GetWindow(typeof(UnityEditor.AnimationWindow));

        if (animWindow == null) {
            return;
        }

        clip = animWindow.animationClip;

        if(clip == null) {
            Debug.Log("clip取得失敗");
            return;
        }

        float frameRate = clip.frameRate;
        float length = clip.length;
        int currentFrame = animWindow.frame;

        float curentTime = currentFrame / frameRate;

        
        //AnimationPropertyディクショナリー初期化
        Dictionary<string,AnimationProperty> animationProperties = new Dictionary<string, AnimationProperty>();
       
        //Bindings取得
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

        //BindingsからAnimationPropertyを生成
        foreach(EditorCurveBinding binding in bindings) {

            string boneName = System.IO.Path.GetFileName(binding.path);

            if (!animationProperties.ContainsKey(boneName)) {
                animationProperties.Add(boneName, new AnimationProperty());
            }

            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

            switch (binding.propertyName) {

                case "m_LocalPosition.x":
                    animationProperties[boneName].posX.AddRange(curve.keys);
                    break;

                case "m_LocalPosition.y":
                    animationProperties[boneName].posY.AddRange(curve.keys);
                    break;

                case "m_LocalPosition.z":
                    animationProperties[boneName].posZ.AddRange(curve.keys);
                    break;

                case "m_LocalRotation.x":
                case "localEulerAnglesRaw.x":
                    animationProperties[boneName].rotX.AddRange(curve.keys);
                    break;

                case "m_LocalRotation.y":
                case "localEulerAnglesRaw.y":
                    animationProperties[boneName].rotY.AddRange(curve.keys);
                    break;

                case "m_LocalRotation.z":
                case "localEulerAnglesRaw.z":
                    animationProperties[boneName].rotZ.AddRange(curve.keys);
                    break;

                case "m_LocalRotation.w":
                    animationProperties[boneName].rotW.AddRange(curve.keys);
                    break;
            }



        }

        //反転
        foreach(MirrorSet mirrorSet in MirrorSetList) {
            //左半身

            if (!animationProperties.ContainsKey(mirrorSet.left.name)) {
                continue;
            }

            AnimationProperty animationPropertyLeft = animationProperties[mirrorSet.left.name];

            if (Double) {
                ReverseDouble(animationPropertyLeft, bindings, mirrorSet.right.name, curentTime) ;
            }
            else {
                Reverse(animationPropertyLeft, bindings, mirrorSet.right.name);
            }

            if (mirrorSet.left == mirrorSet.right) {
                continue;
            }

            //右半身

            if (!animationProperties.ContainsKey(mirrorSet.right.name)) {
                continue;
            }

            AnimationProperty animationPropertyRight = animationProperties[mirrorSet.right.name];

            if (Double) {
                ReverseDouble(animationPropertyRight, bindings, mirrorSet.left.name, curentTime);
            }
            else {
                Reverse(animationPropertyRight, bindings, mirrorSet.left.name);
            }

        }

    }



        void Reverse(AnimationProperty animationProperty, EditorCurveBinding[] bindings,string boneName) {

            foreach (EditorCurveBinding binding in bindings) {

                if (boneName == System.IO.Path.GetFileName(binding.path)) {

                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

                    switch (binding.propertyName) {

                        case "m_LocalPosition.x":
                            curve.keys = animationProperty.GetMirrorPosX();
                            break;

                        case "m_LocalPosition.y":
                            curve.keys = animationProperty.posY.ToArray();
                            break;

                        case "m_LocalPosition.z":
                            curve.keys = animationProperty.posZ.ToArray();
                            break;

                        case "m_LocalRotation.x":
                            curve.keys = animationProperty.GetMirrorRotX();
                            break;
                        case "localEulerAnglesRaw.x":
                            curve.keys = animationProperty.GetMirrorRotX_Euler();
                            break;

                        case "m_LocalRotation.y":
                            curve.keys = animationProperty.GetMirrorRotY();
                            break;
                        case "localEulerAnglesRaw.y":
                            curve.keys = animationProperty.GetMirrorRotY_Euler();
                            break;

                        case "m_LocalRotation.z":
                            curve.keys = animationProperty.GetMirrorRotZ();
                            break;
                        case "localEulerAnglesRaw.z":
                            curve.keys = animationProperty.GetMirrorRotZ_Euler();
                            break;

                        case "m_LocalRotation.w":
                            curve.keys = animationProperty.GetMirrorRotW();
                            break;

                    }

                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                }
            }

        }

    void ReverseDouble(AnimationProperty animationProperty, EditorCurveBinding[] bindings, string boneName, float currentTime) {

        foreach (EditorCurveBinding binding in bindings) {

            if (boneName == System.IO.Path.GetFileName(binding.path)) {

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

                AnimationCurve curveCopy = AnimationUtility.GetEditorCurve(clip, binding);

                switch (binding.propertyName) {

                    case "m_LocalPosition.x":
                        curveCopy.keys = animationProperty.GetMirrorPosX();
                        break;

                    case "m_LocalPosition.y":
                        curveCopy.keys = animationProperty.posY.ToArray();
                        break;

                    case "m_LocalPosition.z":
                        curveCopy.keys = animationProperty.posZ.ToArray();
                        break;

                    case "m_LocalRotation.x":
                        curveCopy.keys = animationProperty.GetMirrorRotX();
                        break;
                    case "localEulerAnglesRaw.x":
                        curveCopy.keys = animationProperty.GetMirrorRotX_Euler();
                        break;

                    case "m_LocalRotation.y":
                        curveCopy.keys = animationProperty.GetMirrorRotY();
                        break;
                    case "localEulerAnglesRaw.y":
                        curveCopy.keys = animationProperty.GetMirrorRotY_Euler();
                        break;

                    case "m_LocalRotation.z":
                        curveCopy.keys = animationProperty.GetMirrorRotZ();
                        break;
                    case "localEulerAnglesRaw.z":
                        curveCopy.keys = animationProperty.GetMirrorRotZ_Euler();
                        break;

                    case "m_LocalRotation.w":
                        curveCopy.keys = animationProperty.GetMirrorRotW();
                        break;
                }

                Keyframe[] copykeyframes = curveCopy.keys;

                for (int i = 0; i < copykeyframes.Length; i++) {
                    copykeyframes[i].time += currentTime;

                    curve.AddKey(copykeyframes[i]);
                }

                //clip.SetCurve(binding.path, typeof(Transform), binding.propertyName, curve);
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

    }


}

#endif