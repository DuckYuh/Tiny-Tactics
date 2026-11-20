# Faction Visual -- Phân biệt Phe

## 1. Tổng quan

Task **Faction Visual** bổ sung khả năng phân biệt quân thuộc các phe
khác nhau trong game AoE Mini.

Trong phạm vi hiện tại: - Hỗ trợ hai phe được tích hợp vào luồng spawn:
**Blue** và **Red**. - Áp dụng cho ba loại quân: **Warrior**, **Archer**
và **Lancer**. - Quân được phân biệt bằng Animator Controller tương ứng
với phe. - Marker của quân trên Minimap được tô màu theo phe.

Mục tiêu là bảo đảm quân được tạo ra từ công trình của phe nào thì hiển
thị đúng animation và màu Minimap của phe đó.

## 2. Luồng hoạt động

Khi một Barracks huấn luyện xong quân:

1.  `BarracksBuilding` phát sự kiện `OnAnyUnitTrained`.
2.  `FactionSpawnVisualBinder` nhận sự kiện và lấy thông tin phe từ
    Barracks.
3.  Binder xác định loại quân dựa trên Animator Controller ban đầu của
    unit, sau đó tìm cấu hình tương ứng trong danh sách mapping.
4.  Binder thêm `FactionVisual` vào unit nếu unit chưa có component này.
5.  `FactionVisual.Configure()` nhận Animator Controller của hai phe và
    thiết lập controller theo faction của Barracks.
6.  Binder cập nhật faction cho `MinimapTarget` bằng `SetFaction()`.
7.  `MinimapMarkerManager` đọc `MinimapTarget.FactionId` và cập nhật màu
    marker trên Minimap.

Nhờ vậy, việc phân biệt phe được xử lý ngay sau khi unit spawn, không
cần tạo một prefab riêng chỉ để gắn logic xử lý faction cho từng phe.

## 3. Các thành phần chính

### 3.1. `FactionVisual.cs`

Chịu trách nhiệm quản lý hiển thị animation theo phe.

Các nhiệm vụ chính: - Lưu faction hiện tại thông qua `FactionId`. - Nhận
Animator Controller tương ứng với từng phe. - Chuyển Animator Controller
khi faction thay đổi. - Tránh gán lại controller nếu controller hiện tại
đã đúng.

Component có các cấu hình controller cho Blue, Red, Purple và Yellow.
Tuy nhiên, luồng spawn được tích hợp trong task này hiện chỉ cấu hình và
xử lý Blue/Red.

### 3.2. `FactionSpawnVisualBinder.cs`

Là cầu nối giữa sự kiện spawn quân của Barracks và hệ thống hiển thị
faction.

Các nhiệm vụ chính: - Đăng ký nhận sự kiện
`BarracksBuilding.OnAnyUnitTrained` khi component được enable và hủy
đăng ký khi disable. - Xác định faction của Barracks. - Tìm mapping phù
hợp cho loại quân dựa trên Animator Controller ban đầu. - Thêm
`FactionVisual` nếu unit chưa có component. - Gọi `Configure()` để áp
dụng Animator Controller của đúng phe. - Cập nhật faction của
`MinimapTarget` để marker hiển thị đúng màu.

Binder sử dụng danh sách mapping được cấu hình trong Inspector. Mỗi
entry liên kết một loại quân với Animator Controller Blue và Red tương
ứng.

### 3.3. `MinimapTarget.cs`

Lưu thông tin faction của đối tượng được hiển thị trên Minimap.

Phương thức được bổ sung:

``` csharp
public void SetFaction(string newFactionId)
{
    if (string.IsNullOrWhiteSpace(newFactionId))
        return;

    factionId = newFactionId;
}
```

Đối với đối tượng có `BuildingInstance` đã được khởi tạo, `FactionId` có
thể lấy trực tiếp từ building. Đối với unit, Binder gọi `SetFaction()`
sau khi xác định faction lúc spawn.

### 3.4. `MinimapMarkerManager.cs`

Quản lý marker của các đối tượng trên Minimap.

Manager đọc `FactionId` từ `MinimapTarget` để xác định màu marker. Trong
cấu hình hiện tại: - Blue: màu xanh/cyan. - Red: màu đỏ. - Faction không
được nhận diện: màu trung tính/trắng.

Manager đã có cơ chế tái sử dụng marker và cập nhật theo chu kỳ; task
này không yêu cầu thay đổi logic quản lý marker.

## 4. Tích hợp trong scene PvE

Trong scene `PvE`, tạo một GameObject quản lý riêng, ví dụ
`FactionSpawnVisualManager`, sau đó:

1.  Thêm component `FactionSpawnVisualBinder`.
2.  Cấu hình danh sách mapping trong Inspector.
3.  Tạo mapping cho `Warrior`, `Archer` và `Lancer`.
4.  Gán Animator Controller Blue và Red tương ứng cho từng entry.

Binder cần được active trong scene để nhận sự kiện spawn từ Barracks.

Không bắt buộc phải gắn sẵn `FactionVisual` lên từng prefab unit vì
Binder sẽ tự thêm component nếu unit chưa có.

## 5. Kiểm thử

Đã kiểm thử trong scene `PvE`:

  Loại quân   Phe Blue         Phe Red
  ----------- ---------------- ----------------
  Warrior     Đúng animation   Đúng animation
  Archer      Đúng animation   Đúng animation
  Lancer      Đúng animation   Đúng animation

Các nội dung kiểm thử bổ sung: - Unit spawn từ Barracks Blue sử dụng
Animator Controller Blue. - Unit spawn từ Barracks Red sử dụng Animator
Controller Red. - Marker Minimap của unit Blue và Red hiển thị đúng
màu. - Marker tiếp tục cập nhật vị trí khi unit di chuyển. - Kiểm tra
Console để bảo đảm không có lỗi phát sinh từ Binder hoặc quá trình cập
nhật faction.

Kết quả kiểm thử trong scene `PvE`: cả ba loại quân ở hai phe đều hiển
thị đúng animation và màu Minimap.

## 6. Phạm vi và giới hạn hiện tại

-   Luồng spawn được tích hợp cho hai phe Blue và Red.
-   Các loại quân được cấu hình gồm Warrior, Archer và Lancer.
-   Binder nhận sự kiện từ `BarracksBuilding`; nếu sau này có công trình
    huấn luyện quân sử dụng luồng spawn khác, cần tích hợp sự kiện tương
    ứng.
-   `FactionVisual` có trường controller cho Purple và Yellow, nhưng
    Binder hiện chưa có mapping spawn cho hai phe này.
-   Task chỉ xử lý hiển thị theo phe và màu marker Minimap; không thay
    đổi logic chiến đấu, di chuyển, chỉ số hoặc hành vi gameplay của
    unit.

## 7. Kết luận

Task **Faction Visual** hoàn thành phần phân biệt phe cho Warrior,
Archer và Lancer giữa Blue và Red trong scene `PvE`.

Animator Controller được chọn theo faction của Barracks tại thời điểm
spawn; faction của unit đồng thời được đồng bộ sang `MinimapTarget` để
marker hiển thị đúng màu. Các trường hợp kiểm thử đã hoàn tất theo phạm
vi nêu trên.
