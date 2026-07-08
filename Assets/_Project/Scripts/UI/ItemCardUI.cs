using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옷장/데이트 패널의 아이템 카드 (ItemCard, CoupleSetCard 공용).
/// 미보유/보유/착용중 상태에 따라 하단 그룹을 다르게 표시하고,
/// 클릭 시 구매 확인 또는 착용 확인 팝업을 연다.
/// ShopManager.OnInventoryChanged, EquipmentManager.OnEquipmentChanged 이벤트로 자동 갱신.
/// </summary>
public class ItemCardUI : MonoBehaviour
{
    [Header("공통")]
    [SerializeField] private Image imgThumbnail;
    [SerializeField] private TMP_Text txtItemName;
    [SerializeField] private Button btnCard;

    [Header("매력도/호감도 (CoupleSetCard는 비워둬도 됨)")]
    [SerializeField] private TMP_Text txtCharmAmount;
    [SerializeField] private GameObject imgCharmIcon;

    [Header("하단 상태 그룹")]
    [SerializeField] private GameObject groupUnowned;
    [SerializeField] private TMP_Text txtPrice;
    [SerializeField] private GameObject groupOwned;
    [SerializeField] private GameObject groupWearing;

    private ShopItemData _itemData;

    private void Awake()
    {
        btnCard.onClick.AddListener(OnClickCard);
    }

    private void OnEnable()
    {
        if (ShopManager.Instance != null)
            ShopManager.Instance.OnInventoryChanged += RefreshState;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += RefreshState;
    }

    private void OnDisable()
    {
        if (ShopManager.Instance != null)
            ShopManager.Instance.OnInventoryChanged -= RefreshState;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= RefreshState;
    }

    // ────────────────────────────────────────────────
    // 초기화
    // ────────────────────────────────────────────────

    public void Setup(ShopItemData itemData)
    {
        _itemData = itemData;

        txtItemName.text = itemData.itemName;

        if (!string.IsNullOrEmpty(itemData.thumbnailPath))
        {
            Sprite thumb = Resources.Load<Sprite>(itemData.thumbnailPath);
            if (thumb != null) imgThumbnail.sprite = thumb;
        }

        // 매력도(여주) / 호감도(남주) 표시
        if (txtCharmAmount != null)
        {
            bool showStat = itemData.charmAmount > 0;
            txtCharmAmount.gameObject.SetActive(showStat);
            if (imgCharmIcon != null) imgCharmIcon.SetActive(showStat);
            if (showStat)
            {
                string label = itemData.IsMaleRelatedItem ? "호감도" : "매력도";
                txtCharmAmount.text = $"+{itemData.charmAmount} {label}";
            }
        }

        RefreshState();
    }

    // ────────────────────────────────────────────────
    // 상태 갱신 (구매/착용/이벤트 시 자동 호출)
    // ────────────────────────────────────────────────

    public void RefreshState()
    {
        if (_itemData == null) return;

        bool isOwned = IsOwnedCheck();
        bool isWearing = IsWearing();

        groupUnowned.SetActive(!isOwned);
        groupOwned.SetActive(isOwned && !isWearing);
        groupWearing.SetActive(isWearing);

        if (!isOwned && txtPrice != null)
            txtPrice.text = _itemData.price.ToString("N0");
    }

    /// <summary>
    /// Set은 멤버 아이템이 하나라도 보유됐을 때 보유중으로 간주.
    /// 개별 아이템은 해당 itemId가 인벤토리에 있을 때 보유중.
    /// </summary>
    private bool IsOwnedCheck()
    {
        if (_itemData.category == ItemCategory.Set)
        {
            var members = ShopManager.Instance.GetSetMembers(_itemData.setGroupId);
            foreach (var member in members)
            {
                if (ShopManager.Instance.IsOwned(member.itemId))
                    return true;
            }
            return false;
        }
        return ShopManager.Instance.IsOwned(_itemData.itemId);
    }

    private bool IsWearing()
    {
        string charId = _itemData.IsMaleRelatedItem ? _itemData.characterId : "player";

        if (_itemData.category == ItemCategory.Set)
        {
            var members = ShopManager.Instance.GetSetMembers(_itemData.setGroupId);
            foreach (var member in members)
            {
                if (!EquipmentManager.Instance.IsEquipped(charId, member.category, member.itemId))
                    return false;
            }
            return members.Count > 0;
        }

        return EquipmentManager.Instance.IsEquipped(charId, _itemData.category, _itemData.itemId);
    }

    // ────────────────────────────────────────────────
    // 클릭 처리
    // ────────────────────────────────────────────────

    private void OnClickCard()
    {
        if (_itemData == null) return;

        bool isOwned = IsOwnedCheck();
        bool isWearing = IsWearing();

        if (isWearing)
        {
            // 착용중 → 착용 해제 확인 팝업
            LobbyManager.Instance.ShowUnequipConfirm(_itemData.itemName, OnConfirmUnequip);
            return;
        }

        if (isOwned)
        {
            // 보유중 → 착용 확인 팝업
            LobbyManager.Instance.ShowEquipConfirm(_itemData.itemName, OnConfirmEquip);
        }
        else
        {
            // 미보유 → 구매 확인 팝업
            Sprite icon = null;
            if (!string.IsNullOrEmpty(_itemData.thumbnailPath))
                icon = Resources.Load<Sprite>(_itemData.thumbnailPath);

            LobbyManager.Instance.ShowPurchaseConfirm(_itemData.itemName, _itemData.price, icon, OnConfirmPurchase);
        }
    }

    private void OnConfirmPurchase()
    {
        ShopManager.Instance.AddToInventory(_itemData.itemId, success =>
        {
            if (success)
                LobbyManager.Instance.ShowPurchaseComplete(); // 구매 완료 팝업
            else
                Debug.LogError($"[ItemCard] 구매 처리 실패: {_itemData.itemName}");
        });
    }

    private void OnConfirmEquip()
    {
        EquipmentManager.Instance.Equip(_itemData, success =>
        {
            if (!success)
                Debug.LogError($"[ItemCard] 착용 처리 실패: {_itemData.itemName}");
        });
    }

    private void OnConfirmUnequip()
    {
        EquipmentManager.Instance.Unequip(_itemData, success =>
        {
            if (!success)
                Debug.LogError($"[ItemCard] 착용 해제 실패: {_itemData.itemName}");
        });
    }
}