using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 출석 체크 날짜 카드.
/// 코인/티켓 보상을 표시하며, 오늘 카드만 클릭 가능하다.
/// </summary>
public class DayCardUI : MonoBehaviour
{
    [SerializeField] private Image imgRewardIcon;
    [SerializeField] private TMP_Text txtDayNum;
    [SerializeField] private TMP_Text txtRewardCount;
    [SerializeField] private GameObject imgChecked;
    [SerializeField] private Button btnCard;

    [Header("보상 아이콘 스프라이트")]
    [SerializeField] private Sprite spriteCoin;
    [SerializeField] private Sprite spriteTicket;

    public void Setup(int day, AttendanceReward reward, DayCardState state, UnityAction onTodayClick)
    {
        txtDayNum.text = $"{day}일";

        // 보상 표시 (티켓이 있으면 티켓 우선 표시, 코인+티켓 둘 다면 "코인+티켓" 형식)
        if (reward != null)
        {
            if (reward.rewardCoin > 0 && reward.rewardTicket > 0)
            {
                imgRewardIcon.sprite = spriteTicket;
                txtRewardCount.text = $"{reward.rewardCoin:N0}+{reward.rewardTicket}";
            }
            else if (reward.rewardTicket > 0)
            {
                imgRewardIcon.sprite = spriteTicket;
                txtRewardCount.text = reward.rewardTicket.ToString();
            }
            else
            {
                imgRewardIcon.sprite = spriteCoin;
                txtRewardCount.text = reward.rewardCoin.ToString("N0");
            }
        }

        imgChecked.SetActive(state == DayCardState.Checked);

        btnCard.onClick.RemoveAllListeners();

        switch (state)
        {
            case DayCardState.Checked:
                btnCard.interactable = false;
                break;

            case DayCardState.Today:
                btnCard.interactable = true;
                btnCard.onClick.AddListener(onTodayClick);
                break;

            case DayCardState.Locked:
                btnCard.interactable = false;
                break;
        }
    }
}

public enum DayCardState
{
    Checked, // 출석 완료
    Today,   // 오늘 출석 가능
    Locked   // 아직 안 옴
}