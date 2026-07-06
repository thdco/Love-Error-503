using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 재화 부족 팝업
/// 코인 또는 티켓이 부족할 때 표시.
/// </summary>
public class InsufficientFundsPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text txtMessage;

    [Header("버튼")]
    [SerializeField] private Button btnClose;

    private void Awake()
    {
        btnClose.onClick.AddListener(OnClickClose);
    }

    /// <summary>
    /// 팝업 열기. 부족한 재화 종류에 따라 메시지를 다르게 표시.
    /// </summary>
    public void Show(CurrencyType currencyType)
    {
        txtMessage.text = currencyType switch
        {
            CurrencyType.Coin => "코인이 부족합니다.",
            CurrencyType.Ticket => "티켓이 부족합니다.",
            _ => "재화가 부족합니다."
        };
        gameObject.SetActive(true);
    }

    private void OnClickClose()
    {
        LobbyManager.Instance.CloseInsufficientFunds();
    }
}

public enum CurrencyType
{
    Coin,
    Ticket
}
