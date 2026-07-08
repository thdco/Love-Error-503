using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 데이트 패널 UI
/// 남주 탭 전환(서하진/강도현/민시우)과 데이트 타입(일반/스페셜) 버튼을 처리한다.
/// 커플룩 카드는 남주별로 캐싱해서 탭 재전환 시 재로드하지 않는다.
/// </summary>
public class DatePanelUI : MonoBehaviour
{
    [Header("남주 탭 버튼")]
    [SerializeField] private Button btnHajin;
    [SerializeField] private Button btnDohyun;
    [SerializeField] private Button btnSiwoo;

    [Header("남주 탭 버튼 배경 이미지 (선택 상태 표시용)")]
    [SerializeField] private Image imgHajin;
    [SerializeField] private Image imgDohyun;
    [SerializeField] private Image imgSiwoo;

    [Header("탭 선택/비선택 색상")]
    [SerializeField] private Color colorSelected = Color.white;
    [SerializeField] private Color colorUnselected = new Color(0.8f, 0.8f, 0.8f);

    [Header("데이트 타입 버튼")]
    [SerializeField] private Button btnNormalDate;
    [SerializeField] private Button btnSpecialDate;

    [Header("남주 캐릭터 이미지")]
    [SerializeField] private Image imgMaleCharacter;

    [Header("남주별 캐릭터 스프라이트")]
    [SerializeField] private Sprite spriteHajin;
    [SerializeField] private Sprite spriteDohyun;
    [SerializeField] private Sprite spriteSiwoo;

    [Header("아이템 그리드")]
    [SerializeField] private Transform content;

    [Header("프리팹")]
    [SerializeField] private ItemCardUI coupleSetCardPrefab;

    private enum MaleCharacter { Hajin, Dohyun, Siwoo }
    private enum DateType { Normal, Special }

    private MaleCharacter _currentCharacter = MaleCharacter.Hajin;
    private DateType _currentDateType = DateType.Normal;

    private void Awake()
    {
        btnHajin.onClick.AddListener(() => SwitchCharacter(MaleCharacter.Hajin));
        btnDohyun.onClick.AddListener(() => SwitchCharacter(MaleCharacter.Dohyun));
        btnSiwoo.onClick.AddListener(() => SwitchCharacter(MaleCharacter.Siwoo));

        btnNormalDate.onClick.AddListener(() => OnClickDateType(DateType.Normal));
        btnSpecialDate.onClick.AddListener(() => OnClickDateType(DateType.Special));
    }

    private void OnEnable()
    {
        SwitchCharacter(MaleCharacter.Hajin);
        SwitchDateType(DateType.Normal);
    }

    // ────────────────────────────────────────────────
    // 남주 탭 전환
    // ────────────────────────────────────────────────

    private void SwitchCharacter(MaleCharacter character)
    {
        _currentCharacter = character;
        RefreshCharacterTabColors();

        imgMaleCharacter.sprite = character switch
        {
            MaleCharacter.Hajin  => spriteHajin,
            MaleCharacter.Dohyun => spriteDohyun,
            MaleCharacter.Siwoo  => spriteSiwoo,
            _ => spriteHajin
        };

        LoadCoupleSetCards(); // 항상 새로 로드
    }

    private void RefreshCharacterTabColors()
    {
        imgHajin.color  = _currentCharacter == MaleCharacter.Hajin  ? colorSelected : colorUnselected;
        imgDohyun.color = _currentCharacter == MaleCharacter.Dohyun ? colorSelected : colorUnselected;
        imgSiwoo.color  = _currentCharacter == MaleCharacter.Siwoo  ? colorSelected : colorUnselected;
    }

    // ────────────────────────────────────────────────
    // 데이트 타입 버튼 - 클릭 시 에피소드 진입
    // ────────────────────────────────────────────────

    private void OnClickDateType(DateType dateType)
    {
        SwitchDateType(dateType);

        // TODO: 에피소드 시스템 연동 후 해금 여부 확인
        // 해금됐으면 → 에피소드 씬으로 전환
        // 미해금이면 → 티켓 차감 후 진입 or 잠금 안내 팝업
        Debug.Log($"[Date] 데이트 타입 클릭: {dateType}, 남주: {_currentCharacter}");
    }

    private void SwitchDateType(DateType dateType)
    {
        _currentDateType = dateType;
    }

    // ────────────────────────────────────────────────
    // 커플룩 카드 목록 로드
    // ────────────────────────────────────────────────

    private void LoadCoupleSetCards()
    {
        // 기존 카드 제거
        foreach (Transform child in content)
            Destroy(child.gameObject);

        string charId = _currentCharacter switch
        {
            MaleCharacter.Hajin  => "hajin",
            MaleCharacter.Dohyun => "dohyun",
            MaleCharacter.Siwoo  => "siwoo",
            _ => "hajin"
        };

        // ItemType.Couple + Set 카테고리 + 남주 characterId로 필터링
        var items = ShopManager.Instance.GetItemsByTypeAndCategory(ItemType.Couple, ItemCategory.Set, charId);

        foreach (var item in items)
        {
            ItemCardUI card = Instantiate(coupleSetCardPrefab, content);
            card.Setup(item);
        }
    }
}