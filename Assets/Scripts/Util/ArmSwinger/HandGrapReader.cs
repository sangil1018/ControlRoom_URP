using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HandGrapReader : MonoBehaviour
{
    public HandGrapState state;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "PlayerHand")
        {
            state.isGrap = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "PlayerHand")
        {
            state.isGrap = false;
        }
    }
}
