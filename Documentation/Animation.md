# Báo cáo Task – Tích hợp Animation (Tiny Tactics)

## 1. Tổng quan

- **Dự án:** Tiny Tactics (AoE Mini) – game RTS 2D Unity.
- **Task:** Tích hợp Animation (Sprint 4, phụ trách: Huy).
- **Phạm vi đã thực hiện và kiểm thử:** Idle, Run/Walk, Attack, Mine/Harvest.
- **Ngoài phạm vi:** Die animation (asset không có sprite; đã thống nhất không triển khai).
- **Trạng thái:** Hoàn thành phần Animation độc lập; chờ Combat/Attack-Move để kích hoạt tấn công từ gameplay.

## 2. Kiến trúc

```text
UnitController (A* Pathfinding, Move, Stop)
  └── UnitAnimationController.SetMoving(bool)

WorkerFSM (Idle / Moving / Harvesting / Returning / Depositing)
  └── UnitAnimationController.SetHarvestType(int) / StopHarvest()

Combat System (tích hợp sau)
  ├── UnitAnimationController.PlayAttack()
  └── UnitAnimationController.PlayDirectionalAttack(targetPosition)

UnitAnimationController → Unity Animator → SpriteRenderer
```

`UnitAnimationController` chỉ phụ trách biểu diễn; không thực hiện tìm đường, khai thác, gây sát thương hay quản lý cooldown. Các Animator parameter được cache bằng `Animator.StringToHash`, và giá trị trạng thái chỉ cập nhật khi có thay đổi. Kiểm tra hỗ trợ parameter `HarvestType`, `Attack`, `AttackDirection` giúp dùng chung script cho nhiều loại unit.

## 3. Chức năng đã triển khai

| Unit | Idle | Run | Attack | Harvest |
|---|---|---|---|---|
| Warrior | Có | Có | Attack1 | Không |
| Archer | Có | Có | Shoot | Không |
| Lancer | Có | Có | Đánh theo hướng | Không |
| Pawn | Có | Có | Không | Gỗ / Vàng / Thịt |

### Idle / Run
- `UnitController` gọi `SetMoving(isMoving)` sau khi xử lý di chuyển.
- `Stop()` đồng bộ animation về trạng thái không di chuyển.
- Animator sử dụng Bool `IsMoving`; các clip Idle và Run lặp (`Loop Time`).

### Harvest (Pawn)
- `WorkerFSM` chuyển `HarvestType` theo tài nguyên: **0 = None, 1 = Wood (Axe), 2 = Gold (Pickaxe), 3 = Meat (Knife)**.
- Khi chuyển vào `Harvesting`, Pawn ngừng di chuyển và phát animation tương ứng.
- Khi rời `Harvesting`, gọi `StopHarvest()`; Pawn có thể quay về Castle hoặc tìm nguồn tài nguyên mới.
- Logic thu thập/nộp tài nguyên giữ nguyên trong `WorkerFSM`.

### Attack
- Warrior: `Attack` Trigger phát clip Attack1.
- Archer: `Attack` Trigger phát clip Shoot.
- Lancer: `AttackDirection` Int chọn một trong 5 clip Right, UpRight, Up, DownRight, Down. Các hướng bên trái dùng lật sprite.
- Attack clip phát một lần rồi trở lại Idle theo transition có Exit Time.
- `PlayAttack()` và `PlayDirectionalAttack(Vector3 targetPosition)` hiện là API biểu diễn, **chưa tự gây sát thương hay tạo projectile**.

### Hướng sprite
- Thống nhất dùng `SpriteRenderer.flipX` thay vì thay đổi dấu `transform.localScale.x` trong `UnitController`, tránh lật sprite hai lần khi Lancer đổi hướng đánh.
- Hướng Lancer được tính từ vector giữa vị trí unit và mục tiêu; sử dụng ngưỡng góc tương đương 22.5° / 67.5° mà không cần `Atan2`.

## 4. Animator Parameters

| Unit | Parameters |
|---|---|
| Warrior | `IsMoving` (Bool), `Attack` (Trigger) |
| Archer | `IsMoving` (Bool), `Attack` (Trigger) |
| Lancer | `IsMoving` (Bool), `Attack` (Trigger), `AttackDirection` (Int) |
| Pawn | `IsMoving` (Bool), `HarvestType` (Int) |

Các transition Idle ↔ Run dùng `Has Exit Time = Off`, `Transition Duration = 0`. Attack dùng Trigger để bắt đầu; clip Attack không lặp, transition về Idle có `Exit Time = 1`. Harvest dùng `HarvestType` để duy trì trạng thái và các clip tương tác được bật lặp.

## 5. Cách sử dụng và yêu cầu

1. Mỗi prefab unit cần có `SpriteRenderer`, `Animator`, `UnitController`, `UnitAnimationController`; Pawn bổ sung `WorkerFSM`.
2. Gán Animator Controller đúng loại quân và đảm bảo tên/kiểu parameter khớp bảng trên.
3. `UnitController` tự điều khiển Idle/Run; `WorkerFSM` tự điều khiển Harvest.
4. Để test Attack khi chưa có Combat, vào **Play Mode → chọn unit → Inspector → UnitAnimationController → Context Menu → Test Attack (Play Mode)**. Lancer có thêm các lệnh test theo hướng.
5. Khi tích hợp Combat, gọi `PlayAttack()` (Warrior/Archer) hoặc `PlayDirectionalAttack(targetPosition)` (Lancer) **tại thời điểm bắt đầu đòn đánh**. Gameplay vẫn phải tự quyết định phạm vi, thời gian, sát thương, projectile và việc có được tấn công hay không.

## 6. Kết quả kiểm thử

Theo kết quả người thực hiện đã xác nhận trong Unity:
- Idle ↔ Run hoạt động cho các unit.
- Warrior/Archer phát Attack một lần và trở lại Idle.
- Lancer chọn được các hướng Attack, kể cả hướng bên trái qua flip.
- Pawn khai thác Wood, Gold, Meat và chuyển animation khi di chuyển/nộp tài nguyên.
- Kiểm thử kết hợp nhiều unit, chuyển hướng và các trường hợp tài nguyên cạn hoàn thành.
- Không ghi nhận lỗi Animator Parameter trong checklist cuối.

## 7. File cần rà soát khi commit

**Script có thay đổi:**
- `Assets/_Project/Scripts/Animation/UnitAnimationController.cs` (+ `.meta` nếu mới).
- `Assets/_Project/Scripts/Units/UnitController.cs`.
- `Assets/_Project/Scripts/Units/WorkerFSM.cs`.

**Asset/prefab có thể thay đổi:**
- Animator Controllers cho `Warrior_Blue`, `Archer_Blue`, `Lancer_Blue`, `Pawn_Blue` trong `Assets/ThirdParty/Tiny Swords/`.
- Prefab Warrior, Archer, Lancer và Pawn **nếu có chỉnh sửa**.
- Các `.anim` đã điều chỉnh `Loop Time`, nếu có.

> Danh sách trên dựa theo các bước tích hợp; kiểm tra `git status`/`git diff` để lấy danh sách file thực tế, không commit các thay đổi asset ngoài phạm vi. Nếu Pawn được đặt trong scene thay vì prefab, kiểm tra việc lưu/commit thay đổi scene tương ứng.

## 8. Giới hạn và công việc tích hợp sau

- Combat/Attack-Move chưa kết nối trực tiếp với Attack Animation; hiện kích hoạt bằng Context Menu để test.
- Chưa đồng bộ hit frame, cooldown, projectile, animation interruption/locking hoặc xử lý Attack trong lúc di chuyển. Các phần này cần làm cùng Combat để tránh xung đột gameplay.
- Chưa tích hợp animation Pawn mang tài nguyên và xây dựng; không thuộc phạm vi đã nghiệm thu của task này.
- Không triển khai Die animation theo quyết định của dự án.

**Kết luận:** Hoàn thành task tích hợp Animation theo phạm vi điều chỉnh, có thể chuẩn bị commit sau khi rà soát diff và prefab.
