using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 구매 완료 팝업. 구매 확인 후 표시되며 확인 버튼으로 닫힌다.
/// </summary>
public class PurchaseCompletePopup : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button btnConfirm;

    private void Awake()
    {
        btnConfirm.onClick.AddListener(OnClickConfirm);
    }

    private void OnClickConfirm()
    {
        LobbyManager.Instance.ClosePurchaseComplete();
    }
}
