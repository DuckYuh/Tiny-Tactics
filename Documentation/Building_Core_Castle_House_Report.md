# Báo cáo Kỹ thuật — Logic Công trình (Core): Lâu đài (Castle) & Nhà dân (House)

> **Dự án:** Tiny Tactics (AoE Mini) — RTS 2D Pixel Art trên Unity  
> **Sprint:** 3 — Đồ án Chuyên ngành (DACN)  
> **Module triển khai:** `Assets/_Project/Scripts/Buildings/`, `Assets/_Project/Scripts/Core/`, `Assets/_Project/Prefabs/Buildings/`  
> **Nguyên tắc bất biến:** Không thay đổi các module lõi đã hoàn thiện (`PathfindingGrid.cs`, `Pathfinding.cs`, `UnitController.cs`, `RTSUnitManager.cs`).

---

## 1. Tổng quan Kiến trúc

Hệ thống công trình cốt lõi được thiết kế theo mô hình Hướng đối tượng phân tầng (Layered OOP), kết hợp các Singleton Service quản lý tài nguyên & dân số, tách biệt giữa dữ liệu logic, vòng đời công trình và hiển thị UI/Visual.

```text
                               ┌────────────────────────────────────────┐
                               │            GameManager                 │
                               │  - OnPlayerDefeated (Defeat Event)     │
                               │  - OnPlayerVictory  (Victory Event)    │
                               └───────────────────▲────────────────────┘
                                                   │ Lâu đài Player sập (HP=0)
┌────────────────────────────────────────┐         │
│          PopulationManager             │         │
│  - CurrentPopulation / MaxPopulation   │         │
│  - OnPopulationChanged (HUD "3/10")    │         │
└──────▲──────────────────────────▲──────┘         │
       │ +5 MaxPop (Lúc khởi đầu) │ +5 MaxPop      │
       │ -5 MaxPop (Khi bị phá)   │ -5 (Khi phá)   │
       │                          │                │
┌──────┴──────────────┐   ┌───────┴─────────────┐  │
│    CastleBuilding   │   │    HouseBuilding    │  │
│  - HP: 2500         │   │  - HP: 600          │  │
│  - Size: 3x3        │   │  - Size: 2x2        │  │
│  - Drop-off Point   │   │  - Dân sự           │  │
└──────────┬──────────┘   └──────────┬──────────┘  │
           │                         │             │
           └────────────┬────────────┘             │
                        ▼                          │
               ┌───────────────────┐               │
               │   BuildingBase    ├───────────────┘
               │  - TakeDamage()   │
               │  - OnDestroyed()  │──► PathfindingGrid.UpdateNodeWalkable(true)
               │  - RallyPoint     │──► RTSCameraController.Shake()
               └───────────────────┘
```

---

## 2. Chi tiết các thành phần đã triển khai

### 2.1. `PopulationManager.cs` (`Assets/_Project/Scripts/Buildings/PopulationManager.cs`)
* **Vai trò:** Quản lý sức chứa và số lượng dân số hiện hữu của người chơi Blue.
* **Cơ chế Singleton & Auto-creation:** Tự động tìm kiếm trong Scene hoặc sinh `[PopulationManager]` nếu chưa có, ngăn ngừa triệt để lỗi NullReferenceException.
* **Chỉ số quản lý:**
  * `CurrentPopulation`: Số lượng quân đội / nông dân đang sống.
  * `MaxPopulation`: Ngưỡng giới hạn sức chứa hiện tại (cung cấp bởi Castle + House).
  * `hardCap`: Trần dân số tối đa toàn game (mặc định 200).
* **Sự kiện giao tiếp:**
  * `public static event Action<int, int> OnPopulationChanged;` phát ra mỗi khi `CurrentPopulation` hoặc `MaxPopulation` biến động, cho phép các UI HUD đăng ký hiển thị định dạng `"3/10"`.
* **API điều phối:**
  * `AddMaxPopulation(int amount)`: Cộng dồn sức chứa.
  * `RemoveMaxPopulation(int amount)`: Trừ sức chứa (chặn không cho nhỏ hơn 0).
  * `CanSpawnUnit(int cost = 1)`: Kiểm tra điều kiện `CurrentPopulation + cost <= MaxPopulation`.
  * `RegisterUnit(int cost = 1)` & `UnregisterUnit(int cost = 1)`: Quản lý vòng đời sinh/tử của Unit.

---

### 2.2. `BuildingBase.cs` (`Assets/_Project/Scripts/Buildings/BuildingBase.cs`)
* **Vai trò:** Lớp trừu tượng (abstract class) cơ sở định nghĩa mọi thuộc tính và hành vi chung của công trình.
* **Thuộc tính Inspector:**
  * `buildingName`: Tên công trình (Castle, House, Barracks,...).
  * `factionId`: Phe sở hữu (`"Blue"` hoặc `"Red"`).
  * `maxHP`, `currentHP`: Chỉ số độ bền công trình.
  * `buildingGridSize`: Kích thước footprint (Castle: 3x3, House: 2x2).
  * `rallyPoint`: Điểm tập kết quân khi sinh ra (`GetRallyPointPosition()`).
  * `cameraShakeDuration`, `cameraShakeMagnitude`: Thông số hiệu ứng rung màn hình khi công trình sập.
* **Logic nhận sát thương & Hủy diệt:**
  * `TakeDamage(int damage)`: Trừ máu, kích hoạt `OnHealthChanged`. Khi `currentHP <= 0`, lập tức kích hoạt `OnDestroyed()`.
  * `OnDestroyed()`:
    1. Phát sự kiện toàn cục `OnAnyBuildingDestroyed`.
    2. Gọi `ReleaseGridOccupancy()`: Cập nhật trực tiếp `PathfindingGrid.Instance.UpdateNodeWalkable(worldPos, true)` cho tất cả các ô trong footprint, đồng thời thông báo giải phóng qua `BuildingOccupancyManager`.
    3. Gọi `TriggerCollapseEffect()`: Rung màn hình thông qua `RTSCameraController.Shake()` và kích hoạt hiệu ứng sập nhà.
    4. Vô hiệu hóa toàn bộ `Collider2D` và tắt highlight visual để ngăn nhận lệnh mới.
    5. Hủy GameObject an toàn.

---

### 2.3. `CastleBuilding.cs` (`Assets/_Project/Scripts/Buildings/CastleBuilding.cs`)
* **Vai trò:** Căn cứ đầu não (Town Center / Castle).
* **Thuộc tính & Chỉ số:**
  * Kích thước lớn nhất: `3x3` ô Grid.
  * Máu dồi dào nhất: Mặc định `2500 HP`.
* **Khởi tạo dân số:**
  * Ngay lúc `Start()`, nếu là Lâu đài của Player (`FactionId == "Blue"`), tự động gọi:
    `PopulationManager.Instance.AddMaxPopulation(5);`
* **Điểm nộp tài nguyên (Drop-off Point):**
  * Tích hợp `[RequireComponent(typeof(CastleDepositArea))]`.
  * Cung cấp tọa độ giao nộp `GetDropOffPosition()` và bán kính tương tác `dropOffRadius`.
  * Nhận diện Nông dân (`WorkerFSM`) khi tiến lại gần hoặc đi qua Trigger Collider2D.
* **Điều kiện Thắng / Thua (Victory / Defeat):**
  * Override `OnDestroyed()`:
    * Nếu Lâu đài **Player (Blue)** bị phá: Gọi `GameManager.TriggerPlayerDefeated()` -> Thua cuộc.
    * Nếu Lâu đài **Địch (Red)** bị phá: Gọi `GameManager.TriggerPlayerVictory()` -> Chiến thắng.
    * Trừ lại sức chứa dân số `-5` khỏi `PopulationManager`.
    * Kích hoạt rung màn hình mạnh (`duration = 0.5s, magnitude = 0.35`).

---

### 2.4. `HouseBuilding.cs` (`Assets/_Project/Scripts/Buildings/HouseBuilding.cs`)
* **Vai trò:** Công trình dân sự cung cấp nhà ở.
* **Thuộc tính & Chỉ số:**
  * Kích thước: `2x2` (hoặc cấu hình theo Footprint).
  * Máu: Mặc định `600 HP`.
* **Vòng đời Dân số:**
  * **Khi xây dựng xong (`Start()`):** Tự động gọi `PopulationManager.Instance.AddMaxPopulation(5)`.
  * **Khi bị phá hủy (`OnDestroyed()` / `OnDestroy()`):** Lập tức gọi `PopulationManager.Instance.RemoveMaxPopulation(5)` và giải phóng các ô Grid bị chiếm.
  * Tích hợp cờ bảo vệ `_populationAdded` để không bao giờ bị trừ âm hoặc trừ trùng lặp dân số.

---

### 2.5. `GameManager.cs` (`Assets/_Project/Scripts/Core/GameManager.cs`)
* **Vai trò:** Quản lý trạng thái trận đấu.
* **Sự kiện tĩnh:**
  * `public static event Action OnPlayerDefeated;`
  * `public static event Action OnPlayerVictory;`
  * `public static event Action<bool> OnGameOver;`
* **Phương thức điều phối:**
  * `TriggerPlayerDefeated()`: Chuyển cờ `isGameOver = true` và phát tín hiệu thua trận.
  * `TriggerPlayerVictory()`: Chuyển cờ `isGameOver = true` và phát tín hiệu thắng trận.

---

### 2.6. Tối ưu `RTSCameraController.cs` & Sửa lỗi Build Player
* **Sửa lỗi biên dịch nghiêm trọng:** Đã đưa phương thức `MoveToPosition(Vector2 worldPosition)` ra ngoài khối `#if UNITY_EDITOR`. Trước đó, phương thức này bị kẹt trong `#if UNITY_EDITOR`, dẫn đến việc build Standalone Player (.exe) sẽ báo lỗi không tìm thấy định nghĩa `MoveToPosition` từ `MinimapController.cs`.
* **Bổ sung Camera Shake:** Cài đặt phương thức `public void Shake(float duration, float magnitude)` và Coroutine dịch chuyển camera ngẫu nhiên trong `LateUpdate()`, tạo cảm giác chấn động khi công trình sập.

---

## 3. Cập nhật Prefab

Đã gắn và cấu hình đầy đủ component cho 4 Prefab công trình trong dự án:
1. `Assets/_Project/Prefabs/Buildings/BlueCastle.prefab`: Gắn `CastleBuilding` (Faction: Blue, HP: 2500, Grid: 3x3, Pop: +5).
2. `Assets/_Project/Prefabs/Buildings/RedCastle.prefab`: Gắn `CastleBuilding` (Faction: Red, HP: 2500, Grid: 3x3, Pop: +5).
3. `Assets/_Project/Prefabs/Buildings/BlueHouse.prefab`: Gắn `HouseBuilding` (Faction: Blue, HP: 600, Grid: 2x2, Pop: +5).
4. `Assets/_Project/Prefabs/Buildings/RedHouse.prefab`: Gắn `HouseBuilding` (Faction: Red, HP: 600, Grid: 2x2, Pop: +5).

Đồng thời trong `BuildingPlacementService.cs`, đã bổ sung cơ chế kiểm tra tự động gắn component tương ứng khi người chơi xây công trình mới trong thời gian thực.

---

## 4. Hướng dẫn Tích hợp & Kiểm thử

### Kịch bản 1: Kiểm tra Dân số ban đầu
1. Khởi động scene `PvE.unity`.
2. Kiểm tra Console: Xuất hiện log `[CastleBuilding] Lâu đài khởi tạo: Đã cấp +5 Sức chứa tối đa.`
3. Gọi `PopulationManager.Instance.MaxPopulation`: Giá trị = `5`.

### Kịch bản 2: Xây thêm Nhà dân (House)
1. Nhấn phím `B`, chọn xây `House` (tiêu tốn 100 Gỗ).
2. Đặt nhà dân xuống đất hợp lệ.
3. Khi nhà dân xuất hiện: `PopulationManager.Instance.MaxPopulation` lập tức tăng lên `10` (5 + 5).

### Kịch bản 3: Phá hủy Nhà dân
1. Chọn Nhà dân, nhấn phím `Delete` hoặc nút `Demolish`.
2. `PopulationManager.Instance.MaxPopulation` lập tức bị trừ về `5`.
3. Kiểm tra các ô Grid dưới chân nhà dân: Đã chuyển thành `isWalkable = true`, lính có thể đi xuyên qua bình thường.

### Kịch bản 4: Điều kiện Thắng / Thua
1. Gọi `TakeDamage(2500)` hoặc phá hủy `BlueCastle`:
   * Console xuất hiện log: `[CastleBuilding] LÂU ĐÀI CỦA NGƯỜI CHƠI BỊ TIÊU DIỆT! THUA CUỘC!`
   * Bắn sự kiện `GameManager.OnPlayerDefeated`.
   * Camera rung chuyển mạnh (Shake).
2. Ngược lại, nếu tiêu diệt `RedCastle`:
   * Bắn sự kiện `GameManager.OnPlayerVictory`.
