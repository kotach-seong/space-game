# 메인화면 직접 수정하기

## 열기
Unity Project 창 → Assets → SpaceJanitors → Scenes → MainMenu를 더블클릭하세요.
Play를 끈 상태에서 편집하고 Ctrl+S로 저장합니다. 먼저 File → Save As로 작업용 씬을 복사해두면 안전합니다.

## 어디를 선택하나요?
Hierarchy → MainMenu_Canvas → DesignFrame 아래에 모든 디자인이 있습니다.

| 오브젝트 | 내용 |
|---|---|
| Title / EnglishTitle | 맨 위 게임 제목 / 영문 제목 |
| Stars / Planet / Orbit_01 / Orbit_02 | 별, 행성, 궤도 장식 |
| HomePanel | 혼자 게임 / 온라인 게임 / 설정 / 나가기 버튼 |
| SoloPanel | 세이브 슬롯 4개 |
| OnlinePanel | 방 목록 / 방 제목 검색 / 방 만들기 |
| CreateRoomPanel | 방 제목 / 공개·비공개 / 비밀번호 |
| JoinPasswordPanel | 잠긴 방 입장 |
| SettingsPanel | 화면 / 소리 / 조작 설정 |
| ShopCheckpointPanel | 상점 체크포인트 안내 |

HomePanel 외에는 기본적으로 꺼져 있습니다. 보고 싶은 Panel을 선택한 뒤 Inspector 맨 위 체크박스를 켜세요. 다른 Panel은 꺼서 겹치지 않게 합니다. Scene 탭의 2D 모드에서 선택 후 F를 누르면 UI가 크게 보입니다. 실행하면 메뉴 코드가 적절한 화면을 선택합니다.

## 글자 바꾸기
버튼을 펼친 뒤 Label 선택 → Text 컴포넌트 → Text 입력칸을 수정합니다.
Title도 Text 입력칸을 수정하면 됩니다. 이번 UI는 TextMeshPro가 아닌 uGUI Text입니다.
글꼴은 .ttf/.otf를 Assets에 넣고 Font 칸에 드래그합니다. 한글 지원 및 사용 권한을 확인하세요. 기본 글꼴은 나눔고딕입니다.
Font Size는 글자 크기, Color는 글자 색입니다.

세이브 진행, 방 이름/인원, 알림 같은 실시간 텍스트는 실행 중 코드가 내용만 갱신합니다. 해당 텍스트의 위치·색·크기·글꼴은 덮어쓰지 않습니다.

## 위치 / 크기 / 배경색
- Rect Transform → Pos X / Pos Y / Width / Height
- Scene의 T키 도구로 드래그
- 둥근 박스와 장식: SJ Space Graphic → Color
- 둥근 정도: SJ Space Graphic → Radius
- 버튼 마우스 반응: Button → Colors
- 일반 사진을 배경으로 넣고 싶으면 UI → Image를 추가해 Source Image를 지정하세요.

별과 행성은 외부 이미지가 아닌 Canvas 도형입니다. Scene에서 크기와 위치, 색을 직접 바꿀 수 있습니다.
MainMenu_Canvas의 Canvas Scaler는 1600×900 기준으로 화면에 맞게 축소/확대합니다.

## 연결은 유지하세요
Button → On Click, SJ Menu 및 SJ Settings Panel 컴포넌트의 오브젝트 연결은 기능에 필요합니다. 외형만 바꿀 때는 삭제하지 마세요. 생성 스크립트 BuildMainMenu는 재생성 시 디자인을 보호하기 위해 기존 MainMenu 씬을 덮어쓰지 않습니다.

## 저장 규칙과 현재 구현 범위
- 슬롯 4개. 새 게임은 상점 도착 전까지 진행을 저장하지 않습니다.
- 현재 청소 5개 + 수리 2개 완료 → Esc → 완료 후 상점: 진행 저장 후 상점 안내 화면.
- 저장된 슬롯은 상점부터 이어갑니다. 현재는 같은 테스트 우주선으로 다음 출동합니다.
- 실제 상점 상품·경제·로그라이크 콘텐츠는 아직 없습니다.
- GameOver() 연결 지점은 해당 슬롯만 초기화합니다. 실제 체력/사망·폭발 3회 시스템은 아직 없으므로 R 복귀는 사망으로 처리하지 않습니다.
- 설정은 슬롯과 별도로 유지합니다.
- 실제 온라인 서버와 음성 채팅은 아직 연결하지 않았습니다. 방 만들기는 로컬 UI 미리보기입니다. 실제 참가자를 표시하지 않습니다. 비밀번호는 로컬 메모리에서만 시험하며 저장 파일에 기록하지 않습니다.
- 전체 음량, 해상도/화면 모드, 그래픽 품질, 밝기, 감도, 상하 반전, 이동/상호작용 키는 연결되어 있습니다.
- 음악/효과음/음성 시스템이 없으므로 개별 음량 및 마이크·눌러 말하기는 설정 저장만 합니다. 마이크 녹음·전송은 하지 않습니다.
- 화면 흔들림 옵션은 저장되지만 현재 카메라 흔들림 효과 자체가 없습니다.

설정의 기본값으로는 편집 값을 바꾸며, 적용을 눌러야 저장됩니다. 뒤로는 적용하지 않은 변경을 버립니다.
저장 파일은 Unity의 Application.persistentDataPath에 space-janitors-profile.json으로 저장되며 마지막 파일 백업은 .bak입니다. 테스트는 별도 격리 저장소를 사용합니다.

## 게임 빌드
File → Build Profiles의 씬 목록에서 MainMenu가 첫 번째, Kestrel_FirstPerson이 두 번째여야 합니다. 현재 프로젝트에는 이 순서로 등록했습니다. MainMenu 씬에서 Play를 누르면 전체 흐름을 테스트할 수 있습니다.
