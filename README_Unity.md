# Control Room - Unity 이관 패키지

대상: Unity 2022.3.62f3 / URP
원본: Indoor.blend (Blender 5.2) -> 정리본 Indoor_UnityExport.blend 에서 추출

## 1. 폴더 구성
FBX/Room_Shell.fbx            벽·바닥·천장·문·랙·에어컨 등 고정 구조 (38 오브젝트)
FBX/Props/*.fbx               반복 에셋 25종. 각 파일은 원점 기준, 프리팹 원본
FBX/Layout.fbx                배치용 Empty 208개
                                <에셋명>__<오브젝트명>   프롭 위치
                                LIGHT__LED_01..25       천장 LED 패널 위치
                                CAM__<이름> / CAMTGT__<이름>  카메라 위치/주시점
FBX/Indoor_Full_Reference.fbx 전체 씬 한 덩어리. 자동 배치 기준 + 검증용. 씬에 직접 쓰지 말 것
Textures/                     타일 텍스처 4장 (바닥 액세스타일, 천장 텍스, 원목, 의자 메쉬망)
Textures/Screens/             화면 이미지 19장 (임시 HMI + GRP_01~13 번호판)
materials_unity.json          머티리얼 30개 정의 (색 sRGB, metallic, smoothness, emission, 타일링, 컷아웃)
screens.json / screen_list.csv  화면 39면: 그룹, 티어, 프레임 수, 실제 크기(m)
props_map.json                프롭 오브젝트 175개 -> 에셋 25종 매핑
Scripts/ScreenSlot.cs         화면 정보 컴포넌트
Scripts/ScreenFlipbook.cs     화면 이미지 교체/플립북
Scripts/Editor/ControlRoomBuilder.cs  자동 빌드 에디터 스크립트

## 2. Unity 프로젝트 내 위치
Assets/ControlRoom/FBX, FBX/Props, Textures, Textures/Screens
Assets/ControlRoom/Data/  (json 3개)
Assets/ControlRoom/Scripts/ (Editor 포함)
빌드 결과: Materials/, Prefabs/, Scenes/ControlRoom.unity (+ .lighting)

## 3. 자동 빌드
처음 한 번: ControlRoom > 0. Setup URP Asset (Assets/ControlRoom/Settings 에 URP 에셋 생성·지정)
메뉴 ControlRoom > 1. Build All -> 2. Bake Lighting -> 3. Save Screenshots
배치: Unity.exe -batchmode -projectPath <프로젝트> -executeMethod ControlRoomURPSetup.SetupAndBuild -quit
로그: 프로젝트 루트 BuildLog_ControlRoom.txt (배치 오차 mm, 누락 머티리얼)
스크린샷: 프로젝트 루트 Screenshots/

Build All 이 하는 일
- FBX 임포터: Scale 1, 카메라/라이트/애니 끔, Lightmap UV 생성(스크린·Layout 제외)
- json 기준 URP Lit 머티리얼 생성, GPU Instancing 켬, 이름으로 리맵(.001 접미사 무시)
- Props 마다 프리팹 생성(Static)
- Full_Reference 의 같은 이름 렌더러에 맞춰 프리팹 배치 -> Blender 와 동일 위치
- 화면 오브젝트에 ScreenSlot 부착, LED 25개 Area Light(Baked), 카메라 4대

## 4. 좌표/스케일
Blender Z-up, 1u = 1m. FBX: Forward -Z / Up Y / Apply Transform / Scale All.
방 14 x 10m, 천장고 3.0m, 슬라브 3.8m. 출입문 벽이 Blender Y=0, 모니터 벽이 +Y.

## 5. 화면(스크린)
- 39면, 13개 시퀀스 그룹. 같은 그룹은 같은 이미지 시퀀스를 공유
- 티어:  Front 24프레임 1920x1080 / Mid 8프레임 1024x576 / Edge 2프레임 512x288
- 화면 UV는 0~1 꽉 채움(16:9). 이미지는 머티리얼 _EmissionMap 에 들어감
- ScreenFlipbook: frames[] 에 텍스처 배열, fps(기본 3), startOffset 으로 같은 그룹끼리 엇박
  SetImage(tex) 호출 시 재생 멈추고 한 장 고정 (버튼/인터랙션용)
  MaterialPropertyBlock 사용 -> 머티리얼 복제 없음, 배칭 유지
- 현재 이미지는 전부 플레이스홀더(TV 화면 보라색 톤은 원본 이미지 색). 실제 HMI 이미지로 교체 필요

## 6. 라이팅 (저사양 미니PC 기준)
- 천장 LED 25개 = Rectangle Area Light, Baked 전용, Soft shadow. 강도 28 (ControlRoomBuilder.cs 상단 LED_INTENSITY)
- 실시간 라이트 없음
- Ambient Flat (0.10, 0.105, 0.12), 스카이박스 없음
- Lightmap: Progressive GPU(실패 시 CPU), 해상도 20, 최대 2048, 바운스 2, AO
- 화면은 Baked Emissive: 화면 이미지를 바꿔도 주변 반사광은 베이크 당시 색으로 고정됨
- 런타임에 실시간 그림자/리얼타임 GI 켜지 말 것

## 7. 주의
- Full_Reference 는 배치 기준. 지우면 Build All 재실행 불가
- 머티리얼 이름 바꾸면 리맵 실패 -> 로그에 WARNING
- 의자 등받이 메쉬망은 Alpha Clip(양면)
- 전체 폴리곤 약 100k tris 목표로 감축된 상태

## 8. 현재 상태 (Unity 2022.3.62f3 배치 빌드 검증)
- 프롭 175개 배치, 누락 0, 배치 오차 0.0mm, 머티리얼 누락 경고 없음
- 베이크 성공(GPU). 작은 오브젝트 UV 겹침 경고 있음 -> 키보드·마우스 등은 Scale In Lightmap 낮추거나 라이트프로브로 돌려도 됨
- 남은 작업: 실제 HMI 이미지, 인터랙션, 필요하면 Volume(Tonemapping) 추가
