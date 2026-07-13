using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 에피소드 씬(03_Episode)의 핵심 스크립트.
/// EpisodeData의 노드를 순서대로 순회하며 대사/선택지/매력도 대결을 연출한다.
/// 진입 방식(메인/일반데이트/스페셜데이트)에 따라 완료 후 처리가 달라진다.
/// </summary>
public class EpisodePlayer : MonoBehaviour
{
    [Header("배경/캐릭터")]
    [SerializeField] private Image imgBackground;
    [SerializeField] private Transform characterSlotLeft;
    [SerializeField] private Transform characterSlotCenter;
    [SerializeField] private Transform characterSlotRight;

    [Header("대사창")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TMP_Text txtSpeakerName;
    [SerializeField] private TMP_Text txtDialogue;

    [Header("선택지")]
    [SerializeField] private GameObject choiceBox;
    [SerializeField] private Transform choiceContent;
    [SerializeField] private ChoiceButtonUI choiceButtonPrefab;

    [Header("매력도 대결")]
    [SerializeField] private GameObject charmBattlePanel;
    [SerializeField] private Slider sliderPlayerCharm;
    [SerializeField] private Slider sliderOpponentCharm;
    [SerializeField] private TMP_Text txtBattleResult;
    [SerializeField] private Button btnGoToCloset;   // "나의 옷장으로"
    [SerializeField] private Button btnStartBattle;  // "대결하기"
    [SerializeField] private Button btnBattleNext;    // 결과 표시 후 다음으로

    [Header("매력도 대결 결과 표시 시간(초)")]
    [SerializeField] private float charmBattleResultDuration = 3f;

    [Header("진행 버튼 (화면 터치로 다음 대사)")]
    [SerializeField] private Button btnAdvance;

    [Header("캐릭터 프리팹")]
    [SerializeField] private CharacterView playerCharacterPrefab;
    [SerializeField] private CharacterView hajinCharacterPrefab;
    [SerializeField] private CharacterView dohyunCharacterPrefab;
    [SerializeField] private CharacterView siwooCharacterPrefab;
    [SerializeField] private CharacterView saemiCharacterPrefab;    // 남새미 (경쟁 상대)
    [SerializeField] private CharacterView npcCharacterPrefab;      // 그 외 조연 전부 대표하는 범용 프리팹

    private EpisodeData _currentEpisode;
    private Dictionary<string, EpisodeNode> _nodeMap;
    private EpisodeNode _currentNode;

    // 진입 컨텍스트 (완료 후 어디로 돌아갈지 결정하기 위함)
    private EpisodePlayContext _context;

    private void Awake()
    {
        btnAdvance.onClick.AddListener(OnClickAdvance);
        btnBattleNext.onClick.AddListener(OnClickBattleNext);
        btnGoToCloset.onClick.AddListener(OnClickGoToCloset);
        btnStartBattle.onClick.AddListener(OnClickStartBattle);
    }

    private void Start()
    {
        var (data, context) = EpisodeManager.Instance.ConsumePendingEpisode();

        if (data == null)
        {
            Debug.LogError("[EpisodePlayer] 씬 진입했지만 로드할 에피소드 데이터가 없습니다.");
            return;
        }

        _currentEpisode = data;
        _context = context;
        _nodeMap = data.nodes.ToDictionary(n => n.nodeId, n => n);

        // 매력도 대결 도중 나갔다 온 경우 → 저장된 대결 노드로 바로 이동
        string pendingBattleNode = EpisodeManager.Instance.ConsumePendingBattleNodeId();
        string startNodeId = !string.IsNullOrEmpty(pendingBattleNode) ? pendingBattleNode : data.nodes[0].nodeId;

        GoToNode(startNodeId);
    }

    // ────────────────────────────────────────────────
    // 진입점
    // ────────────────────────────────────────────────

    /// <summary>씬 로드 후 호출. EpisodeLoader 등에서 컨텍스트를 넘겨준다.</summary>
    public void StartEpisode(EpisodeData episodeData, EpisodePlayContext context)
    {
        _currentEpisode = episodeData;
        _context = context;
        _nodeMap = episodeData.nodes.ToDictionary(n => n.nodeId, n => n);

        // 첫 노드부터 시작
        GoToNode(episodeData.nodes[0].nodeId);
    }

    // ────────────────────────────────────────────────
    // 노드 이동
    // ────────────────────────────────────────────────

    private void GoToNode(string nodeId)
    {
        if (!_nodeMap.TryGetValue(nodeId, out var node))
        {
            Debug.LogError($"[EpisodePlayer] 노드를 찾을 수 없음: {nodeId}");
            return;
        }

        _currentNode = node;

        switch (node.type)
        {
            case "dialogue":
                ShowDialogue(node);
                break;
            case "choice":
                ShowChoice(node);
                break;
            case "enter":
                HandleEnter(node);
                break;
            case "charmBattle":
                ShowCharmBattle(node);
                break;
            case "end":
                OnEpisodeEnd();
                break;
            default:
                Debug.LogWarning($"[EpisodePlayer] 알 수 없는 노드 타입: {node.type}");
                break;
        }
    }

    // ────────────────────────────────────────────────
    // 등장 노드 (여러 캐릭터 동시 등장, 클릭 없이 자동 진행)
    // ────────────────────────────────────────────────

    private void HandleEnter(EpisodeNode node)
    {
        // 기존에 등장해있던 캐릭터 전부 제거 후 새로 생성
        ClearAllCharacters();

        if (node.characters != null)
        {
            foreach (var spawn in node.characters)
                ShowOrUpdateCharacter(spawn.characterId, null, spawn.slot);
        }

        // 유저 입력 없이 바로 다음 노드로
        if (!string.IsNullOrEmpty(node.next))
            GoToNode(node.next);
        else
            Debug.LogWarning($"[EpisodePlayer] enter 노드에 next가 없음: {node.nodeId}");
    }

    private void ClearAllCharacters()
    {
        foreach (var character in _activeCharacters.Values)
        {
            if (character != null)
                Destroy(character.gameObject);
        }
        _activeCharacters.Clear();
    }

    // ────────────────────────────────────────────────
    // 대사 노드
    // ────────────────────────────────────────────────

    private void ShowDialogue(EpisodeNode node)
    {
        dialogueBox.SetActive(true);
        choiceBox.SetActive(false);
        charmBattlePanel.SetActive(false);

        // 배경 전환
        if (!string.IsNullOrEmpty(node.background))
        {
            Sprite bg = Resources.Load<Sprite>(node.background);
            if (bg != null) imgBackground.sprite = bg;
        }

        // 화자 이름 / 대사
        txtSpeakerName.text = string.IsNullOrEmpty(node.speaker) ? string.Empty : GetCharacterDisplayName(node.speaker);
        txtDialogue.text = node.text;

        // 표정 반영 (캐릭터가 이미 씬에 있으면 표정만 갱신, 없으면 등장)
        if (!string.IsNullOrEmpty(node.speaker))
            ShowOrUpdateCharacter(node.speaker, node.expression, node.slot);

        // 현재 화자만 강조, 나머지 등장 캐릭터는 비강조
        RefreshCharacterHighlight(node.speaker);

        btnAdvance.gameObject.SetActive(true);
    }

    private void OnClickAdvance()
    {
        if (_currentNode == null || _currentNode.type != "dialogue") return;

        if (string.IsNullOrEmpty(_currentNode.next))
        {
            Debug.LogWarning("[EpisodePlayer] next가 없는 대사 노드. end 노드를 확인하세요.");
            return;
        }

        GoToNode(_currentNode.next);
    }

    // ────────────────────────────────────────────────
    // 선택지 노드
    // ────────────────────────────────────────────────

    private void ShowChoice(EpisodeNode node)
    {
        dialogueBox.SetActive(true);
        txtSpeakerName.text = string.Empty;
        txtDialogue.text = node.text;
        btnAdvance.gameObject.SetActive(false);

        choiceBox.SetActive(true);
        foreach (Transform child in choiceContent)
            Destroy(child.gameObject);

        foreach (var choice in node.choices)
        {
            ChoiceButtonUI btn = Instantiate(choiceButtonPrefab, choiceContent);
            btn.Setup(choice.text, () => OnClickChoice(choice));
        }
    }

    private void OnClickChoice(EpisodeChoice choice)
    {
        choiceBox.SetActive(false);

        // 데이트 에피소드로 분기하는 선택지인 경우
        if (!string.IsNullOrEmpty(choice.dateEpisodeId))
        {
            EnterDateEpisodeFromChoice(choice);
            return;
        }

        // 일반 분기
        GoToNode(choice.next);
    }

    private void EnterDateEpisodeFromChoice(EpisodeChoice choice)
    {
        var newContext = new EpisodePlayContext
        {
            source = EpisodeSource.MainEpisodeChoice,
            returnToMainNode = choice.returnNode
        };

        if (choice.dateType == "special")
        {
            EpisodeManager.Instance.EnterSpecialDateEpisode(choice.dateEpisodeId, choice.characterId, (success, data, error) =>
            {
                if (!success)
                {
                    Debug.LogWarning($"[EpisodePlayer] 스페셜 데이트 진입 실패: {error}");
                    // 진입 실패 시 메인 스토리는 계속 진행 (returnNode로 복귀)
                    GoToNode(choice.returnNode);
                    return;
                }
                StartEpisode(data, newContext);
            });
        }
        else
        {
            EpisodeManager.Instance.EnterNormalDateEpisode(choice.dateEpisodeId, (success, data, error) =>
            {
                if (!success)
                {
                    Debug.LogWarning($"[EpisodePlayer] 일반 데이트 진입 실패: {error}");
                    GoToNode(choice.returnNode);
                    return;
                }
                StartEpisode(data, newContext);
            });
        }
    }

    // ────────────────────────────────────────────────
    // 매력도 대결 노드
    // ────────────────────────────────────────────────

    private void ShowCharmBattle(EpisodeNode node)
    {
        dialogueBox.SetActive(false);
        choiceBox.SetActive(false);
        btnAdvance.gameObject.SetActive(false);

        charmBattlePanel.SetActive(true);

        float playerPercent = CharmManager.Instance.CharmPercent;
        sliderPlayerCharm.value = playerPercent;
        sliderOpponentCharm.value = node.opponentPercent;
        txtBattleResult.text = string.Empty;

        // 처음엔 선택 버튼(나의 옷장으로 / 대결하기)만 표시
        btnGoToCloset.gameObject.SetActive(true);
        btnStartBattle.gameObject.SetActive(true);
        btnBattleNext.gameObject.SetActive(false);
    }

    /// <summary>"나의 옷장으로" 클릭 - 로비로 나가서 아이템 구매 기회를 준다.</summary>
    private void OnClickGoToCloset()
    {
        EpisodeManager.Instance.SaveCharmBattlePending(_currentEpisode, _context, _currentNode.nodeId);
    }

    /// <summary>"대결하기" 클릭 - 실제 판정 진행.</summary>
    private void OnClickStartBattle()
    {
        btnGoToCloset.gameObject.SetActive(false);
        btnStartBattle.gameObject.SetActive(false);

        StartCoroutine(RunCharmBattle(_currentNode));
    }

    private System.Collections.IEnumerator RunCharmBattle(EpisodeNode node)
    {
        bool resultReceived = false;
        bool isWin = false;

        EpisodeManager.Instance.ResolveCharmBattle(node, win =>
        {
            isWin = win;
            resultReceived = true;
        });

        yield return new WaitUntil(() => resultReceived);

        txtBattleResult.text = isWin ? "승리!" : "패배...";
        _pendingBattleNextNode = isWin ? node.winNext : node.loseNext;

        // 결과를 charmBattleResultDuration초 동안 보여준 뒤 다음으로 진행 가능하게
        yield return new WaitForSeconds(charmBattleResultDuration);

        btnBattleNext.gameObject.SetActive(true);
    }

    private string _pendingBattleNextNode;

    private void OnClickBattleNext()
    {
        if (string.IsNullOrEmpty(_pendingBattleNextNode)) return;
        charmBattlePanel.SetActive(false);
        GoToNode(_pendingBattleNextNode);
    }

    // ────────────────────────────────────────────────
    // 종료 처리
    // ────────────────────────────────────────────────

    private void OnEpisodeEnd()
    {
        dialogueBox.SetActive(false);
        choiceBox.SetActive(false);
        charmBattlePanel.SetActive(false);

        switch (_context.source)
        {
            case EpisodeSource.MainMenu:
                // 메인 에피소드를 처음부터 진입한 경우 → 완료 처리 후 로비 복귀
                EpisodeManager.Instance.CompleteMainEpisode(_ => ReturnToLobby());
                break;

            case EpisodeSource.MainEpisodeChoice:
                // 데이트 에피소드가 끝난 경우 → 원래 메인 에피소드의 지정 노드로 복귀
                if (!string.IsNullOrEmpty(_context.returnToMainNode) && _nodeMap != null)
                {
                    // 데이트 에피소드 노드맵이 아니라 메인 에피소드로 되돌아가야 하므로
                    // 원래 메인 에피소드 데이터를 다시 로드해서 이어간다.
                    ResumeMainEpisodeAt(_context.returnToMainNode);
                }
                else
                {
                    ReturnToLobby();
                }
                break;

            case EpisodeSource.DatePanel:
                // 데이트 패널에서 바로 진입한 독립 데이트 에피소드 → 로비 복귀
                ReturnToLobby();
                break;
        }
    }

    /// <summary>데이트 에피소드 완료 후, 원래 진행 중이던 메인 에피소드로 복귀.</summary>
    private void ResumeMainEpisodeAt(string nodeId)
    {
        string episodeId = $"ep{EpisodeManager.Instance.CurrentMainEpisode:D2}";
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Episodes/Main/{episodeId}");

        if (jsonAsset == null)
        {
            Debug.LogError($"[EpisodePlayer] 메인 에피소드 복귀 실패: {episodeId}");
            ReturnToLobby();
            return;
        }

        EpisodeData mainData = JsonUtility.FromJson<EpisodeData>(jsonAsset.text);
        _currentEpisode = mainData;
        _nodeMap = mainData.nodes.ToDictionary(n => n.nodeId, n => n);
        _context = new EpisodePlayContext { source = EpisodeSource.MainMenu };

        GoToNode(nodeId);
    }

    private void ReturnToLobby()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("02_Lobby");
    }

    // ────────────────────────────────────────────────
    // 캐릭터 표시
    // ────────────────────────────────────────────────

    private Dictionary<string, CharacterView> _activeCharacters = new Dictionary<string, CharacterView>();

    private void ShowOrUpdateCharacter(string characterId, string expression, string slot)
    {
        if (_activeCharacters.ContainsKey(characterId))
        {
            // 이미 등장해있으면 재생성하지 않음 (TODO: 표정 스프라이트 교체 추후 확장)
            return;
        }

        CharacterView prefab = characterId switch
        {
            "player" => playerCharacterPrefab,
            "hajin"  => hajinCharacterPrefab,
            "dohyun" => dohyunCharacterPrefab,
            "siwoo"  => siwooCharacterPrefab,
            "saemi"  => saemiCharacterPrefab,
            _ => npcCharacterPrefab // 등록되지 않은 이름은 전부 범용 조연 프리팹
        };

        if (prefab == null) return;

        // slot 값이 없으면 여주는 left, 남주는 right를 기본값으로 사용
        string resolvedSlot = string.IsNullOrEmpty(slot)
            ? (characterId == "player" ? "left" : "right")
            : slot;

        Transform slotTransform = resolvedSlot switch
        {
            "left"   => characterSlotLeft,
            "center" => characterSlotCenter,
            "right"  => characterSlotRight,
            _ => characterSlotLeft
        };

        CharacterView instance = Instantiate(prefab, slotTransform);

        // 프리팹 자체에 저장된 좌표를 무시하고 슬롯 중앙에 맞춤
        RectTransform rt = instance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        _activeCharacters[characterId] = instance;
    }

    /// <summary>
    /// 현재 화자(speakerId)만 강조하고 나머지 등장 캐릭터는 비강조 처리.
    /// speakerId가 비어있으면(나레이션) 전체를 비강조 상태로 둔다.
    /// </summary>
    private void RefreshCharacterHighlight(string speakerId)
    {
        foreach (var kvp in _activeCharacters)
        {
            bool isSpeaking = !string.IsNullOrEmpty(speakerId) && kvp.Key == speakerId;
            kvp.Value.SetHighlighted(isSpeaking);
        }
    }

    private string GetCharacterDisplayName(string characterId)
    {
        if (characterId == "player")
        {
            string name = AuthManager.Instance?.PlayerName;
            return string.IsNullOrEmpty(name) ? "나" : name;
        }

        return characterId switch
        {
            "hajin"  => "서하진",
            "dohyun" => "강도현",
            "siwoo"  => "민시우",
            "saemi"  => "남새미",
            _ => characterId // 등록되지 않은 이름은 JSON에 적힌 그대로 출력
        };
    }
}

/// <summary>에피소드가 어떤 경로로 시작됐는지 나타내는 컨텍스트.</summary>
public class EpisodePlayContext
{
    public EpisodeSource source;
    public string returnToMainNode; // MainEpisodeChoice인 경우 복귀할 노드 ID
}

public enum EpisodeSource
{
    MainMenu,           // 메인 패널 "에피소드" 버튼으로 진입
    MainEpisodeChoice,  // 메인 에피소드 중 선택지로 데이트 에피소드 진입
    DatePanel           // 데이트 패널에서 직접 진입
}