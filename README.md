# Mingle_Beta_Project
밍글 베타 프로젝트\
사용한 유니티 버전 : 2021.3.14f1 LTS

베타 버전은 아래 링크를 통해 테스트 할 수 있습니다.\
https://linktr.ee/mingle.official

# 담당 업무

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

1. 1 손가락 드래그
본인 캐릭터를 중심으로 회전

2. 2 손가락 드래그
캐릭터 포커스 해제 후 카메라를 상하좌우로 이동

2-1. 2 손가락 드래그 후 1 손가락 드래그 : 다시 캐릭터에 포커스 & 캐릭터 센터에 맞추기

![Screen_Recording_20230417_110813_Unity_Mingle-2](https://user-images.githubusercontent.com/63217600/232361965-0477ccce-b16f-4235-86fd-e7b370da0721.gif)


## 포톤

### 캐릭터 위치, 모션, 커스터마이징 동기화
RPC를 활용하여 동기화 진행

캐릭터 이동 동기화 \
![Screen_Recording_20230417_113600_Unity_Mingle](https://user-images.githubusercontent.com/63217600/232365714-ff8c59d3-d428-4e7f-a075-eb4a05366c63.gif)

캐릭터 이모티콘 애니메이션 동기화 \
![Screen_Recording_20230417_113759_Unity_Mingle](https://user-images.githubusercontent.com/63217600/232365722-d25c2569-7c71-4e97-977f-0c2117f43747.gif)


### Resources 폴더 밖에서 PhotonNetwork.Instantiate() 함수를 사용할 수 있게 구현
검색해도 Resources 폴더 밖에 있는 에셋을 생성하는 법이 안나와서 포톤 내부 코드 분석

아래와 같이 Assets/Test 폴더 안에 "CubePrefab" 을 PhotonNetwork.Instantiate() 함수로 생성하려하면 오류가 나온다.
Resources 폴더에 넣거나 Custom IPunPrefabPool을 생성하라고 한다.
<img width="1751" alt="Screenshot 2023-04-17 at 11 56 08 AM" src="https://user-images.githubusercontent.com/63217600/232367135-440a753e-92fe-4c99-83d9-15061574cbf3.png">



아래 방식처럼 DefaultPool 타입의 변수 생성 후 직접 프리팹을 등록해주면 PhotonNetwork.Instantiate() 함수를 사용할 수 있다.

<img width="1554" alt="Screenshot 2023-04-17 at 1 54 24 PM" src="https://user-images.githubusercontent.com/63217600/232381938-f3afc3d5-212d-499d-877a-e326801b763f.png">

![Screen Recording 2023-04-17 at 12 29 18 PM](https://user-images.githubusercontent.com/63217600/232381859-93eae7cd-ee07-4b8c-99f6-dfa593cdf56c.gif)

밍글 프로젝트에선 개념은 같으나 Addressable을 사용하여 필요한 에셋 다운로드 후 해당 에셋을 DefaultPool에 등록한 뒤 생성하는 방식


### 
