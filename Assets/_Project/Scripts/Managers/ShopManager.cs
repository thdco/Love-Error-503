using System;
using System.Collections.Generic;
using System.Linq;
using BackEnd;
using LitJson;
using UnityEngine;

/// <summary>
/// 아이템 카테고리. Set은 같은 setGroupId를 가진 Item/Hair/Top/Bottom을
/// 한번에 구매/착용시키는 묶음 개념이다.
/// </summary>
public enum ItemCategory
{
    Item,
    Hair,
    Top,
    Bottom,
    Set
}

public enum ItemType
{
    Player, // 여주 전용 → 구매 시 매력도 상승
    Couple, // 커플룩 → 구매 시 해당 남주 호감도 상승
    Male    // 남주 전용 → 구매 시 해당 남주 호감도 상승
}

[Serializable]
public class ShopItemData
{
    public string itemId;
    public string itemName;
    public ItemCategory category;
    public ItemType itemType;
    public string setGroupId;
    public string characterId; // Couple/Male 아이템의 남주 구분 ("hajin"/"dohyun"/"siwoo")
    public bool isDefault;
    public int price;
    public int charmAmount;    // Player: 매력도, Couple/Male: 호감도 증가량
    public string thumbnailPath;
    public string spritePath;

    public bool IsMaleRelatedItem => itemType == ItemType.Couple || itemType == ItemType.Male;
}

/// <summary>
/// 상점 아이템 목록 로드, 구매, 인벤토리 조회를 담당하는 매니저 (싱글톤)
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    // 전체 아이템 마스터 데이터 캐시 (itemId 기준)
    private Dictionary<string, ShopItemData> _allItems = new Dictionary<string, ShopItemData>();

    // 유저 보유 아이템 (itemId 집합)
    private HashSet<string> _ownedItemIds = new HashSet<string>();

    // 인벤토리 변경 시 모든 ItemCardUI에 알려주는 이벤트
    public event Action OnInventoryChanged;

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
        // 차트는 동기 함수라 바로 호출
        LoadAllItems(itemsSuccess =>
        {
            if (!itemsSuccess)
            {
                onComplete?.Invoke(false);
                return;
            }

            LoadOwnedItems(onComplete);
        });
    }

    [Header("차트 설정")]
    [SerializeField] private string shopItemsChartId = "249056";

    private void LoadAllItems(Action<bool> onComplete)
    {
        BackendReturnObject bro = Backend.Chart.GetChartContents(shopItemsChartId);

        if (!bro.IsSuccess())
        {
            Debug.LogError($"[Shop] ShopItems 차트 조회 실패: {bro}");
            onComplete?.Invoke(false);
            return;
        }

        _allItems.Clear();
        LitJson.JsonData rows = bro.FlattenRows();

        foreach (LitJson.JsonData row in rows)
        {
            var item = new ShopItemData
            {
                itemId        = row["itemId"].ToString(),
                itemName      = row["itemName"].ToString(),
                category      = Enum.Parse<ItemCategory>(row["category"].ToString()),
                itemType      = HasKey(row, "itemType") ? Enum.Parse<ItemType>(row["itemType"].ToString()) : ItemType.Player,
                setGroupId    = HasKey(row, "setGroupId")    ? row["setGroupId"].ToString()    : string.Empty,
                characterId   = HasKey(row, "characterId")   ? row["characterId"].ToString()   : string.Empty,
                isDefault     = HasKey(row, "isDefault") && row["isDefault"].ToString().Equals("true", System.StringComparison.OrdinalIgnoreCase),
                price         = int.Parse(row["price"].ToString()),
                charmAmount   = int.Parse(row["charmAmount"].ToString()),
                thumbnailPath = HasKey(row, "thumbnailPath") ? row["thumbnailPath"].ToString() : string.Empty,
                spritePath    = HasKey(row, "spritePath")    ? row["spritePath"].ToString()    : string.Empty,
            };
            _allItems[item.itemId] = item;
        }

        Debug.Log($"[Shop] 차트 로드 완료: {_allItems.Count}개");
        onComplete?.Invoke(true);
    }

    private void LoadOwnedItems(Action<bool> onComplete)
    {
        Backend.GameData.Get("UserInventory", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Shop] UserInventory 조회 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _ownedItemIds.Clear();
            var rows = callback.FlattenRows();

            foreach (JsonData row in rows)
                _ownedItemIds.Add(row["itemId"].ToString());

            Debug.Log($"[Shop] 보유 아이템 로드 완료: {_ownedItemIds.Count}개");
            onComplete?.Invoke(true);
        });
    }

    // ────────────────────────────────────────────────
    // 내부 헬퍼
    // ────────────────────────────────────────────────

    private bool HasKey(JsonData row, string key)
    {
        return row.Keys.Contains(key) && row[key] != null;
    }

    // ────────────────────────────────────────────────
    // 2. 조회
    // ────────────────────────────────────────────────

    /// <summary>전체 아이템 목록 반환 (기본 세트 포함).</summary>
    public List<ShopItemData> GetAllItems()
    {
        return new List<ShopItemData>(_allItems.Values);
    }

    /// <summary>itemType + category로 필터링. isDefault 아이템은 상점에 표시 안 함.</summary>
    public List<ShopItemData> GetItemsByTypeAndCategory(ItemType itemType, ItemCategory category)
    {
        return _allItems.Values
            .Where(i => i.itemType == itemType && i.category == category && !i.isDefault)
            .ToList();
    }

    /// <summary>itemType + category + characterId로 필터링.</summary>
    public List<ShopItemData> GetItemsByTypeAndCategory(ItemType itemType, ItemCategory category, string characterId)
    {
        return _allItems.Values
            .Where(i => i.itemType == itemType && i.category == category && i.characterId == characterId && !i.isDefault)
            .ToList();
    }

    /// <summary>카테고리 + characterId로 필터링 (하위 호환용).</summary>
    public List<ShopItemData> GetItemsByCategory(ItemCategory category, string characterId = null)
    {
        var query = _allItems.Values.Where(i => i.category == category && !i.isDefault);
        if (characterId != null)
            query = query.Where(i => i.characterId == characterId);
        return query.ToList();
    }

    public ShopItemData GetItem(string itemId)
    {
        return _allItems.TryGetValue(itemId, out var item) ? item : null;
    }

    public bool IsOwned(string itemId)
    {
        return _ownedItemIds.Contains(itemId);
    }

    public List<ShopItemData> GetSetMembers(string setGroupId)
    {
        return _allItems.Values
            .Where(i => i.setGroupId == setGroupId && i.category != ItemCategory.Set)
            .ToList();
    }

    /// <summary>전체 여주 아이템 매력도 합 (Set, isDefault 제외).</summary>
    public int GetTotalCharmAmount()
    {
        return _allItems.Values
            .Where(i => i.category != ItemCategory.Set && !i.isDefault && i.itemType == ItemType.Player)
            .Sum(i => i.charmAmount);
    }

    /// <summary>보유 중인 여주 아이템의 매력도 합.</summary>
    public int GetOwnedCharmAmount()
    {
        return _allItems.Values
            .Where(i => i.category != ItemCategory.Set && !i.isDefault && i.itemType == ItemType.Player && _ownedItemIds.Contains(i.itemId))
            .Sum(i => i.charmAmount);
    }

    // ────────────────────────────────────────────────
    // 3. 구매
    // ────────────────────────────────────────────────

    /// <summary>
    /// 아이템 구매. Set이면 묶인 4개 아이템을 전부 인벤토리에 추가한다.
    /// 구매 전 코인 차감은 호출부(팝업)에서 처리하고, 이 함수는 인벤토리 반영만 담당한다.
    /// </summary>
    public void AddToInventory(string itemId, Action<bool> onComplete)
    {
        var item = GetItem(itemId);
        if (item == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        List<string> itemIdsToAdd;

        if (item.category == ItemCategory.Set)
        {
            itemIdsToAdd = GetSetMembers(item.setGroupId)
                .Where(i => !IsOwned(i.itemId))
                .Select(i => i.itemId)
                .ToList();
        }
        else
        {
            itemIdsToAdd = new List<string> { itemId };
        }

        InsertInventoryRows(itemIdsToAdd, 0, success =>
        {
            if (!success)
            {
                onComplete?.Invoke(false);
                return;
            }

            // 여주 아이템 → 매력도 갱신 / 남주 아이템 → 호감도 증가
            if (item.IsMaleRelatedItem)
            {
                AffectionManager.Instance.AddAffection(item.characterId, item.charmAmount, _ =>
                    onComplete?.Invoke(true));
            }
            else
            {
                onComplete?.Invoke(true);
            }
        });
    }

    private void InsertInventoryRows(List<string> itemIds, int index, Action<bool> onComplete)
    {
        if (index >= itemIds.Count)
        {
            CharmManager.Instance.Refresh();
            OnInventoryChanged?.Invoke(); // 모든 카드 상태 갱신
            onComplete?.Invoke(true);
            return;
        }

        Param param = new Param();
        param.Add("itemId", itemIds[index]);

        Backend.GameData.Insert("UserInventory", param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Shop] 인벤토리 추가 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _ownedItemIds.Add(itemIds[index]);
            InsertInventoryRows(itemIds, index + 1, onComplete); // 순차 처리
        });
    }
}