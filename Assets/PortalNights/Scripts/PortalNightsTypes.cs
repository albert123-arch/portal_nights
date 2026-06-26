using System;
using UnityEngine;
using Unity.Netcode;

namespace PortalNights
{
    public enum PortalNightsEnemyKind
    {
        Small,
        Brute
    }

    public enum PortalNightsLane
    {
        Left = 0,
        Right = 1
    }

    public enum PortalNightsGameState
    {
        Planet1_Defense = 0,
        Planet1_RewardChoice = 1,
        Planet1_PortalReady = 2,
        PortalTravel = 3,
        Planet2_ClearArea = 4,
        Planet2_SphereReady = 5,
        Planet2_DefendSphere = 6,
        Planet2_Cleared = 7,
        Failed = 8
    }

    public enum PortalNightsPickupKind
    {
        Coin = 0,
        Heal = 1,
        Armor = 2,
        WeaponDamageBoost = 3,
        TurretDamageBoost = 4
    }

    [Serializable]
    public struct PortalNightsWaveDefinition
    {
        public int smallCount;
        public int bruteCount;
        public float spawnInterval;

        public PortalNightsWaveDefinition(int smallCount, int bruteCount, float spawnInterval)
        {
            this.smallCount = smallCount;
            this.bruteCount = bruteCount;
            this.spawnInterval = spawnInterval;
        }

        public int TotalEnemies => Mathf.Max(0, smallCount) + Mathf.Max(0, bruteCount);
    }

    public static class PortalNightsNet
    {
        public static bool ServerCanWrite(NetworkBehaviour behaviour)
        {
            NetworkManager manager = NetworkManager.Singleton;
            return manager == null || !manager.IsListening || behaviour == null || behaviour.IsServer || !behaviour.IsSpawned;
        }
    }

    public static class PortalNightsMath
    {
        public static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        public static bool TryFlatDirection(Vector3 from, Vector3 to, out Vector3 direction)
        {
            direction = Flat(to - from);
            float magnitude = direction.magnitude;
            if (magnitude <= 0.001f)
            {
                direction = Vector3.forward;
                return false;
            }

            direction /= magnitude;
            return true;
        }
    }
}
