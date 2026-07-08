using UnityEngine;

/// <summary>
/// 로비 씬의 패널 전환을 담당하는 매니저 (싱글톤)
/// MainPanel, ClosetPanel, DatePanel 간의 전환을 처리한다.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject closetPanel;
    [SerializeField] private GameObject datePanel;

    [Header("MainPanel Groups")]
    [SerializeField] private GameObject bottomMenuGroup;

    [Header("HUD")]
    [SerializeField] private GameObject btnBack;

    [Header("Popups")]
    [SerializeField] private GameObject popups;
    [SerializeField] private GameObject purchaseConfirmPopup;
    [SerializeField] private GameObject purchaseCompletePopup;  // 구매 완료
    [SerializeField] private GameObject equipConfirmPopup;
    [SerializeField] private GameObject unequipConfirmPopup;    // 착용 해제
    [SerializeField] private GameObject insufficientFundsPopup;
    [SerializeField] private GameObject attendancePopup;
    [SerializeField] private GameObject questPopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ShowMain();
    }

    // ────────────────────────────────────────────────
    // 패널 전환
    // ────────────────────────────────────────────────

    public void ShowMain()
    {
        mainPanel.SetActive(true);
        closetPanel.SetActive(false);
        datePanel.SetActive(false);
        CloseAllPopups();
        SetBackButton(false);
    }

    public void ShowCloset()
    {
        closetPanel.SetActive(true);
        datePanel.SetActive(false);
        CloseAllPopups();
        SetBackButton(true);
    }

    public void CloseCloset()
    {
        closetPanel.SetActive(false);
        SetBackButton(false);
    }

    public void ShowDate()
    {
        datePanel.SetActive(true);
        closetPanel.SetActive(false);
        CloseAllPopups();
        SetBackButton(true);
    }

    public void CloseDate()
    {
        datePanel.SetActive(false);
        SetBackButton(false);
    }

    // ────────────────────────────────────────────────
    // HUD 이전 버튼
    // ────────────────────────────────────────────────

    public void OnClickBack()
    {
        if (closetPanel.activeSelf)
            CloseCloset();
        else if (datePanel.activeSelf)
            CloseDate();
    }

    private void SetBackButton(bool isActive)
    {
        if (btnBack != null)
            btnBack.SetActive(isActive);
    }

    // ────────────────────────────────────────────────
    // 팝업
    // Popups 부모(어두운 배경 포함)를 먼저 켜고, 그 안의 개별 팝업을 연다.
    // ────────────────────────────────────────────────

    public void ShowPurchaseConfirm(string itemName, int price, Sprite itemIcon, System.Action onConfirm)
    {
        popups.SetActive(true);
        purchaseConfirmPopup.SetActive(true);
        purchaseConfirmPopup.GetComponent<PurchaseConfirmPopup>().Show(itemName, price, itemIcon, onConfirm);
    }

    public void ClosePurchaseConfirm()
    {
        purchaseConfirmPopup.SetActive(false);
        popups.SetActive(false);
    }

    public void ShowPurchaseComplete()
    {
        popups.SetActive(true);
        purchaseCompletePopup.SetActive(true);
    }

    public void ClosePurchaseComplete()
    {
        purchaseCompletePopup.SetActive(false);
        popups.SetActive(false);
    }

    public void ShowEquipConfirm(string itemName, System.Action onConfirm)
    {
        popups.SetActive(true);
        equipConfirmPopup.SetActive(true);
        equipConfirmPopup.GetComponent<EquipConfirmPopup>().Show(itemName, onConfirm);
    }

    public void CloseEquipConfirm()
    {
        equipConfirmPopup.SetActive(false);
        popups.SetActive(false);
    }

    public void ShowUnequipConfirm(string itemName, System.Action onConfirm)
    {
        popups.SetActive(true);
        unequipConfirmPopup.SetActive(true);
        unequipConfirmPopup.GetComponent<UnequipConfirmPopup>().Show(itemName, onConfirm);
    }

    public void CloseUnequipConfirm()
    {
        unequipConfirmPopup.SetActive(false);
        popups.SetActive(false);
    }

    public void ShowInsufficientFunds(CurrencyType currencyType = CurrencyType.Coin)
    {
        popups.SetActive(true);
        insufficientFundsPopup.SetActive(true);
        insufficientFundsPopup.GetComponent<InsufficientFundsPopup>().Show(currencyType);
    }

    public void CloseInsufficientFunds()
    {
        insufficientFundsPopup.SetActive(false);
        popups.SetActive(false);
    }

    public void ShowAttendance()
    {
        popups.SetActive(true);
        attendancePopup.SetActive(true);
    }

    public void CloseAttendance()
    {
        attendancePopup.SetActive(false);
        popups.SetActive(false);
    }

    public void ShowQuest()
    {
        popups.SetActive(true);
        questPopup.SetActive(true);
    }

    public void CloseQuest()
    {
        questPopup.SetActive(false);
        popups.SetActive(false);
    }

    public void CloseAllPopups()
    {
        purchaseConfirmPopup.SetActive(false);
        purchaseCompletePopup.SetActive(false);
        equipConfirmPopup.SetActive(false);
        unequipConfirmPopup.SetActive(false);
        insufficientFundsPopup.SetActive(false);
        attendancePopup.SetActive(false);
        questPopup.SetActive(false);
        popups.SetActive(false);
    }
}