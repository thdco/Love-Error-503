using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 착용 해제 확인 팝업. 착용중인 아이템 클릭 시 표시.
/// </summary>
public class UnequipConfirmPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text txtMessage;

    [Header("버튼")]
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    private Action _onConfirm;

    private void Awake()
    {
        btnConfirm.onClick.AddListener(OnClickConfirm);
        btnCancel.onClick.AddListener(OnClickCancel);
    }

    public void Show(string itemName, Action onConfirm)
    {
        txtMessage.text = $"{itemName} 착용을 해제하시겠습니까?";
        _onConfirm = onConfirm;
        gameObject.SetActive(true);
    }

    private void OnClickConfirm()
    {
        _onConfirm?.Invoke();
        LobbyManager.Instance.CloseUnequipConfirm();
    }

    private void OnClickCancel()
    {
        LobbyManager.Instance.CloseUnequipConfirm();
    }
}
