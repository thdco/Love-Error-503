using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 프리팹에 붙이는 스크립트.
/// EquipmentManager의 착용 데이터를 받아 레이어별 스프라이트를 교체한다.
/// 여주/남주 모두 동일한 스크립트를 사용하며 characterId로 구분한다.
/// </summary>
public class CharacterView : MonoBehaviour
{
    [Header("캐릭터 ID (player/hajin/dohyun/siwoo)")]
    [SerializeField] public string characterId = "player";

    [Header("레이어 (앞 → 뒤 순서: Item > Hair > Top > Bottom)")]
    [SerializeField] private Image imgItem;
    [SerializeField] private Image imgHair;
    [SerializeField] private Image imgTop;
    [SerializeField] private Image imgBottom;

    private void OnEnable()
    {
        EquipmentManager.Instance.OnEquipmentChanged += RefreshLayers;
        RefreshLayers();
    }

    private void OnDisable()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= RefreshLayers;
    }

    // ────────────────────────────────────────────────
    // 레이어 갱신
    // ────────────────────────────────────────────────

    public void RefreshLayers()
    {
        EquipmentSlot slot = EquipmentManager.Instance.GetSlot(characterId);

        SetLayerSprite(imgHair,   slot.hair);
        SetLayerSprite(imgTop,    slot.top);
        SetLayerSprite(imgBottom, slot.bottom);
        SetLayerSprite(imgItem,   slot.item);
    }

    private void SetLayerSprite(Image layer, string itemId)
    {
        if (layer == null) return;

        if (string.IsNullOrEmpty(itemId))
        {
            layer.enabled = false;
            return;
        }

        ShopItemData item = ShopManager.Instance.GetItem(itemId);
        if (item == null || string.IsNullOrEmpty(item.spritePath))
        {
            layer.enabled = false;
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(item.spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"[CharacterView] 스프라이트 없음: {item.spritePath}");
            layer.enabled = false;
            return;
        }

        layer.sprite = sprite;
        layer.enabled = true;
    }
}
