using TMPro;
using UnityEngine;

/// <summary>
/// 코인/젬 잔액을 화면에 표시하는 UI.
/// WalletManager.OnWalletUpdated 이벤트를 구독해서 자동으로 갱신된다.
/// 메인 화면 HUD나 상점 화면 등 재화가 표시될 곳에 붙여서 사용한다.
/// </summary>
public class WalletUI : MonoBehaviour
{
    [Header("재화 텍스트")]
    [SerializeField] private TMP_Text txtCoin;
    [SerializeField] private TMP_Text txtTicket;

    private void OnEnable()
    {
        // 이벤트 구독 - 재화가 바뀔 때마다 자동 갱신
        WalletManager.Instance.OnWalletUpdated += RefreshUI;
        RefreshUI(); // 활성화될 때 즉시 갱신
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (WalletManager.Instance != null)
            WalletManager.Instance.OnWalletUpdated -= RefreshUI;
    }

    private void RefreshUI()
    {
        if (txtCoin != null)
            txtCoin.text = WalletManager.Instance.Coin.ToString("N0"); // 1,000 형식

        if (txtTicket != null)
            txtTicket.text = WalletManager.Instance.Ticket.ToString("N0");
    }
}
