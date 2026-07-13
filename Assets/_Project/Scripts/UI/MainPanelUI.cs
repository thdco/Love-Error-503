using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 패널 UI 버튼 이벤트 처리
/// 하단 메뉴 버튼(에피소드, 데이트, 옷장)과 좌상단 버튼(출석, 퀘스트) 담당
/// </summary>
public class MainPanelUI : MonoBehaviour
{
    [Header("하단 메뉴 버튼")]
    [SerializeField] private Button btnEpisode;
    [SerializeField] private TMPro.TMP_Text txtEpisodeButton; // "에피소드" / "대결하러 가기" 전환
    [SerializeField] private Button btnDate;
    [SerializeField] private Button btnCloset;

    [Header("좌상단 버튼")]
    [SerializeField] private Button btnAttendance;
    [SerializeField] private Button btnQuest;

    [Header("매력도")]
    [SerializeField] private Slider sliderCharm;
    [SerializeField] private TMPro.TMP_Text txtCharm;

    private void Awake()
    {
        btnEpisode.onClick.AddListener(OnClickEpisode);
        btnDate.onClick.AddListener(OnClickDate);
        btnCloset.onClick.AddListener(OnClickCloset);
        btnAttendance.onClick.AddListener(OnClickAttendance);
        btnQuest.onClick.AddListener(OnClickQuest);

        // 매력도 슬라이더는 0~100(%) 고정
        sliderCharm.minValue = 0;
        sliderCharm.maxValue = 100;
    }

    private void OnEnable()
    {
        CharmManager.Instance.OnCharmUpdated += RefreshCharm;
        RefreshCharm();
        RefreshEpisodeButtonLabel();
    }

    private void OnDisable()
    {
        if (CharmManager.Instance != null)
            CharmManager.Instance.OnCharmUpdated -= RefreshCharm;
    }

    // ────────────────────────────────────────────────
    // 버튼 이벤트
    // ────────────────────────────────────────────────

    /// <summary>매력도 대결 대기 중이면 "대결하러 가기"로, 아니면 "에피소드"로 표시.</summary>
    private void RefreshEpisodeButtonLabel()
    {
        if (txtEpisodeButton == null) return;
        txtEpisodeButton.text = EpisodeManager.Instance.HasPendingCharmBattle ? "대결하러 가기" : "에피소드";
    }

    private void OnClickEpisode()
    {
        if (EpisodeManager.Instance.HasPendingCharmBattle)
        {
            // 대결 대기 중이면 에피소드 씬으로 복귀
            EpisodeManager.Instance.ResumeCharmBattle();
            return;
        }

        EpisodeManager.Instance.EnterMainEpisodeAndLoadScene((success, error) =>
        {
            // 티켓 부족 등으로 진입 실패 시
            LobbyManager.Instance.ShowInsufficientFunds(CurrencyType.Ticket);
        });
    }

    private void OnClickDate()
    {
        LobbyManager.Instance.ShowDate();
    }

    private void OnClickCloset()
    {
        LobbyManager.Instance.ShowCloset();
    }

    private void OnClickAttendance()
    {
        LobbyManager.Instance.ShowAttendance();
    }

    private void OnClickQuest()
    {
        LobbyManager.Instance.ShowQuest();
    }

    // ────────────────────────────────────────────────
    // 매력도
    // ────────────────────────────────────────────────

    /// <summary>
    /// 아이템 구매/보유 변경 시 CharmManager.OnCharmUpdated 이벤트로 자동 호출된다.
    /// </summary>
    private void RefreshCharm()
    {
        float percent = CharmManager.Instance.CharmPercent;
        sliderCharm.value = percent;
        txtCharm.text = $"매력도 {percent:F0}%";
    }
}