using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 착용 확인 팝업
/// 보유 중인 아이템 클릭 시 표시. "[아이템 이름]을 착용하시겠습니까?"
/// </summary>
public class EquipConfirmPopup : MonoBehaviour
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

    /// <summary>
    /// 팝업 열기. 아이템 이름과 확인 시 실행할 콜백을 받는다.
    /// </summary>
    public void Show(string itemName, Action onConfirm)
    {
        txtMessage.text = $"{itemName}을(를) 착용하시겠습니까?";
        _onConfirm = onConfirm;
        gameObject.SetActive(true);
    }

    private void OnClickConfirm()
    {
        _onConfirm?.Invoke();
        LobbyManager.Instance.CloseEquipConfirm();
    }

    private void OnClickCancel()
    {
        LobbyManager.Instance.CloseEquipConfirm();
    }
}
