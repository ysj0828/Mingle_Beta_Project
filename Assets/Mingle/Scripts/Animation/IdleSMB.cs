using UnityEngine;
using Photon.Pun;
using Mingle;

public class IdleSMB : StateMachineBehaviour
{
    // private PlayerActionManager _playerManager = null;
    private PhotonView _photonView = null;
    private bool _alreadyTriggered = false;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_photonView == null) _photonView = animator.gameObject.GetComponent<PhotonView>();
        // if (_playerManager == null) _playerManager = animator.gameObject.GetComponent<PlayerActionManager>();
        // _playerManager.CurrentState = PlayerActionManager.AvatarState.Idle;
        // animator.SetBool("")

        // if (_photonView.IsMine) animator.SetFloat("IdleBlend", Random.Range(0, 2));
        // animator.SetBool("isIdle", false);
        _alreadyTriggered = false;
    }

    // public override void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    // {
    // animator.SetBool("isIdle", false);
    // }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!_alreadyTriggered && stateInfo.normalizedTime >= 0.05f)
        {
            // if (_photonView.IsMine) animator.SetFloat("IdleBlend", Random.Range(0, 2));
            animator.SetBool("isIdle", false);
            _alreadyTriggered = true;
        }
    }
}