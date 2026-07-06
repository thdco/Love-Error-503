using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 보상 카드. QuestCardUI의 Grid_Reward 안에 동적으로 생성된다.
/// 코인/티켓 등 재화 아이콘과 수량을 표시한다.
/// </summary>
public class RewardCardUI : MonoBehaviour
{
    [SerializeField] private Image imgReward;
    [SerializeField] private TMP_Text txtRewardCount;

    public void Setup(RewardData reward)
    {
        txtRewardCount.text = reward.rewardAmount.ToString("N0");

        if (!string.IsNullOrEmpty(reward.rewardIconPath))
        {
            Sprite icon = Resources.Load<Sprite>(reward.rewardIconPath);
            if (icon != null) imgReward.sprite = icon;
        }
    }
}