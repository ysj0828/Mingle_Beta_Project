using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mingle;

public class AvatarInteractionSMB : StateMachineBehaviour
{
    private PlayerActionManager _actionManager = null;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_actionManager == null) _actionManager = animator.gameObject.GetComponent<PlayerActionManager>();
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("C_MO_Touch05") && stateInfo.normalizedTime >= 0.99f && _actionManager.CurrentState != AvatarState.Idle)
        {
            _actionManager.CurrentState = AvatarState.Idle;
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _actionManager.CurrentState = AvatarState.Idle;
    }
}

