# AoE Mini — Task Level Design & Grid Map

## 1. Mục tiêu Task

Xây dựng bản đồ PvE trong Unity, tổ chức Tilemap theo từng lớp hiển thị, thiết lập khu vực vật cản/choke point, Spawn Point và kiểm tra khả năng tương tác giữa Level Design với hệ thống A* Pathfinding.

---

## 2. Cấu trúc Map

Scene PvE được tổ chức theo cấu trúc:

```text
PvE
├── Main Camera
├── Managers
├── Map
├── Grid
│   ├── Obstacles
│   ├── Water
│   ├── Foam
│   ├── Ground_Base
│   ├── Ground_Higher
│   └── Ground_Wall
└── SpawnPoints
    ├── PlayerSpawn
    └── EnemySpawn
```

---

## 3. Thiết kế Tilemap

### Obstacles
- Chứa các khu vực thực sự ngăn cản Unit di chuyển.
- Sử dụng `Obstacle` Layer.
- Có Collider để `PathfindingGrid` nhận diện trạng thái không thể đi.
- Đây là lớp Tilemap gameplay quan trọng đối với hệ thống Pathfinding.

### Water
- Lớp nước nền bao phủ toàn map
- Chỉ phục vụ hiển thị.

### Foam
- Lớp bọt, tạo hiệu ứng dưới nước
- Chỉ phục vụ hiển thị.

### Ground_Base
- Lớp địa hình nền chính của map.
- Chỉ phục vụ hiển thị.

### Ground_Higher
- Bổ sung chi tiết và nâng tầng địa hình.
- Chỉ phục vụ hiển thị.

### Ground_Wal
- Bổ sung thêm các loại địa hình, vật thể cản chân.
- Chỉ phục vụ hiển thị.

> Các Tilemap phục vụ hiển thị được tách khỏi Tilemap `Obstacles` để tránh việc chi tiết hình ảnh vô tình ảnh hưởng đến logic di chuyển.

---

## 4. Thiết kế địa hình và Choke Point

Map được thiết kế với:

- Khu vực di chuyển rộng cho Unit.
- Các khu vực vật cản.
- Choke Point tạo điểm thắt trong đường di chuyển.
- Khu vực chuyển tiếp địa hình/dốc theo thiết kế map.
- Khu vực Player và Enemy được bố trí riêng biệt.

### Nguyên tắc thiết kế

Do hệ thống A* hiện tại sử dụng Grid theo từng ô, các vật cản gameplay cần được thiết kế phù hợp với kích thước cell.

Không sử dụng vật cản chỉ chiếm một phần cell để làm gameplay blocker nếu điều đó khiến toàn bộ node bị đánh dấu `non-walkable`.

Các chi tiết hình ảnh có thể nằm trên Ground Tilemap mà không ảnh hưởng đến Pathfinding.

---

## 5. Spawn Point

Đã tạo:

```text
SpawnPoints
├── PlayerSpawn
└── EnemySpawn
```

### Vị trí Spawn

- `PlayerSpawn` được đặt trong khu vực gameplay hợp lệ.
- `EnemySpawn` được đặt trong khu vực gameplay hợp lệ.
- Không đặt trực tiếp trên obstacle.
- Không đặt sát mép map.
- Có đủ khoảng trống xung quanh để Unit có thể hoạt động.
- Có đường đi hợp lệ giữa khu vực Player và Enemy.

> Spawn Point hiện tại chỉ là marker phục vụ Level Design. Hệ thống spawn Unit tự động chưa nằm trong task này.

---

## 6. Thiết lập Camera cho Map

Camera được cấu hình để hỗ trợ việc quan sát và di chuyển trong map RTS:

- Edge Scrolling.
- Mouse Wheel Zoom.
- Giới hạn Zoom.
- Giới hạn Camera theo phạm vi Map.
- Không cho Camera di chuyển ra ngoài Map Bounds.
- Không xử lý camera movement khi con trỏ đang nằm trên UI.

### Kết quả kiểm tra

| Kiểm tra | Kết quả |
|---|---|
| Edge Scrolling | PASS |
| Mouse Wheel Zoom | PASS |
| Camera Clamp theo Map Bounds | PASS |

`Pixel Perfect Camera` được giữ lại như một thành phần rendering. Trong quá trình test RTS Camera, component này được disable để tránh ảnh hưởng đến hành vi Orthographic Zoom.

---

## 7. Kiểm thử Level Design + Pathfinding

Đã thực hiện các bài kiểm tra:

| Test | Kết quả |
|---|---|
| Player → Enemy có đường đi | PASS |
| Unit đi xuyên qua Choke Point | PASS |
| Nhiều Unit đi qua Choke Point | PASS |
| Formation hoạt động khi đi qua Choke Point | PASS |
| Click chuột vào Obstacle | PASS |
| Unit tìm được vị trí Walkable gần Obstacle | PASS |
| Click ngoài Map | PASS / Không crash |

---

## 8. Kết quả Task

Task **Level Design & Grid Map** đã hoàn thành.

Phạm vi đã hoàn thành:

```text
Map Layout
    ↓
Tilemap Structure
    ↓
Ground Layers
    ↓
Obstacle Layer
    ↓
Choke Points
    ↓
Player / Enemy Spawn Points
    ↓
RTS Camera
    ↓
Pathfinding Validation
```

Bản đồ hiện tại đã đáp ứng yêu cầu để tiếp tục phát triển các hệ thống gameplay tiếp theo.

---

## 9. Checklist trước khi Push Git

- [ ] Thoát Play Mode.
- [ ] Kiểm tra lại các thay đổi Tilemap trong Edit Mode.
- [ ] Save Scene.
- [ ] Kiểm tra `Obstacles` đang dùng đúng Layer `Obstacle`.
- [ ] Kiểm tra `PathfindingGrid` đã nhận `Obstacle Layer Mask`.
- [ ] Kiểm tra Camera Bounds khớp với Map.
- [ ] Kiểm tra `PlayerSpawn`.
- [ ] Kiểm tra `EnemySpawn`.
- [ ] Xóa các object test không còn sử dụng.
- [ ] Kiểm tra Unity Console không có Error.
- [ ] Commit và Push.

### Commit đề xuất

```text
feat: complete level design and grid map

- build PvE tilemap structure
- add ground and obstacle layers
- design choke points and gameplay areas
- add player and enemy spawn markers
- implement RTS camera scrolling and zoom
- validate map with A* pathfinding
```

---

## 10. Trạng thái

**LEVEL DESIGN & GRID MAP — COMPLETED**

Không cần bổ sung thêm tính năng Level Design vào milestone này. Các hệ thống như Unit Spawn thực tế, Combat, Dynamic Building hoặc Resource Gathering sẽ được xử lý ở task/milestone riêng.
