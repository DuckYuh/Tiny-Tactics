using UnityEngine;
using TinyTactics.Combat;

namespace TinyTactics.Factions
{
    [DisallowMultipleComponent]
    public sealed class UnitFaction : MonoBehaviour
    {
        [SerializeField] private Faction faction = Faction.Blue;

        public Faction TeamFaction => faction;

        public void SetFaction(Faction newFaction)
        {
            faction = newFaction;
        }

        public bool IsEnemy(UnitFaction other)
        {
            return other != null &&
                   faction != Faction.Neutral &&
                   other.faction != Faction.Neutral &&
                   faction != other.faction;
        }
    }
}