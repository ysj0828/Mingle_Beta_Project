using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mingle;


namespace Mingle
{
    public class InertiaForce : MonoBehaviour
    {
        Transform parentTransform;  // 부모 본의 트랜스폼 
        Rigidbody boneRigidbody;  // 이 스크립트가 포함된 본 오브젝트의 리지드 바디 
        Vector3 prevFrameParentPosition = Vector3.zero;  // 이전 프레임까지의 부모 본의 위치 
        public float power = 0f;  // 관성 가중치 
        public float clampDist = 0.03f;
        // 변경된 위치의 크기 제한, 제한 값이 너무 크면 이 본이 제대로 따라가지 못해서
        // 각 관절들이 이상한 위치로 날아가는 문제가 발생할 수 있다.

        private void Start()
        {
            parentTransform = transform.parent;  // 부모 본의 트랜스폼 
            prevFrameParentPosition = parentTransform.position;  // 이전 프레임의 부모 본의 위치 
            boneRigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            Vector3 delta = (prevFrameParentPosition - parentTransform.position);  // 프레임 위치 차이 
            boneRigidbody.AddForce(Vector3.ClampMagnitude(delta, clampDist) * power);
            // 리지드 바디에 힘 추가
            // 벡터의 길이를 maxLength로 고정시킨 벡터의 복사본을 반환

            prevFrameParentPosition = parentTransform.position;
        }
    }

}
