# Báo cáo hoàn thành — Building System (Tiny Tactics / AoE Mini)

> **Phạm vi:** Hệ thống đặt, chọn, hiển thị thông tin và phá công trình dành cho người chơi **Blue**; dữ liệu prefab có hỗ trợ **Red** để tích hợp AI về sau.  
> **Công nghệ:** Unity 2D, C#, Unity Input System, Physics2D, Pathfinding Grid/A*.  
> **Trạng thái:** Các chức năng trong mục 2 đã được kiểm thử và xác nhận hoạt động; cơ chế **Worker thi công** và **AI Red tự xây** chưa thuộc phần triển khai này.

---

## 1. Tổng quan và kiến trúc

Building System được chia thành các module độc lập nhằm tái sử dụng logic, giảm xung đột với hệ thống chọn quân và thuận tiện mở rộng thêm công trình/phe.

```text
Building Menu (CommandHUD) ── chọn BuildingData ──► BuildingPlacementController
                                                       │
                                                       ├─ Preview + snap Grid
                                                       └─ BuildingPlacementService
                                                            ├─ BuildingPlacementValidator
                                                            │    ├─ PathfindingGrid
                                                            │    └─ Physics2D (Unit Layer)
                                                            ├─ ResourceManager (Wood của Blue)
                                                            ├─ BuildingOccupancyManager
                                                            └─ BuildingInstance + BuildingSelectionVisual

InputController ── click chọn nhà ──► BuildingDemolitionController
                                         ├─ BuildingSelectionVisual
                                         ├─ BuildingSelectionUI (SelectionHUD)
                                         └─ Demolish → Destroy
                                                        └─ BuildingInstance.OnDestroy
                                                            └─ BuildingOccupancyManager.Release
```

**Nguyên tắc:** UI và Input chỉ gửi yêu cầu; dữ liệu nhà nằm trong `BuildingData`; phần kiểm tra và tạo công trình dùng chung được tách khỏi phần điều khiển bằng chuột.

## 2. Các chức năng đã hoàn thành

| Hạng mục | Kết quả |
|---|---|
| `BuildingData` (ScriptableObject) | Lưu tên, `Footprint`, `WoodCost`, `BuildingIcon` và danh sách prefab theo `FactionId`. |
| Hỗ trợ nhiều loại nhà | Mỗi loại nhà có một asset `BuildingData` riêng; có thể thêm nhà mới qua Inspector, không tạo controller riêng. |
| Hỗ trợ prefab theo phe | Mỗi `BuildingData` có danh sách `Faction Prefabs` với nút `+` (ví dụ `Blue`, `Red`). |
| Building Placement | Di chuyển preview theo chuột, căn về Grid, hiển thị trạng thái hợp lệ/không hợp lệ. |
| Placement Validation | Kiểm tra footprint trong bản đồ, các ô `isWalkable`, và Collider2D của Unit trong vùng xây. |
| Chi phí xây | Kiểm tra và tiêu hao Wood khi đặt nhà thành công; không trừ tài nguyên nếu đặt thất bại. |
| Building Occupancy cơ bản | Đánh dấu các ô Grid của công trình không thể đi qua; chặn xây chồng. |
| Building Lifecycle | Gắn `BuildingInstance` lưu `BuildingData`, `FactionId`; tự gọi `Release()` khi công trình bị hủy. |
| Building Selection | Click để chọn nhà Blue/Red, đổi highlight; click đất hoặc chọn Unit để bỏ chọn. |
| Demolish | Phím `Delete` hoặc nút `Demolish` phá nhà **Blue**; không cho người chơi phá nhà **Red** bằng lệnh này. |
| CommandHUD / Building Menu | Nút Build hoặc phím `B` chuyển qua lại giữa `CommandPanel` và `BuildingPanel`; chọn nhà để vào chế độ đặt. |
| SelectionHUD | Hiển thị icon, tên nhà, phe sở hữu; nút `Demolish` chỉ xuất hiện với nhà Blue. |
| Đồng bộ icon UI | `BuildingButton` và `SelectionHUD` lấy sprite qua `BuildingData.BuildingIcon`, giữ kích thước UI cố định (`Preserve Aspect`). |
| Tích hợp Input | Chặn click điều khiển Unit khi đang xây; kiểm tra click trên UI bằng `EventSystem.RaycastAll()` để tránh warning từ Input Action callback. |

**Lưu ý về trạng thái nhà:** Hiện công trình xuất hiện **hoàn chỉnh ngay khi click đặt**; chưa có nền móng, thời gian thi công hoặc tiến trình do Worker tăng.

## 3. Luồng hoạt động

### 3.1. Xây công trình (Blue)

1. Người chơi nhấn `B` / `Btn_Build` để mở danh sách công trình trong `CommandHUD`.
2. Chọn một nút công trình; `BuildingMenuUI` truyền `BuildingData` cho `BuildingPlacementController.StartPlacement(data)`.
3. Controller lấy prefab thông qua `data.GetPrefab("Blue")` và hiển thị preview theo chuột.
4. `BuildingPlacementValidator` kiểm tra toàn bộ footprint trên `PathfindingGrid`, giới hạn bản đồ và Unit Collider2D; preview xanh/đỏ dựa trên vị trí và Wood hiện có.
5. Click trái tại vị trí hợp lệ: `BuildingPlacementService` tạo prefab, đăng ký footprint qua `BuildingOccupancyManager`, tiêu hao Wood từ `ResourceManager`, sau đó khởi tạo `BuildingInstance` và selection visual.
6. Khi thất bại tại bước đăng ký/tiêu hao tài nguyên, service hoàn tác thay đổi Grid và xóa object vừa tạo.

**Quy ước tọa độ:** `anchorWorldPosition` nằm trong ô **dưới-trái** của footprint; công trình (sprite pivot giữa) được tạo tại **tâm footprint** sau khi snap Grid.

### 3.2. Chọn và phá nhà

1. `InputController` nhận click, ưu tiên chọn Unit; nếu không trúng Unit thì thử chọn công trình qua `BuildingDemolitionController`.
2. `BuildingSelectionVisual` đổi màu để đánh dấu nhà được chọn; `BuildingSelectionUI` nhận `OnSelectionChanged` để cập nhật thông tin **không polling mỗi frame**.
3. Người chơi nhấn `Delete` hoặc `Btn_Demolish`; controller chỉ chấp nhận công trình có `FactionId == "Blue"`.
4. Công trình bị `Destroy()`; `BuildingInstance.OnDestroy()` gọi `BuildingOccupancyManager.Release()` để giải phóng footprint.
5. Có thể xây lại trên vị trí vừa phá nếu vị trí không còn vật cản hoặc Unit.

## 4. Các script chính

| File / module | Vai trò |
|---|---|
| `Scripts/Buildings/BuildingData.cs` | Cấu hình mỗi loại nhà, icon, cost, footprint, prefab theo phe. |
| `Scripts/Buildings/BuildingPlacementValidator.cs` | Kiểm tra Grid/Unit; trả về vị trí snap và lý do không hợp lệ. |
| `Scripts/Buildings/BuildingOccupancyManager.cs` | Theo dõi footprint từng nhà, khóa/mở node khi đăng ký hoặc giải phóng. |
| `Scripts/Buildings/BuildingPlacementService.cs` | Logic đặt nhà dùng chung, không phụ thuộc chuột/UI. |
| `Scripts/Buildings/BuildingPlacementController.cs` | Input đặt nhà và preview cho người chơi Blue. |
| `Scripts/Buildings/BuildingInstance.cs` | Thông tin runtime, phe sở hữu và giải phóng Grid khi bị hủy. |
| `Scripts/Buildings/BuildingDemolitionController.cs` | Chọn nhà và xử lý quyền phá nhà Blue. |
| `Scripts/Buildings/BuildingSelectionVisual.cs` | Hiển thị highlight công trình được chọn. |
| `Scripts/UI/BuildingMenuUI.cs` | Chuyển panel Command/Building, hiển thị cost, icon và trạng thái nút. |
| `Scripts/UI/BuildingSelectionUI.cs` | Hiển thị thông tin nhà và gọi Demolish. |
| `Scripts/Input/RTSInputMode.cs` | Chặn world input trong chế độ đặt nhà. |
| `Scripts/Input/InputController.cs` | Tích hợp chọn Building, bảo vệ selection/command của Unit và xử lý UI raycast. |
| `Scripts/Pathfinding/PathfindingGrid.cs` | Cung cấp Grid và trạng thái `PathNode.isWalkable` đã có từ trước. |
| `Scripts/Economy/ResourceManager.cs` | Cung cấp lượng Wood và API tiêu hao tài nguyên. |

> Đường dẫn trên được ghi tương đối với `Assets/_Project/`; tên object/prefab cụ thể có thể khác tùy cách sắp xếp trong Project.

## 5. Cấu hình Unity và tài nguyên cần có

### 5.1. BuildingData và Prefab

- Tạo **một `BuildingData` cho mỗi loại công trình** (Barracks, House, Archery, Stable, ...).
- Đặt `BuildingName`, `Footprint` (theo số ô Grid), `WoodCost`, `BuildingIcon`.
- Trong `Faction Prefabs`, nhấn `+` để thêm các phiên bản phe, tối thiểu theo nhu cầu gameplay: `Blue → <Prefab_Blue>`, `Red → <Prefab_Red>`.
- **`FactionId` phân biệt hoa/thường:** phải nhập đúng `Blue`, `Red`; `blue` sẽ không được nhận là `Blue`.
- Prefab cần `SpriteRenderer` phù hợp với preview và `Collider2D` để chọn nhà; sprite của nhà nên có pivot ở giữa để khớp vị trí snap.
- Với prefab có nhiều SpriteRenderer con, cần kiểm tra hình preview vì bản hiện tại ưu tiên SpriteRenderer ở root.

### 5.2. Grid và Unit

- Scene cần `PathfindingGrid` với kích thước, `cellSize`, `originPosition` và `obstacleLayerMask` đúng bản đồ.
- Tất cả Unit Blue/Red có Collider2D thuộc Layer `Unit` để validator ngăn xây đè lên quân.
- Không đưa Layer `Unit` vào `obstacleLayerMask` của Grid nếu không muốn Unit trở thành vật cản tĩnh khi khởi tạo.
- Scene cần `ResourceManager`, `BuildingOccupancyManager`, `BuildingPlacementController`, `BuildingDemolitionController` và `InputController` đã gán các reference cần thiết.

### 5.3. Hierarchy HUD (minh họa cấu trúc đã triển khai)

```text
Canvas
├── Dragbox
└── GameplayHUD
    ├── TopHUD
    │   └── ResourceHUD
    └── BottomHUD
        ├── CommandHUD
        │   ├── Background
        │   ├── CommandPanel
        │   │   └── Btn_Build
        │   └── BuildingPanel
        │       ├── Header
        │       │   ├── Title
        │       │   └── Btn_Back
        │       └── BuildingList
        │           └── BuildingButton (các instance)
        ├── SelectionHUD
        │   ├── Background
        │   └── BuildingInfoPanel
        │       ├── BuildingIcon
        │       ├── BuildingName
        │       ├── FactionText
        │       └── Btn_Demolish
        └── MinimapHUD
```

- `ResourceHUD` giữ thành prefab độc lập. `CommandHUD`, `SelectionHUD` và các nút Building có thể đóng gói thành các prefab riêng để tái sử dụng.
- `BuildingMenuUI` gắn trên `CommandHUD`: gán `CommandPanel`, `BuildingPanel`, `Btn_Build`, `Btn_Back`, `BuildingPlacementController` và từng entry (`BuildingData`, Button, TMP Name/Cost, Icon Image).
- `BuildingSelectionUI` gắn trên `SelectionHUD` (không gắn vào panel bị tắt): gán `BuildingDemolitionController`, `BuildingInfoPanel`, icon, text và `Btn_Demolish`.
- `BuildingInfoPanel` để Inactive ban đầu; khi chọn nhà, script tự bật.
- `Image` trang trí nên tắt `Raycast Target` nếu không có mục đích chặn click lên map; giữ khả năng nhận click ở Image của Button.
- `MinimapHUD` và nút Options thuộc bố cục HUD, **chưa được mô tả là đã có chức năng minimap/options trong phạm vi Building System**.

## 6. Cách sử dụng trong gameplay

| Thao tác | Kết quả |
|---|---|
| `B` hoặc click `Btn_Build` | Mở/đóng danh sách công trình ở `CommandHUD`. |
| Click một `BuildingButton` | Chọn loại nhà và bắt đầu preview placement. |
| Di chuột trên map | Preview snap Grid; xanh nếu hợp lệ/đủ Wood, đỏ nếu không. |
| Click trái khi preview hợp lệ | Xây công trình của Blue và tiêu hao Wood. |
| `ESC` hoặc chuột phải khi preview | Hủy chế độ đặt nhà. |
| Click công trình | Chọn nhà, bật highlight, cập nhật `SelectionHUD`. |
| Click Unit / ground | Bỏ chọn nhà (Unit vẫn được hệ thống selection xử lý). |
| `Delete` hoặc `Btn_Demolish` | Phá công trình Blue đang chọn; nhà Red không có quyền Demolish từ người chơi. |

## 7. Kiểm thử đã xác nhận

- Chuyển `CommandPanel` ↔ `BuildingPanel` bằng phím B, Build và Back.
- Các loại công trình lấy đúng prefab Blue, footprint, icon và Wood Cost từ `BuildingData`.
- Preview đổi màu theo Grid, vật cản, Unit và lượng tài nguyên; không đặt nhà ngoài map hoặc chồng nhà/Unit.
- Đặt công trình thành công trừ đúng Wood; không đủ Wood thì nút xây bị vô hiệu hóa.
- Input chọn Unit, quét chọn, click UI và lệnh di chuyển không xung đột với chế độ đặt nhà; warning `IsPointerOverGameObject()` trong callback đã xử lý bằng UI raycast.
- Chọn nhà đổi highlight và SelectionHUD; phá nhà Blue bằng Delete/nút Demolish; nhà Red không cho phá theo lệnh của người chơi.
- Sau khi phá, Grid được giải phóng và có thể xây lại tại vị trí cũ; các công trình khác tiếp tục chặn đường.
- Icon UI hiển thị đúng tỷ lệ, không còn bị phóng to do `SetNativeSize()`.

## 8. Giới hạn hiện tại và phần tích hợp tiếp theo

1. **Worker Construction (chưa làm):** Nhà đang được tạo hoàn chỉnh ngay lập tức. Chưa có `ConstructionSite`, `buildProgress`, `AssignToBuild()` và các trạng thái `MovingToBuild`/`Building` hoạt động theo vòng thi công. Đây là phần cần phối hợp với người phụ trách `WorkerFSM` sau khi các module phụ thuộc sẵn sàng.
2. **AI Red (chưa làm):** `BuildingData` có prefab Red và service nhận `factionId`, nhưng chưa có AI quyết định vị trí/timing xây nhà và nguồn tài nguyên Red độc lập. **Không dùng `ResourceManager.Instance` của Blue để trừ gỗ cho AI Red.**
3. **Occupancy nâng cao (tạm hoãn):** Phiên bản đang sử dụng gọi `UpdateNodeWalkable(..., false/true)` khi xây/phá; chưa phân tách vật cản nền và nhiều nguồn chặn động. Nếu bổ sung vật cản động, cần nâng cấp trước khi giải phóng ô Grid để tránh mở nhầm đường.
4. **Sản xuất quân, HP/Combat, nâng cấp nhà:** Không thuộc phạm vi đã hoàn thành; cần module gameplay tương ứng.
5. **Preview:** Đang dựa trên `SpriteRenderer` của prefab; các nhà nhiều lớp sprite/animation cần nâng cấp cách dựng preview nếu hiển thị chưa đúng.
6. **Faction ID:** Đang dùng chuỗi `"Blue"`/`"Red"` có phân biệt hoa/thường; có thể thay bằng `FactionData` hoặc định danh chung ở giai đoạn mở rộng.

## 9. Bàn giao và lưu ý khi đưa lên Git

- Bao gồm **script Building**, script UI/Input được chỉnh sửa, `BuildingData.asset`, các prefab công trình Blue/Red và prefab UI đã sử dụng, kèm sprite/icon phụ thuộc.
- Kiểm tra reference trên `BuildingData`, `BuildingMenuUI`, `BuildingSelectionUI`, `BuildingPlacementController`, `InputController` và các prefab trước khi commit.
- Không cần đưa script test phím số (`BuildingPlacementTest`) vào bản bàn giao nếu đã thay hoàn toàn bằng Building Menu.
- Theo phương án bàn giao hiện tại, **không commit scene PvP**; ưu tiên bàn giao prefab, script, asset và hướng dẫn setup. Nếu không commit Scene test, người tích hợp cần tạo các manager/HUD và gán lại reference theo mục 5.
- Không thay đổi `WorkerFSM`, `RTSUnitManager` hoặc thuật toán A* chỉ để triển khai Building UI/Placement.

---

**Kết luận:** Phạm vi **Building Placement + Building UI + Building Selection/Demolish cho Blue** đã hoàn thành và được kiểm thử. Hệ thống có nền tảng dữ liệu prefab cho Red, nhưng **Worker thi công, AI tự xây và Occupancy đa nguồn** vẫn là các phần tích hợp/cải tiến về sau.
