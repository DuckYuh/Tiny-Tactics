using System;
using UnityEngine;

namespace TinyTactics.Combat
{
    /// <summary>
    /// Đại diện phe phái chiến đấu, đồng bộ với chuỗi FactionId trong FactionVisual và BuildingBase.
    /// </summary>
    public enum Faction
    {
        Neutral = 0,
        Blue = 1,
        Red = 2,
        Purple = 3,
        Yellow = 4
    }

    /// <summary>
    /// Tiện ích chuyển đổi qua lại giữa chuỗi FactionId hiện có và enum Faction.
    /// </summary>
    public static class FactionUtility
    {
        public static Faction FromString(string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                return Faction.Neutral;
            }

            if (string.Equals(factionId, "Blue", StringComparison.OrdinalIgnoreCase))
                return Faction.Blue;

            if (string.Equals(factionId, "Red", StringComparison.OrdinalIgnoreCase))
                return Faction.Red;

            if (string.Equals(factionId, "Purple", StringComparison.OrdinalIgnoreCase))
                return Faction.Purple;

            if (string.Equals(factionId, "Yellow", StringComparison.OrdinalIgnoreCase))
                return Faction.Yellow;

            return Faction.Neutral;
        }

        public static string ToFactionId(Faction faction)
        {
            switch (faction)
            {
                case Faction.Blue:
                    return "Blue";
                case Faction.Red:
                    return "Red";
                case Faction.Purple:
                    return "Purple";
                case Faction.Yellow:
                    return "Yellow";
                default:
                    return "Neutral";
            }
        }
    }

    /// <summary>
    /// Interface chuẩn dùng chung cho mọi thực thể có thể nhận sát thương (Unit và BuildingBase).
    /// </summary>
    public interface IDamageable
    {
        int CurrentHP { get; }
        int MaxHP { get; }
        bool IsDead { get; }
        Faction TeamFaction { get; }

        void TakeDamage(int rawDamage, GameObject attacker = null);
    }
}
