using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 프리팹에 붙이는 스크립트.
/// EquipmentManager의 착용 데이터를 받아 레이어별 스프라이트를 교체한다.
/// 여주/남주 모두 동일한 스크립트를 사용하며 characterId로 구분한다.
/// 대사 중이 아닌 캐릭터는 SetHighlighted(false)로 축소 + 회색톤 처리된다.
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

    [Header("강조/비강조 연출 설정")]
    [SerializeField] private float dimmedScale = 0.9f;      // 비강조 시 축소 비율
    [SerializeField] private Color dimmedColor = new Color(0.6f, 0.6f, 0.6f, 1f); // 비강조 시 회색톤
    [SerializeField] private float transitionSpeed = 8f;    // 전환 속도

    private bool _isHighlighted = true;
    private Vector3 _targetScale = Vector3.one;
    private Color _targetColor = Color.white;

    private void Awake()
    {
        // 등장 직후에는 비강조 상태로 시작 (대사 시작 시 강조됨)
        SetHighlighted(false);
        transform.localScale = _targetScale; // 등장 즉시 축소 상태로 시작 (전환 애니메이션 생략)

        if (imgItem != null) imgItem.color = _targetColor;
        if (imgHair != null) imgHair.color = _targetColor;
        if (imgTop != null) imgTop.color = _targetColor;
        if (imgBottom != null) imgBottom.color = _targetColor;
    }

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

    private void Update()
    {
        // 부드러운 전환 (크기/색상 보간)
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * transitionSpeed);

        SetLayerColorLerp(imgItem);
        SetLayerColorLerp(imgHair);
        SetLayerColorLerp(imgTop);
        SetLayerColorLerp(imgBottom);
    }

    private void SetLayerColorLerp(Image layer)
    {
        if (layer == null) return;
        layer.color = Color.Lerp(layer.color, _targetColor, Time.deltaTime * transitionSpeed);
    }

    // ────────────────────────────────────────────────
    // 강조/비강조 전환
    // ────────────────────────────────────────────────

    /// <summary>
    /// 대사 중인 캐릭터는 true(원래 크기/색상), 아닌 캐릭터는 false(축소+회색톤)로 설정.
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        _isHighlighted = highlighted;
        _targetScale = highlighted ? Vector3.one : Vector3.one * dimmedScale;
        _targetColor = highlighted ? Color.white : dimmedColor;
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