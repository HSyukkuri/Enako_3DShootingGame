using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CQCData",menuName = "é©çÏÉfÅ[É^/CQCData")]
public class CQCData : ScriptableObject
{
    public AnimationClip clip;
    public AudioClip sound;
    public float soundDelay = 0f;
    public float ragdollTime = 1.5f;
    public float playerStateTime = 1.5f;
}
