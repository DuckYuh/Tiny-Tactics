# Hướng dẫn Cấu hình Unity Inspector — Fog of War, Tower & PvE Enemy AI

Tài liệu hợp nhất quy trình thiết lập trên **Unity Editor / Inspector** cho **Bước 4** (`FogOfWarManager`, `VisionProvider`, `FogVisibilityTarget`, `TowerBuilding`) và **Bước 5** (Kinh tế/Dân số theo phe, Căn cứ Red, `EnemyAIController` FSM, Thắng/Thua) trong scene `Assets/_Project/Scenes/Gameplay/PvE.unity`.

---

## Phần 1: Thiết lập Sương mù (`FogOfWarManager`) trên Scene `PvE.unity`

### 1.1. Gắn và cấu hình `FogOfWarManager`
1. Mở `Assets/_Project/Scenes/Gameplay/PvE.unity`.
2. Chọn `Managers` (hoặc tạo GameObject mới tên `FogOfWarManager`), nhấn **Add Component → `FogOfWarManager`** (`TinyTactics.FogOfWar.FogOfWarManager`):
   - **`Player & Timing`**:
     - `Local Player Faction`: `Blue`
     - `Update Interval`: `0.1` *(cập nhật 10 lần/giây)*
   - **`Fog Colors`**:
     - `Unexplored Color`: RGBA `(0, 0, 0, 1)` *(đen đặc — chưa khám phá)*
     - `Explored Color`: RGBA `(0, 0, 0, 0.55)` *(xám mờ — đã đi qua)*
     - `Visible Color`: RGBA `(0, 0, 0, 0)` *(trong suốt — đang nhìn thấy)*
   - **`Renderer Settings`**:
     - `Fog Sprite Renderer`: Để trống (script tự tạo child `FogOfWarOverlay` khớp đúng kích thước `100x100`, `Cell Size = 1`, `Origin = (0, 0, 0)` của `PathfindingGrid`).
     - `Filter Mode`: `Bilinear` *(biên sương mù mềm)* hoặc `Point` *(ô vuông pixel-art)*.
     - `Sorting Order`: `50` *(trong `PvE.unity`, địa hình nằm ở `0..4`, Unit ở `10`, Building ở `11`, Projectile ở `15`, nên `50` phủ lên toàn bộ thế giới 2D)*.
     - `Fog Layer Name`: `FogOfWar` *(Layer 12, đã khai báo sẵn trong `TagManager.asset`)*.

### 1.2. Đảm bảo không che mất UI HUD và Minimap
1. **UI HUD:** Kiểm tra các Canvas (`ResourceHUD`, `BuildingMenuUI`, `GameOverPanel`, `PauseMenuPanel`, Minimap UI) đang dùng **`Render Mode = Screen Space - Overlay`** (mặc định của scene `PvE.unity`). Nếu có Canvas dùng `Screen Space - Camera`, đặt `Order in Layer >= 100`.
2. **Minimap Camera:**
   - Trong Hierarchy của `PvE.unity`, chọn **`MinimapCamera`**.
   - Tại **`Culling Mask`**: **bỏ tick Layer `FogOfWar`** (Layer 12) để Minimap vẫn hiển thị rõ địa hình bản đồ mà không bị lớp phủ đen che mất.
   - *(Nếu muốn Minimap cũng tối theo sương mù thì giữ nguyên tick `FogOfWar` trên `MinimapCamera`.)*

---

## Phần 2: Cấu hình Tầm nhìn (`VisionProvider`), Ẩn/Hiện địch & Tháp canh (`Tower`)

### 2.1. Prefab Unit (`Pawn_Blue`, `Warrior_Blue`, `Archer_Blue`, `Lancer_Blue`, `Monk_Blue`)
Mở từng Prefab trong `Assets/_Project/Prefabs/Units/`:
1. **Add Component → `VisionProvider`** (`TinyTactics.FogOfWar.VisionProvider`):
   - `Sync From Unit Data`: **Bật (`true`)** để tự lấy `VisionRange` từ `UnitDataSO` (`Pawn = 6`, `Warrior = 7`, `Archer = 8`, `Lancer = 7`, `Monk = 7`).
   - `Owner Faction`: `Blue` *(khi Unit được sinh cho phe Red qua `FactionVisual`/`HealthSystem`, `VisionProvider` tự nhận diện phe Red nên không mở sương mù của người chơi)*.
2. **Add Component → `FogVisibilityTarget`** (`TinyTactics.FogOfWar.FogVisibilityTarget`):
   - `Visible In Explored Fog`: **Tắt (`false`)** *(Unit địch chỉ hiện khi nằm trong vùng sáng `2`)*.
   - `Hide Minimap Marker In Fog`: **Bật (`true`)** *(ẩn chấm đỏ địch trên Minimap khi địch đứng trong vùng đen/xám)*.
   - *(An toàn cho Prefab dùng chung cả Blue và Red vì `FogVisibilityTarget` tự giữ hiển thị nếu đối tượng thuộc `Local Player Faction = Blue`.)*

### 2.2. Công trình phe Blue (`BlueCastle`, `BlueBarracks`, `BlueArchery`, `BlueMonastery`, `BlueHouse`)
Mở các Prefab trong `Assets/_Project/Prefabs/Buildings/`:
- **Add Component → `VisionProvider`**:
  - `Sync From Unit Data`: Tắt (`false`).
  - `Vision Radius`: `10` cho `BlueCastle`, `8` cho `BlueBarracks` / `BlueArchery` / `BlueMonastery` / `BlueHouse`.

### 2.3. Công trình Tháp canh (`BlueTower.prefab` & `RedTower.prefab`)
1. Mở **`BlueTower.prefab`** và **`RedTower.prefab`** (`Assets/_Project/Prefabs/Buildings/`):
   - Đặt **Layer = `Building`** (Layer 8) và kiểm tra `BoxCollider2D` đang bật.
   - **Add Component → `TowerBuilding`** (`TinyTactics.Buildings.TowerBuilding`):
     - `Faction Id`: `Blue` (hoặc `Red`).
     - `Vision Radius`: `11` *(tầm nhìn rộng xuyên sương mù để phát hiện địch từ xa)*.
     - Script tự động thêm/đồng bộ `VisionProvider` với bán kính `11f`.
2. Kiểm tra **`Assets/_Project/Data/Buildings/TowerData.asset`** đã gắn đúng `BlueTower` / `RedTower` (`Wood Cost = 100`, `Footprint = 1x2`).

### 2.4. Công trình phe Địch (`RedCastle`, `RedBarracks`, `RedArchery`, `RedMonastery`, `RedHouse`, `RedTower`)
- Trên tất cả Prefab công trình phe `Red`, **Add Component → `FogVisibilityTarget`**:
  - `Visible In Explored Fog`: **Bật (`true`)** *(công trình địch sau khi đã trinh sát qua vẫn giữ bóng mờ dưới lớp xám `Explored`)*.
  - `Hide Minimap Marker In Fog`: Tắt (`false`) nếu muốn giữ biểu tượng nhà địch đã phát hiện trên Minimap, hoặc Bật (`true`) nếu chỉ hiện khi đang thấy.

---

## Phần 3: Cấu hình Kinh tế & Dân số Độc lập cho AI

1. Trong Hierarchy của `PvE.unity`, chọn GameObject chứa **`ResourceManager`** (nằm trong `Managers`):
   - Kiểm tra nhóm **`AI Starting Resources`**:
     - `Ai Starting Meat`: `100`
     - `Ai Starting Wood`: `150`
     - `Ai Starting Gold`: `50`
   - Kho của người chơi (`Blue`) vẫn giữ mặc định `100 Meat / 150 Wood / 50 Gold` và là kho duy nhất phát sự kiện lên `ResourceHUD`.
2. Kiểm tra **`PopulationManager`** có mặt trong `Managers` (tự quản lý riêng sức chứa và dân số hiện tại cho phe `Blue` và phe `Red`).

---

## Phần 4: Dựng Căn cứ phe Red trên Scene `PvE.unity`

### 4.1. Đặt `RedCastle` tại khu vực `EnemySpawn`
1. Kéo **`Assets/_Project/Prefabs/Buildings/RedCastle.prefab`** vào vị trí căn cứ địch (`EnemySpawn`):
   - **Layer:** `Building` (Layer 8), `BoxCollider2D` đang bật.
   - Trên **`CastleBuilding`**:
     - `Faction Id`: **`Red`**
     - `Castle Population Provided`: `5`
     - `Spawn Starting Pawns`: **Bật (`true`)**
     - `Starting Pawn Count`: **`3`** *(tạo 3 nông dân khởi đầu cho AI)*
     - `Pawn Data`: Kéo **`SO_Pawn`**
     - `Pawn Blue Controller` & `Pawn Red Controller`: Kéo Animator Controller của Pawn Blue và Pawn Red (để `ConfigurePawnFaction` đổi đúng màu Red khi sinh Pawn).
   - Tạo child `RallyPoint` đặt trước cửa `RedCastle` và kéo vào ô `Rally Point`.

### 4.2. Đặt Nhà dân & Nhà lính phe Red
1. **Nhà dân (`RedHouse.prefab`):** Đặt **`2–3` nhà `RedHouse`** gần `RedCastle` (`Faction Id = Red`, Layer `Building`).
   - *Lưu ý quan trọng:* Mỗi `House` cộng `+5` sức chứa cho phe Red. Với `RedCastle` (`+5`) + `2 RedHouse` (`+10`) = `15` Max Pop, AI đủ chỗ cho `5` nông dân và đợt quân `6–8` lính (nếu không đặt `RedHouse`, AI sẽ bị kẹt trần dân số ở `5/5`).
2. **Nhà lính (`RedBarracks.prefab` / `RedArchery.prefab`):**
   - Đặt `RedBarracks` (và `RedArchery` nếu muốn) trước căn cứ Red, kiểm tra **Layer = `Building`**.
   - Trên `RedBarracks` (`BarracksBuilding`): `Faction Id = Red`, kéo `SO_Warrior`, `SO_Lancer` (và `SO_Archer` nếu không tách riêng sang `RedArchery`) vào **`Trainable Units`**.
   - Trên `RedArchery` (`ArcheryBuilding`): `Faction Id = Red`, kéo `SO_Archer` vào `Default Archer Data` và `Trainable Units`.

### 4.3. Đặt Nguồn Tài nguyên quanh căn cứ Red
Trong bán kính `50` đơn vị xung quanh `RedCastle`, bố trí đủ cả 3 loại `ResourceDeposit` (không đặt đè lên Layer `Obstacle`):
- **Thịt (Meat):** `Sheep_Resource.prefab` hoặc `SheepSpawner`.
- **Gỗ (Wood):** Cụm `Tree_Resource.prefab`.
- **Vàng (Gold):** `Gold_Resource_M.prefab` / `Gold_Resource_L.prefab`.

### 4.4. Kiểm tra điều kiện Thắng / Thua
- Đảm bảo `BlueCastle` có `Faction Id = Blue` và `RedCastle` có `Faction Id = Red`.
- Đảm bảo `GameOverUIManager` (`GameOverUIController`) đang Active trong Canvas và gỡ `GameOverUITest` (nếu có) trước khi đóng gói.

---

## Phần 5: Cấu hình `EnemyAIController` trên Scene `PvE.unity`

1. Tạo một Empty GameObject trong `PvE.unity` đặt tên **`EnemyAIManager`** (hoặc gắn vào `Managers`).
2. Nhấn **Add Component → `EnemyAIController`** (`TinyTactics.AI.EnemyAIController`) và gán các trường:
   - **`Faction & Core References`**:
     - `Ai Faction`: `Red`
     - `Enemy Castle`: Kéo **`RedCastle`** trong Scene
     - `Player Castle`: Kéo **`BlueCastle`** trong Scene
     - `Enemy Barracks`: Kéo **`RedBarracks`** (và `RedArchery` nếu có) trong Scene vào danh sách
     - `Rally Point`: Tạo Empty Transform `RedArmyRallyPoint` đặt phía trước căn cứ Red (hướng về Choke Point) rồi kéo vào đây
   - **`Unit Data`**:
     - `Pawn Data`: `SO_Pawn`
     - `Warrior Data`: `SO_Warrior`
     - `Archer Data`: `SO_Archer`
     - `Lancer Data`: `SO_Lancer`
   - **`FSM Settings`**:
     - `Decision Interval`: `1.0` *(ra quyết định mỗi 1 giây)*
     - `Target Worker Count`: `5`
     - `Attack Wave Size`: `6`
     - `Wave Size Increment`: `2`
     - `Resource Search Radius`: `50`

---

## Phần 6: Checklist Kiểm thử Play Mode (Fog of War & PvE AI)

Mở `Assets/_Project/Scenes/Gameplay/PvE.unity`, nhấn **Play** và kiểm tra:

1. **Sương mù 3 trạng thái & Tầm nhìn `Tower`:**
   - Vùng chưa có quân Blue đi qua phủ đen (`alpha = 1`); quân Blue đi qua mở vùng sáng (`alpha = 0`), đi khỏi chuyển sang xám mờ (`alpha = 0.55`).
   - Xây hoặc đặt `BlueTower`: mở vòng sáng rộng bán kính `11` xung quanh tháp; khi tháp bị phá hủy, tầm nhìn biến mất ở nhịp cập nhật kế tiếp (`0.1s`).
2. **UI, Minimap & Ẩn quân địch:**
   - UI HUD và nền bản đồ trên Minimap hiển thị bình thường, không bị lớp phủ đen của `FogOfWarOverlay` che khuất.
   - Quân `Red` đứng trong vùng đen/xám tự ẩn hình và chấm đỏ Minimap; bước vào vùng sáng của quân/tháp Blue mới hiện hình.
3. **Kinh tế độc lập của AI Red:**
   - Quan sát 3 Pawn phe `Red` tự đi khai thác Thịt/Gỗ/Vàng và nộp về `RedCastle` (không làm nhảy số tài nguyên trên HUD của người chơi Blue).
   - `RedCastle` tự đẻ thêm Pawn tới khi đạt `5` nông dân.
4. **Huấn luyện & Đợt tấn công (`EnemyAIController`):**
   - `EnemyAIController` chuyển từ `EconomyFocus` → `MilitaryProduction`, đặt hàng đợi lính tại `RedBarracks`/`RedArchery` (lính mới sinh đổi sang màu `Red` và tập kết tại `RedArmyRallyPoint`).
   - Khi đủ `6` quân chiến đấu, AI chuyển sang `Attacking` và ra lệnh `AttackMoveTo` tiến qua Choke Point về phía `BlueCastle` (chủ động đánh quân Blue chặn đường).
   - Nếu đợt 1 bị đẩy lùi hết, AI quay về `EconomyFocus` và nâng ngưỡng đợt 2 lên `8` quân.
5. **Thắng / Thua hoàn chỉnh:**
   - Phá hủy `RedCastle` (`HP <= 0`) → `GameManager.TriggerPlayerVictory()` hiện màn hình **`VICTORY!`**.
   - Để quân AI phá hủy `BlueCastle` (`HP <= 0`) → `GameManager.TriggerPlayerDefeated()` hiện màn hình **`DEFEAT!`**.
