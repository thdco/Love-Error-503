using System;

/// <summary>
/// 퀘스트 마스터 데이터 (차트에서 로드)
/// </summary>
[Serializable]
public class QuestData
{
    public string questId;
    public string title;
    public string description;
    public string questType;  // "ad"/"attendance"/"episode"/"purchase_couple"/"purchase_set"/"date_normal"/"date_special"
    public int goalCount;
    public int rewardCoin;
    public int rewardTicket;
}

/// <summary>
/// 유저별 퀘스트 진행 상태 (UserQuestProgress 테이블에서 로드)
/// </summary>
[Serializable]
public class QuestProgress
{
    public string questId;
    public int currentCount;
    public bool isRewardReceived;
    public string inDate;
}

/// <summary>
/// 보상 데이터 (RewardCardUI에서 사용)
/// </summary>
[Serializable]
public class RewardData
{
    public string rewardType;      // "coin", "ticket"
    public int rewardAmount;
    public string rewardIconPath;  // Resources.Load 경로
}