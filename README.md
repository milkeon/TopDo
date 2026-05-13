# Top Do

전역 단축키로 어디서나 띄우는 WPF 투두 앱입니다.

## 현재 반영된 UI
- 좁은 세로형 글래스모피즘 레이아웃
- 상단 작성 영역 기본 접힘
- `Shift+Enter`로 상세 입력 펼침 / 완료
- 하위 체크리스트 입력
- 하위 체크 전체 완료 시 상위 할 일 자동 완료
- 리스트 항목 hover 시 내용 표시
- 우측 상단 최소화/닫기 버튼 제거
- 단축키 안내 멘트 제거

## 실행
### 요구 사항
- Windows
- .NET SDK
- WPF 실행 환경

### 열기
1. `TopDo.sln` 또는 `TopDo.csproj`를 Visual Studio / Rider / `dotnet` 환경에서 엽니다.
2. 빌드 후 실행합니다.

## 기본 단축키
- `Ctrl+Alt+Space` : 창 표시 / 숨기기

## 다운로드
- 저장소 ZIP 다운로드
- 또는 `git clone`으로 소스 다운로드

## 참고
- 현재 작업 검증은 macOS에서 HTML 미리보기와 스크린샷 기준으로 진행했습니다.
- 이 macOS 환경에는 `dotnet` CLI가 없어 실제 WPF 빌드는 아직 못 했습니다.
