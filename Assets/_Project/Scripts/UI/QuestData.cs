using System;
using System.Collections.Generic;

[Serializable]
public class QuestData
{
    public string title;
    public string description;

    public int currentCount;
    public int goalCount;

    public List<RewardData> rewards = new List<RewardData>();

    public bool isRewardReceived;

    public string inDate;      // Quests 테이블의 고유 id
    public string progressInDate; // UserQuestProgress 테이블의 고유 id (본인 진행 상태 행)
}

[Serializable]
public class RewardData
{
    public string rewardType;   // "coin", "ticket" 등
    public int rewardAmount;
    public string rewardIconPath; // Resources.Load 경로
}