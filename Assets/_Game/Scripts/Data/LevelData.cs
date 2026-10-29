using UnityEngine;

namespace SmashPuzzle.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "SmashPuzzle/LevelData")]
    public class LevelData : ScriptableObject
    {
        public int LevelIndex;
        public int MaxShots = 5;

        [Header("Star Thresholds (shots remaining)")]
        public int ThreeStar = 3; // 남은 발사 횟수 ≥ 3 → 별 3개
        public int TwoStar   = 1; // 남은 발사 횟수 ≥ 1 → 별 2개
        // 별 1개: 클리어만 해도 지급

        [Header("Objectives")]
        public int RequiredDestructibles = 1; // 클리어 조건 최소 파괴 수

        [Header("Unlock")]
        public int EnergyRequired = 1;
        public int CoinsReward = 100;
    }
}
