using System;
using System.Collections.Generic;

/// <summary>
/// 에피소드 JSON 최상위 구조.
/// Resources/Episodes/Main/epXX.json, Resources/Episodes/Date/xxx.json 형태로 저장된다.
/// </summary>
[Serializable]
public class EpisodeData
{
    public string episodeId;
    public List<EpisodeNode> nodes;
}

/// <summary>
/// 에피소드를 구성하는 노드 하나.
/// type에 따라 사용하는 필드가 다르다 (JSON 특성상 전부 nullable하게 둔다).
/// </summary>
[Serializable]
public class EpisodeNode
{
    public string nodeId;
    public string type;          // "dialogue" / "choice" / "charmBattle" / "end"

    // dialogue 전용
    public string speaker;       // 화자 characterId ("player"/"hajin"/"dohyun"/"siwoo", 나레이션이면 빈값)
    public string slot;          // 캐릭터 위치 ("left"/"center"/"right"), 비어있으면 기본값 사용
    public string expression;    // 표정 (예: "smile", "sad")
    public string background;    // 배경 스프라이트 경로
    public string text;          // 대사 내용
    public string next;          // 다음 노드 ID

    // enter 전용 (여러 캐릭터 동시 등장, 클릭 없이 자동으로 next 이동)
    public List<EpisodeCharacterSpawn> characters;

    // choice 전용
    public List<EpisodeChoice> choices;

    // charmBattle 전용
    public int opponentPercent;  // 경쟁 상대 매력도 (20/50/80)
    public int winAffectionBonus; // 승리 시 전체 남주 호감도 보너스 (10/20/30)
    public string winNext;       // 승리 시 다음 노드
    public string loseNext;      // 패배 시 다음 노드
}

/// <summary>
/// enter 타입 노드에서 한 번에 등장시킬 캐릭터 하나.
/// </summary>
[Serializable]
public class EpisodeCharacterSpawn
{
    public string characterId;
    public string slot; // "left"/"center"/"right"
}

/// <summary>
/// choice 타입 노드의 선택지 하나.
/// </summary>
[Serializable]
public class EpisodeChoice
{
    public string text;
    public string next;             // 다음 노드 ID (일반 선택지)
    public string dateEpisodeId;    // 데이트 에피소드로 분기하는 경우 (비어있으면 일반 분기)
    public string dateType;         // "normal" / "special"
    public string characterId;      // 데이트 상대 characterId
    public string returnNode;       // 데이트 에피소드 완료 후 돌아올 메인 에피소드 노드 ID
}