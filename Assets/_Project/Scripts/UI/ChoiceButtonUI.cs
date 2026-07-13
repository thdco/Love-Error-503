using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 에피소드 선택지 버튼. ChoiceBox의 Content 안에 동적으로 생성된다.
/// </summary>
public class ChoiceButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text txtChoice;
    [SerializeField] private Button btnChoice;

    public void Setup(string text, System.Action onClick)
    {
        txtChoice.text = text;
        btnChoice.onClick.RemoveAllListeners();
        btnChoice.onClick.AddListener(() => onClick?.Invoke());
    }
}
