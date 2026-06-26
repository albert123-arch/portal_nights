using System;
using UnityEngine;
using Unity.Netcode;

namespace PortalNights
{
    public sealed class PortalNightsHealth : NetworkBehaviour
    {
        [SerializeField] private float baseMaxHealth = 100f;

        private readonly NetworkVariable<float> maxHealth = new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<float> currentHealth = new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private bool deathRaised;
        private float localMaxHealth = 100f;
        private float localCurrentHealth = 100f;

        public event Action<PortalNightsHealth> Died;
        public event Action<PortalNightsHealth> HealthChanged;

        public float BaseMaxHealth => baseMaxHealth;
        public float MaxHealth => UseLocalRuntimeHealth ? localMaxHealth : maxHealth.Value;
        public float CurrentHealth => UseLocalRuntimeHealth ? localCurrentHealth : currentHealth.Value;
        public float Normalized => MaxHealth <= 0f ? 0f : Mathf.Clamp01(CurrentHealth / MaxHealth);
        public bool IsDead => CurrentHealth <= 0.01f;
        private bool UseLocalRuntimeHealth => Application.isPlaying && !IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                maxHealth.Value = Mathf.Max(1f, baseMaxHealth);
                currentHealth.Value = maxHealth.Value;
            }

            localMaxHealth = Mathf.Max(1f, baseMaxHealth);
            localCurrentHealth = localMaxHealth;
        }

        public override void OnNetworkSpawn()
        {
            maxHealth.OnValueChanged += HandleHealthValueChanged;
            currentHealth.OnValueChanged += HandleHealthValueChanged;

            if (IsServer)
            {
                ServerInitialize(baseMaxHealth, true);
            }
        }

        public override void OnNetworkDespawn()
        {
            maxHealth.OnValueChanged -= HandleHealthValueChanged;
            currentHealth.OnValueChanged -= HandleHealthValueChanged;
        }

        public void SetBaseMaxHealth(float value)
        {
            baseMaxHealth = Mathf.Max(1f, value);
            if (!Application.isPlaying)
            {
                maxHealth.Value = baseMaxHealth;
                currentHealth.Value = baseMaxHealth;
            }

            localMaxHealth = baseMaxHealth;
            localCurrentHealth = Mathf.Min(localCurrentHealth, localMaxHealth);
        }

        public void ServerInitialize(float newMaxHealth, bool fill)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            if (UseLocalRuntimeHealth)
            {
                localMaxHealth = Mathf.Max(1f, newMaxHealth);
                localCurrentHealth = fill ? localMaxHealth : Mathf.Clamp(localCurrentHealth, 0f, localMaxHealth);
                deathRaised = localCurrentHealth <= 0f;
                HealthChanged?.Invoke(this);
                return;
            }

            maxHealth.Value = Mathf.Max(1f, newMaxHealth);
            currentHealth.Value = fill ? maxHealth.Value : Mathf.Clamp(currentHealth.Value, 0f, maxHealth.Value);
            deathRaised = currentHealth.Value <= 0f;
            HealthChanged?.Invoke(this);
        }

        public void DamageServer(float amount)
        {
            if (!PortalNightsNet.ServerCanWrite(this) || amount <= 0f || IsDead)
            {
                return;
            }

            if (UseLocalRuntimeHealth)
            {
                localCurrentHealth = Mathf.Max(0f, localCurrentHealth - amount);
                HealthChanged?.Invoke(this);
                if (localCurrentHealth <= 0f && !deathRaised)
                {
                    deathRaised = true;
                    Died?.Invoke(this);
                }

                return;
            }

            currentHealth.Value = Mathf.Max(0f, currentHealth.Value - amount);
            HealthChanged?.Invoke(this);
            if (currentHealth.Value <= 0f && !deathRaised)
            {
                deathRaised = true;
                Died?.Invoke(this);
                NotifyDiedClientRpc();
            }
        }

        public void HealServer(float amount)
        {
            if (!PortalNightsNet.ServerCanWrite(this) || amount <= 0f || IsDead)
            {
                return;
            }

            if (UseLocalRuntimeHealth)
            {
                localCurrentHealth = Mathf.Min(localMaxHealth, localCurrentHealth + amount);
                HealthChanged?.Invoke(this);
                return;
            }

            currentHealth.Value = Mathf.Min(maxHealth.Value, currentHealth.Value + amount);
            HealthChanged?.Invoke(this);
        }

        public void ServerSetCurrentHealth(float value)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            if (UseLocalRuntimeHealth)
            {
                localCurrentHealth = Mathf.Clamp(value, 0f, localMaxHealth);
                deathRaised = localCurrentHealth <= 0f;
                HealthChanged?.Invoke(this);
                return;
            }

            currentHealth.Value = Mathf.Clamp(value, 0f, maxHealth.Value);
            deathRaised = currentHealth.Value <= 0f;
            HealthChanged?.Invoke(this);
        }

        [ClientRpc]
        private void NotifyDiedClientRpc()
        {
            if (!IsServer)
            {
                Died?.Invoke(this);
            }
        }

        private void HandleHealthValueChanged(float previous, float current)
        {
            HealthChanged?.Invoke(this);
        }
    }
}
