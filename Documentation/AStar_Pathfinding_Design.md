# Thiết kế Hệ thống A* Pathfinding - Tiny Tactics

## 1. Bản chất & Nguyên lý hoạt động của Thuật toán A*

Thuật toán A* (A-Star) là thuật toán tìm đường tối ưu và phổ biến nhất trong các tựa game RTS nhờ sự kết hợp giữa thuật toán Dijkstra (đảm bảo tìm đường ngắn nhất) và Greedy Best-First Search (hướng về phía mục tiêu).

### Công thức cốt lõi
Công thức cơ bản của A* để đánh giá mỗi ô (node) trên lưới là:
`f(n) = g(n) + h(n)`

Trong đó:
- **`g(n)` (Chi phí thực tế)**: Là tổng chi phí đường đi từ điểm bắt đầu (Start Node) đến node hiện tại `n`. `g(n)` đảm bảo thuật toán luôn ưu tiên con đường có tổng khoảng cách đi lại ngắn nhất.
- **`h(n)` (Heuristic)**: Là chi phí ước lượng (chưa chính thức) từ node `n` đến điểm đích (Target Node). Hàm `h(n)` đóng vai trò là "kim chỉ nam", giúp thuật toán tập trung duyệt các node nằm gần đích hơn, thay vì loang ra mọi hướng một cách vô ích.
- **`f(n)` (Tổng chi phí)**: Đánh giá độ tốt của node. Thuật toán sẽ luôn chọn node có `f(n)` thấp nhất để xét tiếp.

### Ưu thế của A* trong game RTS
- **So với BFS (Breadth-First Search)**: BFS loang ra tất cả các hướng như vết dầu loang, cực kỳ tốn tài nguyên trên bản đồ lớn. A* sử dụng `h(n)` để hướng thẳng về phía mục tiêu, tiết kiệm CPU đáng kể.
- **So với Dijkstra**: Dijkstra tính toán khoảng cách thực tế chính xác và xử lý được chi phí địa hình khác nhau (đầm lầy, đường nhựa), nhưng không có hàm heuristic hướng đích, dẫn đến duyệt qua nhiều node dư thừa ở phía ngược lại. A* khắc phục hoàn toàn điểm này nhờ `h(n)`.

---

## 2. Heuristic phù hợp cho Grid 2D hỗ trợ di chuyển 8 hướng

Trong "Tiny Tactics", Grid 2D cho phép quân lính di chuyển 8 hướng (4 hướng thẳng và 4 hướng chéo). 
- **Manhattan Distance** chỉ đo khoảng cách trên trục x và y (hình chữ thập), hoàn toàn bỏ qua việc đi chéo. Việc dùng Manhattan cho 8 hướng sẽ khiến `h(n)` ước lượng bị sai lệch (cao hơn thực tế), làm chậm tốc độ hoặc tìm đường đi dích dắc.
- **Octile Distance** (hoặc Diagonal Distance) là sự lựa chọn hoàn hảo nhất cho 8 hướng.

### Công thức Octile Distance
Gọi `dx = |x_{node} - x_{target}|` và `dy = |y_{node} - y_{target}|`.
Thuật toán sẽ cho phép đi chéo nhiều nhất có thể (tương đương với `min(dx, dy)`), sau đó đi thẳng phần khoảng cách còn lại (`|dx - dy|`).

Theo Octile Distance, chi phí đi thẳng giữa 2 ô liền kề là `10`, và đi chéo là `14` (vì $\sqrt{10^2 + 10^2} \approx 14.14$).
Công thức toán học chính xác trong code C# sẽ là:
`h(n) = 14 * min(dx, dy) + 10 * |dx - dy|`

---

## 3. Quy trình thực thi thuật toán từng bước (Step-by-Step)

1. **Khởi tạo**:
   - `Open Set`: Danh sách các node cần được đánh giá. Khởi tạo chứa `Start Node`.
   - `Closed Set`: Danh sách các node đã được đánh giá xong, sẽ không bao giờ xét lại.
2. **Vòng lặp chính**:
   - Lấy node có `f(n)` thấp nhất từ `Open Set` làm `Current Node` để xét.
   - Kiểm tra: Nếu `Current Node` chính là `Target Node` $\rightarrow$ Kết thúc tìm kiếm (Tìm thấy đường).
   - Loại bỏ `Current Node` khỏi `Open Set` và thêm nó vào `Closed Set`.
3. **Duyệt node lân cận (Neighbors)**:
   - Duyệt qua 8 node xung quanh `Current Node`.
   - Nếu node lân cận là vùng không thể đi (`!isWalkable`) hoặc đã nằm trong `Closed Set` $\rightarrow$ Bỏ qua.
   - **Xử lý Corner Cutting**: Nếu node lân cận đang xét nằm ở góc chéo, ta phải kiểm tra 2 node liền kề tạo thành góc đó. Nếu 1 trong 2 hoặc cả 2 node đó bị block $\rightarrow$ Cấm đi chéo qua góc đó. Nhờ vậy, unit không bị lỗi "chui xuyên khe" giữa 2 bức tường đối góc.
   - Tính tổng chi phí `gCost` dự kiến (bằng `gCost` của Current + chi phí 10 hoặc 14 để đi tới lân cận).
   - Nếu chi phí này thấp hơn `gCost` hiện tại của node lân cận (đã có đường đi ngắn hơn), hoặc node lân cận chưa nằm trong `Open Set`:
     - Cập nhật lại thuộc tính `parent` của node lân cận trỏ về `Current Node`.
     - Cập nhật `gCost` và tính lại `hCost`.
     - Thêm nó vào `Open Set` nếu nó chưa có ở đó.
4. **Truy vết (Retrace Path)**:
   - Khi đã chạm tới đích, dùng vòng lặp `while` gọi thuộc tính `parent` của `Target Node` để dò ngược về `Start Node`.
   - Lưu các toạ độ vào một danh sách, sau đó đảo ngược (Reverse) mảng đó để có đường đi đúng từ Start $\rightarrow$ Target.
5. **Trường hợp biên (Edge Cases)**:
   - *Không tìm thấy đường (Unreachable Target)*: Vòng lặp chính sẽ tiếp tục cho đến khi `Open Set` rỗng sạch mà chưa chạm Target. Lúc này, hàm kết thúc sớm và trả về kết quả rỗng (không thể tới đích).

---

## 4. Áp dụng vào thực tế dự án "Tiny Tactics"

### 4.1 Xử lý Địa hình Tĩnh & Điểm thắt (Choke Points)
Đối với địa hình như rừng núi, hẻm núi hay cây cầu hẹp:
- `PathfindingGrid` khi khởi tạo (`Awake`) sẽ dùng `Physics2D.OverlapBox` hoặc quét Tilemap để map toàn bộ chướng ngại vật tĩnh vào thuộc tính `isWalkable = false` của Grid Array.
- Cùng với luật cấm Corner Cutting, lính sẽ tự động xếp thành hàng dọc khi đi qua những khu vực thắt cổ chai hẹp 1-2 ô mà không bị kẹt lọt vào địa hình. Về sau có thể kết hợp thêm cơ chế *Local Avoidance* (Steering Behaviors) để lính chen lấn nhường đường mượt mà hơn ở các ngã tư.

### 4.2 Xử lý Vật cản Động (Dynamic Building Obstacles)
Cơ chế đồng bộ giữa hệ thống xây dựng nhà và Grid:
- Khi người chơi đặt nền móng Lâu đài hoặc Tu viện, hệ thống lập tức gọi hàm `PathfindingGrid.Instance.UpdateNodeWalkable(worldPos, false)` cho toàn bộ các ô kích thước $3 \times 3$ của công trình.
- Kể từ frame tiếp theo, A* sẽ nhận diện đây là vật cản và quân lính tự động đi vòng.
- Khi công trình bị phá hủy (HP = 0), gọi lại hàm trên với biến `true` để ngay lập tức thông đường.

### 4.3 Tương tác Đơn vị Quân & Khai thác Tài nguyên
- **Nông dân (Khai thác tài nguyên):**
  - Mỏ vàng, Cây xanh thường là `isWalkable = false`. Nếu truyền toạ độ tâm mỏ vàng vào A*, hàm sẽ trả về Unreachable.
  - Hướng xử lý: Thuật toán cần xác định **các ô trống liền kề** (Neighbors có `isWalkable = true`) xung quanh Mỏ vàng. Lấy ô gần Nông dân nhất làm Target đích đến. Khi nông dân tới ô liền kề, chuyển state sang Khai thác (Gathering) thay vì cố đâm đầu vào mỏ.
- **Cơ chế Attack-Move (Phím A + Click):**
  - Đơn vị quân bắt đầu di chuyển dựa theo danh sách Waypoint từ A*. 
  - Đồng thời, mỗi 0.25s (để tối ưu), unit bắn ra `Physics2D.OverlapCircle` quét kẻ địch. 
  - Nếu phát hiện địch, unit ngừng chạy theo mảng Waypoint, quay mục tiêu sang kẻ địch và cập nhật pathfinder lao về phía chúng hoặc đứng bắn trực tiếp từ xa.

### 4.4 Giải pháp Tối ưu Hóa Hiệu Năng (Performance Optimization)
Thuật toán A* cơ bản với `List<T>` rất nặng nếu sử dụng cho 50-100 đơn vị lính.
- **Tối ưu $O(\log N)$ với Binary Min-Heap:** Việc quét vòng lặp để lấy ô có `fCost` nhỏ nhất trong `OpenSet` làm ngốn rất nhiều CPU (độ phức tạp $O(N)$). Bằng cách thiết kế lại `OpenSet` thành cấu trúc dữ liệu **Heap (Priority Queue)**, thời gian sắp xếp và lấy ô nhỏ nhất sẽ cực nhanh.
- **Hệ thống Path Request Manager (Đa luồng / Coroutine):** 
  - Tránh tính toán cho 100 lính trong cùng 1 Frame gây tụt FPS. Cần tạo 1 Queue chứa các "Yêu cầu tìm đường". 
  - Mỗi khung hình, game chỉ giải quyết tối đa 1-3 request. Đơn vị lính sẽ chuyển sang trạng thái "Idle" hoặc đứng tại chỗ 1-2 frame chờ tới lượt đường đi được tính xong rồi mới chạy, đảm bảo tốc độ khung hình (FPS) ổn định >60.

### 4.5 Phân bổ Đội hình & Cơ chế Chống đè lấn lính (Formation & Separation)
Khi điều khiển một đạo quân đông (20–50 đơn vị), nếu tất cả lính cùng di chuyển về một tọa độ click chuột duy nhất, chúng sẽ dồn cục dẫm đè lên nhau (Unit Clumping). Để giải quyết triệt để vấn đề này, kiến trúc RTS kết hợp 2 kỹ thuật:

#### 1. Phân bổ điểm đích theo Vòng tròn đồng tâm (Concentric Rings / Hexagonal Packing)
Thay vì sử dụng Sunflower Spiral (xoắn ốc hoa hướng dương khiến đội hình bị loãng gấp 4-5 lần do bán kính tăng theo $\sqrt{N}$ và góc lệch $137.5^\circ$), hệ thống áp dụng thuật toán **Concentric Rings** (tương tự *StarCraft II* và *Age of Empires*):
* **Nguyên lý:**
  * Đơn vị đầu tiên chiếm vị trí trung tâm click chuột (bán kính $R = 0$).
  * Vòng thứ $k$ có bán kính $R_k = k \times \text{unitSpacing}$, chứa tối đa $6 \times k$ đơn vị.
  * Góc lệch giữa các đơn vị trên vòng $k$ là $\Delta\theta = \frac{2\pi}{6k}$.
* **Hiệu quả:**
  * Với 25 đơn vị, toàn bộ đạo quân chỉ gói gọn trong 2 vòng tròn (bán kính tối đa chỉ $\approx 2.3 \times \text{unitSpacing}$).
  * Mọi đơn vị đều cách đều nhau một khoảng xấp xỉ $\text{unitSpacing}$ (khuyên dùng $0.3 – 0.35$ cho sprite pixel-art), tạo thành khối đội hình gắn kết khít như tổ ong.

#### 2. Khớp điểm đích & Làm mượt đường đi (Path Smoothing)
* **Khớp điểm đích (Destination Snapping):** A* tìm đường dựa trên tâm ô Grid, nhưng điểm đích của từng lính trong đội hình là điểm lẻ. Do đó, điểm mốc cuối cùng trong chuỗi Waypoint được thay thế bằng chính xác tọa độ phân bổ `formationPositions[i]`.
* **Lược bỏ node thẳng hàng (Path Smoothing):** Sử dụng tích vô hướng `Vector2.Dot(dirPrev, dirNext) < 0.999f` để loại bỏ các điểm mốc trung gian thẳng hàng, giúp sprite pixel-art di chuyển mượt mà, không bị giật đổi hướng liên tục.

#### 3. Lực đẩy tách nhau thời gian thực (Separation Steering Force)
* Khi di chuyển theo hàng ngũ hoặc đi qua điểm thắt (Choke Points), các đơn vị lân cận trong bán kính `separationRadius` sẽ tác dụng một lực đẩy ngược chiều:
  $$\vec{F}_{\text{separation}} = \frac{\vec{d}}{\|\vec{d}\|} \times \left(\frac{R_{\text{sep}} - \|\vec{d}\|}{R_{\text{sep}}}\right) \times \text{Strength}$$
* Lực đẩy này được cộng trực tiếp vào vector vận tốc di chuyển, giúp các lính tự động "né" và nhường đường nhau một cách tự nhiên mà không bao giờ bị dẫm chồng hình.

> [!IMPORTANT]
> **Lưu ý khi cấu hình trong Unity Editor:**
> Các biến `unitSpacing` (trong `RTSUnitManager`) và `separationRadius` (trong `UnitController`) sử dụng attribute `[SerializeField]`. Nếu component đã được gắn vào GameObject/Prefab trong Scene, **Unity sẽ ưu tiên giữ nguyên giá trị đã lưu trên Inspector thay vì cập nhật theo giá trị mặc định mới trong code C#**. Luôn kiểm tra và điều chỉnh trực tiếp trên cửa sổ Inspector (`unitSpacing ≈ 0.3`, `separationRadius ≈ 0.25`) để đạt độ kết dính đội hình hoàn hảo.
