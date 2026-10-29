using UnityEngine;
using UnityEngine.Events;
using SmashPuzzle.Data;

namespace SmashPuzzle.Core
{
    public enum GameState { Menu, Playing, Paused, LevelComplete, GameOver }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameData gameData;

        public GameState State { get; private set; }
        public int CurrentLevel { get; private set; }

        public static readonly UnityEvent<GameState> OnStateChanged = new();
        public static readonly UnityEvent<int> OnLevelLoaded = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged.Invoke(newState);
        }

        public void LoadLevel(int levelIndex)
        {
            CurrentLevel = levelIndex;
            SetState(GameState.Playing);
            OnLevelLoaded.Invoke(levelIndex);
        }

        public void PauseGame()  => SetState(GameState.Paused);
        public void ResumeGame() => SetState(GameState.Playing);

        private void OnDestroy()
        {
            OnStateChanged.RemoveAllListeners();
            OnLevelLoaded.RemoveAllListeners();
        }
    }
}
