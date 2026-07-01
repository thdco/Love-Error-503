using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이메일/비밀번호/닉네임 회원가입 패널.
/// </summary>
public class SignupPanelUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField inputEmail;
    [SerializeField] private TMP_InputField inputPassword;
    [SerializeField] private TMP_InputField inputPasswordConfirm;

    [Header("Buttons")]
    [SerializeField] private Button btnSignup;
    [SerializeField] private Button btnBack;

    [Header("Error UI")]
    [SerializeField] private TMP_Text errorText;

    [Header("Panel 참조 (뒤로가기용)")]
    [SerializeField] private GameObject entryPanel;

    private const int MinPasswordLength = 8;

    private void Awake()
    {
        btnSignup.onClick.AddListener(OnClickSignup);
        btnBack.onClick.AddListener(OnClickBack);
    }

    private void Start() { }

    private void OnClickSignup()
    {
        string email = inputEmail.text.Trim();
        string password = inputPassword.text;
        string passwordConfirm = inputPasswordConfirm.text;

        string validationError = Validate(email, password, passwordConfirm);
        if (validationError != null)
        {
            ShowError(validationError);
            return;
        }

        SetInteractable(false);
        ClearError();

        AuthManager.Instance.SignUp(email, password, (success, error) =>
        {
            SetInteractable(true);
            if (success)
                gameObject.SetActive(false);
            else
                ShowError(error);
        });
    }

    private string Validate(string email, string password, string passwordConfirm)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            return "올바른 이메일 형식이 아닙니다.";

        if (password.Length < MinPasswordLength)
            return $"비밀번호는 {MinPasswordLength}자 이상이어야 합니다.";

        if (password != passwordConfirm)
            return "비밀번호가 일치하지 않습니다.";

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
        btnSignup.interactable = value;
        btnBack.interactable = value;
        inputEmail.interactable = value;
        inputPassword.interactable = value;
        inputPasswordConfirm.interactable = value;
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