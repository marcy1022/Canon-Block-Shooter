using UnityEngine;
using UnityEngine.Events;

namespace SmashPuzzle.Core
{
    public class CoinManager : MonoBehaviour
    {
        public static CoinManager Instance { get; private set; }

        private const string KeyCoins = "TotalCoins";

        public int Balance => PlayerPrefs.GetInt(KeyCoins, 0);

        public static readonly UnityEvent<int> OnCoinsChanged = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            PlayerPrefs.SetInt(KeyCoins, Balance + amount);
            PlayerPrefs.Save();
            OnCoinsChanged.Invoke(Balance);
            Debug.Log($"[CoinManager] +{amount} → {Balance}");
        }

        public bool SpendCoins(int amount)
        {
            if (Balance < amount) return false;
            PlayerPrefs.SetInt(KeyCoins, Balance - amount);
            PlayerPrefs.Save();
            OnCoinsChanged.Invoke(Balance);
            Debug.Log($"[CoinManager] -{amount} → {Balance}");
            return true;
        }

        private void OnDestroy() => OnCoinsChanged.RemoveAllListeners();
    }
}
