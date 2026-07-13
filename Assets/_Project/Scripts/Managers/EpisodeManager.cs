using System;
using System.Collections.Generic;
using System.Linq;
using BackEnd;
using LitJson;
using UnityEngine;

/// <summary>
/// 에피소드 시스템 매니저 (싱글톤)
/// 메인 에피소드 진행 상태, 데이트 에피소드 해금 상태를 관리하고
/// 에피소드 JSON 데이터를 Resources에서 로드한다.
/// 실제 노드 순회/연출은 EpisodePlayer(에피소드 씬)가 담당한다.
/// </summary>
public class EpisodeManager : MonoBehaviour
{
    public static EpisodeManager Instance { get; private set; }

    [Header("메인 에피소드 총 개수 (엔딩 판정용)")]
    [SerializeField] private int totalMainEpisodeCount = 10;

    [Header("에피소드 씬 이름")]
    [SerializeField] private string episodeSceneName = "03_Episode";

    // 씬 전환 후 EpisodePlayer가 가져갈 대기 데이터
    public EpisodeData PendingEpisodeData { get; private set; }
    public EpisodePlayContext PendingContext { get; private set; }

    // 매력도 대결 도중 로비로 나갔을 때, 돌아올 위치를 기억하기 위한 대기 상태
    public bool HasPendingCharmBattle { get; private set; }
    private EpisodeData _pendingBattleEpisode;
    private EpisodePlayContext _pendingBattleContext;
    private string _pendingBattleNodeId;

    // 다음에 플레이할 메인 에피소드 번호 (1부터 시작)
    public int CurrentMainEpisode { get; private set; } = 1;

    // 해금된 일반 데이트 에피소드 ID 집합
    private HashSet<string> _unlockedDateEpisodes = new HashSet<string>();

    private string _progressRowInDate;

    public event Action OnEpisodeProgressUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ────────────────────────────────────────────────
    // 1. 초기화 (로그인 후 호출)
    // ────────────────────────────────────────────────

    public void Initialize(Action<bool> onComplete)
    {
        LoadMainProgress(progressSuccess =>
        {
            if (!progressSuccess)
            {
                onComplete?.Invoke(false);
                return;
            }

            LoadUnlockedDateEpisodes(onComplete);
        });
    }

    private void LoadMainProgress(Action<bool> onComplete)
    {
        Backend.GameData.Get("EpisodeProgress", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Episode] 진행 상태 조회 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            var rows = callback.FlattenRows();

            if (rows == null || rows.Count == 0)
            {
                CreateProgressRow(onComplete);
            }
            else
            {
                JsonData row = rows[0];
                _progressRowInDate = row["inDate"].ToString();
                CurrentMainEpisode = int.Parse(row["currentMainEpisode"].ToString());

                Debug.Log($"[Episode] 진행 상태 로드 완료 - 현재 EP{CurrentMainEpisode}");
                OnEpisodeProgressUpdated?.Invoke();
                onComplete?.Invoke(true);
            }
        });
    }

    private void CreateProgressRow(Action<bool> onComplete)
    {
        Param param = new Param();
        param.Add("currentMainEpisode", 1);

        Backend.GameData.Insert("EpisodeProgress", param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Episode] 진행 상태 생성 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _progressRowInDate = callback.GetInDate();
            CurrentMainEpisode = 1;

            Debug.Log("[Episode] 진행 상태 최초 생성 완료");
            OnEpisodeProgressUpdated?.Invoke();
            onComplete?.Invoke(true);
        });
    }

    private void LoadUnlockedDateEpisodes(Action<bool> onComplete)
    {
        Backend.GameData.Get("UnlockedDateEpisodes", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Episode] 데이트 해금 목록 조회 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _unlockedDateEpisodes.Clear();
            var rows = callback.FlattenRows();

            foreach (JsonData row in rows)
                _unlockedDateEpisodes.Add(row["dateEpisodeId"].ToString());

            Debug.Log($"[Episode] 데이트 해금 목록 로드 완료: {_unlockedDateEpisodes.Count}개");
            onComplete?.Invoke(true);
        });
    }

    /// <summary>
    /// 메인 패널 "에피소드" 버튼에서 호출. 진입 성공 시 자동으로 03_Episode 씬으로 전환한다.
    /// </summary>
    public void EnterMainEpisodeAndLoadScene(Action<bool, string> onFail = null)
    {
        EnterMainEpisode((success, data, error) =>
        {
            if (!success)
            {
                Debug.LogWarning($"[Episode] 메인 에피소드 진입 실패: {error}");
                onFail?.Invoke(false, error);
                return;
            }

            PendingEpisodeData = data;
            PendingContext = new EpisodePlayContext { source = EpisodeSource.MainMenu };
            UnityEngine.SceneManagement.SceneManager.LoadScene(episodeSceneName);
        });
    }

    /// <summary>
    /// 데이트 패널에서 직접 진입할 때 호출 (일반 데이트).
    /// </summary>
    public void EnterNormalDateEpisodeAndLoadScene(string dateEpisodeId, Action<bool, string> onFail = null)
    {
        EnterNormalDateEpisode(dateEpisodeId, (success, data, error) =>
        {
            if (!success)
            {
                Debug.LogWarning($"[Episode] 일반 데이트 진입 실패: {error}");
                onFail?.Invoke(false, error);
                return;
            }

            PendingEpisodeData = data;
            PendingContext = new EpisodePlayContext { source = EpisodeSource.DatePanel };
            UnityEngine.SceneManagement.SceneManager.LoadScene(episodeSceneName);
        });
    }

    /// <summary>
    /// 데이트 패널에서 직접 진입할 때 호출 (스페셜 데이트).
    /// </summary>
    public void EnterSpecialDateEpisodeAndLoadScene(string dateEpisodeId, string characterId, Action<bool, string> onFail = null)
    {
        EnterSpecialDateEpisode(dateEpisodeId, characterId, (success, data, error) =>
        {
            if (!success)
            {
                Debug.LogWarning($"[Episode] 스페셜 데이트 진입 실패: {error}");
                onFail?.Invoke(false, error);
                return;
            }

            PendingEpisodeData = data;
            PendingContext = new EpisodePlayContext { source = EpisodeSource.DatePanel };
            UnityEngine.SceneManagement.SceneManager.LoadScene(episodeSceneName);
        });
    }

    /// <summary>
    /// EpisodePlayer가 "나의 옷장으로" 버튼 클릭 시 호출.
    /// 현재 에피소드/노드 정보를 기억해두고 로비로 이동한다.
    /// </summary>
    public void SaveCharmBattlePending(EpisodeData episode, EpisodePlayContext context, string battleNodeId)
    {
        _pendingBattleEpisode = episode;
        _pendingBattleContext = context;
        _pendingBattleNodeId = battleNodeId;
        HasPendingCharmBattle = true;

        UnityEngine.SceneManagement.SceneManager.LoadScene("02_Lobby");
    }

    /// <summary>
    /// 로비의 "대결하러 가기" 버튼 클릭 시 호출.
    /// 저장해둔 에피소드/노드로 복귀한다.
    /// </summary>
    public void ResumeCharmBattle()
    {
        if (!HasPendingCharmBattle) return;

        PendingEpisodeData = _pendingBattleEpisode;
        PendingContext = _pendingBattleContext;

        HasPendingCharmBattle = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene(episodeSceneName);
    }

    /// <summary>EpisodePlayer가 씬 재진입 시 대결 노드 ID를 가져가기 위한 조회.</summary>
    public string ConsumePendingBattleNodeId()
    {
        string nodeId = _pendingBattleNodeId;
        _pendingBattleNodeId = null;
        return nodeId;
    }

    /// <summary>
    /// EpisodePlayer가 씬 시작 시 호출해서 대기 데이터를 가져간다.
    /// 가져간 후에는 비워서 중복 사용을 방지한다.
    /// </summary>
    public (EpisodeData data, EpisodePlayContext context) ConsumePendingEpisode()
    {
        var result = (PendingEpisodeData, PendingContext);
        PendingEpisodeData = null;
        PendingContext = null;
        return result;
    }

    // ────────────────────────────────────────────────
    // 2. 메인 에피소드
    // ────────────────────────────────────────────────

    /// <summary>메인 에피소드 진입. 티켓 1개 차감 후 성공하면 에피소드 데이터를 반환.</summary>
    public void EnterMainEpisode(Action<bool, EpisodeData, string> onComplete)
    {
        WalletManager.Instance.SpendTicket(1, (spendSuccess, error) =>
        {
            if (!spendSuccess)
            {
                onComplete?.Invoke(false, null, error);
                return;
            }

            string episodeId = $"ep{CurrentMainEpisode:D2}";
            EpisodeData data = LoadEpisodeJson($"Episodes/Main/{episodeId}");

            if (data == null)
            {
                onComplete?.Invoke(false, null, "에피소드 데이터를 찾을 수 없습니다.");
                return;
            }

            onComplete?.Invoke(true, data, null);
        });
    }

    /// <summary>메인 에피소드 완료 처리. 마지막 에피소드면 1로 순환, 아니면 다음 번호로.</summary>
    public void CompleteMainEpisode(Action<bool> onComplete)
    {
        int nextEpisode = CurrentMainEpisode >= totalMainEpisodeCount ? 1 : CurrentMainEpisode + 1;

        Param param = new Param();
        param.Add("currentMainEpisode", nextEpisode);

        Backend.GameData.UpdateV2("EpisodeProgress", _progressRowInDate, Backend.UserInDate, param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Episode] 진행 상태 저장 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            bool wasEnding = CurrentMainEpisode >= totalMainEpisodeCount;
            CurrentMainEpisode = nextEpisode;

            if (wasEnding)
            {
                Debug.Log("[Episode] 엔딩 클리어! 대량 보상 지급");
                GiveEndingReward();
                QuestManager.Instance.RecordProgress("episode");
            }

            Debug.Log($"[Episode] 다음 EP{CurrentMainEpisode}로 진행");
            OnEpisodeProgressUpdated?.Invoke();
            onComplete?.Invoke(true);
        });
    }

    private void GiveEndingReward()
    {
        // TODO: 실제 보상량은 기획 확정 후 조정
        WalletManager.Instance.AddCoin(2000);
        WalletManager.Instance.AddTicket(3);
    }

    // ────────────────────────────────────────────────
    // 3. 데이트 에피소드
    // ────────────────────────────────────────────────

    public bool IsDateEpisodeUnlocked(string dateEpisodeId)
    {
        return _unlockedDateEpisodes.Contains(dateEpisodeId);
    }

    /// <summary>
    /// 일반 데이트 에피소드 진입. 이미 해금됐으면 바로 로드, 아니면 1000코인 소모 후 해금.
    /// </summary>
    public void EnterNormalDateEpisode(string dateEpisodeId, Action<bool, EpisodeData, string> onComplete)
    {
        if (IsDateEpisodeUnlocked(dateEpisodeId))
        {
            LoadDateEpisode(dateEpisodeId, onComplete);
            return;
        }

        const int unlockCost = 1000;
        WalletManager.Instance.SpendCoin(unlockCost, (spendSuccess, error) =>
        {
            if (!spendSuccess)
            {
                onComplete?.Invoke(false, null, error);
                return;
            }

            UnlockDateEpisode(dateEpisodeId, unlockSuccess =>
            {
                if (!unlockSuccess)
                {
                    onComplete?.Invoke(false, null, "해금 저장에 실패했습니다.");
                    return;
                }

                LoadDateEpisode(dateEpisodeId, onComplete);
            });
        });
    }

    /// <summary>
    /// 스페셜 데이트 에피소드 진입. 해당 남주의 커플룩 보유 여부로 진입 가능 여부 판단.
    /// </summary>
    public void EnterSpecialDateEpisode(string dateEpisodeId, string characterId, Action<bool, EpisodeData, string> onComplete)
    {
        bool hasCoupleLook = HasAnyCoupleLook(characterId);

        if (!hasCoupleLook)
        {
            onComplete?.Invoke(false, null, "커플룩을 보유해야 진입할 수 있습니다.");
            return;
        }

        LoadDateEpisode(dateEpisodeId, onComplete);
    }

    private bool HasAnyCoupleLook(string characterId)
    {
        var coupleSets = ShopManager.Instance.GetItemsByTypeAndCategory(ItemType.Couple, ItemCategory.Set, characterId);
        return coupleSets.Any(set => ShopManager.Instance.IsOwned(set.itemId));
    }

    private void UnlockDateEpisode(string dateEpisodeId, Action<bool> onComplete)
    {
        Param param = new Param();
        param.Add("dateEpisodeId", dateEpisodeId);

        Backend.GameData.Insert("UnlockedDateEpisodes", param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Episode] 데이트 해금 저장 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _unlockedDateEpisodes.Add(dateEpisodeId);
            QuestManager.Instance.RecordProgress("purchase_couple"); // 필요 시 별도 타입으로 분리 가능
            onComplete?.Invoke(true);
        });
    }

    private void LoadDateEpisode(string dateEpisodeId, Action<bool, EpisodeData, string> onComplete)
    {
        EpisodeData data = LoadEpisodeJson($"Episodes/Date/{dateEpisodeId}");

        if (data == null)
        {
            onComplete?.Invoke(false, null, "에피소드 데이터를 찾을 수 없습니다.");
            return;
        }

        onComplete?.Invoke(true, data, null);
    }

    // ────────────────────────────────────────────────
    // 4. 매력도 대결
    // ────────────────────────────────────────────────

    /// <summary>
    /// 매력도 대결 결과 판정. 플레이어 매력도가 상대보다 높으면 승리.
    /// 승리 시 전체 남주 호감도에 보너스를 더한다.
    /// </summary>
    public void ResolveCharmBattle(EpisodeNode battleNode, Action<bool> onComplete)
    {
        bool isWin = CharmManager.Instance.CharmPercent > battleNode.opponentPercent;

        if (!isWin)
        {
            onComplete?.Invoke(false);
            return;
        }

        int bonus = battleNode.winAffectionBonus;
        string[] allCharacters = { "hajin", "dohyun", "siwoo" };
        int remaining = allCharacters.Length;

        foreach (var charId in allCharacters)
        {
            AffectionManager.Instance.AddAffection(charId, bonus, _ =>
            {
                remaining--;
                if (remaining <= 0)
                    onComplete?.Invoke(true);
            });
        }
    }

    // ────────────────────────────────────────────────
    // 내부 헬퍼
    // ────────────────────────────────────────────────

    private EpisodeData LoadEpisodeJson(string resourcePath)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

        if (jsonAsset == null)
        {
            Debug.LogError($"[Episode] JSON 파일을 찾을 수 없음: {resourcePath}");
            return null;
        }

        try
        {
            return JsonUtility.FromJson<EpisodeData>(jsonAsset.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Episode] JSON 파싱 실패: {resourcePath}\n{e}");
            return null;
        }
    }
}