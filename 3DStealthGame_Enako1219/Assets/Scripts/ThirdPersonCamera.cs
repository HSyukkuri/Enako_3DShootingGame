using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class ThirdPersonCamera : MonoBehaviour {

    public CinemachineVirtualCamera aimCam;
    public CinemachineVirtualCamera aimCrawlCam;
    public CinemachineVirtualCamera normalCam;
    public CinemachineVirtualCamera bodyCam;
    public CinemachineVirtualCamera eventCam;
    public CinemachineVirtualCamera jankenCam;
    public CinemachineBrain brainCam;

    public bool lockCursor;
    float mouseSensitivity;

    public Vector2 verticalMinMax = new Vector2(-40, 85);
    public Transform lookTarget;
    public LayerMask collisionMask;


    float rotationSmoothTime = 0.12f;

    public float horizontal { get; private set; }
    public float vertical { get; private set; }

    Vector2 currentMouseDelta = Vector2.zero;
    Vector2 currentMouseDeltaVelocity = Vector2.zero;


    PlayerController playerCon;

    GameManager gm;

    CinemachineFramingTransposer cinemaFramingTransAIM;
    CinemachineFramingTransposer cinemaFramingTransNOR;
    
    public const float HEIGHT_stand_Nor = 1.23f;
    public const float HEIGHT_stand_Aim = 1.3f;

    public const float HEIGHT_squat_Nor = 1.1f;
    public const float HEIGHT_squat_Aim = 1f;

    public const float Z_Aim = -0.65f;


    float x_offset = 0f;
    float x_offsetVelocity = 0f;

    float y_offset = 1.23f;
    float y_offsetVelocity = 0f;


    private void Start() {
        gm = GameManager.instance;


        playerCon = FindObjectOfType<PlayerController>();

        mouseSensitivity = 2;

        cinemaFramingTransAIM = aimCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        cinemaFramingTransNOR = normalCam.GetCinemachineComponent<CinemachineFramingTransposer>();


        vertical = normalCam.transform.eulerAngles.x;
        horizontal = normalCam.transform.eulerAngles.y;


        x_offset = cinemaFramingTransAIM.m_TrackedObjectOffset.x;
    }

    private void Update() {
        cinemaFramingTransAIM.m_TrackedObjectOffset.x = Mathf.SmoothDamp(cinemaFramingTransAIM.m_TrackedObjectOffset.x, x_offset, ref x_offsetVelocity, 0.1f);
        cinemaFramingTransAIM.m_TrackedObjectOffset.y = Mathf.SmoothDamp(cinemaFramingTransAIM.m_TrackedObjectOffset.y, y_offset, ref y_offsetVelocity, 0.1f);

        cinemaFramingTransNOR.m_TrackedObjectOffset.y = cinemaFramingTransAIM.m_TrackedObjectOffset.y;


        switch (playerCon.currentState) {
            case PlayerController.State.Aim_Stand:
            case PlayerController.State.Aim_Shoot:
            case PlayerController.State.Aim_Shoot_OutOfAmmo:
            case PlayerController.State.Crawling_Aim: {
                    CameraAngleUpdateAim();
                    return;
                }

            default: {
                    CameraAngleUpdate();
                    return;
                }
        }

    }

    public void CameraAngleUpdate() {
        Vector2 targetMouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseDeltaVelocity, rotationSmoothTime);

        vertical -= currentMouseDelta.y * mouseSensitivity;
        vertical = Mathf.Clamp(vertical, verticalMinMax.x, verticalMinMax.y);

        horizontal += currentMouseDelta.x * mouseSensitivity;
        horizontal = Mathf.Repeat(horizontal, 360);


        normalCam.transform.eulerAngles = new Vector3(vertical,horizontal);
        aimCam.transform.eulerAngles = new Vector3(vertical, horizontal);
        aimCrawlCam.transform.eulerAngles = new Vector3(vertical, horizontal);
        bodyCam.transform.eulerAngles = new Vector3(vertical, horizontal);
        jankenCam.transform.eulerAngles = new Vector3(vertical, horizontal);

    }

    public void CameraAngleUpdateAim() {

        currentMouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        vertical -= currentMouseDelta.y * mouseSensitivity * 0.5f;
        vertical = Mathf.Clamp(vertical, verticalMinMax.x, verticalMinMax.y);

        horizontal += currentMouseDelta.x * mouseSensitivity * 0.5f;
        horizontal = Mathf.Repeat(horizontal, 360);


        normalCam.transform.eulerAngles = new Vector3(vertical, horizontal);
        aimCam.transform.eulerAngles = new Vector3(vertical, horizontal);
        aimCrawlCam.transform.eulerAngles = new Vector3(vertical, horizontal);
        bodyCam.transform.eulerAngles = new Vector3(vertical, horizontal);
    }

    private void LateUpdate() {
        CheckCollisions();
    }


    void CheckCollisions() {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000, collisionMask, QueryTriggerInteraction.Collide)) {
            lookTarget.position = hit.point;
        }
        else {
            lookTarget.position = transform.position + transform.forward * 30;
        }
    }

    public void SetCameraHeight(float height) {
        y_offset = height;
    }

    public void SwitchAimCameraSide() {
        x_offset *= -1f;
    }

    public void SwitchAimCameraSide(bool left) {

        if (left) {
            if (x_offset < 0) {
                return;
            }

            x_offset *= -1f;
        }
        else {

            if(x_offset > 0) {
                return;
            }

            x_offset *= -1f;
        }

    }


    public void OnEventEnd() {
        normalCam.enabled = false;
        vertical = eventCam.transform.eulerAngles.x;
        if(vertical > verticalMinMax.y) {
            vertical -= 360;
        }
        else
        if(vertical < verticalMinMax.x){
            vertical += 360;
        }
        horizontal = eventCam.transform.eulerAngles.y;
        normalCam.transform.position = eventCam.transform.position;
        normalCam.transform.rotation = eventCam.transform.rotation;
        normalCam.enabled = true;
        normalCam.Priority = 10;

        
    }

    public void SetMouseSensitivity(float sensitivity) {
        mouseSensitivity = sensitivity;
    }

}
