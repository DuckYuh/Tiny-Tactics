# BÁO CÁO KỸ THUẬT: HỆ THỐNG SẢN SINH QUÂN ĐỘI TẠI NHÀ LÍNH (BARRACKS PRODUCTION SYSTEM)
**Dự án:** Tiny Tactics (Sprint 3 - DACN)  
**Tiêu chuẩn:** RTS Gameplay Systems & Architecture  
**Tác giả:** Senior Gameplay Systems & RTS AI Engineer  

---

## 1. TỔNG QUAN HỆ THỐNG
Hệ thống **Sản sinh quân đội tại Nhà lính (Barracks)** cung cấp cơ chế huấn luyện, quản lý hàng đợi và triển khai quân đội thời gian thực (RTS) cho 3 loại binh chủng chiến thuật:
1. **Binh sĩ (Warrior - Tanker)**: Tiên phong, chịu đòn và che chắn.
2. **Cung thủ (Archer - Đánh xa)**: Sát thương tầm xa, rỉa máu từ phía sau.
3. **Lính giáo (Lancer - Cơ động/Sát thương)**: Tốc độ cao, xung kích và quấy rối.

Hệ thống được thiết kế theo mô hình hướng dữ liệu (**Data-Driven Design**) với `ScriptableObject`, đồng bộ chặt chẽ với:
- [ResourceManager](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Scripts/Economy/ResourceManager.cs): Khấu trừ và hoàn trả tài nguyên tức thời.
- [PopulationManager](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Scripts/Buildings/PopulationManager.cs): Đăng ký và giải phóng dân số tức thời.
- [BuildingBase](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Scripts/Buildings/BuildingBase.cs): Kế thừa kết cấu công trình, quản lý máu và điểm tập kết Rally Point.
- [UnitController](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Scripts/Units/UnitController.cs): Ra lệnh di chuyển tự động ra Rally Point với cơ chế né tránh đè lấn (Separation Steering).

> [!NOTE]
> Tuyệt đối giữ nguyên vẹn các tệp lõi Navigation và Movement: `PathfindingGrid.cs`, `Pathfinding.cs`, `UnitController.cs`, `RTSUnitManager.cs`.

---

## 2. BẢNG THÔNG SỐ BINH CHỦNG (CHUẨN GDD SPRINT 3)

| Binh chủng | Vai trò | Thịt (Meat) | Gỗ (Wood) | Vàng (Gold) | Thời gian luyện (s) | Dân số (Pop) | Speed | Prefab & SO Asset |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **Binh sĩ (Warrior)** | Tanker | **60** | 0 | 0 | **5.0s** | **1** | 3.5 | [SO_Warrior.asset](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Data/Units/SO_Warrior.asset) |
| **Cung thủ (Archer)** | Đánh xa | **40** | **30** | 0 | **7.0s** | **1** | 3.2 | [SO_Archer.asset](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Data/Units/SO_Archer.asset) |
| **Lính giáo (Lancer)** | Cơ động / Sát thương | **50** | 0 | **20** | **6.0s** | **1** | 4.2 | [SO_Lancer.asset](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Data/Units/SO_Lancer.asset) |

---

## 3. KIẾN TRÚC MÃ NGUỒN C#

### 3.1. `UnitDataSO.cs`
- **Đường dẫn:** [UnitDataSO.cs](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Scripts/ScriptableObjects/Units/UnitDataSO.cs)
- **Mục đích:** `ScriptableObject` lưu trữ cấu hình tĩnh của từng đơn vị lính.
- **Trường dữ liệu & Thuộc tính:**
  - `string unitName`: Tên hiển thị đơn vị lính.
  - `GameObject unitPrefab`: Prefab quân lính hoàn chỉnh (chứa `UnitController`, `CircleCollider2D`, `Animator`, `SelectableUnit`, `MinimapTarget`).
  - `Sprite unitIcon`: Icon đại diện phục vụ UI huấn luyện.
  - `int meatCost`, `int woodCost`, `int goldCost`: Chi phí tài nguyên cần để huấn luyện.
  - `float trainTime`: Thời gian hồi chiêu huấn luyện (Cooldown).
  - `int populationCost`: Chi phí dân số chiếm dụng (mặc định = 1).
- **Phương thức tiện ích:**
  - `public bool CanAfford()`: Kiểm tra nhanh tài nguyên hiện có qua `ResourceManager.Instance.HasEnough()`.

### 3.2. `BarracksBuilding.cs`
- **Đường dẫn:** [BarracksBuilding.cs](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Scripts/Buildings/BarracksBuilding.cs)
- **Kế thừa:** [BuildingBase](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Scripts/Buildings/BuildingBase.cs)
- **Quy trình hoạt động:**
  1. **Thêm vào hàng đợi (`TryQueueUnit`):**
     - Kiểm tra kích thước hàng đợi: `_trainingQueue.Count < maxQueueSize` (mặc định 5).
     - Kiểm tra khả năng chi trả tài nguyên: `ResourceManager.Instance.HasEnough(meat, wood, gold)`.
     - Kiểm tra giới hạn dân số: `PopulationManager.Instance.CanSpawnUnit(populationCost)`.
     - Trừ tài nguyên ngay lập tức thông qua `ResourceManager.Instance.SpendResource()`.
     - Khóa dân số tức thời thông qua `PopulationManager.Instance.RegisterUnit()`.
     - Bổ sung đơn vị vào danh sách `_trainingQueue` và kích hoạt sự kiện `OnQueueUpdated`.
  2. **Vòng lặp huấn luyện (`ProcessTrainingQueue`):**
     - Đơn vị ở vị trí số `0` sẽ được tiến hành huấn luyện.
     - `_currentTimer` đếm ngược từ `trainTime` về 0 dựa trên `Time.deltaTime`.
     - Tỷ lệ hoàn thành `Progress = 1f - (_currentTimer / _totalTimer)` được phát thanh qua Event `OnTrainingProgressChanged`.
  3. **Khởi tạo và điều hướng lính (`SpawnUnit`):**
     - Khi `_currentTimer <= 0f`: Thực hiện `Instantiate` Prefab tại tọa độ `spawnPoint.position`.
     - Lấy component `UnitController` trên lính mới sinh và kích hoạt lệnh `MoveTo(rallyPointPos)`.
     - Thuật toán Separation trong `UnitController` đảm bảo lính mới không bị kẹt hay đè lên nhau kể cả khi xuất hiện cùng một vị trí.
     - Xóa đơn vị khỏi hàng đợi và chuẩn bị huấn luyện đơn vị kế tiếp.
  4. **Hủy hàng đợi & Hoàn tiền (`CancelQueue`):**
     - Hủy đơn vị theo chỉ số `index` trong hàng đợi.
     - Hoàn trả **100% tài nguyên** đã trừ (`meatCost`, `woodCost`, `goldCost`) về kho thông qua `ResourceManager.Instance.AddResource()`.
     - Giải phóng dân số đã giữ chỗ qua `PopulationManager.Instance.UnregisterUnit()`.
     - Nếu hủy đơn vị đang chạy ở vị trí `0`, đồng hồ `_currentTimer` sẽ lập tức reset cho đơn vị kế tiếp.
  5. **Bảo vệ hủy công trình (`OnDestroyed` & `OnDestroy`):**
     - Khi Nhà lính bị đánh sập hoặc bị xóa, phương thức `ClearAndRefundAllQueue()` tự động hoàn trả toàn bộ tài nguyên và dân số của mọi đơn vị còn kẹt trong hàng đợi.

---

## 4. CHI TIẾT TÍCH HỢP TÀI NGUYÊN & PREFAB

### 4.1. Prefab Quân đội (`Assets/_Project/Prefabs/Units/`)
Đã hoàn tất cấu hình 3 Prefab lính Blue với đầy đủ Animator và Sprite từ Tiny Swords:
1. [Warrior_Blue.prefab](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Prefabs/Units/Warrior_Blue.prefab):
   - Sprite: `Warrior_Idle_0`
   - Animator: `Warrior_Blue.controller`
   - Tốc độ di chuyển (`moveSpeed`): 3.5
2. [Archer_Blue.prefab](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Prefabs/Units/Archer_Blue.prefab):
   - Sprite: `Archer_Idle_0`
   - Animator: `Archer_Blue.controller`
   - Tốc độ di chuyển (`moveSpeed`): 3.2
3. [Lancer_Blue.prefab](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Prefabs/Units/Lancer_Blue.prefab):
   - Sprite: `Lancer_Idle_0`
   - Animator: `Lancer_Blue.controller`
   - Tốc độ di chuyển (`moveSpeed`): 4.2

### 4.2. Prefab Nhà Lính (`Assets/_Project/Prefabs/Buildings/`)
Đã gán và liên kết tự động `BarracksBuilding` vào:
- [BlueBarracks.prefab](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Prefabs/Buildings/BlueBarracks.prefab)
- [RedBarracks.prefab](file:///d:/DACN/Tiny-Tactics/Assets/_Project/Prefabs/Buildings/RedBarracks.prefab)
Các Prefab đã được cấu hình danh sách `trainableUnits` trỏ trực tiếp đến 3 `ScriptableObject`: `SO_Warrior`, `SO_Archer`, `SO_Lancer`.

---

## 5. HƯỚNG DẪN TEST TRÊN UNITY EDITOR
1. **Mở Scene chính:** `Main_CoreGameplay.unity`.
2. **Kiểm tra Inspector của BlueBarracks:**
   - Trường `Trainable Units` hiển thị đủ 3 SO: `SO_Warrior`, `SO_Archer`, `SO_Lancer`.
   - `Spawn Point` và `Rally Point` đã được gán hoặc tự động xác định ở mép dưới công trình.
3. **Thực thi thử nghiệm qua Script / Console / UI:**
   - Gọi `barracks.TryQueueUnit(barracks.TrainableUnits[0])` để xin đẻ lính Warrior:
     - Thịt bị trừ 60 đơn vị.
     - `CurrentPopulation` tăng lên 1/5.
     - Thanh Cooldown chạy trong 5 giây.
     - Sau 5 giây, lính Warrior xuất hiện tại điểm Spawn và tự động rảo bước tới Rally Point.
   - Thử nghiệm bấm đẻ 5 lính liên tiếp để kiểm tra `maxQueueSize = 5`.
   - Thử nghiệm gọi `CancelQueue(0)` để kiểm tra việc hoàn tiền 100% và trả lại 1 dân số.
