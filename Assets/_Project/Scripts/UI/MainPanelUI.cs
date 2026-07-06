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
    }

    private void Start()
    {
        RefreshCharm();
    }

    // ────────────────────────────────────────────────
    // 버튼 이벤트
    // ────────────────────────────────────────────────

    private void OnClickEpisode()
    {
        // TODO: 에피소드 씬으로 전환
        // SceneManager.LoadScene("03_Episode");
        Debug.Log("[Main] 에피소드 버튼 클릭");
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
    /// 옷장에서 아이템 착용/해제 시 호출해서 매력도 갱신
    /// </summary>
    public void RefreshCharm()
    {
        // TODO: CharmManager 연동 후 실제 값으로 교체
        int charmValue = 0;
        sliderCharm.value = charmValue;
        txtCharm.text = $"매력도 {charmValue}";
    }
}
