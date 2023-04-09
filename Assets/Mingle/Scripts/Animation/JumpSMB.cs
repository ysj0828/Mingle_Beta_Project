using UnityEngine;

public class JumpSMB : StateMachineBehaviour
{
    // private PlayerActionManager _playerManager = null;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // if (_playerManager == null)
        // {
        //     _playerManager = animator.gameObject.GetComponent<PlayerActionManager>();
        // }
        // _playerManager.CurrentState = _playerManager.AvatarState.Jumping;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {

    }
}