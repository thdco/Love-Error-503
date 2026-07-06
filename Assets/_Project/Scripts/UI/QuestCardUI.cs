using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestCardUI : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtDetail;

    [Header("Progress")]
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text txtAchievementRate;

    [Header("Reward")]
    [SerializeField] private Transform rewardGrid; // Grid_Reward
    [SerializeField] private RewardCardUI rewardCardPrefab;

    [Header("Button")]
    [SerializeField] private Button btnGet;

    private QuestData currentQuest;

    public void Initialize(QuestData quest)
    {
        currentQuest = quest;

        txtTitle.text = quest.title;
        txtDetail.text = quest.description;

        slider.maxValue = quest.goalCount;
        slider.value = quest.currentCount;

        txtAchievementRate.text = $"{quest.currentCount}/{quest.goalCount}";

        RefreshRewards(quest.rewards);

        btnGet.interactable =
            quest.currentCount >= quest.goalCount &&
            !quest.isRewardReceived;

        btnGet.onClick.RemoveAllListeners();
        btnGet.onClick.AddListener(GetReward);
    }

    private void RefreshRewards(System.Collections.Generic.List<RewardData> rewards)
    {
        // 기존 보상 카드 제거
        foreach (Transform child in rewardGrid)
            Destroy(child.gameObject);

        // 보상 개수만큼 RewardCard 동적 생성
        foreach (RewardData reward in rewards)
        {
            RewardCardUI card = Instantiate(rewardCardPrefab, rewardGrid);
            card.Setup(reward);
        }
    }

    private void GetReward()
    {
        // TODO: Backend 보상 수령 처리 (QuestManager 연동 후 구현)
        btnGet.interactable = false;
    }
}