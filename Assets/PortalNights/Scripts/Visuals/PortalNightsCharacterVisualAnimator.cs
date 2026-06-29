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

            DisableRootMotion();
        }

        private void OnEnable()
        {
            DisableRootMotion();
        }

        public void SetAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
            DisableRootMotion();
        }

        public void SetMoving(bool moving)
        {
            if (CanDriveAnimator())
            {
                animator.SetBool(MovingHash, moving);
            }
        }

        public void SetRunning(bool running)
        {
            if (CanDriveAnimator())
            {
                animator.SetBool(RunningHash, running);
            }
        }

        public void SetMoveSpeed(float normalizedSpeed)
        {
            if (CanDriveAnimator())
            {
                animator.SetFloat(MoveSpeedHash, Mathf.Clamp01(normalizedSpeed));
            }
        }

        public void TriggerAttack()
        {
            if (CanDriveAnimator())
            {
                animator.SetTrigger(AttackHash);
            }
        }

        public void TriggerHit()
        {
            if (CanDriveAnimator())
            {
                animator.SetTrigger(HitHash);
            }
        }

        public void TriggerDeath()
        {
            if (CanDriveAnimator())
            {
                animator.SetTrigger(DeathHash);
            }
        }

        public void TriggerSpecial()
        {
            if (CanDriveAnimator())
            {
                animator.SetTrigger(SpecialHash);
            }
        }

        private void DisableRootMotion()
        {
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
        }

        private bool CanDriveAnimator()
        {
            return animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
        }
    }
}
