using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mingle;
using System;

public class EmoticonSMB : StateMachineBehaviour
// public class EmoticonSMB : SMBInheritTest
{
    private AvatarRPC _rpc = null;
    private PlayerActionManager _actionManager = null;
    // private static PlayerActionManager _staticActionManager = null;
    private bool alreadyTriggered = false;

    private bool triggerBool = false;

    private DateTime startTime;

    private bool canTriggerIdle = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_rpc == null) _rpc = animator.gameObject.GetComponent<AvatarRPC>();
        if (_actionManager == null)
        {
            _actionManager = animator.gameObject.GetComponent<PlayerActionManager>();
            // _staticActionManager = _actionManager;
        }

        _actionManager.CurrentState = AvatarState.Animation;
        // _actionManager.CurrentStateSMB = this;

        animator.SetInteger("Emoticon", 0);

        animator.SetBool("EmoticonFinished", false);
        alreadyTriggered = false;

        _actionManager.AllowIdleTransition = false;

        // animator.ResetTrigger("idleTrigger");
        canTriggerIdle = false;

        // Debug.LogWarning("<color=white> time :  </color>" + DateTime.Now);
        startTime = DateTime.Now;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.normalizedTime >= 0.99f && !triggerBool && canTriggerIdle && _actionManager.emoticonStartTime < DateTime.Now)
        {
            triggerBool = true;
            // animator.SetTrigger("IdleTrigger");
            _actionManager.CurrentState = AvatarState.Idle;
        }

        if (stateInfo.normalizedTime >= 0.75f && !canTriggerIdle)
        {
            canTriggerIdle = true;
            triggerBool = false;
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    // override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {

    // }
}
