
using UnityEngine;
using TinyTactics.Units;
using TinyTactics.Economy;

namespace TinyTactics.Audio
{
    /// <summary>
    /// Lắng nghe sự kiện nông dân khai thác tài nguyên
    /// và phát SFX tương ứng.
    ///
    /// Không can thiệp vào WorkerFSM hoặc Economy Logic.
    /// </summary>
    public sealed class ResourceHarvestSFX : MonoBehaviour
    {
        private void OnEnable()
        {
            WorkerFSM.OnAnyResourceHarvested += HandleResourceHarvested;
        }

        private void OnDisable()
        {
            WorkerFSM.OnAnyResourceHarvested -= HandleResourceHarvested;
        }

        private void HandleResourceHarvested(
            ResourceType resourceType,
            Vector3 worldPosition)
        {
            SFXType soundType;

            switch (resourceType)
            {
                case ResourceType.Wood:
                    soundType = SFXType.WoodChop;
                    break;

                case ResourceType.Gold:
                    soundType = SFXType.GoldMine;
                    break;

                case ResourceType.Meat:
                    soundType = SFXType.FoodGather;
                    break;

                default:
                    return;
            }

            SFXManager.Instance?.PlayAtPosition(
                soundType,
                worldPosition
            );
        }
    }
}
