using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System;

public class AvatarMoveScript : MonoBehaviour
{
    #region Private Fields
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Animator _animator;
    [SerializeField] private PhotonView _photonView;

    #endregion

    #region Public fields
    #endregion

    private void Start()
    {
        //! change from get component to direct reference in the inspector
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _photonView = GetComponent<PhotonView>();

        // CameraManager.EventActionFunction += TapObserver;
    }

    private void OnDestroy()
    {
        // CameraManager.EventActionFunction -= TapObserver;
    }


    #region Observer Functions
    private void TapVectorObserver()
    {
        // _photonView.RPC("RPCFunctionNameHere", RpcTarget.All, CameraManager.TapPosition);
    }

    private void TapAvatarObserver()
    {
        // _photonView.RPC("RPCFunctionNameHere", RpcTarget.All, CameraManager.TapPosition);
    }
    #endregion

    #region Coroutines
    private IEnumerator SimpleParabolic(Vector3 hitPoint)
    {
        _agent.ResetPath();
        _agent.enabled = false;
        _animator.SetBool("isJumping", true);
        Vector3 JumpingInitialPosition = transform.position;

        float ParabolicHeight = Mathf.Clamp(Vector3.Distance(JumpingInitialPosition, hitPoint) * 0.25f, 3, 10);

        float interpolant = 0;

        while (true)
        {
            //fps 60
            interpolant += 0.025f;

            //fps 30
            // interpolant += 0.05f;

            transform.position = JumpingTrajectory(JumpingInitialPosition, hitPoint, ParabolicHeight, interpolant);

            Vector2 transformVector2 = new Vector2(transform.position.x, transform.position.z);
            Vector2 tapPositionVector2 = new Vector2(hitPoint.x, hitPoint.z);

            if ((transformVector2 - tapPositionVector2).magnitude <= 0.01f)
            {
                yield break;
            }

            if (_photonView.IsMine)
            {
                yield return new WaitForEndOfFrame();
            }

            else if (!_photonView.IsMine)
            {
                // if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "jump01_loop") anim.Play("jump01_loop");
                yield return null;
            }
        }
    }

    #endregion

    private Vector3 JumpingTrajectory(Vector3 start, Vector3 end, float height, float t)
    {
        Func<float, float> f = x => -4 * height * x * x + 4 * height * x;

        var mid = Vector3.Lerp(start, end, t);

        return new Vector3(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t), mid.z);
    }
}
