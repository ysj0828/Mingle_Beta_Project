using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Mingle;

public class MoveSMB : StateMachineBehaviour
{
    private NavMeshAgent _agent = null;
    private PhotonView _photonView = null;
    private PlayerActionManager _playerManager = null;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_agent == null) _agent = animator.gameObject.GetComponent<NavMeshAgent>();
        if (_photonView == null) _photonView = animator.gameObject.GetComponent<PhotonView>();
        if (_playerManager == null) _playerManager = animator.gameObject.GetComponent<PlayerActionManager>();
        // _playerManager.CurrentState = _playerManager.AvatarState.Moving;

        if (_photonView.IsMine) animator.SetFloat("WalkBlend", Random.Range(0, 2));
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_playerManager.CurrentState != AvatarState.ObjectInteraction)
        {
            _playerManager.CurrentState = AvatarState.Idle;
        }

    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_agent.enabled == false) return;
        // Debug.Log("remaining distance move smb : " + _agent.remainingDistance);
        if (_agent?.remainingDistance <= _agent?.stoppingDistance + 0.1f && _playerManager.CurrentState != AvatarState.ObjectInteraction && _playerManager.CurrentState != AvatarState.Idle)
        {
            // Debug.Log("remain : " + _agent.remainingDistance);
            _agent?.ResetPath();
            // Debug.LogWarning("debug in move smb update");
            _playerManager.CurrentState = AvatarState.Idle;
        }
    }
}
