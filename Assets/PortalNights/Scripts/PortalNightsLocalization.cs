using System.Collections.Generic;
using UnityEngine;

namespace PortalNights
{
    public enum PortalNightsLanguage
    {
        Russian,
        English
    }

    public static class PortalNightsLocalization
    {
        private static readonly Dictionary<string, string> Ru = new Dictionary<string, string>
        {
            { "hud.wave", "ВОЛНА" },
            { "hud.enemies", "ВРАГИ" },
            { "hud.core", "ЯДРО" },
            { "hud.player", "ГЕРОЙ" },
            { "hud.coins", "МОНЕТЫ" },
            { "hud.score", "СЧЕТ" },
            { "hud.time", "ВРЕМЯ" },
            { "hud.bosses", "БОССЫ" },
            { "hud.staff", "ПЕРСОНАЛ" },
            { "hud.spheres", "СФЕРЫ" },
            { "hud.deaths", "СМЕРТИ" },
            { "hud.turrets", "ТУРЕЛИ" },
            { "hud.upgrades", "УЛУЧШЕНИЯ" },
            { "hud.universe", "ВСЕЛЕННАЯ" },
            { "hud.planet", "ПЛАНЕТА" },
            { "hud.buildReady", "E  ПОСТРОИТЬ ТУРЕЛЬ" },
            { "hud.upgradeReady", "E  УЛУЧШИТЬ ТУРЕЛЬ  {0}>{1}" },
            { "hud.upgradeLevelReady", "E  УЛУЧШИТЬ ТУРЕЛЬ УР. {0}" },
            { "hud.buildCost", "ЦЕНА" },
            { "hud.noBuild", "НАЙДИТЕ ПЛОЩАДКУ СТРОИТЕЛЬСТВА" },
            { "hud.turretMaxLevel", "ТУРЕЛЬ МАКСИМАЛЬНОГО УРОВНЯ" },
            { "hud.wait", "СЛЕДУЮЩАЯ ВОЛНА ЧЕРЕЗ" },
            { "hud.gameOver", "ЯДРО УНИЧТОЖЕНО — НАЖМИТЕ R" },
            { "hud.chooseReward", "ВЫБЕРИТЕ НАГРАДУ" },
            { "hud.portalReady", "ПОРТАЛ ГОТОВ" },
            { "hud.portalLocked", "ПОРТАЛ ЗАКРЫТ" },
            { "hud.planet2", "ПЛАНЕТА 2 — CRYSTAL MOON" },
            { "hud.planet3", "ПЛАНЕТА 3 — ASH RELAY STATION" },
            { "hud.planet4", "ПЛАНЕТА 4 — SWARM EXPANSE" },
            { "hud.planet5", "ПЛАНЕТА 5 — CRIMSON SINGULARITY" },
            { "hud.planet4Portal", "ПОРТАЛ ПЛАНЕТЫ 5 ОТКРЫТ" },
            { "hud.sphereReady", "СФЕРА ГОТОВА" },
            { "hud.relaySphereReady", "СФЕРА РЕЛЕ ГОТОВА" },
            { "hud.defendSphere", "ЗАЩИЩАЙТЕ СФЕРУ" },
            { "hud.prepareDefense", "ГОТОВЬТЕ ОБОРОНУ {0}" },
            { "hud.relayCharge", "ЗАРЯД РЕЛЕ {0}%" },
            { "hud.planetCleared", "ПЛАНЕТА ОЧИЩЕНА" },
            { "hud.planet3Cleared", "ПЛАНЕТА 3 ОЧИЩЕНА" },
            { "hud.planet4Cleared", "ПЛАНЕТА 4 ОЧИЩЕНА" },
            { "hud.relayDestroyed", "РЕЛЕ УНИЧТОЖЕНО — НАЖМИТЕ R" },
            { "hud.swarmOverrun", "РОЙ ПРОРВАЛСЯ — НАЖМИТЕ R" },
            { "hud.destroyHealingSphere", "РАЗРУШЬТЕ СФЕРУ ИСЦЕЛЕНИЯ" },
            { "hud.killBosses", "ИСЦЕЛЕНИЕ ОТКЛЮЧЕНО — УБЕЙТЕ БОССОВ" },
            { "hud.restoreSphere", "ВОССТАНОВИТЕ СФЕРУ" },
            { "hud.restoringSphere", "ВОССТАНОВЛЕНИЕ СФЕРЫ" },
            { "hud.sphereRestored", "СФЕРА ВОССТАНОВЛЕНА" },
            { "hud.crimsonLost", "CRIMSON SINGULARITY ПОТЕРЯНА — НАЖМИТЕ R" },
            { "hud.sphereLost", "СФЕРА ПОТЕРЯНА — НАЖМИТЕ R" },
            { "hud.controls", "WASD ДВИЖЕНИЕ   МЫШЬ ПРИЦЕЛ   ЛКМ/SPACE ОГОНЬ   SHIFT БЕГ   E СТРОЙКА" },
            { "hud.restart", "R РЕСТАРТ   ESC КУРСОР" },
            { "hud.enterPortal", "E ВОЙТИ В ПОРТАЛ" },
            { "hud.activateSphere", "E АКТИВИРОВАТЬ СФЕРУ" },
            { "hud.rewardOptions", "1  +15% УРОН ОРУЖИЯ     2  +15% УРОН ТУРЕЛЕЙ     3  РЕМОНТ ЯДРА +150" },
            { "hud.sphereReadyRestore", "СФЕРА ГОТОВА К ВОССТАНОВЛЕНИЮ" },
            { "hud.universeComplete", "ВСЕЛЕННАЯ ЗАВЕРШЕНА" },
            { "hud.finalScore", "ИТОГОВЫЙ СЧЕТ" },
            { "hud.localLeaderboard", "ЛОКАЛЬНАЯ ТАБЛИЦА" },
            { "hud.leaderboardHeader", "№    ИМЯ         СЧЕТ     ВС  ВРЕМЯ   ВРАГИ    БОССЫ" },
            { "hud.noLeaderboard", "ПОКА НЕТ ЗАПИСЕЙ" },
            { "hud.defaultPlayerName", "Командир" },
            { "hud.runSummary", "ИТОГИ ЗАБЕГА" },
            { "hud.enterNextUniverse", "E У ПОРТАЛА: ВОЙТИ ВО ВСЕЛЕННУЮ {0}" },
            { "hud.showHideLeaderboard", "L: ЛОКАЛЬНАЯ ТАБЛИЦА" },
            { "hud.leaderboardToggle", "L СКРЫТЬ/ПОКАЗАТЬ" },
            { "hud.enemiesKilled", "ВРАГИ УНИЧТОЖЕНЫ" },
            { "hud.bossesDefeated", "БОССЫ ПОБЕЖДЕНЫ" },
            { "hud.staffSaved", "ПЕРСОНАЛ СПАСЕН" },
            { "hud.spheresRestored", "СФЕРЫ ВОССТАНОВЛЕНЫ" },
            { "hud.turretsBuiltUpgraded", "ТУРЕЛИ ПОСТРОЕНО/УЛУЧШЕНО" },
            { "hud.objective", "ЦЕЛЬ" },
            { "hud.objectiveRelay", "СФЕРА РЕЛЕ" },
            { "hud.objectiveClearArea", "ЗАЧИСТИТЕ ЗОНУ" },
            { "hud.healingSphere", "СФЕРА ИСЦЕЛЕНИЯ" },
            { "hud.enemiesDefeated", "ВРАГИ УБИТЫ" },
            { "hud.sphereHp", "СФЕРА" },
            { "hud.bossesCannotDie", "БОССЫ БЕССМЕРТНЫ" },
            { "hud.stabilizers", "СТАБИЛИЗАТОРЫ" },
            { "hud.universeStabilized", "ВСЕЛЕННАЯ СТАБИЛИЗИРОВАНА" },
            { "hud.staffRescued", "ПЕРСОНАЛ СПАСЕН" },
            { "hud.staffAtSphere", "ПЕРСОНАЛ У СФЕРЫ" },
            { "hud.charge", "ЗАРЯД" },
            { "hud.rifts", "РАЗЛОМЫ" },
            { "hud.riftsClosed", "РАЗЛОМЫ ЗАКРЫТЫ" },
            { "hud.activeRifts", "АКТИВНЫЕ РАЗЛОМЫ" },
            { "hud.hold", "УДЕРЖАНИЕ" },
            { "hud.solar", "SOLAR" },
            { "hud.behemoth", "BEHEMOTH" },
            { "hud.allies", "СОЮЗНИКИ" },

            { "objective.defendCore", "ЗАЩИЩАЙТЕ ЯДРО" },
            { "objective.chooseReward", "ВЫБЕРИТЕ НАГРАДУ" },
            { "objective.enterPortal", "ВОЙДИТЕ В ПОРТАЛ" },
            { "objective.clearArea", "ЗАЧИСТИТЕ ЗОНУ" },
            { "objective.activateSphere", "АКТИВИРУЙТЕ СФЕРУ" },
            { "objective.defendSphere", "ЗАЩИЩАЙТЕ СФЕРУ" },
            { "objective.areaCleared", "ЗОНА ЗАЧИЩЕНА" },
            { "objective.rescueStaff", "СПАСИТЕ ПЕРСОНАЛ" },
            { "objective.closeRifts", "ЗАКРОЙТЕ РАЗЛОМЫ" },
            { "objective.destroyCorruptedSphere", "РАЗРУШЬТЕ ИСКАЖЕННУЮ СФЕРУ" },
            { "objective.killBothBosses", "УНИЧТОЖЬТЕ ОБОИХ БОССОВ" },
            { "objective.restoreSphere", "ВОССТАНОВИТЕ СФЕРУ" },
            { "objective.universeComplete", "ВСЕЛЕННАЯ ЗАВЕРШЕНА" },
            { "objective.failed", "ЗАДАНИЕ ПРОВАЛЕНО" },

            { "progress.wave", "ВОЛНА {0}/{1}" },
            { "progress.nextWave", "СЛЕДУЮЩАЯ ВОЛНА ЧЕРЕЗ {0}" },
            { "progress.pressRewards", "НАЖМИТЕ 1 / 2 / 3" },
            { "progress.requiredTurrets", "ВСЕ НУЖНЫЕ ТУРЕЛИ ГОТОВЫ" },
            { "progress.enemies", "ВРАГИ: {0}" },
            { "progress.holdCore", "ДЕРЖИТЕ ПОЗИЦИЮ У ЯДРА" },
            { "progress.nextPlanetOnline", "СЛЕДУЮЩАЯ ПЛАНЕТА НА СВЯЗИ" },
            { "progress.staffAtSphere", "ПЕРСОНАЛ У СФЕРЫ: {0}/2" },
            { "progress.buildTime", "ВРЕМЯ СТРОЙКИ: {0}" },
            { "progress.relayCharge", "ЗАРЯД РЕЛЕ: {0}%" },
            { "progress.killsRifts", "УБИТО: {0}/{1}   РАЗЛОМЫ: {2}/4" },
            { "progress.planet5PortalOpen", "ПОРТАЛ ПЛАНЕТЫ 5 ОТКРЫТ" },
            { "progress.sphere", "СФЕРА: {0}" },
            { "progress.bossesDefeated", "БОССЫ ПОБЕЖДЕНЫ: {0}/2" },
            { "progress.stabilizers", "СТАБИЛИЗАТОРЫ: {0}/{1}" },
            { "progress.bossesHealing", "БОССЫ ЛЕЧАТСЯ" },
            { "progress.enterNextUniverse", "ВОЙДИТЕ В СЛЕДУЮЩУЮ ВСЕЛЕННУЮ" },
            { "progress.pressRetry", "НАЖМИТЕ R, ЧТОБЫ ПОВТОРИТЬ" },

            { "prompt.enterFinalWorld", "E ВОЙТИ В ФИНАЛЬНЫЙ МИР" },
            { "prompt.holdCloseRift", "УДЕРЖИВАЙТЕ E, ЧТОБЫ ЗАКРЫТЬ {0}{1}" },
            { "prompt.holdStabilize", "УДЕРЖИВАЙТЕ E, ЧТОБЫ СТАБИЛИЗИРОВАТЬ {0}{1}" },
            { "prompt.holdReleaseStaff", "УДЕРЖИВАЙТЕ E, ЧТОБЫ ОСВОБОДИТЬ {0}{1}" },
            { "prompt.holdRevive", "УДЕРЖИВАЙТЕ E, ЧТОБЫ ПОДНЯТЬ{0}" },
            { "prompt.holdActivateRelay", "УДЕРЖИВАЙТЕ E, ЧТОБЫ АКТИВИРОВАТЬ СФЕРУ РЕЛЕ{0}" },

            { "marker.activate", "АКТИВИРОВАТЬ" },
            { "marker.relay", "РЕЛЕ" },
            { "marker.corruptedSphere", "ИСКАЖЕННАЯ СФЕРА" },
            { "marker.staff", "ПЕРСОНАЛ" },
            { "marker.closeRift", "ЗАКРЫТЬ РАЗЛОМ" },
            { "marker.stabilizer", "СТАБИЛИЗАТОР" },

            { "transition.universeShift", "СДВИГ ВСЕЛЕННОЙ" },
            { "transition.planetTitle", "ПЛАНЕТА {0}" },
            { "transition.transit", "СИГНАЛ СМЕЩАЕТСЯ...\nПЕРЕХОД ЧЕРЕЗ ПОРТАЛ..." },
            { "transition.planet1Empowered", "ПЛАНЕТА 1 — ВРАГИ УСИЛЕНЫ" },

            { "toast.notEnoughCoins", "Недостаточно монет" },
            { "toast.turretBuilt", "Турель построена" },
            { "toast.turretUpgraded", "Турель улучшена" },
            { "toast.turretMaxed", "Турель уже максимального уровня" },
            { "toast.turretMissing", "Префаб турели отсутствует" },
            { "toast.noPad", "Подойдите ближе к площадке" },
            { "toast.coreLost", "Ядро пало" },
            { "toast.waveClear", "Волна очищена" },
            { "toast.portalCharging", "Портал заряжается — обе линии" },
            { "toast.rewardChoice", "Награда за волну {0} — нажмите 1 / 2 / 3" },
            { "toast.portalReady", "Портал готов" },
            { "toast.activateSphere", "E активировать сферу" },
            { "toast.universeComplete", "Вселенная завершена" },
            { "toast.universeEntered", "ВСЕЛЕННАЯ {0} — ВРАГИ УСИЛЕНЫ — ПЛАНЕТА 1" },
            { "toast.weaponDamageReward", "+15% урон оружия игрока" },
            { "toast.turretDamageReward", "+15% урон турелей" },
            { "toast.coreRepairReward", "Ядро отремонтировано +150" },
            { "toast.coinGain", "МОНЕТЫ +{0}" },
            { "toast.heal", "ЛЕЧЕНИЕ +40" },
            { "toast.armor", "БРОНЯ +30% 30 С" },
            { "toast.weaponDamageBoost", "УРОН ОРУЖИЯ +20% 30 С" },
            { "toast.turretDamageBoost", "УРОН ТУРЕЛЕЙ +20% 30 С" },
            { "toast.areaCleared", "Зона зачищена" },
            { "toast.sphereActivated", "Сфера активирована" },
            { "toast.bossesDefeated", "Боссы побеждены" },
            { "toast.riftClosed", "Разлом закрыт" },
            { "toast.sphereRestored", "Сфера восстановлена" },
            { "toast.bossHeal", "ЛЕЧЕНИЕ БОССА" },
            { "toast.level", "УР. {0}" },
            { "toast.online", "{0} ГОТОВ {1}/{2}" },
            { "toast.multiplier", "ОЗ x{0}  УРОН x{1}  СЧЕТ x{2}" },
            { "toast.system", "СИСТЕМА" },

            { "state.Planet1_Defense", "Планета 1 — защита ядра" },
            { "state.Planet1_RewardChoice", "Выберите награду" },
            { "state.Planet1_PortalReady", "Портал готов" },
            { "state.PortalTravel", "Переход через портал" },
            { "state.Planet2_ClearArea", "Планета 2 — зачистите зону" },
            { "state.Planet2_SphereReady", "Сфера готова" },
            { "state.Planet2_DefendSphere", "Защищайте сферу" },
            { "state.Planet2_Cleared", "Планета 2 очищена" },
            { "state.Planet3_FindStaff", "Планета 3 — спасите персонал" },
            { "state.Planet3_ReleaseStaff", "Освободите персонал" },
            { "state.Planet3_EscortToSphere", "Ведите персонал к сфере" },
            { "state.Planet3_SphereReady", "Сфера реле готова" },
            { "state.Planet3_SphereActivation", "Активация сферы реле" },
            { "state.Planet3_DefensePreparation", "Готовьте оборону реле" },
            { "state.Planet3_DefendSphere", "Защищайте реле" },
            { "state.Planet3_Cleared", "Планета 3 очищена" },
            { "state.Planet3_Failed", "Реле уничтожено" },
            { "state.Planet4_Arrival", "Планета 4 — Swarm Expanse" },
            { "state.Planet4_HordeActive", "Закройте разломы" },
            { "state.Planet4_RiftClosing", "Закрытие разлома" },
            { "state.Planet4_ExitPortalReady", "Портал планеты 5 открыт" },
            { "state.Planet4_Cleared", "Планета 4 очищена" },
            { "state.Planet4_Failed", "Рой прорвался" },
            { "state.Planet5_Arrival", "Планета 5 — Crimson Singularity" },
            { "state.Planet5_BossIntro", "Боссы пробуждаются" },
            { "state.Planet5_DestroyHealingSphere", "Разрушьте сферу исцеления" },
            { "state.Planet5_KillBosses", "Убейте боссов" },
            { "state.Planet5_RestoreSphereReady", "Восстановите сферу" },
            { "state.Planet5_RestoringSphere", "Восстановление сферы" },
            { "state.Planet5_SphereRestored", "Сфера восстановлена" },
            { "state.Planet5_UniverseComplete", "Вселенная завершена" },
            { "state.Planet5_Failed", "Задание провалено" },
            { "state.Failed", "Задание провалено" }
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "hud.wave", "WAVE" },
            { "hud.enemies", "ENEMIES" },
            { "hud.core", "CORE" },
            { "hud.player", "HERO" },
            { "hud.coins", "COINS" },
            { "hud.buildReady", "E  BUILD TURRET" },
            { "hud.upgradeReady", "E  UPGRADE TURRET  {0}>{1}" },
            { "hud.buildCost", "COST" },
            { "hud.noBuild", "FIND A BUILD PAD" },
            { "hud.wait", "NEXT WAVE IN" },
            { "hud.gameOver", "CORE LOST - PRESS R" },
            { "toast.notEnoughCoins", "Not enough coins" },
            { "toast.turretBuilt", "Turret online" },
            { "toast.turretUpgraded", "Turret upgraded" },
            { "toast.turretMaxed", "Turret is already max level" },
            { "toast.turretMissing", "Turret prefab missing" },
            { "toast.noPad", "Move closer to a build pad" },
            { "toast.coreLost", "The Core has fallen" },
            { "toast.waveClear", "Wave cleared" }
        };

        private static readonly Dictionary<string, string> LegacyRu = new Dictionary<string, string>
        {
            { "DEFEND THE CORE", "ЗАЩИЩАЙТЕ ЯДРО" },
            { "CHOOSE REWARD", "ВЫБЕРИТЕ НАГРАДУ" },
            { "ENTER THE PORTAL", "ВОЙДИТЕ В ПОРТАЛ" },
            { "CLEAR THE AREA", "ЗАЧИСТИТЕ ЗОНУ" },
            { "ACTIVATE THE SPHERE", "АКТИВИРУЙТЕ СФЕРУ" },
            { "DEFEND THE SPHERE", "ЗАЩИЩАЙТЕ СФЕРУ" },
            { "RESCUE STAFF", "СПАСИТЕ ПЕРСОНАЛ" },
            { "CLOSE RIFTS", "ЗАКРОЙТЕ РАЗЛОМЫ" },
            { "DESTROY THE CORRUPTED SPHERE", "РАЗРУШЬТЕ ИСКАЖЕННУЮ СФЕРУ" },
            { "KILL BOTH BOSSES", "УНИЧТОЖЬТЕ ОБОИХ БОССОВ" },
            { "RESTORE THE SPHERE", "ВОССТАНОВИТЕ СФЕРУ" },
            { "UNIVERSE COMPLETE", "ВСЕЛЕННАЯ ЗАВЕРШЕНА" },
            { "OBJECTIVE FAILED", "ЗАДАНИЕ ПРОВАЛЕНО" },
            { "PRESS R TO RETRY", "НАЖМИТЕ R, ЧТОБЫ ПОВТОРИТЬ" },
            { "PRESS 1 / 2 / 3", "НАЖМИТЕ 1 / 2 / 3" },
            { "ALL REQUIRED TURRETS ONLINE", "ВСЕ НУЖНЫЕ ТУРЕЛИ ГОТОВЫ" },
            { "HOLD POSITION AT THE CORE", "ДЕРЖИТЕ ПОЗИЦИЮ У ЯДРА" },
            { "AREA CLEARED", "ЗОНА ЗАЧИЩЕНА" },
            { "NEXT PLANET ONLINE", "СЛЕДУЮЩАЯ ПЛАНЕТА НА СВЯЗИ" },
            { "ENTER NEXT UNIVERSE", "ВОЙДИТЕ В СЛЕДУЮЩУЮ ВСЕЛЕННУЮ" },
            { "PORTAL CHARGING - BOTH LANES", "Портал заряжается — обе линии" },
            { "PORTAL READY", "Портал готов" },
            { "E ACTIVATE SPHERE", "E активировать сферу" },
            { "SPHERE READY", "Сфера готова" },
            { "SPHERE ACTIVATED", "Сфера активирована" },
            { "BOSSES DEFEATED", "Боссы побеждены" },
            { "RIFT CLOSED", "Разлом закрыт" },
            { "SPHERE RESTORED", "Сфера восстановлена" },
            { "BOSS HEAL", "ЛЕЧЕНИЕ БОССА" },
            { "PLANET 2: CRYSTAL MOON", "Планета 2 — Crystal Moon" },
            { "PLANET 3 - ASH RELAY STATION", "Планета 3 — Ash Relay Station" },
            { "PLANET 4 - SWARM EXPANSE: KILL THE SWARM", "Планета 4 — Swarm Expanse: уничтожьте рой" },
            { "PLANET 5 - CRIMSON SINGULARITY", "Планета 5 — Crimson Singularity" },
            { "PLANET CLEARED - ASH RELAY STATION ONLINE", "Планета очищена — Ash Relay Station на связи" },
            { "SPHERE LOST - PRESS R TO RETRY", "Сфера потеряна — нажмите R" },
            { "RELAY SPHERE DESTROYED - PRESS R TO RETRY", "Сфера реле уничтожена — нажмите R" },
            { "PLANET 3 CLEARED - STAFF RESCUED - RELAY STABILIZED", "Планета 3 очищена — персонал спасен, реле стабилизировано" },
            { "PLANET 5 PORTAL OPEN - ENTER THE FINAL WORLD", "Портал планеты 5 открыт — войдите в финальный мир" },
            { "PLANET 5 PORTAL OPEN", "Портал планеты 5 открыт" },
            { "DESTROY THE HEALING SPHERE - BOSS HEALING ACTIVE", "Разрушьте сферу исцеления — боссы лечатся" },
            { "HEALING DISABLED - KILL THE BOSSES", "Исцеление отключено — убейте боссов" },
            { "BOSSES DEFEATED - RESTORE THE SPHERE", "Боссы побеждены — восстановите сферу" },
            { "STABILIZERS ONLINE - HOLD E TO STABILIZE", "Стабилизаторы готовы — удерживайте E" },
            { "SPHERE RESTORED - UNIVERSE STABILIZED", "Сфера восстановлена — вселенная стабилизирована" },
            { "RELAY SPHERE READY - HOLD E TO ACTIVATE", "Сфера реле готова — удерживайте E" },
            { "DEFEND RELAY SPHERE", "Защищайте сферу реле" },
            { "RELAY ONLINE +350 COINS - BUILD DEFENSES", "Реле включено +350 монет — стройте оборону" },
            { "PLANET 3 MAP MISSING", "Карта планеты 3 не найдена" },
            { "PLANET 4 MAP MISSING", "Карта планеты 4 не найдена" },
            { "PLANET 5 MAP MISSING", "Карта планеты 5 не найдена" },
            { "PLANET 5 MAP NOT BUILT YET", "Карта планеты 5 еще не построена" },
            { "+15% PLAYER WEAPON DAMAGE", "+15% урон оружия игрока" },
            { "+15% TURRET DAMAGE", "+15% урон турелей" },
            { "CORE REPAIRED +150", "Ядро отремонтировано +150" },
            { "HEAL +40", "Лечение +40" },
            { "ARMOR +30% 30S", "Броня +30% 30 с" },
            { "WEAPON DAMAGE +20% 30S", "Урон оружия +20% 30 с" },
            { "TURRET DAMAGE +20% 30S", "Урон турелей +20% 30 с" }
        };

        public static PortalNightsLanguage CurrentLanguage { get; private set; } = PortalNightsLanguage.Russian;

        public static void SetLanguage(PortalNightsLanguage language)
        {
            CurrentLanguage = language;
        }

        public static string Text(string key)
        {
            Dictionary<string, string> table = CurrentLanguage == PortalNightsLanguage.Russian ? Ru : En;
            if (table.TryGetValue(key, out string value))
            {
                return value;
            }

            return En.TryGetValue(key, out string fallback) ? fallback : key;
        }

        public static string Format(string key, params object[] args)
        {
            string template = Text(key);
            return args == null || args.Length == 0 ? template : string.Format(template, args);
        }

        public static string StateText(PortalNightsGameState state)
        {
            return Text("state." + state);
        }

        public static string LocalizeRuntimeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || CurrentLanguage == PortalNightsLanguage.English)
            {
                return value ?? string.Empty;
            }

            string trimmed = value.Trim();
            if (LegacyRu.TryGetValue(trimmed, out string exact))
            {
                return exact;
            }

            if (trimmed.StartsWith("COINS +", System.StringComparison.OrdinalIgnoreCase))
            {
                return "МОНЕТЫ +" + trimmed.Substring("COINS +".Length);
            }

            if (trimmed.StartsWith("WAVE ", System.StringComparison.OrdinalIgnoreCase) && trimmed.Contains(" REWARD - PRESS 1 / 2 / 3"))
            {
                string wave = trimmed.Substring("WAVE ".Length, trimmed.IndexOf(" REWARD", System.StringComparison.Ordinal) - "WAVE ".Length);
                return Format("toast.rewardChoice", wave);
            }

            if (trimmed.StartsWith("UNIVERSE ", System.StringComparison.OrdinalIgnoreCase) && trimmed.Contains(" - ENEMIES EMPOWERED - PLANET 1"))
            {
                string universe = trimmed.Substring("UNIVERSE ".Length, trimmed.IndexOf(" - ", System.StringComparison.Ordinal) - "UNIVERSE ".Length);
                return Format("toast.universeEntered", universe);
            }

            if (trimmed.StartsWith("WAVE ", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace("WAVE", "ВОЛНА").Replace("NEXT WAVE IN", "СЛЕДУЮЩАЯ ВОЛНА ЧЕРЕЗ");
            }

            if (trimmed.StartsWith("ENEMIES:", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace("ENEMIES:", "ВРАГИ:");
            }

            if (trimmed.StartsWith("STAFF AT SPHERE:", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace("STAFF AT SPHERE:", "ПЕРСОНАЛ У СФЕРЫ:");
            }

            if (trimmed.StartsWith("RIFTS CLOSED:", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace("RIFTS CLOSED:", "РАЗЛОМЫ ЗАКРЫТЫ:");
            }

            if (trimmed.StartsWith("SPHERE HP:", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace("SPHERE HP:", "СФЕРА:");
            }

            if (trimmed.EndsWith(" RELEASED - ESCORT TO RELAY", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace(" RELEASED - ESCORT TO RELAY", " ОСВОБОЖДЕН — ВЕДИТЕ К РЕЛЕ");
            }

            if (trimmed.EndsWith(" REVIVED", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace(" REVIVED", " В СТРОЮ");
            }

            if (trimmed.EndsWith(" AT RELAY SPHERE", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace(" AT RELAY SPHERE", " У СФЕРЫ РЕЛЕ");
            }

            if (trimmed.EndsWith(" WEAK - HOLD E TO CLOSE", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace(" WEAK - HOLD E TO CLOSE", " ОСЛАБЛЕН — УДЕРЖИВАЙТЕ E");
            }

            if (trimmed.EndsWith(" CLOSED", System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Replace(" CLOSED", " ЗАКРЫТ");
            }

            if (trimmed.StartsWith("THE SPHERE SAVED ", System.StringComparison.OrdinalIgnoreCase))
            {
                return "СФЕРА СПАСЛА " + trimmed.Substring("THE SPHERE SAVED ".Length);
            }

            if (trimmed.StartsWith("LVL ", System.StringComparison.OrdinalIgnoreCase))
            {
                return "УР. " + trimmed.Substring("LVL ".Length);
            }

            if (trimmed.Contains(" ONLINE "))
            {
                return trimmed.Replace(" ONLINE ", " ГОТОВ ");
            }

            if (trimmed.Contains("HP x") && trimmed.Contains("DMG x") && trimmed.Contains("SCORE x"))
            {
                return trimmed.Replace("HP x", "ОЗ x").Replace("DMG x", "УРОН x").Replace("SCORE x", "СЧЕТ x");
            }

            return trimmed;
        }
    }
}
