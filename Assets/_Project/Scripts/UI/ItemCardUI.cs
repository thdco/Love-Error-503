using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옷장/데이트 패널의 아이템 카드 (ItemCard, CoupleSetCard 공용).
/// 미보유/보유/착용중 상태에 따라 하단 그룹을 다르게 표시하고,
/// 클릭 시 구매 확인 또는 착용 확인 팝업을 연다.
/// </summary>
public class ItemCardUI : MonoBehaviour
{
    [Header("공통")]
    [SerializeField] private Image imgThumbnail;
    [SerializeField] private TMP_Text txtItemName;
    [SerializeField] private Button btnCard;

    [Header("매력도 (ItemCard 전용, CoupleSetCard는 비워둬도 됨)")]
    [SerializeField] private TMP_Text txtCharmAmount;
    [SerializeField] private GameObject imgCharmIcon;

    [Header("하단 상태 그룹")]
    [SerializeField] private GameObject groupUnowned;
    [SerializeField] private TMP_Text txtPrice; // Group_Unowned 안

    [SerializeField] private GameObject groupOwned;
    [SerializeField] private GameObject groupWearing;

    private ShopItemData _itemData;

    private void Awake()
    {
        btnCard.onClick.AddListener(OnClickCard);
    }

    /// <summary>카드 데이터 설정 및 상태(미보유/보유/착용중) 갱신.</summary>
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
                // 남주 아이템이면 호감도, 여주 아이템이면 매력도
                string label = itemData.IsMaleRelatedItem ? "호감도" : "매력도";
                txtCharmAmount.text = $"+{itemData.charmAmount} {label}";
            }
        }

        RefreshState();
    }

    /// <summary>보유/착용 상태가 바뀔 때마다 호출해서 하단 그룹을 갱신.</summary>
    public void RefreshState()
    {
        bool isOwned = ShopManager.Instance.IsOwned(_itemData.itemId);
        bool isWearing = IsWearing();

        groupUnowned.SetActive(!isOwned);
        groupOwned.SetActive(isOwned && !isWearing);
        groupWearing.SetActive(isWearing);

        if (!isOwned && txtPrice != null)
            txtPrice.text = _itemData.price.ToString("N0");
    }

    /// <summary>
    /// 현재 착용 중인지 확인.
    /// Set은 4개 슬롯이 전부 이 세트의 아이템과 일치할 때 착용중으로 간주.
    /// </summary>
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
        bool isOwned = ShopManager.Instance.IsOwned(_itemData.itemId);
        bool isWearing = IsWearing();

        if (isWearing)
            return; // 이미 착용 중이면 반응 없음

        if (isOwned)
        {
            // 보유 중 → 착용 확인 팝업
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
                RefreshState();
            else
                Debug.LogError($"[ItemCard] 구매 처리 실패: {_itemData.itemName}");
        });
    }

    private void OnConfirmEquip()
    {
        EquipmentManager.Instance.Equip(_itemData, success =>
        {
            if (success)
                RefreshState();
            else
                Debug.LogError($"[ItemCard] 착용 처리 실패: {_itemData.itemName}");
        });
    }
}