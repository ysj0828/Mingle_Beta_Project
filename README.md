# Mingle_Beta_Project
밍글 베타 프로젝트\
사용한 유니티 버전 : 2021.3.14f1 LTS

# 담당 업무

담당 업무 스크립트는 Mingle_Beta_Project/Assets/Mingle/Scripts/Animation 혹은 Camera 혹은 Manager/PlayerActionManager.cs, PlayerManager.cs, PhotonManager.cs 에서 확인 할 수 있습니다.

## 캐릭터 애니메이션

Mecanim Animation System을 활용

<img width="1153" alt="Screenshot 2023-04-04 at 12 12 11 PM" src="https://user-images.githubusercontent.com/63217600/229677957-e3345628-a869-46e1-b381-9ed9670fe9d8.png">

### 이모티콘 애니메이션

같은 이모티콘이어도 여러 애니메이션 중 랜덤으로 애니메이션을 재생해야 하기 때문에 BlendTree를 활용

![Screen Recording 2023-04-04 at 4 23 59 PM](https://user-images.githubusercontent.com/63217600/229719332-226b7bdf-2299-49bf-af3f-4c8e90540e9d.gif)

### 캐릭터 이동

걷기 / 뛰기

![Screen Recording 2023-04-04 at 4 33 57 PM-3](https://user-images.githubusercontent.com/63217600/229948766-906925e2-8f7d-497f-9a4d-fa909676e2c9.gif)

점프

![Screen-Recording-2023-04-04-at-4-2](https://user-images.githubusercontent.com/63217600/229948549-cc6bed2e-e357-4663-9e12-9d2cef878c7e.gif)



## 카메라 조작

1 손가락 드래그
본인 캐릭터를 중심으로 회전
TODO - 동영상 찍어서 올리기

2 손가락 드래그
캐릭터 포커스 해제 후 카메라를 상하좌우로 이동
TODO - 동영상 찍어서 올리기


## 포톤

### 캐릭터 위치, 모션, 커스터마이징 동기화
RPC를 활용하여 동기화 진행
TODO - 동영상 찍어서 

### Resources 폴더 밖에서 PhotonNetwork.Instantiate() 함수를 사용할 수 있게 구현
TODO - 코드 설명

### 
