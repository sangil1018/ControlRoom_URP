using Common.Extension;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;



public class PickupEventTrigger : ColliderEventTrigger
{
    [Header("[Pickup]")]
    [SerializeField] private GameObject mPickupObject = null;
    [SerializeField] protected string mEquipItemName = string.Empty;

    public override void Initiailze()
    {
        base.Initiailze();

        if (mPickupObject != null)
        {
            mPickupObject.SetActiveSelf(true);
        }
        else
        {
            DLogger.Error("트리거 설정 오류로 초기화 실패!");
        }
    }

    public override void OnTriggerEvent()
    {
        if (mHasTriggered == false)
        {
            // 아이템 오브젝트 픽업 처리
            mPickupObject.SetActiveSelf(false);

            // 아이템 착용 이벤트 호출
            if (mEquipItemName.IsNullOrEmpty() == false)
            {

            }

            base.OnTriggerEvent();
        }

        Debug.Log("[ItemPickupTrigger] 트리거 이벤트 호출됨.");
    }
}
