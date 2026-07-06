using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 구매 확인 팝업
/// 미보유 아이템 클릭 시 표시. 확인 시 코인 차감 후 인벤토리에 추가.
/// </summary>
public class PurchaseConfirmPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text txtItemName;
    [SerializeField] private TMP_Text txtPrice;
    [SerializeField] private Image imgItemIcon;
    [SerializeField] private Image imgCurrencyIcon;

    [Header("버튼")]
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    private int _price;
    private Action _onConfirm;

    private void Awake()
    {
        btnConfirm.onClick.AddListener(OnClickConfirm);
        btnCancel.onClick.AddListener(OnClickCancel);
    }

    /// <summary>
    /// 팝업 열기. 아이템 정보와 확인 시 실행할 콜백을 받는다.
    /// </summary>
    public void Show(string itemName, int price, Sprite itemIcon, Action onConfirm)
    {
        txtItemName.text = itemName;
        txtPrice.text = price.ToString("N0");
        if (itemIcon != null) imgItemIcon.sprite = itemIcon;
        _price = price;
        _onConfirm = onConfirm;
        gameObject.SetActive(true);
    }

    private void OnClickConfirm()
    {
        // 코인 잔액 확인
        if (WalletManager.Instance.Coin < _price)
        {
            // 코인 부족 → 구매 확인 팝업 닫고 재화 부족 팝업 표시
            LobbyManager.Instance.ClosePurchaseConfirm();
            LobbyManager.Instance.ShowInsufficientFunds();
            return;
        }

        // 코인 차감
        WalletManager.Instance.SpendCoin(_price, (success, error) =>
        {
            if (success)
            {
                _onConfirm?.Invoke();
                LobbyManager.Instance.ClosePurchaseConfirm();
            }
            else
            {
                Debug.LogWarning($"[PurchaseConfirm] 구매 실패: {error}");
            }
        });
    }

    private void OnClickCancel()
    {
        LobbyManager.Instance.ClosePurchaseConfirm();
    }
}
