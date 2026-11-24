# Hướng dẫn Cấu hình Unity Inspector — Combat, Projectile, Monk & Attack-Move

Tài liệu hợp nhất quy trình thiết lập trên **Unity Editor / Inspector** cho toàn bộ hệ thống chiến đấu (Bước 1–3):
- **Bước 1:** `UnitDataSO`, `IDamageable`, `HealthSystem`, `UnitAnimationController` (`Die`).
- **Bước 2:** `GameInputActions` (`AttackMove`), `RTSInputMode`, `InputController`, `UnitCombatController`.
- **Bước 3:** `ArrowProjectile`, `MonkHealerController` (`SO_Monk`), `ArcheryBuilding`, `MonasteryBuilding`, `SFXManager`.

---

## Phần 1: Cấu hình Dữ liệu Đơn vị (`UnitDataSO`)

### 1.1. Bảng thông số chuẩn toàn dự án
4 asset (`SO_Pawn`, `SO_Warrior`, `SO_Archer`, `SO_Lancer`) trong `Assets/_Project/Data/Units/` đã được điền sẵn các chỉ số dưới đây; riêng `SO_Monk` tạo mới theo hướng dẫn ở mục **1.2**.

| Asset | Meat / Wood / Gold | Train Time | Max HP | Damage / Heal | Attack Range | Cooldown | Move Speed | Armor | Vision |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **SO_Pawn** | `50 / 0 / 0` | `5s` | `60` | `0` | `0` | `0` | `3.5` | `0` | `6` |
| **SO_Warrior** | `60 / 0 / 0` | `5s` | `150` | `15` | `1.2` | `1.2s` | `2.8` | `5` | `7` |
| **SO_Archer** | `40 / 30 / 0` | `7s` | `70` | `20` | `6.0` | `1.8s` | `3.3` | `0` | `8` |
| **SO_Lancer** | `50 / 0 / 20` | `6s` | `110` | `25` | `1.5` | `1.5s` | `3.5` | `2` | `7` |
| **SO_Monk** *(tạo mới)* | `60 / 0 / 50` | `8s` | `80` | `50` *(Heal)* | `5.0` | `3.0s` | `2.5` | `0` | `7` |

### 1.2. Tạo mới asset `SO_Monk.asset`
1. Trong Project window, vào `Assets/_Project/Data/Units/`.
2. Chuột phải → **Create → Tiny Tactics → Unit Data**, đặt tên **`SO_Monk`**.
3. Điền các nhóm trong **Inspector** đúng theo bảng trên:
   - **`General Info`**: `Unit Name = Monk`, `Unit Icon` chọn frame đầu tiên của `Assets/ThirdParty/Tiny Swords/Units/Blue Units/Monk/Idle.png`. (`Unit Prefab` sẽ kéo sau khi tạo xong `Monk_Blue.prefab` ở mục **3.3**).
   - **`Resource Costs`**: `Meat Cost = 60`, `Wood Cost = 0`, `Gold Cost = 50`.
   - **`Training & Population`**: `Train Time = 8`, `Population Cost = 1`.
   - **`Combat Stats`**: `Max HP = 80`, `Damage = 50`, `Attack Range = 5`, `Attack Cooldown = 3`, `Move Speed = 2.5`, `Armor = 0`, `Vision Range = 7`.

---

## Phần 2: Tạo Prefab Đạn bay (`ArrowProjectile`) & Hiệu ứng Hồi máu (`HealEffect`)

Thực hiện bước này trước để sẵn sàng gán prefab vào `Archer_Blue` và `Monk_Blue` ở Phần 3.

### 2.1. Tạo `ArrowProjectile.prefab`
1. Trong Project window, vào `Assets/ThirdParty/Tiny Swords/Units/Extra/Arrow/`, mở rộng `Arrow.png` để thấy sprite con `Arrow_0` (hướng sang phải, `Pixels Per Unit = 64`).
2. Tạo Empty GameObject trong Scene, đặt tên **`ArrowProjectile`**:
   - **Layer:** `Projectile` (Layer 10).
   - **Add Component → `SpriteRenderer`**: Gán sprite `Arrow_0`, đặt `Order in Layer = 15`.
   - **Add Component → `ArrowProjectile`** (`TinyTactics.Combat.ArrowProjectile`):
     - `Speed`: `12`
     - `Arc Height Factor`: `0.12` (đặt `0` nếu muốn bay thẳng tuyệt đối)
     - `Max Arc Height`: `1.5`
     - `Arrival Threshold`: `0.15`
     - `Play SFX`: `true`
3. Kéo vào `Assets/_Project/Prefabs/Combat/ArrowProjectile.prefab` (tạo thư mục `Combat` trong `Prefabs`) rồi xóa bản tạm trong Scene.

### 2.2. Tạo `HealEffect.prefab` *(tùy chọn)*
1. Tạo Empty GameObject trong Scene, đặt tên **`HealEffect`**:
   - **Add Component → `SpriteRenderer`**: đặt `Order in Layer = 15`.
   - **Add Component → `Animator`**: kéo Controller `Assets/ThirdParty/Tiny Swords/Units/Extra/Heal Effect/Heal.controller`.
2. Kéo vào `Assets/_Project/Prefabs/Combat/HealEffect.prefab` rồi xóa bản tạm trong Scene. *(Nếu bỏ qua bước này, Unit được hồi máu vẫn nháy xanh lá bằng code).*

---

## Phần 3: Cấu hình Prefab Unit

Mở các Prefab trong `Assets/_Project/Prefabs/Units/` ở **Prefab Mode**. Toàn bộ Unit đều cần **Layer = `Unit` (Layer 7)** và **`CircleCollider2D`** đang bật.

### 3.1. Unit Cận chiến (`Warrior_Blue`, `Lancer_Blue`)
1. **Add Component → `HealthSystem`** (`TinyTactics.Combat.HealthSystem`):
   - `Unit Data`: Kéo `SO_Warrior` hoặc `SO_Lancer`.
   - `Default Faction`: `Blue`.
   - `Death Cleanup Delay`: `1.0`.
2. **Add Component → `UnitCombatController`** (`TinyTactics.Combat.UnitCombatController`):
   - `Unit Data`: Kéo `SO_Warrior` hoặc `SO_Lancer` (hoặc để trống để tự đọc từ `HealthSystem`).
   - `Aggro Range`: `5.0`.
   - `Scan Interval`: `0.15` | `Chase Repath Interval`: `0.25`.
   - `Target Layer Mask`: Chọn **`Unit`** (Layer 7) và **`Building`** (Layer 8).
   - `Is Ranged`: Tắt (`false`).
3. Trên **`UnitController`**: kiểm tra `Unit Layer Mask` = `Unit` (Layer 7). `Move Speed` sẽ tự đồng bộ từ `UnitDataSO.MoveSpeed` khi chạy.

### 3.2. Unit Đánh xa (`Archer_Blue`)
1. **Add Component → `HealthSystem`**: gán `Unit Data = SO_Archer`, `Default Faction = Blue`, `Death Cleanup Delay = 1.0`.
2. Tạo một Empty child bên trong `Archer_Blue` tên **`FirePoint`**, đặt tọa độ khoảng `(0.2, 0.15, 0)`.
3. **Add Component → `UnitCombatController`**:
   - `Unit Data`: Kéo `SO_Archer`.
   - `Aggro Range`: `6.5` hoặc `7.0` *(lưu ý runtime tự kẹp `>= AttackRange = 6.0`)*.
   - `Scan Interval`: `0.15` | `Chase Repath Interval`: `0.25`.
   - `Target Layer Mask`: Chọn **`Unit`** (Layer 7) và **`Building`** (Layer 8).
   - `Is Ranged`: **Bật (`true`)**.
   - `Projectile Prefab`: Kéo `Assets/_Project/Prefabs/Combat/ArrowProjectile.prefab`.
   - `Fire Point`: Kéo child `FirePoint`.

### 3.3. Unit Hỗ trợ (`Monk_Blue`)
1. Duplicate `Warrior_Blue.prefab` thành **`Assets/_Project/Prefabs/Units/Monk_Blue.prefab`** để giữ sẵn `UnitController`, `SelectableUnit`, `MinimapTarget`, `UnitAnimationController`, `CircleCollider2D`.
2. Trên **`Animator`**: gán Animator Controller của Monk Blue (xem **Phần 4.2**).
3. Trên **`HealthSystem`**: đổi `Unit Data` sang **`SO_Monk`**, `Default Faction = Blue`, `Death Cleanup Delay = 1.0`.
4. Gỡ `UnitCombatController` (nếu có từ bản duplicate) và **Add Component → `MonkHealerController`** (`TinyTactics.Combat.MonkHealerController`):
   - `Unit Data`: Kéo `SO_Monk`.
   - `Aggro Range`: `6.0` | `Scan Interval`: `0.15` | `Chase Repath Interval`: `0.25`.
   - `Target Layer Mask`: Chỉ chọn **`Unit`** (Layer 7).
   - `Aoe Heal Radius`: `1.5`.
   - `Heal Effect Prefab`: Kéo `HealEffect.prefab` (nếu đã tạo ở mục **2.2**).
   - `Play SFX`: `true`.
5. Quay lại **`SO_Monk.asset`** (mục **1.2**) và kéo **`Monk_Blue.prefab`** vào ô **`Unit Prefab`**.

### 3.4. Nông dân (`Pawn_Blue`)
1. Mở `Assets/_Project/Prefabs/Units/Pawn_Blue.prefab`.
2. **Add Component → `HealthSystem`**: gán `Unit Data = SO_Pawn`, `Default Faction = Blue`, `Death Cleanup Delay = 1.0`.
3. **Không gắn `UnitCombatController`** (Nông dân chỉ dùng `WorkerFSM`; khi chọn hỗn hợp Pawn và lính rồi nhấn `A + Click`, Pawn vẫn nhận lệnh hành quân theo đội hình từ `RTSUnitManager`).
4. Giữ `UnitController.Move Speed = 3.5` trên Prefab.

### 3.5. Dựng nhanh Unit phe Địch (`Red`) để test trong Scene `PvE`
- Kéo `Warrior_Blue` / `Archer_Blue` / `Lancer_Blue` vào gần `EnemySpawn`:
  - Nếu có **`FactionVisual`**: đổi `Faction Id = Red` (hoặc gán `Red Controller` tương ứng trong `Assets/_Project/Animations/Factions/`).
  - Nếu chưa có `FactionVisual`: đổi trực tiếp **`HealthSystem` → `Default Faction = Red`**.

---

## Phần 4: Cấu hình Animator (`Die` & `Monk`)

### 4.1. Trạng thái `Die` (`UnitAnimationController`)
Theo `Documentation/Animation.md`, bộ sprite Tiny Swords hiện chưa có clip `Die` nên `UnitAnimationController` hoạt động theo 2 chế độ:
- **Chưa có clip `Die`:** Không cần chỉnh Animator Controller. Khi chết, `PlayDie()` vẫn tự tắt `IsMoving`, reset `Attack`/`HarvestType`, khóa toàn bộ chuyển đổi trạng thái khác (`_isDead = true`), và `HealthSystem` chờ `Death Cleanup Delay` (`1.0s`) rồi hủy object.
- **Khi bổ sung clip `Die`:**
  1. Mở Animator Controller của Unit (ví dụ `Warrior_Blue.controller`).
  2. Tab **Parameters** → tạo Trigger `Die` (hoặc Bool `Die` / `IsDead`).
  3. Tạo state `Die` gắn Animation Clip chết (tắt **Loop Time** trên clip).
  4. Tạo transition **`Any State` → `Die`** với Condition `Die` (hoặc `IsDead == true`), tắt **`Can Transition To Self`**, `Has Exit Time = Off`, `Transition Duration = 0`.
  5. Nếu tên clip chứa chữ `"Die"` (ví dụ `Warrior_Die`), `HealthSystem` tự lấy đúng độ dài clip qua `GetDeathAnimationDuration()`.

### 4.2. Animator cho `Monk` (`Monk_Blue` & `Monk_Red`)
1. Copy `Assets/ThirdParty/Tiny Swords/Units/Blue Units/Monk/Monk Blue Animations/Monk_Blue.controller` (và bản `Monk_Red.controller` hoặc tạo `AnimatorOverrideController`) sang `Assets/_Project/Animations/Factions/Monk/`.
2. Thêm Parameters chuẩn khớp `UnitAnimationController`:
   - `IsMoving` (**Bool**): điều khiển chuyển `Monk_Idle_Blue` ↔ `Monk_Run_Blue` (`Has Exit Time = Off`, `Transition Duration = 0`).
   - `Attack` (**Trigger**): chuyển `Any State` / `Idle` → `Monk_Heal_Blue` rồi quay lại `Idle` (`Exit Time = 1`).
3. Trong Scene `PvE`, chọn GameObject chứa **`FactionSpawnVisualBinder`**, thêm 1 entry `Monk` gán `Blue Controller` và `Red Controller` để Monastery phe Red tự đổi màu quân khi sinh Monk.

---

## Phần 5: Cấu hình Công trình (`BuildingBase`, `ArcheryBuilding`, `MonasteryBuilding`)

### 5.1. Yêu cầu chung cho mọi Công trình (`IDamageable`)
Kiểm tra các Prefab trong `Assets/_Project/Prefabs/Buildings/` (`BlueCastle`, `RedCastle`, `BlueBarracks`, `RedBarracks`, `BlueHouse`, `RedHouse`, `BlueArchery`, `RedArchery`, `BlueMonastery`, `RedMonastery`):
- GameObject gốc (hoặc child chứa Collider) thuộc **Layer `Building` (Layer 8)** để `UnitCombatController.Target Layer Mask` quét trúng.
- Có ít nhất 1 **`Collider2D`** đang bật (`Enabled = true`).
- Trường **`Faction Id`** khớp phe sở hữu (`Blue` hoặc `Red`).

### 5.2. Tách `ArcheryBuilding` và `MonasteryBuilding`
1. **`BlueArchery.prefab` & `RedArchery.prefab`:**
   - Gỡ `BarracksBuilding` cũ (và gỡ `Missing Script` nếu có trên `RedArchery`).
   - **Add Component → `ArcheryBuilding`** (`TinyTactics.Buildings.ArcheryBuilding`):
     - `Faction Id`: `Blue` (hoặc `Red`).
     - `Default Archer Data`: Kéo **`SO_Archer`**.
     - `Trainable Units`: Chỉ giữ 1 phần tử là **`SO_Archer`** (`OnValidate` tự lọc nếu kéo nhầm quân khác).
2. **`BlueMonastery.prefab` & `RedMonastery.prefab`:**
   - Gỡ `BarracksBuilding` cũ và gỡ `Missing Script` còn sót trên prefab.
   - **Add Component → `MonasteryBuilding`** (`TinyTactics.Buildings.MonasteryBuilding`):
     - `Faction Id`: `Blue` (hoặc `Red`).
     - `Default Monk Data`: Kéo **`SO_Monk`**.
     - `Trainable Units`: Chỉ giữ 1 phần tử là **`SO_Monk`**.
3. **`BlueBarracks.prefab` & `RedBarracks.prefab`:** Giữ nguyên `BarracksBuilding` với `Trainable Units` (gồm `SO_Warrior`, `SO_Archer`, `SO_Lancer` hoặc chỉ `SO_Warrior`, `SO_Lancer` nếu muốn tách hẳn cung thủ sang `Archery`).

---

## Phần 6: Cấu hình Input, Cursor & Âm thanh

### 6.1. `GameInputActions.inputactions`
Mở `Assets/_Project/Input/GameInputActions.inputactions`:
- Kiểm tra trong map **`Player`** đã có Action **`AttackMove`** (`Action Type = Button`, Binding = `A [Keyboard]`).
- Nếu chỉnh sửa lại asset: bấm **Save Asset**, đảm bảo **Generate C# Class** trỏ đúng về `Assets/_Project/Scripts/Input/GameInputActions.cs` và bấm **Apply**.

### 6.2. `InputController` trong Scene `PvE`
Mở `Assets/_Project/Scenes/Gameplay/PvE.unity`, chọn GameObject gắn **`InputController`** (trong `Managers`):
- `Player Faction`: `Blue`.
- `Attack Move Cursor` *(tùy chọn)*: Gán texture tâm ngắm (cần bật **Texture Type = `Cursor`** và **Read/Write** trong Import Settings).
- `Attack Move Cursor Hotspot`: Điểm tâm click (ví dụ `(0, 0)` hoặc `(16, 16)` cho icon 32x32).
- Đảm bảo `Managers` có đủ `SelectionSystem`, `RTSUnitManager`, `PathfindingGrid`.

### 6.3. `SFXManager` *(tùy chọn)*
Trong Scene `PvE`, chọn **`SFXManager`** → **`Sound Entries`**, bổ sung 3 loại mới nếu đã có file âm thanh:
- `ArrowShoot`
- `ArrowHit`
- `MonkHeal`

---

## Phần 7: Checklist Kiểm thử Tổng hợp trong Play Mode

Mở `Assets/_Project/Scenes/Gameplay/PvE.unity`, nhấn **Play** và kiểm tra lần lượt:

1. **Điều khiển `Attack-Move` (`A + Click`), Di chuyển thường & Đánh chỉ định:**
   - Chọn nhóm lính (`Warrior` / `Archer` / `Lancer` / `Monk`) → nhấn **`A`**: đổi con trỏ (nếu có gán) và vào mode `AttackMove`.
   - Nhấn **Chuột phải** hoặc **Esc** trong mode `AttackMove`: thoát về `Normal`, không ra lệnh chạy và không mất chọn quân.
   - Nhấn **`A`** → **Chuột trái** qua phía bên kia đội địch: lính hành quân theo A*, gặp địch tự dừng đánh rồi tiếp tục đi tới đích.
   - **Chuột phải mặt đất:** Lính chạy thẳng qua địch, tới đích đứng yên mới tự phòng thủ.
   - **Chuột phải trực tiếp vào Unit hoặc `RedCastle`:** Lính bám đuổi và đánh đúng mục tiêu; phá `RedCastle` hết HP kích hoạt màn hình `VICTORY!`.
2. **Giáp, Chết & Dân số (`HealthSystem`):**
   - `Warrior` (`Armor = 5`) nhận `15` damage chỉ mất `10` HP/đòn; đòn có `Damage <= Armor` vẫn mất tối thiểu `1` HP.
   - Unit phe `Blue` khi chết tắt `Collider2D`, ngừng di chuyển, trừ dân số trên HUD đúng 1 lần và biến mất sau `~1.0s` (hoặc hết clip `Die`).
3. **Bắn xa (`Archer` & `ArrowProjectile`):**
   - `Archer_Blue` dừng ở tầm `~6.0` và bắn `ArrowProjectile` bay cong nhẹ, xoay đúng hướng bay.
   - Mục tiêu chết giữa lúc tên đang bay: mũi tên bay nốt tới vị trí cuối rồi tự hủy, Console không báo `NullReferenceException`.
4. **Hồi máu diện rộng (`MonkHealerController`):**
   - `Monk_Blue` tự dừng khi có đồng minh mất máu trong tầm `5.0–6.0`, cast phép mỗi `3.0s` hồi `+50 HP` cho cả nhóm đồng minh trong bán kính `1.5` (kèm nháy xanh lá / `HealEffect`).
   - Khi đi cùng đội hình bằng `A + Click`, hết đồng minh bị thương thì `Monk` tự tiếp tục di chuyển.
5. **Huấn luyện tại `Archery` & `Monastery`:**
   - Chọn `BlueArchery` chỉ hiện nút đẻ `Archer` (`40 Meat + 30 Wood`).
   - Chọn `BlueMonastery` chỉ hiện nút đẻ `Monk` (`60 Meat + 50 Gold`).
