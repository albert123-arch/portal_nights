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

    public enum PortalNightsPlanet4EnemyVariant
    {
        Swarmer = 0,
        Runner = 1,
        Brute = 2
    }

    public enum PortalNightsDamageTargetKind
    {
        Generic = 0,
        Planet5HealingSphere = 1,
        Planet5Boss = 2
    }

    public enum PortalNightsSphereVisualState
    {
        Corrupted = 0,
        DamagedCore = 1,
        Restored = 2
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
        Failed = 8,
        Planet3_Arrival = 9,
        Planet3_FindStaff = 10,
        Planet3_ReleaseStaff = 11,
        Planet3_EscortToSphere = 12,
        Planet3_SphereReady = 13,
        Planet3_SphereActivation = 14,
        Planet3_DefensePreparation = 15,
        Planet3_DefendSphere = 16,
        Planet3_Cleared = 17,
        Planet3_Failed = 18,
        Planet4_Arrival = 19,
        Planet4_HordeActive = 20,
        Planet4_RiftClosing = 21,
        Planet4_ExitPortalReady = 22,
        Planet4_Cleared = 23,
        Planet4_Failed = 24,
        Planet5_Arrival = 25,
        Planet5_BossIntro = 26,
        Planet5_DestroyHealingSphere = 27,
        Planet5_KillBosses = 28,
        Planet5_RestoreSphereReady = 29,
        Planet5_Failed = 30,
        Planet5_RestoringSphere = 31,
        Planet5_SphereRestored = 32,
        Planet5_UniverseComplete = 33
    }

    public enum PortalNightsStaffState
    {
        Captured = 0,
        Releasing = 1,
        Following = 2,
        WaitingAtSphere = 3,
        Downed = 4,
        Safe = 5
    }

    public enum PortalNightsRiftState
    {
        Dormant = 0,
        Charging = 1,
        Active = 2,
        Closing = 3,
        Weakening = 4,
        Closable = 5,
        Closed = 6
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
