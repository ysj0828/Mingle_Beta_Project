using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectSMB : StateMachineBehaviour
{
    // private PlayerActionManager _playerManager = null;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // if (_playerManager == null) _playerManager = animator.gameObject.GetComponent<PlayerActionManager>();

        // Destroy Effect in RPC? - need to test
        //// PlayerActionManager playerManager = animator.GetComponent<PlayerActionManager>();
        // _playerManager.DestroyEffect();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //// PlayerPlayerActionManagerManager playerManager = animator.GetComponent<PlayerActionManager>();
        //_playerManager.DestroyEffect();

        animator.SetBool("isIdle", true);
    }

    // public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {

    // }
}