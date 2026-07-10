using System;
using BackEnd;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 뒤끝(BackEnd) SDK를 통한 모든 인증 로직(초기화, 게스트 로그인, 자동 로그인,
/// 회원가입, 이메일 로그인, 계정 연동)을 전담하는 싱글톤.
/// UI 스크립트는 이 클래스의 public 메서드만 호출하고, 콜백으로 성공/실패와
/// 에러 메시지를 돌려받는다.
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    [Header("로그인 성공 후 전환할 씬 이름")]
    [SerializeField] private string mainSceneName = "MainMenu";

    // 게스트 계정 식별을 위해 기기에 저장해두는 키 (자동 로그인용)
    private const string GuestUuidKey = "BACKEND_GUEST_UUID";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeBackend();
    }

    // ────────────────────────────────────────────────
    // 0. SDK 초기화
    // ────────────────────────────────────────────────
    private void InitializeBackend()
    {
        BackendReturnObject bro = Backend.Initialize(); // 세션 자동 갱신은 기본값 사용

        if (!bro.IsSuccess())
        {
            Debug.LogError($"[Auth] 뒤끝 초기화 실패: {bro}");
            EntryPanelUI.Instance?.ShowError("서버 연결에 실패했습니다. 네트워크를 확인해주세요.");
            return;
        }

        Debug.Log("[Auth] 뒤끝 초기화 성공");
        TryAutoLogin();
    }

    // ────────────────────────────────────────────────
    // 1. 자동 로그인 (이전에 게스트 로그인한 기록이 있으면 시도)
    // ────────────────────────────────────────────────
    private void TryAutoLogin()
    {
        if (!PlayerPrefs.HasKey(GuestUuidKey))
        {
            // 최초 실행 → EntryPanel에서 유저가 선택하도록 둔다
            EntryPanelUI.Instance?.ShowEntryPanel();
            return;
        }

        // 저장된 게스트 UUID로 자동 로그인 시도
        BackendReturnObject bro = Backend.BMember.GuestLogin();

        if (bro.IsSuccess())
        {
            Debug.Log("[Auth] 자동 로그인 성공");
            OnLoginSuccess();
        }
        else
        {
            Debug.LogWarning($"[Auth] 자동 로그인 실패: {bro}");
            EntryPanelUI.Instance?.ShowEntryPanel();
        }
    }

    // ────────────────────────────────────────────────
    // 2. 게스트 로그인 (신규 계정 즉시 발급)
    // ────────────────────────────────────────────────
    public void GuestLogin(Action<bool, string> onComplete)
    {
        // 기기에 저장된 토큰이 있으면 토큰으로 먼저 로그인 시도
        BackendReturnObject bro = Backend.BMember.LoginWithTheBackendToken();

        if (!bro.IsSuccess())
        {
            // 토큰 없거나 만료 → 새 게스트 계정 생성
            bro = Backend.BMember.GuestLogin();
        }

        if (bro.IsSuccess())
        {
            Debug.Log("[Auth] 게스트 로그인 성공");
            OnLoginSuccess();
            onComplete?.Invoke(true, null);
        }
        else
        {
            string msg = ParseError(bro);
            Debug.LogWarning($"[Auth] 게스트 로그인 실패: {bro}");
            onComplete?.Invoke(false, msg);
        }
    }

    // ────────────────────────────────────────────────
    // 3. 일반 회원가입 (이메일 + 비밀번호)
    // ────────────────────────────────────────────────
    public void SignUp(string id, string password, Action<bool, string> onComplete)
    {
        BackendReturnObject bro = Backend.BMember.CustomSignUp(id, password);

        if (!bro.IsSuccess())
        {
            onComplete?.Invoke(false, ParseError(bro));
            return;
        }

        Debug.Log("[Auth] 회원가입 성공, 이어서 로그인 진행");

        // 가입 직후 바로 로그인
        BackendReturnObject loginBro = Backend.BMember.CustomLogin(id, password);
        if (!loginBro.IsSuccess())
        {
            onComplete?.Invoke(false, ParseError(loginBro));
            return;
        }

        OnLoginSuccess();
        onComplete?.Invoke(true, null);
    }

    // ────────────────────────────────────────────────
    // 4. 이메일 로그인
    // ────────────────────────────────────────────────
    public void Login(string id, string password, Action<bool, string> onComplete)
    {
        BackendReturnObject bro = Backend.BMember.CustomLogin(id, password);

        if (bro.IsSuccess())
        {
            Debug.Log("[Auth] 로그인 성공");
            OnLoginSuccess();
            onComplete?.Invoke(true, null);
        }
        else
        {
            onComplete?.Invoke(false, ParseError(bro));
        }
    }

    // ────────────────────────────────────────────────
    // 5. 게스트 계정 → 이메일 계정 연동 (페더레이션)
    //    기기 변경 시 데이터 유실을 막기 위한 핵심 기능
    // ────────────────────────────────────────────────
    // ────────────────────────────────────────────────
    // 5. 게스트 계정 → 페더레이션(소셜) 계정 연동
    //    TODO: 실제 소셜 로그인(구글/애플 등) SDK 연동 시,
    //    Backend.BMember.AuthorizeFederation(token, FederationType.Google) 형태로 구현 예정.
    //    (TransferAccountForGuest는 존재하지 않는 함수라 제거함)
    // ────────────────────────────────────────────────
    public void LinkAccount(string id, string password, Action<bool, string> onComplete)
    {
        Debug.LogWarning("[Auth] LinkAccount는 아직 구현되지 않았습니다. 소셜 로그인 SDK 연동 후 채워주세요.");
        onComplete?.Invoke(false, "아직 지원하지 않는 기능입니다.");
    }

    // ────────────────────────────────────────────────
    // 내부 헬퍼
    // ────────────────────────────────────────────────
    // ────────────────────────────────────────────────
    // 6. 플레이어 이름 (로그인 이후 별도 설정 화면에서 사용)
    // ────────────────────────────────────────────────

    /// <summary>
    /// 현재 로그인된 유저의 플레이어 이름을 조회한다.
    /// 아직 설정한 적 없으면 onComplete(true, null)로 콜백된다.
    /// </summary>
    public void GetPlayerName(Action<bool, string> onComplete)
    {
        Backend.GameData.Get("PlayerProfile", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogWarning($"[Auth] GetPlayerName 실패: {callback}");
                onComplete?.Invoke(false, null);
                return;
            }

            var rows = callback.FlattenRows();
            Debug.Log($"[Auth] GetPlayerName rows: {rows?.Count}");

            if (rows == null || rows.Count == 0)
            {
                onComplete?.Invoke(true, null);
                return;
            }

            string playerName = rows[0]["playerName"].ToString();
            Debug.Log($"[Auth] playerName: {playerName}");
            onComplete?.Invoke(true, playerName);
        });
    }

    /// <summary>
    /// 플레이어 이름을 신규 저장(최초 1회)한다.
    /// 이후 이름 변경 기능이 필요해지면 UpdateV2로 별도 메서드를 추가하면 된다.
    /// </summary>
    public void SetPlayerName(string playerName, Action<bool, string> onComplete)
    {
        Param param = new Param();
        param.Add("playerName", playerName);

        Backend.GameData.Insert("PlayerProfile", param, callback =>
        {
            if (callback.IsSuccess())
            {
                onComplete?.Invoke(true, null);
            }
            else
            {
                Debug.LogWarning($"[Auth] 플레이어 이름 저장 실패: {callback}");
                onComplete?.Invoke(false, ParseError(callback));
            }
        });
    }

    // ────────────────────────────────────────────────
    // 내부 헬퍼
    // ────────────────────────────────────────────────

    /// <summary>
    /// 로그인/회원가입/게스트로그인 공통 성공 후처리.
    /// 플레이어 이름이 설정되어 있는지 확인해서 분기한다.
    /// </summary>
    private bool _isLoggingIn = false;

    private void OnLoginSuccess()
    {
        if (_isLoggingIn) return;
        _isLoggingIn = true;

        // 재화 → 상점 아이템/인벤토리 → 착용 정보 → 이름 확인 순서로 초기화
        WalletManager.Instance.InitializeWallet(walletSuccess =>
        {
            ShopManager.Instance.Initialize(shopSuccess =>
            {
                CharmManager.Instance.Refresh();

                AffectionManager.Instance.Initialize(affectionSuccess =>
                {
                    EquipmentManager.Instance.Initialize(equipmentSuccess =>
                    {
                        QuestManager.Instance.Initialize(questSuccess =>
                        {
                            GetPlayerName((success, playerName) =>
                            {
                                if (string.IsNullOrEmpty(playerName))
                                {
                                    PlayerNameSetupUI.Instance?.Show();
                                }
                                else
                                {
                                    LoadMainScene();
                                }
                            });
                        });
                    });
                });
            });
        });
    }

    /// <summary>플레이어 이름 설정이 끝났을 때 PlayerNameSetupUI에서 호출.</summary>
    public void LoadMainScene()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    /// <summary>
    /// 뒤끝 에러 코드를 사용자에게 보여줄 한글 메시지로 변환.
    /// 실제 프로젝트에서는 errorCode 전체 목록 기준으로 더 세분화하면 좋다.
    /// </summary>
    private string ParseError(BackendReturnObject bro)
    {
        string code = bro.GetStatusCode();

        switch (code)
        {
            case "409": return "이미 사용 중인 아이디입니다.";
            case "401": return "아이디 또는 비밀번호가 올바르지 않습니다.";
            case "400": return "입력값을 다시 확인해주세요.";
            default: return $"오류가 발생했습니다. (코드: {code})";
        }
    }
}