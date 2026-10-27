# Báo cáo Kết quả Task – Interactive Minimap (Tiny Tactics)

## 1. Tổng quan

- **Dự án:** Tiny Tactics (AoE Mini) – RTS 2D Pixel Art trên Unity.
- **Sprint:** 3 (Tuần 5–6).
- **Phụ trách:** Huy.
- **Task:** Interactive Minimap – radar góc phải dưới HUD, hỗ trợ click điều hướng Camera.
- **Trạng thái:** Hoàn thành; các bước 1–4.4 đã được kiểm thử trong Unity theo xác nhận của người triển khai.

Minimap hiển thị toàn bộ bản đồ, khung vùng nhìn của Main Camera và các Marker đại diện cho Unit/Building theo phe Blue/Red. Người chơi có thể click hoặc kéo chuột trên Minimap để điều hướng Camera.

## 2. Luồng hoạt động

```text
World Map / Tilemap
       │
       ▼
MinimapCamera → RT_Minimap (RenderTexture) → MinimapDisplay (RawImage)
                                                │
                       ┌────────────────────────┴────────────────────────┐
                       ▼                                                 ▼
              MinimapController                                  MinimapViewport
              Click / Drag UI                                    Khung vùng nhìn
                       │                                                 ▲
                       ▼                                                 │
              RTSCameraController ────────────────────────────────────────┘
                       │
                       ▼
                 Main Camera

Unit / Building (MinimapTarget)
             │ OnEnable / OnDisable
             ▼
     MinimapMarkerManager
       ├── Mapping World → UI
       ├── Màu theo FactionId
       ├── Cập nhật theo chu kỳ
       └── Object Pooling → MinimapMarker (Image)
```

- **Hiển thị nền:** MinimapCamera render các layer cần thiết vào `RT_Minimap`; `RawImage` trình bày trong `MinimapHUD`.
- **Điều hướng:** `MinimapController` nhận `IPointerDownHandler` / `IDragHandler`, chuyển tọa độ click UI chuẩn hóa `[0,1]` sang World Position theo `RTSCameraController.MapBounds`; camera được giới hạn trong map.
- **Viewport Indicator:** `MinimapViewport` tính kích thước và vị trí vùng quan sát từ `MainCamera.orthographicSize`, `Camera.aspect` và `MapBounds`, cập nhật `RectTransform` thông qua anchors.
- **Marker:** `MinimapTarget` đánh dấu đối tượng; `MinimapMarkerManager` cấp phát/tái sử dụng `MinimapMarker` và cập nhật vị trí trên minimap.

## 3. Các thành phần triển khai

| Thành phần | Vai trò |
|---|---|
| `RTSCameraController.cs` | Điều khiển Main Camera; cung cấp `MainCamera`, `MapBounds` và `MoveToPosition()` để minimap dùng lại logic clamp. |
| `MinimapController.cs` | Xử lý click/drag trên `MinimapDisplay`, chuyển tọa độ UI thành vị trí thế giới. |
| `MinimapViewport.cs` | Đồng bộ khung nhìn trên minimap khi Camera di chuyển hoặc zoom. |
| `MinimapTarget.cs` | Component gắn trên Unit/Building, công bố `TargetType`, `FactionId`, `WorldPosition` và tự đăng ký/hủy đăng ký. |
| `MinimapMarker.cs` | Hiển thị Marker UI; cấu hình sprite, màu, kích thước và vị trí. |
| `MinimapMarkerManager.cs` | Quản lý Target, cập nhật Marker và tái sử dụng bằng Object Pooling. |
| `MinimapCamera` | Camera orthographic riêng để render nền minimap. |
| `RT_Minimap` | RenderTexture chứa hình nền minimap (đã thử nghiệm cấu hình 256×256). |
| `MinimapDisplay` | RawImage trong HUD góc phải dưới. |
| `MarkerContainer`, `ViewportIndicator` | Vùng chứa Marker UI và khung biểu diễn vùng nhìn. |

**Vị trí script theo cấu trúc hiện tại:** `Assets/_Project/Scripts/Camera/` (các script Minimap mới có namespace `TinyTactics.UI`).

## 4. Cấu hình và cách dùng

1. Đặt `MinimapCamera` bao quát toàn bản đồ, Projection = **Orthographic**, Target Texture = `RT_Minimap`. Cấu hình Culling Mask chỉ bao gồm những layer cần cho nền map để tránh render trùng Unit/Building đã được biểu diễn bằng Marker.
2. Trong `Canvas > GameplayHUD > BottomHUD > MinimapHUD`, đặt `MinimapDisplay` (RawImage) và gán Texture = `RT_Minimap`. Bật **Raycast Target** trên RawImage để nhận click/drag.
3. Gắn `MinimapController` lên `MinimapDisplay`, kéo `Main Camera` (có `RTSCameraController`) vào trường `Camera Controller`.
4. Tạo `ViewportIndicator` bên trong `MinimapDisplay`, gắn `MinimapViewport` và liên kết `Main Camera`, `MinimapDisplay`. Tắt **Raycast Target** của ảnh khung.
5. Tạo `MarkerContainer` phủ toàn bộ `MinimapDisplay`, gắn `MinimapMarkerManager`, liên kết `Main Camera`, chính `MarkerContainer`, prefab `MinimapMarker` và hai sprite hình tròn/hình vuông. Container không cần Image/RawImage để tránh chặn input.
6. Gắn `MinimapTarget` lên GameObject gốc của từng **Unit prefab** (`Target Type = Unit`) và **Building prefab** (`Target Type = Building`). Cấu hình `Faction ID = Blue/Red` tương ứng; `Marker Position` có thể để trống hoặc chỉ định điểm đại diện mong muốn.
7. Đảm bảo `RTSCameraController.MapBounds` khớp phạm vi world map (đã test map 100×100, origin (0,0), cell size 1). Map và khung UI cần cùng tỷ lệ hiển thị để chuyển đổi tọa độ chính xác.

**Quy ước hiển thị:** Unit = chấm tròn nhỏ (~5×5 UI); Building = hình vuông lớn hơn (~9×9 UI); Blue = xanh, Red = đỏ. Phạm vi task chỉ bao gồm Unit và Building, **không** có Marker tài nguyên hoặc chế độ lọc Economy/Military.

## 5. Hiệu năng và tích hợp hệ thống

- **Component-based:** Minimap không thay đổi logic A*, Separation hay FSM của Unit và không cần chỉnh sửa `UnitController.cs`.
- **Object Pooling:** Marker được thu hồi vào pool khi Target disable/destroy và tái sử dụng khi spawn/xây công trình mới; tránh instantiate/destroy UI liên tục.
- **Update định kỳ:** `MinimapMarkerManager` đặt tần suất cập nhật khoảng **10 lần/giây** thay vì từng frame.
- **Input UI:** `IPointerDownHandler` và `IDragHandler` được xử lý bởi EventSystem; ảnh Marker/Viewport tắt Raycast Target để không cản thao tác.
- **Building:** `BuildingInstance.FactionId` được sử dụng với Building đã `Initialize`; prefab/đối tượng đặt sẵn có `Faction ID` dự phòng trong `MinimapTarget`.

## 6. Kết quả kiểm thử

Các hạng mục sau được xác nhận hoạt động sau khi test trong Unity:

- Minimap hiển thị đúng ở **góc phải dưới HUD** và giữ nguyên bản đồ khi Main Camera di chuyển/zoom.
- Click hoặc drag trên minimap di chuyển Main Camera đúng vị trí và giữ camera trong ranh giới map.
- `ViewportIndicator` di chuyển theo camera, co giãn theo zoom và không chắn input.
- Unit và Building hiển thị Marker theo loại (tròn/vuông) và màu phe (Blue/Red).
- Marker Unit cập nhật theo di chuyển; camera di chuyển/zoom không làm sai vị trí Marker.
- Unit spawn hoặc Building được xây dựng sẽ tự có Marker nếu prefab mang `MinimapTarget`.
- Disable/Enable hoặc Destroy đối tượng làm Marker ẩn/hiện/thu hồi đúng; không xuất hiện Marker trùng lặp trong các bài test.
- Kiểm tra vòng đời, tái sử dụng Marker, thao tác UI và Profiler đã hoàn tất theo checklist Bước 4.4.

## 7. Giới hạn và lưu ý mở rộng

- **Fog of War (Sprint 5):** Hiện Minimap chưa lọc vị trí đối phương theo tầm nhìn. Khi tích hợp Fog of War cần ẩn Marker Unit địch ngoài vùng quan sát, và quyết định quy tắc hiển thị Building địch đã khám phá.
- **Map Bounds:** MinimapController, MinimapViewport và MarkerManager phải sử dụng cùng một `MapBounds`; nếu thay đổi kích thước/origin map hoặc tỷ lệ UI cần kiểm tra lại mapping.
- **RenderTexture:** Nên kiểm tra Culling Mask tránh render nền bị trùng Unit/Building với Marker UI. Có thể giảm tần suất render Camera nền nếu cần tối ưu thêm.
- **Thứ tự cập nhật:** `RTSCameraController` và `MinimapViewport` đều cập nhật trong LateUpdate; nếu quan sát thấy lệch 1 frame, cân nhắc Script Execution Order hoặc cơ chế đồng bộ sự kiện.
- **Phân biệt phe runtime:** Phiên bản Bước 4.4 hỗ trợ cập nhật màu Marker theo FactionId đang đọc; trường hợp game thực sự cho đổi phe runtime cần test riêng.

## 8. Tệp cần đưa vào commit

- Scripts: `MinimapController.cs`, `MinimapViewport.cs`, `MinimapTarget.cs`, `MinimapMarker.cs`, `MinimapMarkerManager.cs` và chỉnh sửa liên quan trong `RTSCameraController.cs` (nếu chưa có trên nhánh).
- UI Prefab/Hierarchy: `MinimapHUD`, `MinimapDisplay`, `MarkerContainer`, `ViewportIndicator`, `MinimapMarker.prefab`.
- Asset: `RT_Minimap.renderTexture`, sprite Marker Unit/Building (nếu tạo mới).
- Các thay đổi trong **prefab Unit/Building** do thêm `MinimapTarget` và file `.meta` tương ứng.
- Tài liệu kỹ thuật này.

**Phạm vi nghiệm thu:** Task Interactive Minimap hoàn thành theo Sprint 3. Tích hợp Fog of War thuộc Sprint 5 và không nằm trong phạm vi nghiệm thu hiện tại.
