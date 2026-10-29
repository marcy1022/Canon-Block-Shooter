using UnityEngine;

namespace SmashPuzzle.Data
{
    [CreateAssetMenu(fileName = "GameData", menuName = "SmashPuzzle/GameData")]
    public class GameData : ScriptableObject
    {
        [Header("Camera")]
        public float CameraOrthographicSize = 5.4f;

        [Header("Energy / BM")]
        public int MaxEnergy = 5;
        public float EnergyRegenSeconds = 1800f; // 30분 (역기획서 §4.4)

        [Header("Economy")]
        public int CoinPerStar = 50;
        public int BoosterCostCoins = 100;

        [Header("Cannon")]
        public float MinAngle    = -10f; // 가이드 Step 2 스펙
        public float MaxAngle    =  80f;
        public float MinForce    =   5f;
        public float MaxForce    =  20f;
        public float MaxLaunchForce = 20f; // 하위 호환용 (CannonShooter에서 MaxForce 사용)

        [Header("Projectile")]
        public float ProjectileLifetime = 5f;
    }
}
