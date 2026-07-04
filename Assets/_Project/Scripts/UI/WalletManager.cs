using System;
using BackEnd;
using UnityEngine;

/// <summary>
/// 코인/티켓 재화 시스템 매니저 (싱글톤)
/// - 코인: 상점에서 옷 아이템을 구매할 때 사용하는 재화
/// - 티켓: 에피소드를 열람할 때 사용하는 재화
/// 재화 조회, 증가, 차감을 담당한다. 모든 증감은 서버에서 처리해 부정행위를 방지한다.
/// </summary>
public class WalletManager : MonoBehaviour
{
    public static WalletManager Instance { get; private set; }

    // 로컬 캐시 (UI 표시용)
    public int Coin { get; private set; }
    public int Ticket { get; private set; }

    // 재화 변경 시 UI에 알려주는 이벤트
    public event Action OnWalletUpdated;

    private string _walletRowInDate; // 내 Wallet 행의 고유 키

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

    // ────────────────────────────────────────────────
    // 1. 로그인 후 초기화 (AuthManager.OnLoginSuccess에서 호출)
    // ────────────────────────────────────────────────

    /// <summary>
    /// Wallet 행이 있으면 불러오고, 없으면 새로 만든다.
    /// </summary>
    public void InitializeWallet(Action<bool> onComplete)
    {
        Backend.GameData.Get("Wallet", new Where(), callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Wallet] 조회 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            var rows = callback.FlattenRows();

            if (rows == null || rows.Count == 0)
            {
                // 최초 로그인 - Wallet 행 새로 생성
                CreateWallet(onComplete);
            }
            else
            {
                // 기존 데이터 로드
                _walletRowInDate = rows[0]["inDate"].ToString();
                Coin = int.Parse(rows[0]["coin"].ToString());
                Ticket = int.Parse(rows[0]["ticket"].ToString());

                Debug.Log($"[Wallet] 불러오기 성공 - 코인: {Coin}, 티켓: {Ticket}");
                OnWalletUpdated?.Invoke();
                onComplete?.Invoke(true);
            }
        });
    }

    private void CreateWallet(Action<bool> onComplete)
    {
        Param param = new Param();
        param.Add("coin", 0);
        param.Add("ticket", 0);

        Backend.GameData.Insert("Wallet", param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Wallet] 생성 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            _walletRowInDate = callback.GetInDate();
            Coin = 0;
            Ticket = 0;

            Debug.Log("[Wallet] 최초 생성 완료");
            OnWalletUpdated?.Invoke();
            onComplete?.Invoke(true);
        });
    }

    // ────────────────────────────────────────────────
    // 2. 재화 추가
    // ────────────────────────────────────────────────

    public void AddCoin(int amount, Action<bool> onComplete = null)
    {
        UpdateWallet("coin", amount, onComplete);
    }

    public void AddTicket(int amount, Action<bool> onComplete = null)
    {
        UpdateWallet("ticket", amount, onComplete);
    }

    // ────────────────────────────────────────────────
    // 3. 재화 차감 (잔액 부족 시 실패 반환)
    // ────────────────────────────────────────────────

    /// <summary>옷 아이템 구매 등에 사용.</summary>
    public void SpendCoin(int amount, Action<bool, string> onComplete)
    {
        if (Coin < amount)
        {
            onComplete?.Invoke(false, "코인이 부족합니다.");
            return;
        }
        UpdateWallet("coin", -amount, success => onComplete?.Invoke(success, success ? null : "서버 오류가 발생했습니다."));
    }

    /// <summary>에피소드 열람 등에 사용.</summary>
    public void SpendTicket(int amount, Action<bool, string> onComplete)
    {
        if (Ticket < amount)
        {
            onComplete?.Invoke(false, "티켓이 부족합니다.");
            return;
        }
        UpdateWallet("ticket", -amount, success => onComplete?.Invoke(success, success ? null : "서버 오류가 발생했습니다."));
    }

    // ────────────────────────────────────────────────
    // 내부 헬퍼
    // ────────────────────────────────────────────────

    private void UpdateWallet(string column, int delta, Action<bool> onComplete)
    {
        if (string.IsNullOrEmpty(_walletRowInDate))
        {
            Debug.LogError("[Wallet] walletRowInDate가 없습니다. InitializeWallet을 먼저 호출하세요.");
            onComplete?.Invoke(false);
            return;
        }

        Param param = new Param();

        // delta가 양수면 덧셈, 음수면 뺄셈 연산으로 서버에 원자적으로 반영
        if (delta >= 0)
            param.AddCalculation(column, GameInfoOperator.addition, delta);
        else
            param.AddCalculation(column, GameInfoOperator.subtraction, -delta);

        // _walletRowInDate로 특정 행을 지정하기 위한 Where 조건
        Where where = new Where();
        where.Equal("inDate", _walletRowInDate);

        Backend.GameData.UpdateWithCalculation("Wallet", where, param, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError($"[Wallet] {column} 업데이트 실패: {callback}");
                onComplete?.Invoke(false);
                return;
            }

            // 로컬 캐시 업데이트
            if (column == "coin") Coin += delta;
            else if (column == "ticket") Ticket += delta;

            Debug.Log($"[Wallet] {column} {delta:+#;-#} 완료 → 코인: {Coin}, 티켓: {Ticket}");
            OnWalletUpdated?.Invoke();
            onComplete?.Invoke(true);
        });
    }
}