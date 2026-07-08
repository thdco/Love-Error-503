using System;
using UnityEngine;

/// <summary>
/// 매력도(%) 계산을 담당하는 매니저 (싱글톤)
/// 매력도 = 보유 중인 아이템의 매력도 합 / 게임 내 전체 아이템 매력도 합 × 100
/// 착용 여부와 무관하게 "보유"만으로 계산된다.
/// </summary>
public class CharmManager : MonoBehaviour
{
    public static CharmManager Instance { get; private set; }

    public float CharmPercent { get; private set; }

    public event Action OnCharmUpdated;

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

    private void OnEnable()
    {
        // ShopManager 인벤토리가 갱신될 때마다(구매 시) 매력도 재계산
        // ShopManager에 구매 완료 이벤트가 없다면, 구매 성공 콜백에서 직접 Refresh() 호출하는 방식으로 사용
    }

    /// <summary>
    /// 매력도 재계산. ShopManager의 인벤토리 데이터가 최신 상태여야 한다.
    /// 아이템 구매 성공, 로그인 초기화 완료 시 호출한다.
    /// </summary>
    public void Refresh()
    {
        int totalCharm = ShopManager.Instance.GetTotalCharmAmount();
        int ownedCharm = ShopManager.Instance.GetOwnedCharmAmount();

        CharmPercent = totalCharm <= 0 ? 0f : (float)ownedCharm / totalCharm * 100f;

        Debug.Log($"[Charm] 매력도 갱신: {ownedCharm}/{totalCharm} ({CharmPercent:F1}%)");
        OnCharmUpdated?.Invoke();
    }
}
