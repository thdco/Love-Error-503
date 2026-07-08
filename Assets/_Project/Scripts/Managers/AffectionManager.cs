using System;
using BackEnd;
using LitJson;
using UnityEngine;

/// <summary>
/// 남주별 호감도를 관리하는 매니저 (싱글톤)
/// 남주 아이템/커플룩 구매 시 호감도가 증가한다.
/// </summary>
public class AffectionManager : MonoBehaviour
{
    public static AffectionManager Instance { get; private set; }

    public int AffectionHajin { get; private set; }
    public int AffectionDohyun { get; private set; }
    public int AffectionSiwoo { get; private set; }

    public event Action OnAffectionUpdated;

    private string _affectionRowInDate;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ────────────────────────────────────────────────
    // 1. 초기화 (로그인 후 호출)
    // ────────────────────────────────────────────────

    public void Initialize(Action<bool> onComplete)
    {
        Backend.GameData.Get("Affection", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Affection] 조회 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            var rows = callback.FlattenRows();

            if (rows == null || rows.Count == 0)
            {
                CreateAffectionRow(onComplete);
            }
            else
            {
                JsonData row = rows[0];
                _affectionRowInDate = row["inDate"].ToString();
                AffectionHajin = int.Parse(row["hajin"].ToString());
                AffectionDohyun = int.Parse(row["dohyun"].ToString());
                AffectionSiwoo = int.Parse(row["siwoo"].ToString());

                Debug.Log($"[Affection] 불러오기 완료 - 하진: {AffectionHajin}, 도현: {AffectionDohyun}, 시우: {AffectionSiwoo}");
                OnAffectionUpdated?.Invoke();
                onComplete?.Invoke(true);
            }
        });
    }

    private void CreateAffectionRow(Action<bool> onComplete)
    {
        Param param = new Param();
        param.Add("hajin", 0);
        param.Add("dohyun", 0);
        param.Add("siwoo", 0);

        Backend.GameData.Insert("Affection", param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Affection] 생성 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _affectionRowInDate = callback.GetInDate();
            AffectionHajin = 0;
            AffectionDohyun = 0;
            AffectionSiwoo = 0;

            Debug.Log("[Affection] 최초 생성 완료");
            OnAffectionUpdated?.Invoke();
            onComplete?.Invoke(true);
        });
    }

    // ────────────────────────────────────────────────
    // 2. 호감도 증가
    // ────────────────────────────────────────────────

    /// <summary>
    /// 남주 아이템 구매 시 호출. characterId에 해당하는 남주의 호감도를 증가시킨다.
    /// </summary>
    public void AddAffection(string characterId, int amount, Action<bool> onComplete = null)
    {
        if (amount <= 0)
        {
            onComplete?.Invoke(true);
            return;
        }

        string column = characterId switch
        {
            "hajin" => "hajin",
            "dohyun" => "dohyun",
            "siwoo" => "siwoo",
            _ => null
        };

        if (column == null)
        {
            Debug.LogWarning($"[Affection] 알 수 없는 characterId: {characterId}");
            onComplete?.Invoke(false);
            return;
        }

        Param param = new Param();
        param.AddCalculation(column, GameInfoOperator.addition, amount);

        Backend.GameData.UpdateWithCalculation("Affection", new Where(), param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Affection] 호감도 업데이트 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            switch (column)
            {
                case "hajin": AffectionHajin += amount; break;
                case "dohyun": AffectionDohyun += amount; break;
                case "siwoo": AffectionSiwoo += amount; break;
            }

            Debug.Log($"[Affection] {characterId} 호감도 +{amount}");
            OnAffectionUpdated?.Invoke();
            onComplete?.Invoke(true);
        });
    }

    /// <summary>특정 남주의 호감도를 반환.</summary>
    public int GetAffection(string characterId)
    {
        return characterId switch
        {
            "hajin" => AffectionHajin,
            "dohyun" => AffectionDohyun,
            "siwoo" => AffectionSiwoo,
            _ => 0
        };
    }
}
