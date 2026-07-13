using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로그인/회원가입/게스트 로그인 성공 직후, 아직 플레이어 이름이 없는 경우에만 노출되는 패널.
/// 이름 저장이 끝나면 AuthManager.LoadMainScene()을 호출해 메인 씬으로 넘어간다.
/// Instance 싱글톤 패턴을 쓰지 않는다 (비활성 오브젝트는 Awake가 씬 로드 시 호출되지 않기 때문).
/// 대신 AuthManager 등 외부에서 이 컴포넌트를 직접 참조(Inspector 연결)해서 Show()를 호출한다.
/// </summary>
public class PlayerNameSetupUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField inputPlayerName;

    [Header("Buttons")]
    [SerializeField] private Button btnConfirm;

    [Header("Error UI")]
    [SerializeField] private TMP_Text errorText;

    private const int MinNameLength = 2;
    private const int MaxNameLength = 12;

    private void Awake()
    {
        btnConfirm.onClick.AddListener(OnClickConfirm);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        ClearError();
        inputPlayerName.text = string.Empty;
    }

    private void OnClickConfirm()
    {
        string playerName = inputPlayerName.text.Trim();

        string validationError = Validate(playerName);
        if (validationError != null)
        {
            ShowError(validationError);
            return;
        }

        SetInteractable(false);
        ClearError();

        AuthManager.Instance.SetPlayerName(playerName, (success, error) =>
        {
            if (success)
            {
                AuthManager.Instance.LoadMainScene();
            }
            else
            {
                SetInteractable(true);
                ShowError(error);
            }
        });
    }

    private string Validate(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return "플레이어 이름을 입력해주세요.";

        if (playerName.Length < MinNameLength || playerName.Length > MaxNameLength)
            return $"이름은 {MinNameLength}~{MaxNameLength}자로 입력해주세요.";

        return null;
    }

    private void SetInteractable(bool value)
    {
        btnConfirm.interactable = value;
        inputPlayerName.interactable = value;
    }

    private void ShowError(string message)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }

    private void ClearError()
    {
        errorText.text = string.Empty;
        errorText.gameObject.SetActive(false);
    }
}