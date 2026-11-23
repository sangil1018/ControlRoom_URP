using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UniRx;
using UniRx.Triggers;
using UnityEngine.Events;
using Common.Extension;


public class PhaseEventTrigger : MonoBehaviour
{
    [Header("[EventTrigger]")]
    [SerializeField] protected bool mIsRepeatable = false;

    [Header("[Render]")]    
    [SerializeField] protected Outline mOulineRender = null;
    [SerializeField] protected GameObject mGuideArrow = null;

    protected bool mHasTriggered = false;
    protected bool mIsActivePhase = false;
    protected UnityAction mCompletedEvent = null;

    public virtual void Initiailze()
    {
        mHasTriggered = false;  
        
        mGuideArrow.SetActiveSelf(false);
        mOulineRender.SetEnableSelf(false);
    }

    public virtual void Deinitiailze()
    {
        mCompletedEvent = null;
    }

    public virtual void OnActivePhase(UnityAction callback)
    {
        mIsActivePhase = true;
        mCompletedEvent = callback;

        mGuideArrow.SetActiveSelf(true);
        mOulineRender.SetEnableSelf(true);
    }

    public virtual void OnTriggerEvent()
    {
        if (mHasTriggered == false)
        {
            mHasTriggered = true; 

            mGuideArrow.SetActiveSelf(false);
            mOulineRender.SetEnableSelf(false);

            if (mCompletedEvent != null)
            {
                mCompletedEvent.Invoke();
                mCompletedEvent = null;
            }
        }  
    }
}
