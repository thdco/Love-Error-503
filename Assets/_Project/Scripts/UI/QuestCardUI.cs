using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 카드 UI. QuestData + QuestProgress를 받아 표시한다.
/// 달성 완료 시 보상 수령 버튼 활성화, 수령 완료 시 비활성화.
/// </summary>
public class QuestCardUI : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtDetail;

    [Header("Progress")]
    [SerializeField] private Slider sliderProgress;
    [SerializeField] private TMP_Text txtAchievementRate;

    [Header("Reward")]
    [SerializeField] private Transform rewardGrid;
    [SerializeField] private RewardCardUI rewardCardPrefab;

    [Header("Button")]
    [SerializeField] private Button btnGet;
    [SerializeField] private TMP_Text txtBtnGet;

    private QuestData _questData;
    private QuestProgress _questProgress;

    public void Initialize(QuestData quest, QuestProgress progress)
    {
        _questData = quest;
        _questProgress = progress;

        txtTitle.text = quest.title;
        txtDetail.text = quest.description;

        sliderProgress.maxValue = quest.goalCount;
        sliderProgress.value = progress.currentCount;
        txtAchievementRate.text = $"{progress.currentCount}/{quest.goalCount}";

        RefreshRewards(quest);

        btnGet.onClick.RemoveAllListeners();
        btnGet.onClick.AddListener(OnClickGetReward);

        RefreshState();
    }

    public void RefreshState()
    {
        if (_questData == null) return;

        _questProgress = QuestManager.Instance.GetProgress(_questData.questId);

        sliderProgress.value = _questProgress.currentCount;
        txtAchievementRate.text = $"{_questProgress.currentCount}/{_questData.goalCount}";

        bool isCompleted = _questProgress.currentCount >= _questData.goalCount;
        bool isReceived = _questProgress.isRewardReceived;

        btnGet.interactable = isCompleted && !isReceived;

        if (isReceived)
            txtBtnGet.text = "완료";
        else if (isCompleted)
            txtBtnGet.text = "받기";
        else
            txtBtnGet.text = "진행중";
    }

    private void RefreshRewards(QuestData quest)
    {
        foreach (Transform child in rewardGrid)
            Destroy(child.gameObject);

        if (quest.rewardCoin > 0)
        {
            RewardCardUI card = Instantiate(rewardCardPrefab, rewardGrid);
            card.Setup(new RewardData
            {
                rewardType = "coin",
                rewardAmount = quest.rewardCoin,
                rewardIconPath = "Icons/coin_icon"
            });
        }

        if (quest.rewardTicket > 0)
        {
            RewardCardUI card = Instantiate(rewardCardPrefab, rewardGrid);
            card.Setup(new RewardData
            {
                rewardType = "ticket",
                rewardAmount = quest.rewardTicket,
                rewardIconPath = "Icons/ticket_icon"
            });
        }
    }

    private void OnClickGetReward()
    {
        QuestManager.Instance.ClaimReward(_questData.questId, success =>
        {
            if (success)
                RefreshState();
            else
                Debug.LogWarning($"[QuestCard] 보상 수령 실패: {_questData.questId}");
        });
    }
}