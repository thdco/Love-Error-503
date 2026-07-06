using System;
using System.Collections.Generic;
using BackEnd;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 출석 체크 팝업
/// 7일 단위로 순환되는 출석 보상을 표시하고, 오늘 출석 체크를 처리한다.
/// </summary>
public class AttendancePopup : MonoBehaviour
{
    [Header("Day 카드 (1~6일차)")]
    [SerializeField] private List<DayCardUI> leftGridCards; // 순서대로 Day1~6

    [Header("Day7 카드")]
    [SerializeField] private DayCardUI day7Card;

    [Header("닫기 버튼")]
    [SerializeField] private Button btnClose;

    [Header("보상 데이터 (1~7일차 코인 보상량)")]
    [SerializeField] private int[] rewardCoins = { 50, 50, 100, 100, 150, 150, 300 };

    private int _currentDay; // 1~7
    private bool _isTodayChecked;

    private void Awake()
    {
        btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnEnable()
    {
        LoadAttendanceData();
    }

    // ────────────────────────────────────────────────
    // 출석 데이터 로드
    // ────────────────────────────────────────────────

    private void LoadAttendanceData()
    {
        Backend.GameData.Get("Attendance", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Attendance] 조회 실패: {callback}");
                return;
            }

            var rows = callback.FlattenRows();

            if (rows == null || rows.Count == 0)
            {
                // 최초 출석 - Day1부터 시작
                _currentDay = 1;
                _isTodayChecked = false;
            }
            else
            {
                int lastDay = int.Parse(rows[0]["currentDay"].ToString());
                string lastCheckedDate = rows[0]["lastCheckedDate"].ToString();
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

                if (lastCheckedDate == today)
                {
                    // 오늘 이미 체크함
                    _currentDay = lastDay;
                    _isTodayChecked = true;
                }
                else
                {
                    // 다음 날짜 - 다음 Day로 진행 (7일 지나면 1로 순환)
                    _currentDay = lastDay >= 7 ? 1 : lastDay + 1;
                    _isTodayChecked = false;
                }
            }

            RefreshUI();
        });
    }

    private void RefreshUI()
    {
        for (int i = 0; i < leftGridCards.Count; i++)
        {
            int day = i + 1;
            leftGridCards[i].Setup(
                day,
                rewardCoins[i],
                GetCardState(day),
                OnClickTodayCard
            );
        }

        day7Card.Setup(
            7,
            rewardCoins[6],
            GetCardState(7),
            OnClickTodayCard
        );
    }

    private DayCardState GetCardState(int day)
    {
        if (day < _currentDay) return DayCardState.Checked;
        if (day == _currentDay) return _isTodayChecked ? DayCardState.Checked : DayCardState.Today;
        return DayCardState.Locked;
    }

    // ────────────────────────────────────────────────
    // 출석 체크 (오늘 카드 클릭 시 호출)
    // ────────────────────────────────────────────────

    public void OnClickTodayCard()
    {
        if (_isTodayChecked) return;

        int rewardAmount = rewardCoins[_currentDay - 1];

        WalletManager.Instance.AddCoin(rewardAmount, success =>
        {
            if (!success)
            {
                Debug.LogError("[Attendance] 보상 지급 실패");
                return;
            }

            SaveAttendanceData();
        });
    }

    private void SaveAttendanceData()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        Backend.GameData.Get("Attendance", new Where(), getCallback =>
        {
            var rows = getCallback.FlattenRows();
            Param param = new Param();
            param.Add("currentDay", _currentDay);
            param.Add("lastCheckedDate", today);

            if (rows == null || rows.Count == 0)
            {
                // 최초 생성
                Backend.GameData.Insert("Attendance", param, insertCallback =>
                {
                    if (insertCallback.IsSuccess())
                    {
                        _isTodayChecked = true;
                        RefreshUI();
                    }
                });
            }
            else
            {
                // 기존 행 업데이트
                string inDate = rows[0]["inDate"].ToString();
                Where where = new Where();
                where.Equal("inDate", inDate);

                Backend.GameData.Update("Attendance", where, param, updateCallback =>
                {
                    if (updateCallback.IsSuccess())
                    {
                        _isTodayChecked = true;
                        RefreshUI();
                    }
                });
            }
        });
    }

    // ────────────────────────────────────────────────
    // 닫기
    // ────────────────────────────────────────────────

    private void OnClickClose()
    {
        LobbyManager.Instance.CloseAttendance();
    }
}

public enum DayCardState
{
    Checked, // 출석 완료
    Today,   // 오늘 출석 가능
    Locked   // 아직 안 옴
}