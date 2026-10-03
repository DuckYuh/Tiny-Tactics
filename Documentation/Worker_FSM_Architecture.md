# Tài Liệu Kỹ Thuật: Hệ Thống FSM Nông Dân Khai Thác & Xây Dựng (Tiny Tactics)

Tài liệu này chuẩn hóa mô hình vòng lặp kinh tế (Eco-Loop) và hướng dẫn cấu hình máy trạng thái hữu hạn (FSM) cho đơn vị **Nông dân (Pawn)** trong dự án **Tiny Tactics**[cite: 2, 3].

---

## 1. Lý Thuyết Kiến Trúc: Vòng Lặp FSM Nông Dân (Worker State Machine)

Trong các tựa game RTS như *Age of Empires* hay *Warcraft*, đơn vị nông dân hoạt động độc lập theo một chu trình khép kín tự động:


[Idle] ---> Nhận lệnh chuột phải vào Tài nguyên/Công trình
│
├──> [MovingToResource] --(Đến nơi)--> [Harvesting]
│                                           │ (Túi đầy 10/10)
│                                           v
│    [Harvesting] <--(Đến nơi)-- [ReturningToCastle]
│         ^                             │ (Chạm Castle)
│         └──── (Nộp xong) ◄────────────┘
│
└──> [MovingToBuild] --(Đến nơi)--> [Building] ---> [Idle khi xong]


### Các trạng thái nòng cốt:
1. **Idle:** Đứng chờ lệnh[cite: 2].
2. **MovingToResource:** Sử dụng thuật toán A* của `UnitController` để tiếp cận ô trống liền kề của mỏ[cite: 2, 5].
3. **Harvesting:** Đứng tại chỗ, mỗi chu kỳ $T$ giây thu nạp $X$ lượng tài nguyên vào túi chứa[cite: 2, 3].
4. **ReturningToCastle:** Tự động định vị Lâu đài (Castle) đồng minh gần nhất và di chuyển về[cite: 2, 3].
5. **Depositing:** Chuyển tài nguyên từ balo của nông dân vào kho dự trữ toàn cục (`ResourceManager`), sau đó tự động tái lập lộ trình quay lại điểm tài nguyên vừa khai thác[cite: 2, 3].
6. **Building:** Tương tác với công trình xây dựng để tăng thanh tiến trình (Build Progress)[cite: 2, 3].

---

## 2. Hướng Dẫn Thiết Lập Component Trên Unity Editor

Để tích hợp module này mà không làm ảnh hưởng đến hệ thống điều khiển và di chuyển đã hoàn thiện trước đó[cite: 1, 2, 7], hãy thực hiện theo đúng các bước sau:

### Bước 1: Thiết lập Lâu đài làm Điểm nộp (Castle Drop-off)
1. Chọn Prefab hoặc GameObject **Castle** trên Scene[cite: 2, 3].
2. Gán component `CastleDepositArea.cs`.
3. Thêm một `BoxCollider2D` hoặc `CircleCollider2D`:
   - Tích chọn **Is Trigger = True**.
   - Bán kính bao trùm sát chân móng thành trì để nông dân chạm vào là nộp được đồ[cite: 2, 3].

### Bước 2: Chuẩn hóa Prefab Tài nguyên (Cây gỗ, Mỏ vàng, Đàn cừu)
1. Tạo Prefab cho từng loại tài nguyên từ asset Tiny Swords[cite: 2, 3]:
   - `Tree_Resource`: Gắn `ResourceDeposit.cs`, chọn `ResourceType = Wood`, `CurrentAmount = 100`[cite: 2, 3].
   - `Gold_Resource`: Gắn `ResourceDeposit.cs`, chọn `ResourceType = Gold`, `CurrentAmount = 300`[cite: 2, 3].
   - `Sheep_Resource`: Gắn `ResourceDeposit.cs`, chọn `ResourceType = Meat`, `CurrentAmount = 150`[cite: 2, 3].
2. **Thiết lập Collider & Layer:**
   - Đảm bảo các đối tượng này có Layer là `Obstacle` và mang Collider 2D tĩnh để A* không cho unit đi xuyên vào tim đối tượng[cite: 1, 2].

### Bước 3: Cấu hình Prefab Nông dân (Pawn)
1. Mở Prefab Nông dân (`Pawn_Blue`)[cite: 2, 3].
2. **Chỉ số sinh tồn cơ bản:**
   - Máu (HP): `50` (máu thấp, không có khả năng tự vệ)[cite: 2, 3].
   - Sát thương (Damage): `0` (không có khả năng tấn công)[cite: 2, 3].
3. Giữ nguyên component `UnitController` đã hoạt động tốt[cite: 5].
4. Bấm **Add Component** -> Thêm `WorkerFSM.cs`:
   - `Max Capacity`: `10`
   - `Interaction Range`: `1.0` (khoảng cách vừa đủ đứng cạnh ô tài nguyên để thao tác)
   - `Target Castle`: Gán Castle của người chơi (hoặc để trống để script tự tìm Castle qua Tag)[cite: 2, 3].

### Bước 4: Khởi tạo Bộ Quản lý Tài nguyên (ResourceManager)
1. Trong cửa sổ **Hierarchy**, tạo một GameObject rỗng tên là `GameplayManager`.
2. Gắn script `ResourceManager.cs`.
3. Khởi tạo tài nguyên ban đầu: Thịt: 100, Gỗ: 150, Vàng: 50[cite: 2, 3].
4. Khi nông dân nộp đồ vào Lâu đài, `ResourceManager` sẽ nhận tín hiệu và tự động cập nhật số liệu[cite: 2, 3].

---

## 3. Quy Tắc Bảo Toàn Kiến Trúc (Zero-Regression Policy)
- **Không sửa `Pathfinding.cs`:** Nông dân chỉ đơn thuần là người tiêu dùng dịch vụ tìm đường[cite: 1, 5].
- **Không sửa `RTSUnitManager.cs`:** Khi click chuột phải vào đất trống, nông dân vẫn di chuyển theo đội hình như lính chiến đấu bình thường; chỉ khi click chuột phải trúng Collider của `ResourceDeposit` hoặc `ConstructionSite`, lệnh FSM đặc thù mới được kích hoạt[cite: 4, 5].
- **Cơ chế 3 đơn vị mở màn:** Game khởi đầu bằng 3 Nông dân được spawn sẵn gần Lâu đài[cite: 2, 3]. Cả 3 đơn vị này mặc định ở trạng thái `Idle` chờ người chơi phân bổ đi thu hoạch tài nguyên[cite: 2].

---

## 4. Hạn Chế Hiện Tại & Kế Hoạch Task Tiếp Theo (TODO)
Hiện tại, hệ thống thi công công trình đang được tạm gác lại. Để hoàn thiện vòng lặp xây dựng, task tiếp theo cần thực hiện các hạng mục sau:
1. **Tạo script `ConstructionSite.cs`:** Đại diện cho nền móng công trình đang xây, có chứa thanh tiến độ `buildProgress`.
2. **Cập nhật `WorkerFSM.cs`:** 
   - Bổ sung hàm `AssignToBuild(ConstructionSite site)`.
   - Hoàn thiện logic xử lý cho 2 trạng thái `MovingToBuild` và `Building`.
3. **Hoàn thành công trình:** Khi Nông dân "gõ búa" đạt 100% tiến độ, `ConstructionSite` sẽ biến thành công trình thật (ví dụ Lâu đài hoặc Trụ phòng thủ), và Nông dân tự động chuyển về trạng thái `Idle`.