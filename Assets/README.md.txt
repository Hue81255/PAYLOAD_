# Camera System (Cinemachine)

구역 오브젝트를 클릭하면 해당 구역 카메라로 이동함

---


클릭
→ Trigger 실행
→ Animator 상태 변경
→ StateDrivenCamera가 카메라 전환

---

## 사용 방법

1. 구역 오브젝트에 `RegionClickTrigger` 추가

2. Collider 추가

3. Inspector 설정

- Camera Animator  
→ Animator가 붙어있는 오브젝트 연결

- Trigger Name  
→ Animator Parameter 이름 그대로 입력

예:
Dong gu

---

##  Animator 설정

- Parameters에 Trigger 생성
- 이름은 Trigger Name과 동일

- State 생성
- 각 State를 해당 Virtual Camera와 연결

---

##  카메라 설정

- 각 구역마다 Virtual Camera

예:
- Dong gu vcam
- Seo gu vcam
- Daegu vcam

- StateDrivenCamera에서  
State ↔ Virtual Camera 연결

---


##  테스트

Play → 구역 클릭 → 카메라 이동 확인

-----------------------------------------------------------------------
해킹 성공시 파티클 이펙트

###  기능
특정 이벤트 발생 시 (예: 해킹 성공)  
→ 파티클 효과 실행  

현재는 테스트용으로 Space 키 입력 시 실행되도록 구현

---

###  구성
- Particle System을 이용한 시각 효과

---

###  사용 방법
- 이벤트 발생 시 파티클 재생

---------------------------------------------


 코인 텍스트 이펙트

###  기능
코인 획득 시  
→ 숫자 증가  
→ 텍스트가 커졌다가 원래 크기로 돌아오며 피드백 제공

현재는 테스트용으로 C 키 입력 시 실행됨

---

###  구성
- TextMeshPro UI 텍스트
- DOTween을 이용한 스케일 애니메이션

---

### 사용 방법
- 코인 값 변경 시 텍스트 업데이트
- 동시에 스케일 애니메이션 실행

---

###  참고
- 텍스트는 기본 크기(Scale 1) 기준으로 애니메이션 적용
