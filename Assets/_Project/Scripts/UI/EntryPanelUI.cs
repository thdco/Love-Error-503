using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 첫 진입 화면. "게스트로 시작하기 / 로그인 / 회원가입" 3개 버튼을 가진다.
/// 다른 패널(Login, Signup)의 첫 화면 역할도 겸한다.
/// </summary>
public class EntryPanelUI : MonoBehaviour
{
    public static EntryPanelUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject splashPanel;
    [SerializeField] private GameObject entryPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject signupPanel;

    [Header("Buttons")]
    [SerializeField] private Button btnGuestLogin;
    [SerializeField] private Button btnGoToLogin;
    [SerializeField] private Button btnGoToSignup;

    [Header("Error UI")]
    [SerializeField] private TMP_Text errorText;

    private void Awake()
    {
        Instance = this;

        // AuthManager.Start()보다 먼저 버튼 리스너를 등록해두기 위해 Awake에서 처리
        btnGuestLogin.onClick.AddListener(OnClickGuestLogin);
        btnGoToLogin.onClick.AddListener(() => SwitchPanel(loginPanel));
        btnGoToSignup.onClick.AddListener(() => SwitchPanel(signupPanel));

        splashPanel.SetActive(true);
        entryPanel.SetActive(false);
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
    }

    private void Start()
    {
        // 초기 패널 세팅은 Awake로 이동, Start는 비워둠
    }

    /// <summary>AuthManager가 자동 로그인에 실패했을 때 호출한다.</summary>
    public void ShowEntryPanel()
    {
        splashPanel.SetActive(false);
        SwitchPanel(entryPanel);
    }

    public void ShowError(string message)
    {
        if (errorText == null) return;
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }

    private void OnClickGuestLogin()
    {
        SetButtonsInteractable(false);
        ClearError();

        AuthManager.Instance.GuestLogin((success, error) =>
        {
            SetButtonsInteractable(true);
            if (success)
                entryPanel.SetActive(false);
            else
                ShowError(error);
        });
    }

    private void SwitchPanel(GameObject target)
    {
        entryPanel.SetActive(target == entryPanel);
        loginPanel.SetActive(target == loginPanel);
        signupPanel.SetActive(target == signupPanel);
        ClearError();
    }

    private void SetButtonsInteractable(bool value)
    {
        btnGuestLogin.interactable = value;
        btnGoToLogin.interactable = value;
        btnGoToSignup.interactable = value;
    }

    private void ClearError()
    {
        if (errorText == null) return;
        errorText.text = string.Empty;
        errorText.gameObject.SetActive(false);
    }

    public void HideSplash()
    {
        splashPanel.SetActive(false);
    }
}