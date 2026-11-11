# Tài liệu kỹ thuật – Game Flow: Win/Loss UI (Tiny Tactics)

## 1. Tổng quan

Task **Hoàn thiện Game Flow – Win/Loss UI** thuộc Giai đoạn 1 (DACN), Sprint 6. Phần triển khai hiện tại tập trung vào màn hình kết quả trong scene **PvE** và điều hướng sau trận; chưa phải nghiệm thu toàn bộ vòng chơi với AI.

**Phạm vi đã thực hiện và kiểm thử theo xác nhận của người triển khai:**
- Tạo `GameOverPanel` gồm nền phủ tối (`BackgroundOverlay`), cửa sổ kết quả (`ResultWindow`), tiêu đề, mô tả, nút **Play Again** và **Main Menu**.
- Tạo `GameOverUIController.cs` trong `Assets/_Project/Scripts/UI/`, gắn vào GameObject luôn hoạt động (`GameOverUIManager`).
- Nhận kết quả từ sự kiện `GameManager.OnGameOver(bool)` và hiển thị **Victory** / **Defeat**.
- Tạm dừng gameplay bằng `Time.timeScale = 0` khi kết thúc trận; khôi phục `Time.timeScale = 1` trước khi tải lại scene hoặc về Main Menu.
- Sử dụng hành vi **Demolish Castle Blue** hiện có làm thao tác đầu hàng, không bổ sung logic phá nhà mới.

## 2. Cơ chế hoạt động

```text
Castle Red bị phá ──> CastleBuilding ──> GameManager.TriggerPlayerVictory()
                                                     │
Castle Blue bị phá ─> CastleBuilding ──> GameManager.TriggerPlayerDefeated()
                                                     │
                                                     ▼
                                         GameManager.OnGameOver(bool)
                                                     │
                                                     ▼
                                          GameOverUIController
                                                     │
                                      Hiển thị panel + Pause game
                                                     │
                              ┌──────────────────────┴───────────────────┐
                              ▼                                          ▼
                        Play Again                                  Main Menu
                       Tải lại PvE                              Tải MainMenu
```

`GameOverUIController` chỉ **đăng ký và hủy đăng ký sự kiện** (`OnEnable` / `OnDisable`), không truy vấn trạng thái liên tục trong `Update`, không trực tiếp xử lý AI hoặc Combat.

## 3. Cấu hình trong Unity

- Scene sử dụng: `Assets/_Project/Scenes/Gameplay/PvE.unity`.
- Đặt `GameOverPanel` trong Canvas, nằm sau các HUD khác để hiển thị trên cùng; mặc định **Inactive**.
- Tạo GameObject `GameOverUIManager` **Active** để giữ component `GameOverUIController` nhận sự kiện kể cả khi panel bị ẩn.
- Trong Inspector, liên kết `GameOverPanel`, `ResultTitle`, `ResultDescription`, `PlayAgainButton`, `MainMenuButton`.
- Đặt `Main Menu Scene Name` là `MainMenu`, bảo đảm `PvE` và `MainMenu` có trong Scene List/Build Profiles.
- Hai nút được đăng ký `onClick` trong code; không cần gắn trùng sự kiện tại Inspector.

## 4. Cách sử dụng và kiểm thử

1. Mở `PvE`, bấm Play: `GameOverPanel` phải ẩn.
2. Kiểm thử Victory qua `GameManager.TriggerPlayerVictory()` hoặc phá Castle Red; kiểm thử Defeat qua `GameManager.TriggerPlayerDefeated()` hoặc Demolish Castle Blue.
3. Khi có kết quả, panel hiện đúng nội dung và gameplay dừng.
4. **Play Again** tải lại trận PvE; **Main Menu** quay về scene MainMenu. Cả hai khôi phục `Time.timeScale` trước khi chuyển scene.
5. Kiểm thử nhiều lượt liên tiếp để xác nhận không kẹt pause, không lặp panel và không có exception nghiêm trọng.

Trong giai đoạn phát triển có thể dùng `GameOverUITest` (F8 = Victory, F9 = Defeat) để kích hoạt sự kiện. **Gỡ component test khỏi scene và không đưa cơ chế phím tắt test vào bản phát hành.**

## 5. Phạm vi tích hợp và giới hạn

- **Không chỉnh sửa** `GameManager.cs`, `CastleBuilding.cs`, `BuildingDemolitionController.cs`, Combat hoặc AI để tránh conflict với phần việc khác.
- Việc chọn Castle Blue rồi Demolish để đầu hàng tận dụng `CastleBuilding.OnDestroy()` đang gọi `TriggerPlayerDefeated()`.
- **Lưu ý kỹ thuật:** Cơ chế trên cũng có nguy cơ phát Defeat khi Castle Blue bị hủy trong quá trình dọn scene. Cần theo dõi log khi chuyển scene; chưa giải quyết tận gốc nếu giữ nguyên script gameplay.
- Nghiệm thu Win/Loss qua quá trình chiến đấu PvE thực tế vẫn cần AI và Combat hoàn thiện.

## 6. Kết luận

**Hoàn thành phần Win/Loss UI và điều hướng sau trận cho Giai đoạn 1** theo phạm vi độc lập; đã kiểm thử qua các trường hợp được xác nhận trong GF-3. Phần kiểm thử end-to-end với AI PvE sẽ thực hiện khi các hệ thống liên quan sẵn sàng.
