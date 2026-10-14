using System;
using UnityEngine;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Service tạo công trình dùng chung cho Player Blue và AI Red.
    /// Không phụ thuộc Input hoặc UI.
    /// </summary>
    public static class BuildingPlacementService
    {
        public static bool TryPlace(
            BuildingData data,
            string factionId,
            Vector3 anchorWorldPosition,
            Func<int, bool> trySpendWood,
            out GameObject buildingInstance)
        {
            buildingInstance = null;

            if (data == null ||
                !data.IsValid() ||
                string.IsNullOrWhiteSpace(factionId) ||
                trySpendWood == null)
            {
                return false;
            }

            BuildingOccupancyManager occupancy =
                BuildingOccupancyManager.Instance;

            if (occupancy == null)
                return false;

            GameObject prefab = data.GetPrefab(factionId);

            if (prefab == null)
                return false;

            // Kiểm tra vị trí, Grid và Unit.
            if (!BuildingPlacementValidator.TryValidatePlacement(
                    data,
                    anchorWorldPosition,
                    out Vector3 snappedPosition,
                    out _))
            {
                return false;
            }

            // Tạo công trình tại tâm footprint.
            GameObject instance = UnityEngine.Object.Instantiate(
                prefab,
                snappedPosition,
                prefab.transform.rotation);

            // Đăng ký các ô Grid mà công trình chiếm.
            if (!occupancy.TryOccupy(
                    instance,
                    data,
                    anchorWorldPosition))
            {
                UnityEngine.Object.Destroy(instance);
                return false;
            }

            // Tiêu hao tài nguyên của đúng phe.
            bool paid = false;

            try
            {
                paid = trySpendWood(data.WoodCost);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (!paid)
            {
                // Thanh toán thất bại:
                // Hoàn tác Grid rồi xóa công trình.
                occupancy.Release(instance);
                UnityEngine.Object.Destroy(instance);
                return false;
            }

            // Thêm hoặc lấy component quản lý vòng đời.
            BuildingInstance lifecycle =
                instance.GetComponent<BuildingInstance>();

            if (lifecycle == null)
                lifecycle = instance.AddComponent<BuildingInstance>();

            lifecycle.Initialize(data, factionId);

            // Bổ sung hiệu ứng chọn cho công trình.
            // Chỉ thêm nếu prefab chưa có component.
            if (instance.GetComponent<BuildingSelectionVisual>() == null)
            {
                instance.AddComponent<BuildingSelectionVisual>();
            }

            buildingInstance = instance;

            Debug.Log(
                $"[BuildingPlacement] {factionId} xây " +
                $"{data.BuildingName} thành công.");

            return true;
        }
    }
}