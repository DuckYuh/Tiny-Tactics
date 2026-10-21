# Báo cáo kỹ thuật — Main Menu, Bootstrap & Audio System (Tiny Tactics)

## 1. Tổng quan

Task xây dựng luồng khởi động và giao diện chính cho game **Tiny Tactics (AoE Mini)** trên Unity, bao gồm Main Menu, điều hướng PvE, Settings và âm thanh nền theo scene.

**Trạng thái:** Hoàn thành và đã kiểm thử thủ công theo xác nhận của người thực hiện.

## 2. Chức năng đã hoàn thành

| Module | Chức năng |
|---|---|
| Bootstrap | Khởi tạo hệ thống dùng chung và tải MainMenu bất đồng bộ với Loading Screen, thanh tiến trình và phần trăm |
| Main Menu | Hiển thị Play PvE, PvP, Lobby, Settings và Quit Game |
| Scene Navigation | Play PvE tải scene gameplay; PvP/Lobby hiện trên UI nhưng bị vô hiệu hóa |
| Settings | Điều chỉnh Master/Music/SFX, bật/tắt fullscreen và lưu bằng PlayerPrefs |
| AudioManager | Singleton, DontDestroyOnLoad, phát BGM theo scene qua AudioMixer |
| Shuffle Playlist | Phát ngẫu nhiên toàn bộ bài hợp lệ đúng một lần mỗi vòng, xáo trộn lại sau khi hết vòng; tránh lặp bài giữa hai vòng nếu có từ hai bài |

## 3. Kiến trúc và luồng xử lý

```text
Bootstrap (Build Index 0)
├── BootstrapLoader
├── AudioManager  [Root GameObject]
└── LoadingCanvas
       |
       v
MainMenu
├── MainMenuController
├── MainMenuPanel
└── SettingsPanel / SettingsController
       |
       +--> Play PvE --> PvE
       +--> Settings --> AudioManager / AudioMixer / PlayerPrefs
```

- `BootstrapLoader` tải MainMenu qua `SceneManager.LoadSceneAsync()` và điều khiển Loading UI.
- `AudioManager` là GameObject **root** của Bootstrap để `DontDestroyOnLoad()` hoạt động; không tạo thêm AudioManager trong MainMenu/PvE.
- `AudioManager` nhận sự kiện `SceneManager.activeSceneChanged` để chọn playlist MainMenu hoặc PvE.
- `MainMenuController` xử lý các nút Play PvE, Settings, Back, Quit; các nút PvP và Lobby giữ `Interactable = false`.
- `SettingsController` đồng bộ Slider/Toggle với AudioManager và PlayerPrefs; AudioMixer đảm bảo âm lượng dùng chung xuyên scene.

## 4. Playlist và AudioMixer

### Playlist

- **MainMenu:** 2 bài (có thể thêm bớt trong Inspector).
- **Gameplay/PvE:** 4 bài (có thể thêm bớt trong Inspector).
- Thuật toán **Fisher–Yates Shuffle** xáo trộn danh sách index của các AudioClip hợp lệ, độ phức tạp O(n) mỗi vòng.
- Phát từng bài theo thứ tự đã xáo trộn; hết một vòng mới tạo thứ tự mới.
- Với ít nhất hai bài, tránh để bài đầu của vòng mới trùng bài cuối vòng trước.
- Sử dụng một AudioSource BGM và coroutine kiểm tra khi bài kết thúc, không cần logic chọn bài trong `Update()`.

### Mixer

```text
Master (MasterVolume)
├── Music (MusicVolume)
└── SFX (SFXVolume)
```

Các tham số exposed trong AudioMixer được chỉnh bởi `AudioManager` sau khi chuyển giá trị slider 0–1 sang dB. Giá trị được lưu trong PlayerPrefs và nạp lại khi khởi động.

**Lưu ý:** Âm lượng SFX chỉ ảnh hưởng đến những AudioSource được route vào group `SFX`; cần bảo đảm các hiệu ứng âm thanh gameplay sử dụng đúng group này.

## 5. Loading Screen

- `LoadingCanvas` nằm trong Bootstrap, có nền, logo, progress bar và văn bản phần trăm.
- `BootstrapLoader` bắt đầu tải MainMenu bất đồng bộ và chờ `operation.progress >= 0.9f` trước khi kích hoạt scene.
- Có thời gian hiển thị tối thiểu chỉnh được từ Inspector, giúp tránh màn hình loading nháy quá nhanh.
- Loading Screen hiện chỉ áp dụng cho đường đi **Bootstrap → MainMenu**. Chuyển **MainMenu → PvE** vẫn sử dụng cơ chế chuyển scene của `MainMenuController`.

## 6. Thiết lập và sử dụng

1. Thêm `Bootstrap`, `MainMenu` và scene PvE vào Scene List, để Bootstrap ở Build Index 0.
2. Giữ GameObject `AudioManager` ở **root** của Bootstrap, gán MainAudioMixer, Music Group và AudioClip[] cho từng playlist.
3. Gán các reference cho `BootstrapLoader` (ProgressBarFill, ProgressPercent) và cấu hình tên scene MainMenu.
4. Gán `MainMenuController` vào MainMenuManager và nối OnClick cho Play PvE, Settings, Back, Quit.
5. Tắt Interactable với PvP/Lobby; giữ SettingsPanel tắt mặc định.
6. Gán Slider và Toggle của SettingsPanel cho `SettingsController`; Slider có Min = 0, Max = 1, Whole Numbers = Off.
7. Chạy thử từ scene Bootstrap. Khi phát hành, kiểm tra lại độ phân giải và fullscreen trên bản build.

Các đường dẫn script trong dự án có thể nằm dưới `Assets/_Project/Scripts/`:

```text
Audio/AudioManager.cs
Core/BootstrapLoader.cs
UI/MainMenuController.cs
UI/SettingsController.cs
```

## 7. Kết quả kiểm thử

Người thực hiện đã xác nhận hoàn tất kiểm tra chức năng Main Menu, Audio/Settings, Shuffle Playlist và Bootstrap Loading Screen:

- Bootstrap tải MainMenu; Loading Screen hiển thị và hoàn tất chuyển scene.
- Play PvE hoạt động; PvP/Lobby chưa tương tác được theo chủ đích.
- MainMenu/PvE dùng các playlist riêng, phát lần lượt hết lượt shuffle rồi tiếp tục vòng mới.
- AudioManager tồn tại khi chuyển scene; lỗi `DontDestroyOnLoad only works for root GameObjects` đã được sửa bằng cách đưa GameObject lên root.
- Settings điều chỉnh và lưu Master/Music/SFX; fullscreen đã được kiểm tra theo checklist.

## 8. Phạm vi chưa triển khai / mở rộng sau

- Lobby/PvP hiện chỉ là nút khóa; chưa triển khai scene hay logic mạng.
- Loading Screen cho MainMenu → PvE chưa có; có thể tái sử dụng UI và loader khi cần.
- Chưa có crossfade giữa các bài/playlist.
- Khi thêm scene mới, cần cấu hình playlist tương ứng hoặc mở rộng hệ thống ánh xạ scene → playlist.
- Kiểm tra giấy phép các file nhạc trước khi đưa tài nguyên lên repository.

## 9. Checklist trước commit

- [ ] Scene Bootstrap và MainMenu được lưu.
- [ ] Script, AudioMixer, prefab/asset UI và các file `.meta` cần thiết có trong commit.
- [ ] Scene List và reference không bị thiếu.
- [ ] Không có AudioManager trùng trong MainMenu hoặc PvE.
- [ ] Không commit sửa đổi scene PvE ngoài phạm vi đã được nhóm thống nhất.
- [ ] Quyền sử dụng nhạc nền cho phép lưu vào repository.
- [ ] Build thử không có lỗi Console nghiêm trọng.
