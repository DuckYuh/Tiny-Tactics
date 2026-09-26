# Báo cáo Kết quả Sprint 1 - Core RTS Systems (Tiny Tactics)

Tài liệu này tổng hợp lại toàn bộ cơ chế hoạt động và cách thiết lập các hệ thống đã hoàn thành trong Sprint 1, bao gồm: **Input System (Quét chọn lính, Phím tắt)**, **Selection System (Quản lý chọn/hủy chọn)**, và **Pathfinding System (Tìm đường A* và Di chuyển theo đội hình)**.

---

## 1. Cơ chế Hoạt động Tổng thể

Luồng dữ liệu của trò chơi được thiết kế theo cấu trúc module độc lập và kết nối với nhau một cách tuần tự:

1. **Nhận diện Input (Chuột & Phím):** 
   - Thông qua `InputController`, mọi thao tác Click chuột (Single Select) hoặc Kéo chuột (Drag Select) sẽ được chuyển đổi sang toạ độ không gian 2D.
   - Thao tác kéo chuột vẽ ra một khung UI (Dragbox). Khi người chơi thả chuột, `InputController` tìm mọi quân lính nằm trong khung này.
2. **Quản lý Chọn quân (Selection):** 
   - `SelectionSystem` duy trì một danh sách các lính đang được chọn. 
   - Nếu người chơi ấn tổ hợp phím `Ctrl + 1/2/3`, `ArmyHotkeySystem` sẽ ghi nhớ danh sách này. Khi ấn phím `1/2/3`, danh sách lính được gọi lại thành công.
3. **Ra lệnh và Xếp Đội hình (Command & Formation):** 
   - Khi Click chuột phải xuống đất, `RTSUnitManager` sẽ nhận toạ độ đích.
   - Thay vì dồn toàn bộ lính vào 1 điểm, `RTSUnitManager` sử dụng thuật toán Vòng tròn đồng tâm (Spiral Formation) để chia toạ độ đích thành nhiều điểm nhỏ (cách nhau một khoảng `Unit Spacing`). Mỗi lính sẽ nhận một điểm đích riêng.
4. **Tìm đường và Di chuyển (Pathfinding & Movement):**
   - Từng lính (thông qua `UnitController`) nhận điểm đích và gọi dịch vụ A* (`Pathfinding.cs`). 
   - A* tính toán đường đi né vật cản dựa trên lưới `PathfindingGrid`. Nếu điểm đích bị nằm trong tường, A* tự động tìm ô trống gần nhất (BFS) để lính đứng bọc ngoài tường.
   - Lính di chuyển bằng `transform.position`. Khi lại gần nhau, lực Separation Force sẽ kích hoạt để chúng đẩy nhẹ nhau ra, tạo cảm giác di chuyển bầy đàn tự nhiên.

---

## 2. Hướng dẫn Gán Component (Scene Setup)

Để một Scene (màn chơi) hoạt động trơn tru với toàn bộ tính năng trên, bạn cần thực hiện theo các bước thiết lập dưới đây.

### 2.1. Thiết lập UI Dragbox (Khung quét chuột)
1. Trong cửa sổ **Hierarchy**, tạo `UI -> Canvas`.
2. Tạo một `UI -> Image` nằm trong Canvas, đặt tên là `DragBox`.
3. Trong component **Image**: 
   - Set **Sprite**: tìm và cài cursor_04.
   - Set **Color** thành một màu bán trong suốt (VD: Xanh lá, Alpha = 0.3).
   - **Tắt (Uncheck) Raycast Target** để khung không chặn click chuột của bạn.
4. Disable (tắt) GameObject `DragBox` này đi (Hệ thống sẽ tự bật nó lên khi bạn kéo chuột).

### 2.2. Thiết lập các Manager
Tạo một GameObject rỗng tên là `Managers` để gom nhóm các hệ thống trung tâm. Add các component sau vào:

- **Pathfinding Grid:**
  - `Grid Width`, `Grid Height`: Kích thước tổng của lưới.
  - `Cell Size`: Độ phân giải lưới (Khuyên dùng: `0.5` hoặc `1`).
  - `Obstacle Layer Mask`: Trỏ tới đúng Layer của bức tường/vật cản (VD: Layer `Obstacle`).
- **Selection System:** Không cần cấu hình gì thêm.
- **RTS Unit Manager:**
  - `Unit Spacing`: Khoảng cách khi lính đứng tụ tập lại (VD: `0.3`).
- **Army Hotkey System:** (Tùy chọn) Hỗ trợ phím tắt đạo quân.
- **Input Controller:**
  - `Main Camera`: Kéo Main Camera của Scene vào.
  - `Selection Box`: Kéo GameObject UI `DragBox` vừa tạo ở bước 2.1 vào đây.

### 2.3. Thiết lập Vật cản (Obstacle / Building)
Để lính không đi xuyên qua vách núi, vách nhà, bạn cần:
1. Gán một **Layer** riêng cho vật cản (VD: tạo Layer `Obstacle` và gán cho chúng).
2. Thêm component **BoxCollider2D** hoặc **PolygonCollider2D** cho vật thể để bao quanh vùng chân/móng nhà.
3. *Lưu ý:* Bật Gizmos (ở tab Scene) để kiểm tra xem `Pathfinding Grid` có bôi đỏ (khoá) đúng khu vực của vật cản hay không. Lưới phải bôi đỏ thì lính mới biết đường né.

### 2.4. Thiết lập Lính (Unit)
Tạo Prefab lính với các cấu hình bắt buộc sau:
1. Đặt GameObject của lính thuộc Layer `Unit` (Hoặc layer tuỳ chọn).
2. **Circle Collider 2D:**
   - Tick chọn **Is Trigger** (Bắt buộc để thuật toán quét chuột hoạt động mà không bị vướng cơ chế vật lý chặn đường).
   - Chỉnh `Radius` nhỏ gọn quanh tâm lính.
3. **Selectable Unit (Script):**
   - Gắn một GameObject vòng tròn trang trí dưới chân lính vào ô `Selection Indicator`. Nó sẽ tự động bật/tắt khi lính được chọn.
4. **Unit Controller (Script):**
   - `Move Speed`: Tốc độ di chuyển.
   - `Unit Layer Mask`: **Chỉ chọn Layer của Unit**. (Đây là layer để các lính nhận diện nhau và tự đẩy nhau ra. KHÔNG chọn layer Obstacle ở đây để tránh việc lính bị tường đẩy).
   - `Separation Radius`: Bán kính đẩy nhau (Khoảng `0.2`).
