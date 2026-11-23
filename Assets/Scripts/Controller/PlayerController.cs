using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Hands;

public class PlayerController : MonoBehaviour
{
    [Header("Arm Swinger")]
    [SerializeField] private ArmSwinger_Hand mArmSwinger = null;

    [Header("[Head Tracker]")]
    [SerializeField] private Transform mHeadTrackerNode = null;
    [SerializeField] private Transform mHeadTrackerPoint = null;
    [SerializeField] private bool mIsHeadTracking = false;
    [Space(7)]

    [Header("[Hand Visual Controllers]")]
    [SerializeField] private XRHandMeshController[] m_handVisualLeft;
    [SerializeField] private XRHandMeshController[] m_handVisualRight;
    [Header("[Default Hand Renderers]")]
    [SerializeField] public SkinnedMeshRenderer leftHandRenderer;
    [SerializeField] public SkinnedMeshRenderer rightHandRenderer;
    [Header("[Hand Meshes]")]
    [SerializeField] private Mesh m_meshBaseLeft;
    [SerializeField] private Mesh m_meshBaseRight;
    [Space(7)]
    [SerializeField] private Mesh m_meshGloveLeft;
    [SerializeField] private Mesh m_meshGloveRight;
    [FormerlySerializedAs("m_materialBase")]
    [Header("[Hand Materials]")]
    [SerializeField] private Material m_materialBaseLeft;
    [SerializeField] private Material m_materialBaseRight;
    [SerializeField] private Material m_materialGlove;

    public enum HandType { None, Left, Right }

    static public PlayerController Instance { get; private set; } = null;


    private void Awake()
    {
        Instance = this;

        // 헤드 트래킹 구독 처리
        this.UpdateAsObservable()
            .TakeWhile(_ => mIsHeadTracking)
            .Where(_ => mHeadTrackerNode != null)
            .Subscribe(_ => OnUpdateHeadTracking())
            .AddTo(this); // 오브젝트 파괴 시 자동 해제

        // 핸드 컨트롤러 초기화
        //SetHandMeshEnable(true);
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void SetPosition(Transform targetPosition)
    {
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

        //if (mArmSwinger != null)
        //    mArmSwinger.armSwingingPaused = false;

        //float angle = targetPosition.rotation.eulerAngles.y;
        //Vector3 distance = targetPosition.position;

        //if (mHeadTrackerNode)
        //{
        //    angle -= mHeadTrackerNode.rotation.eulerAngles.y;
        //    distance -= mHeadTrackerNode.position;
        //    distance.y = 0;
        //}
        //transform.Rotate(0f, angle, 0f);
        //transform.position += distance;

        //if (mArmSwinger != null)
        //    mArmSwinger.armSwingingPaused = true;
    }

    public void SetHandBase()
    {
        leftHandRenderer.sharedMesh = m_meshBaseLeft;
        rightHandRenderer.sharedMesh = m_meshBaseRight;

        leftHandRenderer.material = m_materialBaseLeft;
        rightHandRenderer.material = m_materialBaseRight;
    }

    public void SetHandGlove()
    {
        leftHandRenderer.sharedMesh = m_meshGloveLeft;
        rightHandRenderer.sharedMesh = m_meshGloveRight;

        leftHandRenderer.material = m_materialGlove;
        rightHandRenderer.material = m_materialGlove;
    } 

    public void SetHandMeshEnable(bool _active)
    {
        SetHandMeshEnable(HandType.None, _active);
    }

    public void SetHandMeshEnable(HandType _handType, bool _active)
    {
        if (HandType.Right != _handType)
        {
            //for (int i = 0; i < m_handVisualLeft.Length; i++)
            //    m_handVisualLeft[i].m_ShowMeshWhenTrackingIsAcquired = _active;
            leftHandRenderer.enabled = _active;
        }
        if (HandType.Left != _handType)
        {
            //for (int i = 0; i < m_handVisualRight.Length; i++)
            //    m_handVisualRight[i].m_ShowMeshWhenTrackingIsAcquired = _active;
            rightHandRenderer.enabled = _active;
        }
    }

    private void OnUpdateHeadTracking()
    {

    }
}
