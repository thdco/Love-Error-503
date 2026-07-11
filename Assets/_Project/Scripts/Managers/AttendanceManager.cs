using System;
using System.Collections.Generic;
using BackEnd;
using LitJson;
using UnityEngine;

/// <summary>
/// 출석 체크 시스템 매니저 (싱글톤)
/// - 보상 데이터: AttendanceRewards 차트 (날짜별 코인/티켓)
/// - 출석 상태: Attendance 테이블 (currentDay, lastCheckedDate)
/// 7일 단위로 순환하며, 하루에 한 번만 체크 가능하다.
/// </summary>
public class AttendanceManager : MonoBehaviour
{
    public static AttendanceManager Instance { get; private set; }

    [Header("차트 설정")]
    [SerializeField] private string attendanceRewardsChartId = ""; // AttendanceRewards 차트 파일 ID

    // 날짜(1~7)별 보상 데이터
    private Dictionary<int, AttendanceReward> _rewardMap = new Dictionary<int, AttendanceReward>();

    // 출석 상태
    public int CurrentDay { get; private set; }        // 오늘 체크해야 할(또는 체크한) 일차 (1~7)
    public bool IsTodayChecked { get; private set; }

    private string _attendanceRowInDate;

    public event Action OnAttendanceUpdated;

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
        if (!LoadRewardChart())
        {
            onComplete?.Invoke(false);
            return;
        }

        LoadAttendanceState(onComplete);
    }

    private bool LoadRewardChart()
    {
        BackendReturnObject bro = Backend.Chart.GetChartContents(attendanceRewardsChartId);

        if (!bro.IsSuccess())
        {
            Debug.LogError($"[Attendance] 보상 차트 로드 실패: {bro}");
            return false;
        }

        _rewardMap.Clear();
        JsonData rows = bro.FlattenRows();

        foreach (JsonData row in rows)
        {
            var reward = new AttendanceReward
            {
                day          = int.Parse(row["day"].ToString()),
                rewardCoin   = int.Parse(row["rewardCoin"].ToString()),
                rewardTicket = int.Parse(row["rewardTicket"].ToString()),
            };
            _rewardMap[reward.day] = reward;
        }

        Debug.Log($"[Attendance] 보상 차트 로드 완료: {_rewardMap.Count}일치");
        return true;
    }

    private void LoadAttendanceState(Action<bool> onComplete)
    {
        Backend.GameData.Get("Attendance", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Attendance] 상태 조회 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            var rows = callback.FlattenRows();

            if (rows == null || rows.Count == 0)
            {
                // 최초 출석 - Day1부터 시작, 아직 체크 안 함
                CurrentDay = 1;
                IsTodayChecked = false;
                _attendanceRowInDate = null;
            }
            else
            {
                JsonData row = rows[0];
                _attendanceRowInDate = row["inDate"].ToString();

                int lastDay = int.Parse(row["currentDay"].ToString());
                string lastCheckedDate = row["lastCheckedDate"].ToString();
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

                if (lastCheckedDate == today)
                {
                    CurrentDay = lastDay;
                    IsTodayChecked = true;
                }
                else
                {
                    // 다음 날 - 다음 일차로 진행 (7일 지나면 1로 순환)
                    CurrentDay = lastDay >= 7 ? 1 : lastDay + 1;
                    IsTodayChecked = false;
                }
            }

            Debug.Log($"[Attendance] 상태 로드 완료 - Day{CurrentDay}, 오늘 체크: {IsTodayChecked}");
            OnAttendanceUpdated?.Invoke();
            onComplete?.Invoke(true);
        });
    }

    // ────────────────────────────────────────────────
    // 2. 조회
    // ────────────────────────────────────────────────

    /// <summary>특정 일차의 보상 데이터를 반환.</summary>
    public AttendanceReward GetReward(int day)
    {
        return _rewardMap.TryGetValue(day, out var reward) ? reward : null;
    }

    /// <summary>특정 일차의 카드 상태를 반환.</summary>
    public DayCardState GetCardState(int day)
    {
        if (day < CurrentDay) return DayCardState.Checked;
        if (day == CurrentDay) return IsTodayChecked ? DayCardState.Checked : DayCardState.Today;
        return DayCardState.Locked;
    }

    // ────────────────────────────────────────────────
    // 3. 출석 체크
    // ────────────────────────────────────────────────

    /// <summary>
    /// 오늘 출석 체크. 보상 지급 → 상태 저장 → 퀘스트 진행도 기록 순으로 처리.
    /// </summary>
    public void CheckToday(Action<bool> onComplete)
    {
        if (IsTodayChecked)
        {
            Debug.LogWarning("[Attendance] 오늘 이미 출석 체크함");
            onComplete?.Invoke(false);
            return;
        }

        var reward = GetReward(CurrentDay);
        if (reward == null)
        {
            Debug.LogError($"[Attendance] Day{CurrentDay} 보상 데이터 없음");
            onComplete?.Invoke(false);
            return;
        }

        // 보상 지급 (코인 → 티켓 순차)
        GiveReward(reward, rewardSuccess =>
        {
            if (!rewardSuccess)
            {
                onComplete?.Invoke(false);
                return;
            }

            SaveAttendanceState(saveSuccess =>
            {
                if (saveSuccess)
                {
                    IsTodayChecked = true;
                    QuestManager.Instance.RecordProgress("attendance");
                    OnAttendanceUpdated?.Invoke();
                }
                onComplete?.Invoke(saveSuccess);
            });
        });
    }

    private void GiveReward(AttendanceReward reward, Action<bool> onComplete)
    {
        if (reward.rewardCoin > 0)
        {
            WalletManager.Instance.AddCoin(reward.rewardCoin, coinSuccess =>
            {
                if (!coinSuccess)
                {
                    onComplete?.Invoke(false);
                    return;
                }
                GiveTicket(reward, onComplete);
            });
        }
        else
        {
            GiveTicket(reward, onComplete);
        }
    }

    private void GiveTicket(AttendanceReward reward, Action<bool> onComplete)
    {
        if (reward.rewardTicket > 0)
        {
            WalletManager.Instance.AddTicket(reward.rewardTicket, ticketSuccess =>
                onComplete?.Invoke(ticketSuccess));
        }
        else
        {
            onComplete?.Invoke(true);
        }
    }

    private void SaveAttendanceState(Action<bool> onComplete)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        Param param = new Param();
        param.Add("currentDay", CurrentDay);
        param.Add("lastCheckedDate", today);

        if (string.IsNullOrEmpty(_attendanceRowInDate))
        {
            // 최초 생성
            Backend.GameData.Insert("Attendance", param, callback =>
            {
                if (!callback.IsSuccess())
                {
                    Debug.LogError($"[Attendance] 상태 Insert 실패: {callback}");
                    onComplete?.Invoke(false);
                    return;
                }

                _attendanceRowInDate = callback.GetInDate();
                onComplete?.Invoke(true);
            });
        }
        else
        {
            Backend.GameData.UpdateV2("Attendance", _attendanceRowInDate, Backend.UserInDate, param, callback =>
            {
                if (!callback.IsSuccess())
                {
                    Debug.LogError($"[Attendance] 상태 Update 실패: {callback}");
                    onComplete?.Invoke(false);
                    return;
                }

                onComplete?.Invoke(true);
            });
        }
    }
}

/// <summary>날짜별 출석 보상 데이터 (차트에서 로드)</summary>
[Serializable]
public class AttendanceReward
{
    public int day;          // 1~7
    public int rewardCoin;
    public int rewardTicket;
}