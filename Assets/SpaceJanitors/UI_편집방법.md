# 직접 UI 꾸미기

씬: Assets/Frigate/Scenes/Kestrel_FirstPerson.unity

1. Play를 끈 상태에서 씬을 엽니다.
2. Hierarchy에서 UI_Canvas를 펼칩니다.
3. MissionPanel을 선택하면 미션 박스 전체를 이동할 수 있습니다.
4. MissionPanel 아래 MissionCounts를 선택하면 수거/수리 숫자의 글꼴, 크기, 색을 수정할 수 있습니다.
5. Rect Transform의 Pos X / Pos Y와 Width / Height로 위치와 크기를 바꿉니다. Scene 탭의 2D 모드에서 T키로 드래그할 수도 있습니다. F키는 선택한 UI를 화면에 맞춥니다.
6. 글자는 Text 컴포넌트의 Font / Font Size / Color에서 수정합니다. 이번 버전은 TextMeshPro가 아니라 기본 uGUI Text입니다.
7. 배경은 각 Panel의 Image → Color에서 바꿉니다. A는 투명도입니다.
8. Ctrl+S로 씬을 저장합니다. Play 중 변경한 값은 종료하면 되돌아갈 수 있습니다.

## 오브젝트 이름
- MissionPanel: 미션 배경 / Title: 제목 / MissionCounts: 수거·수리 수치 / MissionStatus: 목표
- StatusPanel: 중력·안전줄 상태
- ControlsPanel: 하단 조작 안내
- Crosshair: 조준점
- InteractionPanel: 물건·수리 상호작용 안내
- NoticePanel: 알림
- PausePanel: 시작·일시정지 메뉴. 편집할 때 Inspector 맨 위 체크박스로 잠시 활성화하면 보입니다.

미션 수치 문구는 UI_Canvas의 SJ Canvas HUD → Mission Format에서 수정합니다. {0}은 수거 개수, {1}은 수리 개수입니다. MissionCounts의 Text 내용은 실행 중 수치로 교체됩니다. 색·크기·폰트·위치는 코드가 덮어쓰지 않습니다.

새 글꼴은 사용 권한이 있는 .ttf/.otf 파일을 Project 창에 넣고 Text의 Font 칸에 드래그하세요. 한글을 포함한 폰트가 필요합니다. 기본값은 프로젝트에 포함한 나눔고딕이며 라이선스는 UI/Font-License.txt에 있습니다.

버튼의 On Click 연결과 SJ Canvas HUD의 오브젝트 연결은 유지하세요. Canvas Scaler가 해상도에 맞춰 UI 크기를 조정합니다. 옛 코드 UI는 Canvas가 활성화되어 있으면 표시하지 않습니다.

기존 씬 백업: Assets/Frigate/Scenes/Kestrel_BeforeCanvas.unity
맵 생성 스크립트를 다시 실행하면 씬이 재생성될 수 있으므로 디자인 작업 전 File → Save As로 나만의 씬 복사본을 만드는 것을 권장합니다.
