using System;
using System.Collections.Generic;
using System.Linq;
using BackEnd;
using LitJson;
using UnityEngine;

/// <summary>
/// 퀘스트 시스템 매니저 (싱글톤)
/// 퀘스트 마스터 데이터(차트)와 유저별 진행 상태(GameData)를 관리한다.
/// 퀘스트 달성 조건이 충족되면 RecordProgress()를 호출해 진행도를 기록한다.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("차트 설정")]
    [SerializeField] private string questsChartId = ""; // 뒤끝 콘솔 Quests 차트 파일 ID

    // 퀘스트 마스터 데이터 (questId 기준)
    private Dictionary<string, QuestData> _questMap = new Dictionary<string, QuestData>();

    // 유저 퀘스트 진행 상태 (questId 기준)
    private Dictionary<string, QuestProgress> _progressMap = new Dictionary<string, QuestProgress>();

    public event Action OnQuestUpdated;

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
    // 1. 초기화
    // ────────────────────────────────────────────────

    public void Initialize(Action<bool> onComplete)
    {
        LoadQuestChart(chartSuccess =>
        {
            if (!chartSuccess)
            {
                onComplete?.Invoke(false);
                return;
            }

            LoadQuestProgress(onComplete);
        });
    }

    private void LoadQuestChart(Action<bool> onComplete)
    {
        BackendReturnObject bro = Backend.Chart.GetChartContents(questsChartId);

        if (!bro.IsSuccess())
        {
            Debug.LogError($"[Quest] 차트 로드 실패: {bro}");
            onComplete?.Invoke(false);
            return;
        }

        _questMap.Clear();
        JsonData rows = bro.FlattenRows();

        foreach (JsonData row in rows)
        {
            var quest = new QuestData
            {
                questId      = row["questId"].ToString(),
                title        = row["title"].ToString(),
                description  = row["description"].ToString(),
                questType    = row["questType"].ToString(),
                goalCount    = int.Parse(row["goalCount"].ToString()),
                rewardCoin   = int.Parse(row["rewardCoin"].ToString()),
                rewardTicket = int.Parse(row["rewardTicket"].ToString()),
            };
            _questMap[quest.questId] = quest;
        }

        Debug.Log($"[Quest] 차트 로드 완료: {_questMap.Count}개");
        onComplete?.Invoke(true);
    }

    private void LoadQuestProgress(Action<bool> onComplete)
    {
        Backend.GameData.Get("UserQuestProgress", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Quest] 진행 상태 로드 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _progressMap.Clear();
            var rows = callback.FlattenRows();

            foreach (JsonData row in rows)
            {
                var progress = new QuestProgress
                {
                    questId          = row["questId"].ToString(),
                    currentCount     = int.Parse(row["currentCount"].ToString()),
                    isRewardReceived = row["isRewardReceived"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase),
                    inDate           = row["inDate"].ToString(),
                };
                _progressMap[progress.questId] = progress;
            }

            Debug.Log($"[Quest] 진행 상태 로드 완료: {_progressMap.Count}개");
            onComplete?.Invoke(true);
        });
    }

    // ────────────────────────────────────────────────
    // 2. 조회
    // ────────────────────────────────────────────────

    public List<QuestData> GetAllQuests()
    {
        return _questMap.Values.ToList();
    }

    public QuestProgress GetProgress(string questId)
    {
        return _progressMap.TryGetValue(questId, out var progress) ? progress : new QuestProgress { questId = questId };
    }

    public bool IsCompleted(string questId)
    {
        if (!_questMap.TryGetValue(questId, out var quest)) return false;
        var progress = GetProgress(questId);
        return progress.currentCount >= quest.goalCount;
    }

    public bool IsRewardReceived(string questId)
    {
        return GetProgress(questId).isRewardReceived;
    }

    // ────────────────────────────────────────────────
    // 3. 진행도 기록
    // 퀘스트 타입별 달성 조건이 충족될 때 외부에서 호출
    // ────────────────────────────────────────────────

    /// <summary>
    /// 특정 타입의 퀘스트 진행도를 amount만큼 증가시킨다.
    /// 예: 광고 시청 완료 → RecordProgress("ad", 1)
    /// </summary>
    public void RecordProgress(string questType, int amount = 1)
    {
        var targets = _questMap.Values
            .Where(q => q.questType == questType && !IsRewardReceived(q.questId))
            .ToList();

        foreach (var quest in targets)
        {
            var progress = GetProgress(quest.questId);
            if (progress.currentCount >= quest.goalCount) continue;

            int newCount = Mathf.Min(progress.currentCount + amount, quest.goalCount);
            UpdateProgress(quest.questId, newCount, progress.inDate);
        }
    }

    private void UpdateProgress(string questId, int newCount, string existingInDate)
    {
        Param param = new Param();
        param.Add("questId", questId);
        param.Add("currentCount", newCount);
        param.Add("isRewardReceived", false);

        if (string.IsNullOrEmpty(existingInDate))
        {
            // 최초 기록 → Insert
            Backend.GameData.Insert("UserQuestProgress", param, callback =>
            {
                if (!callback.IsSuccess())
                {
                    Debug.LogError($"[Quest] 진행도 Insert 실패: {callback}");
                    return;
                }

                _progressMap[questId] = new QuestProgress
                {
                    questId      = questId,
                    currentCount = newCount,
                    isRewardReceived = false,
                    inDate       = callback.GetInDate()
                };

                Debug.Log($"[Quest] {questId} 진행도 저장: {newCount}");
                OnQuestUpdated?.Invoke();
            });
        }
        else
        {
            // 기존 행 업데이트
            Backend.GameData.UpdateV2("UserQuestProgress", existingInDate, Backend.UserInDate, param, callback =>
            {
                if (!callback.IsSuccess())
                {
                    Debug.LogError($"[Quest] 진행도 Update 실패: {callback}");
                    return;
                }

                _progressMap[questId].currentCount = newCount;
                Debug.Log($"[Quest] {questId} 진행도 업데이트: {newCount}");
                OnQuestUpdated?.Invoke();
            });
        }
    }

    // ────────────────────────────────────────────────
    // 4. 보상 수령
    // ────────────────────────────────────────────────

    public void ClaimReward(string questId, Action<bool> onComplete)
    {
        if (!IsCompleted(questId))
        {
            Debug.LogWarning($"[Quest] {questId} 아직 달성 안 됨");
            onComplete?.Invoke(false);
            return;
        }

        if (IsRewardReceived(questId))
        {
            Debug.LogWarning($"[Quest] {questId} 이미 보상 수령함");
            onComplete?.Invoke(false);
            return;
        }

        var quest = _questMap[questId];
        var progress = GetProgress(questId);

        // 코인 지급
        if (quest.rewardCoin > 0)
            WalletManager.Instance.AddCoin(quest.rewardCoin);

        // 티켓 지급
        if (quest.rewardTicket > 0)
            WalletManager.Instance.AddTicket(quest.rewardTicket);

        // 보상 수령 상태 저장
        Param param = new Param();
        param.Add("questId", questId);
        param.Add("currentCount", progress.currentCount);
        param.Add("isRewardReceived", true);

        Backend.GameData.UpdateV2("UserQuestProgress", progress.inDate, Backend.UserInDate, param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Quest] 보상 수령 저장 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _progressMap[questId].isRewardReceived = true;
            Debug.Log($"[Quest] {questId} 보상 수령 완료 (코인: {quest.rewardCoin}, 티켓: {quest.rewardTicket})");
            OnQuestUpdated?.Invoke();
            onComplete?.Invoke(true);
        });
    }
}
