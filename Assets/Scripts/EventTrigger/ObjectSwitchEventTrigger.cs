using Common.Extension;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;



public class ObjectSwitchEventTrigger : ColliderEventTrigger
{
    [Header("[Switching]")]
    [SerializeField] private GameObject mBeforeObject = null;
    [SerializeField] private GameObject mAfterObject = null;

    public override void Initiailze()
    {
        base.Initiailze();

        mBeforeObject.SetActiveSelf(true);
        mAfterObject.SetActiveSelf(false);
    }

    public override void OnTriggerEvent()
    {
        if (mHasTriggered == false)
        {
            mAfterObject.SetActiveSelf(true);
            mBeforeObject.SetActiveSelf(false);

            base.OnTriggerEvent();
        }

        Debug.Log("[ObjectSwitchEventTrigger] 트리거 이벤트 호출됨.");
    }
}
