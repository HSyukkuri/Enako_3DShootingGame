using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BodyPart {
    Head,
    Chest,
    Hip,
    ArmUpper_R,
    ArmLower_R,
    LegUpper_R,
    LegLower_R,
    ArmUpper_L,
    ArmLower_L,
    LegUpper_L,
    LegLower_L,
}

public interface ITakeDamage{
    void TakeDamage(float damage,Vector3 hitPos , Vector3 hitDirection,BodyPart bodyPart);
}

public interface IVisible {
    void Detected();
}
