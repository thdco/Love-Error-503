using System;
using System.Collections.Generic;
using BackEnd;
using LitJson;
using UnityEngine;

/// <summary>
/// 캐릭터별 착용 상태를 관리하는 매니저 (싱글톤)
/// 여주(player) + 남주 3명(hajin/dohyun/siwoo)의 착용 슬롯을 UserEquipment 테이블 1행에 저장한다.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    // 캐릭터별 착용 상태 (characterId → 슬롯)
    private Dictionary<string, EquipmentSlot> _equipmentMap = new Dictionary<string, EquipmentSlot>();

    private string _rowInDate;

    public static readonly string[] CharacterIds = { "player", "hajin", "dohyun", "siwoo" };

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 캐릭터별 슬롯 초기화
        foreach (var id in CharacterIds)
            _equipmentMap[id] = new EquipmentSlot();
    }

    // ────────────────────────────────────────────────
    // 1. 초기화
    // ────────────────────────────────────────────────

    public void Initialize(Action<bool> onComplete)
    {
        Backend.GameData.Get("UserEquipment", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Equipment] 조회 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            var rows = callback.FlattenRows();

            if (rows == null || rows.Count == 0)
            {
                // 최초 로그인 → 기본 세트 자동 착용
                ApplyDefaultEquipment(onComplete);
            }
            else
            {
                JsonData row = rows[0];
                _rowInDate = row["inDate"].ToString();

                foreach (var id in CharacterIds)
                {
                    _equipmentMap[id] = new EquipmentSlot
                    {
                        hair   = GetOrEmpty(row, $"{id}_equippedHair"),
                        top    = GetOrEmpty(row, $"{id}_equippedTop"),
                        bottom = GetOrEmpty(row, $"{id}_equippedBottom"),
                        item   = GetOrEmpty(row, $"{id}_equippedItem"),
                    };
                }

                Debug.Log("[Equipment] 착용 정보 불러오기 완료");
                OnEquipmentChanged?.Invoke();
                onComplete?.Invoke(true);
            }
        });
    }

    /// <summary>최초 로그인 시 isDefault = true인 아이템을 자동 착용.</summary>
    private void ApplyDefaultEquipment(Action<bool> onComplete)
    {
        var allItems = ShopManager.Instance.GetAllItems();

        foreach (var item in allItems)
        {
            if (!item.isDefault) continue;

            string charId = item.IsMaleRelatedItem ? item.characterId : "player";

            if (!_equipmentMap.ContainsKey(charId)) continue;

            switch (item.category)
            {
                case ItemCategory.Hair:   _equipmentMap[charId].hair   = item.itemId; break;
                case ItemCategory.Top:    _equipmentMap[charId].top    = item.itemId; break;
                case ItemCategory.Bottom: _equipmentMap[charId].bottom = item.itemId; break;
                case ItemCategory.Item:   _equipmentMap[charId].item   = item.itemId; break;
            }
        }

        CreateEquipmentRow(onComplete);
    }

    private void CreateEquipmentRow(Action<bool> onComplete)
    {
        Param param = BuildParam();

        Backend.GameData.Insert("UserEquipment", param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Equipment] 생성 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _rowInDate = callback.GetInDate();
            Debug.Log("[Equipment] 착용 정보 최초 생성 완료");
            OnEquipmentChanged?.Invoke();
            onComplete?.Invoke(true);
        });
    }

    // ────────────────────────────────────────────────
    // 2. 착용
    // ────────────────────────────────────────────────

    /// <summary>
    /// 아이템 착용. characterId가 비어있으면 여주("player")에 착용.
    /// Set이면 해당 캐릭터의 4개 슬롯을 한번에 교체.
    /// </summary>
    public void Equip(ShopItemData item, Action<bool> onComplete)
    {
        string charId = item.IsMaleRelatedItem ? item.characterId : "player";

        if (!_equipmentMap.ContainsKey(charId))
        {
            Debug.LogWarning($"[Equipment] 알 수 없는 characterId: {charId}");
            onComplete?.Invoke(false);
            return;
        }

        if (item.category == ItemCategory.Set)
        {
            var members = ShopManager.Instance.GetSetMembers(item.setGroupId);
            foreach (var member in members)
            {
                switch (member.category)
                {
                    case ItemCategory.Hair:   _equipmentMap[charId].hair   = member.itemId; break;
                    case ItemCategory.Top:    _equipmentMap[charId].top    = member.itemId; break;
                    case ItemCategory.Bottom: _equipmentMap[charId].bottom = member.itemId; break;
                    case ItemCategory.Item:   _equipmentMap[charId].item   = member.itemId; break;
                }
            }
        }
        else
        {
            switch (item.category)
            {
                case ItemCategory.Hair:   _equipmentMap[charId].hair   = item.itemId; break;
                case ItemCategory.Top:    _equipmentMap[charId].top    = item.itemId; break;
                case ItemCategory.Bottom: _equipmentMap[charId].bottom = item.itemId; break;
                case ItemCategory.Item:   _equipmentMap[charId].item   = item.itemId; break;
            }
        }

        SaveEquipment(onComplete);
    }

    /// <summary>아이템 착용 해제. 해당 슬롯을 빈 문자열로 초기화한다.</summary>
    public void Unequip(ShopItemData item, Action<bool> onComplete)
    {
        string charId = item.IsMaleRelatedItem ? item.characterId : "player";

        if (!_equipmentMap.ContainsKey(charId))
        {
            onComplete?.Invoke(false);
            return;
        }

        if (item.category == ItemCategory.Set)
        {
            var members = ShopManager.Instance.GetSetMembers(item.setGroupId);
            foreach (var member in members)
            {
                switch (member.category)
                {
                    case ItemCategory.Hair:   _equipmentMap[charId].hair   = string.Empty; break;
                    case ItemCategory.Top:    _equipmentMap[charId].top    = string.Empty; break;
                    case ItemCategory.Bottom: _equipmentMap[charId].bottom = string.Empty; break;
                    case ItemCategory.Item:   _equipmentMap[charId].item   = string.Empty; break;
                }
            }
        }
        else
        {
            switch (item.category)
            {
                case ItemCategory.Hair:   _equipmentMap[charId].hair   = string.Empty; break;
                case ItemCategory.Top:    _equipmentMap[charId].top    = string.Empty; break;
                case ItemCategory.Bottom: _equipmentMap[charId].bottom = string.Empty; break;
                case ItemCategory.Item:   _equipmentMap[charId].item   = string.Empty; break;
            }
        }

        SaveEquipment(onComplete);
    }

    private void SaveEquipment(Action<bool> onComplete)
    {
        Param param = BuildParam();

        Backend.GameData.UpdateV2("UserEquipment", _rowInDate, Backend.UserInDate, param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Equipment] 착용 저장 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log("[Equipment] 착용 정보 저장 완료");
            OnEquipmentChanged?.Invoke();
            onComplete?.Invoke(true);
        });
    }

    // ────────────────────────────────────────────────
    // 3. 조회
    // ────────────────────────────────────────────────

    public EquipmentSlot GetSlot(string characterId)
    {
        return _equipmentMap.TryGetValue(characterId, out var slot) ? slot : new EquipmentSlot();
    }

    public bool IsEquipped(string characterId, ItemCategory category, string itemId)
    {
        if (!_equipmentMap.TryGetValue(characterId, out var slot)) return false;

        return category switch
        {
            ItemCategory.Hair   => slot.hair   == itemId,
            ItemCategory.Top    => slot.top    == itemId,
            ItemCategory.Bottom => slot.bottom == itemId,
            ItemCategory.Item   => slot.item   == itemId,
            _ => false
        };
    }

    // ────────────────────────────────────────────────
    // 내부 헬퍼
    // ────────────────────────────────────────────────

    private Param BuildParam()
    {
        Param param = new Param();
        foreach (var id in CharacterIds)
        {
            var slot = _equipmentMap[id];
            param.Add($"{id}_equippedHair",   slot.hair);
            param.Add($"{id}_equippedTop",    slot.top);
            param.Add($"{id}_equippedBottom", slot.bottom);
            param.Add($"{id}_equippedItem",   slot.item);
        }
        return param;
    }

    private string GetOrEmpty(JsonData row, string key)
    {
        return row.Keys.Contains(key) && row[key] != null ? row[key].ToString() : string.Empty;
    }
}

/// <summary>캐릭터 1명의 착용 슬롯.</summary>
[Serializable]
public class EquipmentSlot
{
    public string hair   = string.Empty;
    public string top    = string.Empty;
    public string bottom = string.Empty;
    public string item   = string.Empty;
}