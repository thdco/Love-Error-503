using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이메일/비밀번호 로그인 패널.
/// </summary>
public class LoginPanelUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField inputEmail;
    [SerializeField] private TMP_InputField inputPassword;

    [Header("Buttons")]
    [SerializeField] private Button btnLogin;
    [SerializeField] private Button btnBack;

    [Header("Error UI")]
    [SerializeField] private TMP_Text errorText;

    [Header("Panel 참조 (뒤로가기용)")]
    [SerializeField] private GameObject entryPanel;

    private void Awake()
    {
        btnLogin.onClick.AddListener(OnClickLogin);
        btnBack.onClick.AddListener(OnClickBack);
    }

    private void Start() { }

    private void OnClickLogin()
    {
        string email = inputEmail.text.Trim();
        string password = inputPassword.text;

        string validationError = Validate(email, password);
        if (validationError != null)
        {
            ShowError(validationError);
            return;
        }

        SetInteractable(false);
        ClearError();

        AuthManager.Instance.Login(email, password, (success, error) =>
        {
            SetInteractable(true);
            if (success)
                gameObject.SetActive(false);
            else
                ShowError(error);
        });
    }

    private string Validate(string email, string password)
    {
        if (string.IsNullOrEmpty(email))
            return "이메일을 입력해주세요.";

        if (!email.Contains("@"))
            return "올바른 이메일 형식이 아닙니다.";

        if (string.IsNullOrEmpty(password))
            return "비밀번호를 입력해주세요.";

        return null;
    }

    private void OnClickBack()
    {
        gameObject.SetActive(false);
        entryPanel.SetActive(true);
        ClearError();
    }

    private void SetInteractable(bool value)
    {
        btnLogin.interactable = value;
        btnBack.interactable = value;
        inputEmail.interactable = value;
        inputPassword.interactable = value;
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