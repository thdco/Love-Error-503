using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옷장 패널 UI
/// 카테고리 탭 전환(나의 옷장 / 커플룩 / 그의 옷장)과 닫기 버튼을 처리한다.
/// 아이템 목록 로드 및 구매/착용 처리는 추후 ClosetSystem에서 담당 예정
/// </summary>
public class ClosetPanelUI : MonoBehaviour
{
    [Header("카테고리 탭 버튼")]
    [SerializeField] private Button btnMyCloset;
    [SerializeField] private Button btnCoupleLook;
    [SerializeField] private Button btnHisCloset;

    [Header("탭 버튼 배경 이미지 (선택 상태 표시용)")]
    [SerializeField] private Image imgMyCloset;
    [SerializeField] private Image imgCoupleLook;
    [SerializeField] private Image imgHisCloset;

    [Header("탭 선택/비선택 색상")]
    [SerializeField] private Color colorSelected = Color.white;
    [SerializeField] private Color colorUnselected = new Color(0.8f, 0.8f, 0.8f);

    [Header("닫기 버튼")]
    [SerializeField] private Button btnClose;

    [Header("아이템 그리드")]
    [SerializeField] private Transform content; // Scroll_ItemGrid > Viewport > Content

    [Header("프리팹")]
    [SerializeField] private GameObject itemCardPrefab;

    private enum ClosetTab { MyCloset, CoupleLook, HisCloset }
    private ClosetTab _currentTab = ClosetTab.MyCloset;

    private void Awake()
    {
        btnMyCloset.onClick.AddListener(() => SwitchTab(ClosetTab.MyCloset));
        btnCoupleLook.onClick.AddListener(() => SwitchTab(ClosetTab.CoupleLook));
        btnHisCloset.onClick.AddListener(() => SwitchTab(ClosetTab.HisCloset));
        btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnEnable()
    {
        // 패널 열릴 때마다 첫 번째 탭으로 초기화
        SwitchTab(ClosetTab.MyCloset);
    }

    // ────────────────────────────────────────────────
    // 탭 전환
    // ────────────────────────────────────────────────

    private void SwitchTab(ClosetTab tab)
    {
        _currentTab = tab;
        RefreshTabColors();

        // TODO: 탭에 맞는 아이템 목록 로드 (ClosetSystem 연동 후 구현)
        Debug.Log($"[Closet] 탭 전환: {tab}");
    }

    private void RefreshTabColors()
    {
        imgMyCloset.color = _currentTab == ClosetTab.MyCloset ? colorSelected : colorUnselected;
        imgCoupleLook.color = _currentTab == ClosetTab.CoupleLook ? colorSelected : colorUnselected;
        imgHisCloset.color = _currentTab == ClosetTab.HisCloset ? colorSelected : colorUnselected;
    }

    // ────────────────────────────────────────────────
    // 닫기
    // ────────────────────────────────────────────────

    private void OnClickClose()
    {
        LobbyManager.Instance.CloseCloset();
    }
}
