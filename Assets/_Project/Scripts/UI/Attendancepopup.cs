using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 출석 체크 팝업 (UI 전담)
/// DayCard 프리팹을 동적으로 생성해서 1~7일차를 표시한다.
/// 출석 로직은 AttendanceManager가 담당한다.
/// </summary>
public class AttendancePopup : MonoBehaviour
{
    [Header("Day 카드 프리팹")]
    [SerializeField] private DayCardUI dayCardPrefab;       // 1~6일차용
    [SerializeField] private DayCardUI dayCardLargePrefab;  // 7일차용 (1x2 크기)

    [Header("Day 카드 배치 부모")]
    [SerializeField] private Transform leftGrid;   // 1~6일차가 들어갈 부모 (Grid Layout Group)
    [SerializeField] private Transform rightSlot;  // 7일차가 들어갈 부모

    [Header("닫기 버튼")]
    [SerializeField] private Button btnClose;

    private DayCardUI[] _cards = new DayCardUI[7]; // index 0~6 = Day1~7

    private void Awake()
    {
        btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnEnable()
    {
        AttendanceManager.Instance.OnAttendanceUpdated += RefreshUI;
        GenerateCards();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (AttendanceManager.Instance != null)
            AttendanceManager.Instance.OnAttendanceUpdated -= RefreshUI;
    }

    // ────────────────────────────────────────────────
    // 카드 생성 (한 번만)
    // ────────────────────────────────────────────────

    private void GenerateCards()
    {
        // 이미 생성됐으면 스킵
        if (_cards[0] != null) return;

        for (int i = 0; i < 6; i++)
        {
            DayCardUI card = Instantiate(dayCardPrefab, leftGrid);
            _cards[i] = card;
        }

        DayCardUI day7 = Instantiate(dayCardLargePrefab, rightSlot);
        _cards[6] = day7;
    }

    // ────────────────────────────────────────────────
    // UI 갱신
    // ────────────────────────────────────────────────

    private void RefreshUI()
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            int day = i + 1;
            AttendanceReward reward = AttendanceManager.Instance.GetReward(day);
            DayCardState state = AttendanceManager.Instance.GetCardState(day);
            _cards[i].Setup(day, reward, state, OnClickTodayCard);
        }
    }

    // ────────────────────────────────────────────────
    // 출석 체크
    // ────────────────────────────────────────────────

    private void OnClickTodayCard()
    {
        AttendanceManager.Instance.CheckToday(success =>
        {
            if (!success)
                Debug.LogWarning("[AttendancePopup] 출석 체크 실패");
        });
    }

    private void OnClickClose()
    {
        LobbyManager.Instance.CloseAttendance();
    }
}