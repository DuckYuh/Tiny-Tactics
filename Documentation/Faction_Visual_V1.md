# Tài liệu kỹ thuật — Hệ thống Phân biệt Phe (Faction Visual)

**Dự án:** Tiny Tactics (AoE Mini)  
**Giai đoạn:** DACN — Giai đoạn 1  
**Phạm vi:** Player Blue và AI Red  
**Branch phát triển:** `feature/faction-visual`  
**Branch chuẩn bị PR:** `feature/faction-visual-clean`

---

## 1. Mục tiêu và kết quả

Hệ thống phân biệt phe bằng sprite/animation riêng cho hai đội **Blue** và **Red**, tái sử dụng Animator Controller theo từng loại quân thay vì sao chép logic di chuyển hoặc tấn công. Không dùng `SpriteRenderer.color` để nhuộm màu vì asset Tiny Swords có sprite sheet và AnimationClip riêng cho từng phe.

| Đơn vị | Blue | Red | Kết quả kiểm thử |
|---|---|---|---|
| Warrior | Controller gốc | Animator Override | Idle, Run, Attack; sinh từ Barracks |
| Archer | Controller gốc | Animator Override | Idle, Run, Shoot; sinh từ Barracks |
| Lancer | Controller gốc | Animator Override | Idle, Run, Attack theo hướng; sinh từ Barracks |
| Pawn | Controller gốc | Animator Override | Idle, Run, Harvest; kiểm thử visual riêng |

**Đã kiểm thử:** Chuyển visual Blue/Red của bốn loại Unit; Barracks Blue/Red huấn luyện Warrior, Archer, Lancer và các animation tương ứng hoạt động. Kết quả theo các bài kiểm thử thực hiện trong Unity, chưa phải xác nhận Combat/AI hoàn chỉnh.

## 2. Kiến trúc hoạt động

```text
BarracksBuilding
   └── SpawnUnit(UnitDataSO.UnitPrefab)
        └── OnAnyUnitTrained(BarracksBuilding, GameObject)
             └── FactionSpawnVisualBinder
                  ├── Đọc FactionId từ BuildingInstance (nếu initialized)
                  ├── Nếu không có, đọc BarracksBuilding.FactionId
                  ├── Xác định loại quân dựa trên Blue Animator Controller
                  └── FactionVisual.Configure(Blue, Red, factionId)
                       └── Đổi RuntimeAnimatorController

UnitController / WorkerFSM / Combat: độc lập với lớp FactionVisual
UnitAnimationController: tiếp tục điều khiển state, parameter animation
```

- **`FactionVisual`** chỉ xử lý màu phe bằng cách chuyển `RuntimeAnimatorController`; `SetFaction(string)` đổi visual tại runtime. `Configure(...)` nhận controller Blue/Red và phe, được bổ sung cho quá trình spawn.
- **`FactionSpawnVisualBinder`** lắng nghe sự kiện tĩnh `BarracksBuilding.OnAnyUnitTrained` bằng `OnEnable`, hủy đăng ký tại `OnDisable`. Không phải sửa trực tiếp `BarracksBuilding` để thử nghiệm.
- **`UnitAnimationController`** vẫn quản lý các parameter như `IsMoving`, `Attack`, `AttackDirection`, `HarvestType`; không tạo logic animation khác theo phe.
- **Building** đã có cơ chế riêng: `BuildingData.GetPrefab(factionId)` chọn prefab Blue/Red; không thay đổi trong task này.

Thiết kế này sử dụng **Observer Pattern** (event spawn) và **AnimatorOverrideController**. Không cần `Update()` riêng cho FactionVisual/Binder hoặc quét tất cả Unit trong scene mỗi frame.

## 3. Các file chính

| Thành phần | Đường dẫn trong project |
|---|---|
| Component visual | `Assets/_Project/Scripts/Factions/FactionVisual.cs` |
| Component tích hợp spawn | `Assets/_Project/Scripts/Factions/FactionSpawnVisualBinder.cs` |
| Warrior Red Override | `Assets/_Project/Animations/Factions/Warrior/Warrior_Red_Override.overrideController` |
| Archer Red Override | `Assets/_Project/Animations/Factions/Archer/Archer_Red_Override.overrideController` |
| Lancer Red Override | `Assets/_Project/Animations/Factions/Lancer/Lancer_Red_Override.overrideController` |
| Pawn Red Override | `Assets/_Project/Animations/Factions/Pawn/Pawn_Red_Override.overrideController` |
| Clip và controller gốc | `Assets/ThirdParty/Tiny Swords/` |

Các script/scene thử nghiệm nếu lưu giữ: `FactionSpawnTest.cs`, `FactionVisualTest`, `FactionSpawnIntegrationTest`. Chúng chỉ phục vụ kiểm thử, không phải thành phần bắt buộc của scene gameplay.

## 4. Cách sử dụng và yêu cầu

### Yêu cầu

1. Prefab Unit có `Animator` và controller Blue đúng với mapping; `UnitAnimationController` đã cấu hình state/parameter hợp lệ.
2. Mỗi Animator Override Red tham chiếu controller **Blue tương ứng** và map đúng clip Idle/Run/Attack/Harvest theo loại quân.
3. Scene phải có **một** `FactionSpawnVisualBinder` hoạt động, được gán mapping cho Warrior, Archer, Lancer. Nếu cần Pawn từ nguồn spawn mới, phải bổ sung cơ chế khởi tạo tương ứng.
4. Barracks có `FactionId` là `Blue` hoặc `Red`; `UnitDataSO` có prefab và chi phí hợp lệ. Để huấn luyện, scene phải có các manager tài nguyên, dân số và hệ thống Grid/pathfinding cần thiết.

### Thiết lập Binder

Trong scene gameplay (khi được phép tích hợp), tạo một GameObject quản lý và gắn `FactionSpawnVisualBinder`:

| Loại quân | Blue Controller | Red Controller |
|---|---|---|
| Warrior | `Warrior_Blue.controller` | `Warrior_Red_Override` |
| Archer | `Archer_Blue.controller` | `Archer_Red_Override` |
| Lancer | `Lancer_Blue.controller` | `Lancer_Red_Override` |

Khi Barracks sinh quân, Binder nhận event và gọi `FactionVisual.Configure(blueController, redController, factionId)`. Nếu thiếu config hoặc nhận diện không đúng Blue Controller, hệ thống cảnh báo và bỏ qua chuyển màu.

**Quan trọng:** `FactionVisual.FactionId` chỉ là **phe hiển thị**; không được sử dụng làm nguồn phe chiến đấu chính thức cho đến khi thống nhất API với hệ thống Combat/AI.

## 5. Kiểm thử đã thực hiện

- Warrior Blue ↔ Red: Idle, Run, Attack; thử trực tiếp và sau khi sinh từ Barracks.
- Archer Blue ↔ Red: Idle, Run, Shoot; sinh từ Barracks.
- Lancer Blue ↔ Red: Idle, Run và các hướng Attack; sinh từ Barracks.
- Pawn Blue ↔ Red: Idle, Run và các giá trị `HarvestType` 0 (ngừng), 1 (Wood), 2 (Gold), 3 (Meat) trong môi trường kiểm thử.
- Hai Barracks có `FactionId` tương ứng; đơn vị mới nhận bộ animation đúng màu theo kết quả kiểm thử.
- Chưa chỉnh sửa logic Combat, Attack-Move hay HP trong task này.

### Cách hoạt động và kiểm thử trong scene Test

Có hai scene kiểm thử độc lập, không cần chỉnh sửa prefab Unit hoặc scene gameplay gốc:

- **`FactionVisualTest`:** Đặt các Unit Blue vào scene và gắn `FactionVisual` trên **instance kiểm thử**. Gán controller Blue và `*_Red_Override`, sau đó vào **Play Mode** và chọn `Test Faction Blue/Red` từ Context Menu của component. Kiểm tra Idle, Run, Attack (Warrior/Archer/Lancer) và Harvest (Pawn); mục đích là xác nhận **đổi màu animation đúng**, chưa kiểm thử sinh quân.
- **`FactionSpawnIntegrationTest`:** Dùng bản sao scene gameplay có đủ `ResourceManager`, `PopulationManager` và Grid, đặt `BlueBarracks`, `RedBarracks` cùng một `FactionSpawnVisualBinder` đã map Warrior/Archer/Lancer. Gắn `FactionSpawnTest` lên GameObject quản lý, gán hai Barracks, chọn `Test Unit Index` theo danh sách `TrainableUnits`, vào **Play Mode** rồi gọi `Test Spawn Blue` hoặc `Test Spawn Red` từ Context Menu. Sau khi Training Queue chạy xong, kiểm tra Unit xuất hiện với `FactionVisual.FactionId`, Animator Controller và animation đúng phe.

**Luồng dễ hình dung:** `Bấm Test Spawn Red → Red Barracks huấn luyện → Instantiate Unit → phát OnAnyUnitTrained → Binder đọc FactionId = Red → FactionVisual chuyển sang Red Override`. Nếu scene thiếu tài nguyên/dân số, cần bảo đảm đủ điều kiện huấn luyện trước khi test. Kết quả này chỉ xác minh **hiển thị theo phe**, không xác minh logic chọn địch của Combat/AI.

## 6. Giới hạn và phần chờ tích hợp

1. **Event sau Instantiate:** `OnAnyUnitTrained` được phát sau khi tạo GameObject và gọi `MoveTo`. Vì vậy visual được cấu hình muộn, có thể xuất hiện một frame Blue trên unit Red; quan trọng hơn, thông tin phe gameplay chưa có trước `Awake`/`OnEnable` của Unit.
2. **Không bao phủ mọi đường spawn:** Binder hiện hỗ trợ unit huấn luyện từ Barracks. Pawn đặt sẵn, unit do AI Instantiate trực tiếp và các loại nhà sinh quân khác cần cơ chế khởi tạo riêng.
3. **Combat/AI chưa đồng bộ:** Cần một nguồn `FactionId` thống nhất để nhận diện đồng minh/kẻ địch, chọn mục tiêu và hiển thị, tốt nhất tại thời điểm khởi tạo Unit trước khi gameplay bắt đầu.
4. **Kinh tế/dân số:** Các manager đã kiểm tra sử dụng trạng thái chung, nên bài test Barracks Blue/Red chưa chứng minh quản lý tài nguyên/dân số độc lập theo phe.
5. **Giai đoạn 1 chỉ Blue/Red:** Purple/Yellow không nằm trong phạm vi nghiệm thu hiện tại. Không làm animation Die do bộ asset không có sprite phù hợp.

**Trạng thái tài liệu:** Phần Faction Visual và bài test Barracks Blue/Red đã hoàn thành theo kết quả nhóm ghi nhận. Phần nguồn dữ liệu phe chung cho Combat/AI và các đường spawn khác là hạng mục tích hợp sau, **không được xem là đã hoàn tất**.
