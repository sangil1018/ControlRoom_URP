using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;


public class PlayerController : MonoBehaviour
{
    [Header("Player Pose")]
    [SerializeField] private bool mIsSitting = false;
    [SerializeField] private CharacterController mCharacterController = null;
    [SerializeField] private XROrigin mXROrigin = null;
    [SerializeField] private Camera mXRCamera = null;

    [Header("Arm Swinger")]
    [SerializeField] private ArmSwinger_Hand mArmSwinger = null;

    [Header("[Head Tracker]")]
    [SerializeField] private Transform mHeadTrackerNode = null;
    [SerializeField] private Transform mHeadTrackerPoint = null;
    [SerializeField] private bool mIsHeadTracking = false;

    static public PlayerController Instance { get; private set; } = null;


    private void Awake()
    {
        Instance = this;

        // 헤드 트래킹 구독 처리
        //this.UpdateAsObservable()
        //    .TakeWhile(_ => mIsHeadTracking)
        //    .Where(_ => mHeadTrackerNode != null)
        //    .Subscribe(_ => OnUpdateHeadTracking())
        //    .AddTo(this); // 오브젝트 파괴 시 자동 해제
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void SetPosition(Transform targetPosition)
    {
        bool armSwingingPaused = false;
        if (mArmSwinger != null)
        {
            armSwingingPaused = mArmSwinger.armSwingingPaused;
            mArmSwinger.armSwingingPaused = false;
        }

        bool characterControllerEnable = false;
        if (mCharacterController != null)
        {
            characterControllerEnable = mCharacterController.enabled;
            mCharacterController.enabled = false;
        }       

        float angle = targetPosition.rotation.eulerAngles.y;
        Vector3 distance = targetPosition.position;

        if (mHeadTrackerNode)
        {
            angle -= mHeadTrackerNode.rotation.eulerAngles.y;
            distance -= mHeadTrackerNode.position;
            distance.y = 0;
        }
        transform.Rotate(0f, angle, 0f);
        transform.position += distance;

        if (mArmSwinger != null)
            mArmSwinger.armSwingingPaused = armSwingingPaused;
        if (mCharacterController != null)
            mCharacterController.enabled = characterControllerEnable;
    }

    public void SetArmSwingerPaused(bool paused)
    {
        mArmSwinger.armSwingingPaused = paused;
        mArmSwinger.preventionsPaused = paused;
        mArmSwinger.playAreaHeightAdjustmentPaused = paused;
    }

    public void SetSitPoseOnChair(Transform sitOrigin)
    {
        if (sitOrigin == null || mXROrigin == null)
        {
            return;
        }

        if (mArmSwinger != null)
        {
            SetArmSwingerPaused(true);
        }

        if (mXRCamera != null)
        {
            if (mXRCamera.GetComponent<TrackedPoseDriver>())
                mXRCamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
        }

        if (mCharacterController != null)
            mCharacterController.enabled = false;

        mXROrigin.transform.SetParent(sitOrigin, false);
        mXROrigin.transform.localPosition  = Vector3.zero;
        // 애니메이션 방향이 틀어져서 Y축 회전 보정
        mXROrigin.transform.localRotation = Quaternion.AngleAxis(180f, Vector3.up);
    }

    private void OnUpdateHeadTracking()
    {

    }

    //public void SetSitOnChair()
    //{
    //    if (mIsSitting) return;

    //    // 1. 나중에 일어날 때를 대비해 원래 위치 저장
    //    savedOriginPos = xrOrigin.position;
    //    savedOriginRot = xrOrigin.rotation;

    //    // CharacterController가 위치를 막지 않도록 일시 비활성화
    //    if (mCharacterController != null) 
    //        mCharacterController.enabled = false;

    //    // 2. 시선 방향(Y축 회전) 맞추기
    //    float angleDiff = seatEyeTarget.eulerAngles.y - mainCamera.eulerAngles.y;
    //    xrOrigin.RotateAround(mainCamera.position, Vector3.up, angleDiff);

    //    // 3. 눈(Main Camera) 위치를 고정 좌표(seatEyeTarget)에 정확히 일치시키기
    //    Vector3 positionOffset = seatEyeTarget.position - mainCamera.position;
    //    xrOrigin.position += positionOffset;

    //    mIsSitting = true;
    //}

    //public void SetStandUp()
    //{
    //    if (!mIsSitting) return;

    //    xrOrigin.position = savedOriginPos;
    //    xrOrigin.rotation = savedOriginRot;

    //    if (mCharacterController != null)
    //        mCharacterController.enabled = true;
        
    //    mIsSitting = false;
    //}
}
