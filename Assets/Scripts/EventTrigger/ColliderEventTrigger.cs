using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Events;


//[RequireComponent(typeof(Collider))]
public class ColliderEventTrigger : PhaseEventTrigger
{
    [Header("[Collider]")]
    [SerializeField] private LayerMask mTriggerLayer;
    [SerializeField] private Collider mCollider;

    private void Awake()
    {
        if (mCollider == null)
            mCollider = GetComponent<Collider>();
    }

    public override void Initiailze()
    {
        if (mCollider != null)
        {
            mCollider.OnTriggerEnterAsObservable()
            .TakeWhile(IsTakeWhile) // 반복 호출 및 구독 해제 설정
            .Where(IsTargetLayer)
            .Subscribe(OnTriggerSubscribe)
            .AddTo(this); // 오브젝트 파괴 시 자동 디스포즈(메모리 해제)       
        }             

        base.Initiailze();
    }

    public override void OnActivePhase(UnityAction callback)
    {
        base.OnActivePhase(callback);
    }

    private bool IsTakeWhile(Collider collider) => 
        mIsActivePhase && (mIsRepeatable || !mHasTriggered);

    private bool IsTargetLayer(Collider collider) =>
        (mTriggerLayer.value & (1 << collider.gameObject.layer)) != 0;

    private void OnTriggerSubscribe(Collider other) => OnTriggerEvent(); 
}
