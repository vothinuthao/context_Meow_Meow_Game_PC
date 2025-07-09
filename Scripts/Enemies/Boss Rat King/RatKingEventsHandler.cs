using UnityEngine;

namespace TwoSleepyCatsStudio.Enemy
{
    public class RatKingEventsHandler : MonoBehaviour
    {
        [SerializeField] RatKingBehavior ratKing;

        public void DashHit()
        {
            ratKing.OnDashHit();
        }

        public void SqueakWave()
        {
            ratKing.OnSqueakWave();
        }
    }
}