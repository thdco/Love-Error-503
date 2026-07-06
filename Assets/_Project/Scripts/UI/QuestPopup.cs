using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestPopup : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popups;

    [SerializeField] private Button btnClose;

    [Header("Quest")]
    [SerializeField] private Transform content;

    [SerializeField] private QuestCardUI questCardPrefab;

    private readonly List<QuestCardUI> cards = new();

    private void Awake()
    {
        btnClose.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        LoadQuest();
    }

    public void LoadQuest()
    {
        Clear();

        // TODO : Backend에서 Quest 가져오기

        List<QuestData> questList = DummyQuest();

        foreach (QuestData quest in questList)
        {
            QuestCardUI card =
                Instantiate(questCardPrefab, content);

            card.Initialize(quest);

            cards.Add(card);
        }
    }

    private void Clear()
    {
        foreach (QuestCardUI card in cards)
        {
            Destroy(card.gameObject);
        }

        cards.Clear();
    }

    private void Close()
    {
        LobbyManager.Instance.CloseQuest();
    }

    private List<QuestData> DummyQuest()
    {
        return new List<QuestData>()
        {
            new QuestData()
            {
                title = "튜토리얼 완료",
                description = "게임 시작하기",
                currentCount = 1,
                goalCount = 1,
                rewards = new List<RewardData>
                {
                    new RewardData { rewardType = "coin", rewardAmount = 100 }
                }
            },
            new QuestData()
            {
                title = "데이트 3회",
                description = "데이트를 3번 진행",
                currentCount = 1,
                goalCount = 3,
                rewards = new List<RewardData>
                {
                    new RewardData { rewardType = "coin", rewardAmount = 200 },
                    new RewardData { rewardType = "ticket", rewardAmount = 1 }
                }
            }
        };
    }
}