using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DayCardUI : MonoBehaviour
{
    [SerializeField] private Image imgRewardIcon;
    [SerializeField] private TMP_Text txtDayNum;
    [SerializeField] private TMP_Text txtRewardCount;
    [SerializeField] private GameObject imgChecked;
    [SerializeField] private Button btnCard;

    public void Setup(int day, int rewardCoin, DayCardState state, UnityAction onTodayClick)
    {
        txtDayNum.text = $"{day}일";
        txtRewardCount.text = rewardCoin.ToString();

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