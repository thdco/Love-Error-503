using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 팝업. QuestManager에서 실데이터를 불러와 QuestCardUI를 동적 생성한다.
/// QuestManager.OnQuestUpdated 이벤트로 자동 갱신된다.
/// </summary>
public class QuestPopup : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private Button btnClose;

    [Header("Quest")]
    [SerializeField] private Transform content;
    [SerializeField] private QuestCardUI questCardPrefab;

    private readonly List<QuestCardUI> _cards = new();

    private void Awake()
    {
        btnClose.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        QuestManager.Instance.OnQuestUpdated += Refresh;
        LoadQuest();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestUpdated -= Refresh;
    }

    public void LoadQuest()
    {
        Clear();

        List<QuestData> questList = QuestManager.Instance.GetAllQuests();

        foreach (QuestData quest in questList)
        {
            QuestCardUI card = Instantiate(questCardPrefab, content);
            QuestProgress progress = QuestManager.Instance.GetProgress(quest.questId);
            card.Initialize(quest, progress);
            _cards.Add(card);
        }
    }

    /// <summary>퀘스트 진행도 변경 시 전체 카드 갱신.</summary>
    private void Refresh()
    {
        foreach (var card in _cards)
            card.RefreshState();
    }

    private void Clear()
    {
        foreach (QuestCardUI card in _cards)
            Destroy(card.gameObject);
        _cards.Clear();
    }

    private void Close()
    {
        LobbyManager.Instance.CloseQuest();
    }
}