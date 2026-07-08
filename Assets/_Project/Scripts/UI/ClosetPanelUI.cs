using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옷장 패널 UI
/// 카테고리 탭별로 Content를 분리해서 활성/비활성으로 전환한다.
/// 그의 옷장 탭은 남주 선택 탭(서하진/강도현/민시우)을 추가로 표시한다.
/// </summary>
public class ClosetPanelUI : MonoBehaviour
{
    [Header("카테고리 탭 버튼")]
    [SerializeField] private Button btnMyCloset;
    [SerializeField] private Button btnCoupleLook;
    [SerializeField] private Button btnHisCloset;

    [Header("카테고리 탭 배경 이미지")]
    [SerializeField] private Image imgMyCloset;
    [SerializeField] private Image imgCoupleLook;
    [SerializeField] private Image imgHisCloset;

    [Header("그의 옷장 - 남주 선택 탭 버튼 (기본 비활성)")]
    [SerializeField] private GameObject goHajin;    // Btn_Hajin 오브젝트
    [SerializeField] private GameObject goDohyun;   // Btn_Dohyun 오브젝트
    [SerializeField] private GameObject goSiwoo;    // Btn_Siwoo 오브젝트
    [SerializeField] private Button btnHajin;
    [SerializeField] private Button btnDohyun;
    [SerializeField] private Button btnSiwoo;
    [SerializeField] private Image imgHajin;
    [SerializeField] private Image imgDohyun;
    [SerializeField] private Image imgSiwoo;

    [Header("카테고리 탭 선택/비선택 색상")]
    [SerializeField] private Color colorSelected = Color.white;
    [SerializeField] private Color colorUnselected = new Color(0.8f, 0.8f, 0.8f);

    [Header("남주 탭 선택/비선택 색상")]
    [SerializeField] private Color hisColorSelected = Color.white;
    [SerializeField] private Color hisColorUnselected = new Color(0.8f, 0.8f, 0.8f);

    [Header("탭별 Content")]
    [SerializeField] private Transform contentMyCloset;
    [SerializeField] private Transform contentCoupleLook;
    [SerializeField] private Transform contentHisCloset;

    [Header("프리팹")]
    [SerializeField] private ItemCardUI itemCardPrefab;
    [SerializeField] private ItemCardUI itemCardCouplePrefab;

    private enum ClosetTab { MyCloset, CoupleLook, HisCloset }
    private enum HisCharacter { Hajin, Dohyun, Siwoo }

    private ClosetTab _currentTab = ClosetTab.MyCloset;
    private HisCharacter _currentCharacter = HisCharacter.Hajin;

    // 탭별 로드 완료 여부
    private bool _myClosetLoaded;
    private bool _coupleLookLoaded;

    // 그의 옷장은 남주별로 로드 여부 관리
    private bool _hajinLoaded;
    private bool _dohyunLoaded;
    private bool _siwooLoaded;

    private void Awake()
    {
        btnMyCloset.onClick.AddListener(() => SwitchTab(ClosetTab.MyCloset));
        btnCoupleLook.onClick.AddListener(() => SwitchTab(ClosetTab.CoupleLook));
        btnHisCloset.onClick.AddListener(() => SwitchTab(ClosetTab.HisCloset));

        btnHajin.onClick.AddListener(() => SwitchHisCharacter(HisCharacter.Hajin));
        btnDohyun.onClick.AddListener(() => SwitchHisCharacter(HisCharacter.Dohyun));
        btnSiwoo.onClick.AddListener(() => SwitchHisCharacter(HisCharacter.Siwoo));
    }

    private void OnEnable()
    {
        ResetLoadFlags();
        SwitchTab(ClosetTab.MyCloset);
    }

    private void ResetLoadFlags()
    {
        _myClosetLoaded = false;
        _coupleLookLoaded = false;
        _hajinLoaded = false;
        _dohyunLoaded = false;
        _siwooLoaded = false;
    }

    // ────────────────────────────────────────────────
    // 카테고리 탭 전환
    // ────────────────────────────────────────────────

    private void SwitchTab(ClosetTab tab)
    {
        _currentTab = tab;
        RefreshCategoryTabColors();

        // Content 활성/비활성
        contentMyCloset.gameObject.SetActive(tab == ClosetTab.MyCloset);
        contentCoupleLook.gameObject.SetActive(tab == ClosetTab.CoupleLook);
        contentHisCloset.gameObject.SetActive(tab == ClosetTab.HisCloset);

        // 그의 옷장 탭일 때만 남주 선택 버튼 표시
        bool showHisTabs = tab == ClosetTab.HisCloset;
        goHajin.SetActive(showHisTabs);
        goDohyun.SetActive(showHisTabs);
        goSiwoo.SetActive(showHisTabs);

        switch (tab)
        {
            case ClosetTab.MyCloset:
                if (!_myClosetLoaded) LoadMyCloset();
                break;
            case ClosetTab.CoupleLook:
                if (!_coupleLookLoaded) LoadCoupleLook();
                break;
            case ClosetTab.HisCloset:
                // 기본으로 첫 번째 남주(서하진) 선택
                SwitchHisCharacter(_currentCharacter);
                break;
        }
    }

    private void RefreshCategoryTabColors()
    {
        imgMyCloset.color   = _currentTab == ClosetTab.MyCloset   ? colorSelected : colorUnselected;
        imgCoupleLook.color = _currentTab == ClosetTab.CoupleLook ? colorSelected : colorUnselected;
        imgHisCloset.color  = _currentTab == ClosetTab.HisCloset  ? colorSelected : colorUnselected;
    }

    // ────────────────────────────────────────────────
    // 그의 옷장 - 남주 탭 전환
    // ────────────────────────────────────────────────

    private void SwitchHisCharacter(HisCharacter character)
    {
        _currentCharacter = character;
        RefreshHisCharacterTabColors();

        // 남주 변경 시 Content_HisCloset 내용 갱신
        bool loaded = character switch
        {
            HisCharacter.Hajin  => _hajinLoaded,
            HisCharacter.Dohyun => _dohyunLoaded,
            HisCharacter.Siwoo  => _siwooLoaded,
            _ => false
        };

        if (!loaded) LoadHisCloset(character);
    }

    private void RefreshHisCharacterTabColors()
    {
        imgHajin.color  = _currentCharacter == HisCharacter.Hajin  ? hisColorSelected : hisColorUnselected;
        imgDohyun.color = _currentCharacter == HisCharacter.Dohyun ? hisColorSelected : hisColorUnselected;
        imgSiwoo.color  = _currentCharacter == HisCharacter.Siwoo  ? hisColorSelected : hisColorUnselected;
    }

    // ────────────────────────────────────────────────
    // 아이템 목록 로드
    // ────────────────────────────────────────────────

    private void LoadMyCloset()
    {
        var categories = new[]
        {
            ItemCategory.Set,
            ItemCategory.Item,
            ItemCategory.Hair,
            ItemCategory.Top,
            ItemCategory.Bottom
        };

        foreach (var category in categories)
        {
            var items = ShopManager.Instance.GetItemsByTypeAndCategory(ItemType.Player, category);
            foreach (var item in items)
            {
                ItemCardUI card = Instantiate(itemCardPrefab, contentMyCloset);
                card.Setup(item);
            }
        }

        _myClosetLoaded = true;
    }

    private void LoadCoupleLook()
    {
        var items = ShopManager.Instance.GetItemsByTypeAndCategory(ItemType.Couple, ItemCategory.Set);
        foreach (var item in items)
        {
            ItemCardUI card = Instantiate(itemCardCouplePrefab, contentCoupleLook);
            card.Setup(item);
        }

        _coupleLookLoaded = true;
    }

    private void LoadHisCloset(HisCharacter character)
    {
        // 기존 카드 제거 후 해당 남주 아이템 로드
        foreach (Transform child in contentHisCloset)
            Destroy(child.gameObject);

        string charId = character switch
        {
            HisCharacter.Hajin  => "hajin",
            HisCharacter.Dohyun => "dohyun",
            HisCharacter.Siwoo  => "siwoo",
            _ => "hajin"
        };

        var categories = new[]
        {
            ItemCategory.Set,
            ItemCategory.Item,
            ItemCategory.Hair,
            ItemCategory.Top,
            ItemCategory.Bottom
        };

        foreach (var category in categories)
        {
            var items = ShopManager.Instance.GetItemsByTypeAndCategory(ItemType.Male, category, charId);
            foreach (var item in items)
            {
                ItemCardUI card = Instantiate(itemCardPrefab, contentHisCloset);
                card.Setup(item);
            }
        }

        switch (character)
        {
            case HisCharacter.Hajin:  _hajinLoaded  = true; break;
            case HisCharacter.Dohyun: _dohyunLoaded = true; break;
            case HisCharacter.Siwoo:  _siwooLoaded  = true; break;
        }
    }

}