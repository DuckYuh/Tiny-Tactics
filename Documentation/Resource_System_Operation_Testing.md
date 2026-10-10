# Hướng dẫn Vận hành và Kiểm thử (Operation & Testing) Hệ thống Tài nguyên

Tài liệu này trình bày chi tiết cách thiết lập, sử dụng và kiểm thử (testing) hệ thống **Resource Spawner & Lifecycle System** (Hệ thống Sinh & Quản lý vòng đời tài nguyên) bao gồm: `MapResourceManager`, `SheepSpawner` và `ResourceDeposit` vừa được phát triển.

---

## 1. Hướng dẫn thiết lập (Setup) trên Unity Editor

### 1.1 Thiết lập `MapResourceManager` (Quản lý tài nguyên tổng)
Đây là trái tim của hệ thống quản lý tài nguyên. Nó phải luôn tồn tại trên mọi cảnh (Scene) có gameplay.

1. Tại cửa sổ **Hierarchy**, tạo một GameObject rỗng mới.
2. Đặt tên GameObject là `GameManagers` (hoặc `ResourceManager`).
3. Nhấp `Add Component`, tìm và thêm Script **`MapResourceManager`**.
4. *(Tùy chọn)* Đặt GameObject này tại tọa độ `(0, 0, 0)` để dễ quản lý.
5. **Lưu ý:** Script này tự động quyét tìm tất cả các cục Gỗ, Vàng, Cừu (các Object có chứa `ResourceDeposit`) ngay khi bắt đầu (Start), do đó bạn không cần phải kéo thả tài nguyên thủ công vào Script này.

### 1.2 Thiết lập `SheepSpawner` (Hệ thống sinh Cừu)
Dùng để tự động sinh tài nguyên (Thịt) theo chu kỳ tránh việc map bị cạn kiệt nguồn cung cấp.

1. Tại cửa sổ **Hierarchy**, tạo một GameObject rỗng. Đặt tên là `SheepSpawner_Area1` (Bạn có thể tạo nhiều vùng chăn thả khác nhau).
2. Nhấp `Add Component`, thêm Script **`SheepSpawner`**.
3. Cấu hình các thông số trên Inspector:
   - **Sheep Prefab:** Kéo thả Prefab con Cừu (Prefab này bắt buộc phải có gắn script `ResourceDeposit` và `resourceType = Meat`).
   - **Spawn Interval:** Tốc độ sinh sản (Ví dụ: `15` giây/con).
   - **Max Active Sheep:** Số lượng tối đa cừu được phép có mặt trên sân do Spawner này sinh ra (Ví dụ: `10` con, giúp map không bị giật lag nếu người chơi không thu thập).
   - **Spawn Radius:** Chỉnh vòng tròn bán kính khu vực spawn cừu. Bạn bật nút **Gizmos** trong màn hình Scene để thấy vòng tròn lưới màu Xanh lá.
4. Kéo di chuyển GameObject `SheepSpawner_Area1` tới vị trí bãi cỏ trống mà bạn muốn cừu xuất hiện.

---

## 2. Tích hợp API vào Worker FSM (Nông dân AI)

Nhờ hệ thống này, Nông dân (Worker) của bạn sẽ thông minh hơn khi khai thác. Khi một cây gỗ bị đốn hạ hết, bạn có thể gọi API sau trong `Worker_FSM` để tìm cây gỗ mới gần nhất:

```csharp
// Khi cây gỗ đang chặt bị hết (OnDepleted), chuyển sang trạng thái "Tìm tài nguyên mới"
private void FindNextResource()
{
    float maxSearchRadius = 25f; // Bán kính tối đa nông dân chịu đi bộ
    
    ResourceDeposit nextTarget = MapResourceManager.Instance.GetClosestResource(
        transform.position, 
        ResourceType.Wood, 
        maxSearchRadius
    );

    if (nextTarget != null)
    {
        // Gán mục tiêu mới và cho Nông dân đi tới đó
        SetTarget(nextTarget.gameObject);
        ChangeState(WorkerState.MovingToResource);
    }
    else
    {
        // Quanh đây hết gỗ rồi, đứng chơi hoặc đi về kho
        ChangeState(WorkerState.Idle);
    }
}
```

---

## 3. Kịch bản Kiểm thử (Test Cases)

Dưới đây là các kịch bản bạn cần thực hiện trong chế độ Play Mode để đảm bảo hệ thống không có bug.

### Test Case 1: Vòng đời và Cập nhật PathfindingGrid
**Mục đích:** Đảm bảo khi đốn cây / khai thác mỏ xong, ô đất đó có thể đi qua lại được.
- **Bước 1:** Kéo 1 khối hộp làm nông dân (hoặc dùng Nông dân thật) đi tới đốn một cây Gỗ (`currentAmount = 10`).
- **Bước 2:** Bấm Play. Theo dõi Inspector của cây Gỗ xem `currentAmount` giảm dần.
- **Bước 3:** Khi `currentAmount` về `<= 0`, kiểm tra xem Cây Gỗ có bị biến mất (Destroy) không.
- **Bước 4:** Chọn Nông dân, click chuột phải vào đúng vị trí cái cây vừa biến mất.
- **Kết quả mong đợi:** Nông dân đi thẳng vào ô đất đó mà không bị vướng / phải đi vòng. Bật Gizmos PathfindingGrid sẽ thấy ô đó mất màu đỏ (hết vật cản).

### Test Case 2: Tính ổn định của SheepSpawner
**Mục đích:** Kiểm tra khả năng giới hạn số lượng và logic chống "kẹt tường".
- **Bước 1:** Chỉnh `Spawn Interval = 1f` (1 giây sinh 1 con để test cho nhanh) và `Max Active Sheep = 5`. Đặt Spawner gần một vách núi hoặc tòa nhà (vùng có `isWalkable = false`).
- **Bước 2:** Bấm Play và chờ 5 giây. Đếm số cừu trên màn hình.
- **Bước 3:** Đợi thêm 10 giây. Đếm số cừu trên màn hình.
- **Bước 4:** Cho nông dân giết/thu thập 2 con cừu.
- **Kết quả mong đợi:**
  - Cừu KHÔNG BAO GIỜ sinh ra đè lên vách núi hay nhà cửa.
  - Ban đầu sẽ đẻ ra đúng 5 con và dừng lại, không đẻ thêm dù có đợi 10 giây.
  - Sau khi giết 2 con (còn 3 con), hệ thống tự động sinh thêm 2 con để bù vào (về lại 5 con).

### Test Case 3: Nông dân Tự Chuyển Mục Tiêu (Re-targeting)
**Mục đích:** Kiểm tra xem `MapResourceManager` có đăng ký tài nguyên cừu mới sinh không và tính toán chính xác mục tiêu gần nhất.
- **Bước 1:** Thiết lập 1 bãi spawn Cừu. Đứng đợi đến khi có 3 con cừu xuất hiện.
- **Bước 2:** Dùng script nông dân tự động tìm Cừu (Meat) gần nhất thông qua `GetClosestResource()`. 
- **Bước 3:** Quan sát con cừu mà Nông dân đang nhắm tới, xóa con cừu đó bằng tay (bấm Delete ở cửa sổ Hierarchy).
- **Kết quả mong đợi:** FSM của Nông dân phát hiện mục tiêu bị mất, tự động kích hoạt lại hàm tìm kiếm, nhắm vào 1 trong 2 con cừu còn lại gần nhất và tiếp tục làm việc, không văng lỗi `NullReferenceException` trên Console.
