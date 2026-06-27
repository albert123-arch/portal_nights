using UnityEngine;

namespace PortalNights.Visuals
{
    public sealed class PortalNightsCharacterVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int MovingHash = Animator.StringToHash("Moving");
        private static readonly int RunningHash = Animator.StringToHash("Running");
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int SpecialHash = Animator.StringToHash("Special");

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        public void SetAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
        }

        public void SetMoving(bool moving)
        {
            if (animator != null)
            {
                animator.SetBool(MovingHash, moving);
            }
        }

        public void SetRunning(bool running)
        {
            if (animator != null)
            {
                animator.SetBool(RunningHash, running);
            }
        }

        public void SetMoveSpeed(float normalizedSpeed)
        {
            if (animator != null)
            {
                animator.SetFloat(MoveSpeedHash, Mathf.Clamp01(normalizedSpeed));
            }
        }

        public void TriggerAttack()
        {
            animator?.SetTrigger(AttackHash);
        }

        public void TriggerHit()
        {
            animator?.SetTrigger(HitHash);
        }

        public void TriggerDeath()
        {
            animator?.SetTrigger(DeathHash);
        }

        public void TriggerSpecial()
        {
            animator?.SetTrigger(SpecialHash);
        }
    }
}
