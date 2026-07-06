using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 데이트 패널 UI
/// 남주 탭 전환(서하진/강도현/민시우)과 데이트 타입(일반/스페셜) 버튼을 처리한다.
/// CoupleSetCard는 ItemCard와 동일하게 미보유/보유/착용중 상태로 나뉜다.
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

    [Header("데이트 타입 버튼")]
    [SerializeField] private Button btnNormalDate;
    [SerializeField] private Button btnSpecialDate;

    [Header("탭 선택/비선택 색상")]
    [SerializeField] private Color colorSelected = Color.white;
    [SerializeField] private Color colorUnselected = new Color(0.8f, 0.8f, 0.8f);

    [Header("남주 캐릭터 이미지")]
    [SerializeField] private Image imgMaleCharacter;

    [Header("남주별 캐릭터 스프라이트")]
    [SerializeField] private Sprite spriteHajin;
    [SerializeField] private Sprite spriteDohyun;
    [SerializeField] private Sprite spriteSiwoo;

    [Header("아이템 그리드")]
    [SerializeField] private Transform content;

    [Header("프리팹")]
    [SerializeField] private GameObject coupleSetCardPrefab;

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
            MaleCharacter.Hajin => spriteHajin,
            MaleCharacter.Dohyun => spriteDohyun,
            MaleCharacter.Siwoo => spriteSiwoo,
            _ => spriteHajin
        };

        // 남주 변경 시 현재 데이트 타입 기준으로 아이템 목록 갱신
        LoadCoupleSetCards();
        Debug.Log($"[Date] 남주 전환: {character}");
    }

    private void RefreshCharacterTabColors()
    {
        imgHajin.color = _currentCharacter == MaleCharacter.Hajin ? colorSelected : colorUnselected;
        imgDohyun.color = _currentCharacter == MaleCharacter.Dohyun ? colorSelected : colorUnselected;
        imgSiwoo.color = _currentCharacter == MaleCharacter.Siwoo ? colorSelected : colorUnselected;
    }

    // ────────────────────────────────────────────────
    // 데이트 타입 버튼
    // 클릭 시 에피소드 진입 (해금 여부 확인 후)
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
        LoadCoupleSetCards();
    }

    // ────────────────────────────────────────────────
    // 커플룩 카드 목록 로드
    // ────────────────────────────────────────────────

    private void LoadCoupleSetCards()
    {
        // 기존 카드 제거
        foreach (Transform child in content)
            Destroy(child.gameObject);

        // TODO: 상점 시스템 연동 후 실제 데이터로 교체
        // 현재는 샘플 카드 2개 생성
        for (int i = 0; i < 2; i++)
        {
            GameObject card = Instantiate(coupleSetCardPrefab, content);
            SetupCoupleSetCard(card, i);
        }
    }

    /// <summary>
    /// 카드 상태 설정 (미보유/보유/착용중)
    /// 상점 시스템 연동 후 실제 인벤토리 데이터 기반으로 교체 예정
    /// </summary>
    private void SetupCoupleSetCard(GameObject card, int index)
    {
        // 미보유/보유/착용중 그룹
        GameObject groupUnowned = card.transform.Find("Group_Bottom/Group_Unowned").gameObject;
        GameObject groupOwned = card.transform.Find("Group_Bottom/Group_Owned").gameObject;
        GameObject groupWearing = card.transform.Find("Group_Bottom/Group_Wearing").gameObject;

        // TODO: 실제 인벤토리 데이터로 상태 판단
        // 임시로 미보유 상태로 표시
        groupUnowned.SetActive(true);
        groupOwned.SetActive(false);
        groupWearing.SetActive(false);

        // 버튼 클릭 이벤트
        Button btn = card.GetComponent<Button>();
        btn.onClick.AddListener(() => OnClickCoupleSetCard(card, groupUnowned.activeSelf, groupOwned.activeSelf));
    }

    private void OnClickCoupleSetCard(GameObject card, bool isUnowned, bool isOwned)
    {
        // 아이템 이름 가져오기
        string itemName = card.transform.Find("Text_SetName").GetComponent<TMP_Text>().text;

        if (isUnowned)
        {
            // 가격 가져오기
            string priceText = card.transform.Find("Group_Bottom/Group_Unowned/Text_Price").GetComponent<TMP_Text>().text;
            int price = int.Parse(priceText.Replace(",", ""));

            LobbyManager.Instance.ShowPurchaseConfirm(itemName, price, null, () =>
            {
                // TODO: 상점 시스템 연동 후 인벤토리 추가 처리
                Debug.Log($"[Date] {itemName} 구매 완료");
            });
        }
        else if (isOwned)
        {
            LobbyManager.Instance.ShowEquipConfirm(itemName, () =>
            {
                // TODO: 상점 시스템 연동 후 착용 처리
                Debug.Log($"[Date] {itemName} 착용 완료");
            });
        }
    }
}