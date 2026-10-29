using UnityEngine;

namespace SmashPuzzle.Level
{
    // PlayerPrefs 기반 진행도/코인 저장. 서버 없는 Mock 구현.
    public class LevelProgressManager : MonoBehaviour
    {
        public static LevelProgressManager Instance { get; private set; }

        private const string KeyMaxUnlocked = "MaxUnlockedLevel";
        private const string KeyTotalCoins  = "TotalCoins";
        private const string KeyTotalStars  = "TotalStars";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 최초 실행: 레벨 1 해금
            if (!PlayerPrefs.HasKey(KeyMaxUnlocked))
                PlayerPrefs.SetInt(KeyMaxUnlocked, 1);
        }

        public bool IsLevelUnlocked(int levelId)  => levelId <= GetMaxUnlockedLevel();
        public int  GetMaxUnlockedLevel()          => PlayerPrefs.GetInt(KeyMaxUnlocked, 1);
        public int  GetStars(int levelId)          => PlayerPrefs.GetInt(StarsKey(levelId), 0);
        public int  GetTotalCoins()                => PlayerPrefs.GetInt(KeyTotalCoins, 0);
        public int  GetTotalStars()                => PlayerPrefs.GetInt(KeyTotalStars, 0);

        public void SaveProgress(int levelId, int stars, int coinsEarned)
        {
            int prevStars = GetStars(levelId);
            if (stars > prevStars)
            {
                int starDiff = stars - prevStars;
                PlayerPrefs.SetInt(StarsKey(levelId), stars);
                PlayerPrefs.SetInt(KeyTotalStars, GetTotalStars() + starDiff);
            }

            // 코인은 항상 지급 (재도전 시에도)
            PlayerPrefs.SetInt(KeyTotalCoins, GetTotalCoins() + coinsEarned);

            // 다음 레벨 해금
            if (stars > 0)
            {
                int nextLevel = levelId + 1;
                if (nextLevel > GetMaxUnlockedLevel())
                    PlayerPrefs.SetInt(KeyMaxUnlocked, nextLevel);
            }

            PlayerPrefs.Save();
        }

        public void AddCoins(int amount)
        {
            PlayerPrefs.SetInt(KeyTotalCoins, GetTotalCoins() + amount);
            PlayerPrefs.Save();
        }

        public bool SpendCoins(int amount)
        {
            int current = GetTotalCoins();
            if (current < amount) return false;
            PlayerPrefs.SetInt(KeyTotalCoins, current - amount);
            PlayerPrefs.Save();
            return true;
        }

        private static string StarsKey(int levelId) => $"Level_{levelId}_Stars";
    }
}
