using UnityEngine;

namespace SmashPuzzle.Projectile
{
    // ProjectilePool이 Cannonball 반환을 감지하기 위한 도우미.
    // Cannonball 프리팹에 함께 붙여서 사용.
    public class PoolReturnNotifier : MonoBehaviour
    {
        private Cannonball      _ball;
        private ProjectilePool  _pool;

        public void Setup(Cannonball ball, ProjectilePool pool)
        {
            _ball = ball;
            _pool = pool;
        }

        private void OnDisable()
        {
            if (_pool != null && _ball != null)
                _pool.Return(_ball);
        }
    }
}
