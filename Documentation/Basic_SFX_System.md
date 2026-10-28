# Báo cáo Kết quả Task — Basic SFX System (Tiny Tactics)

## 1. Tổng quan

Task **Âm thanh cơ bản (Basic SFX System)** được triển khai cho game RTS 2D **Tiny Tactics**. Hệ thống phát hiệu ứng âm thanh theo thao tác UI và sự kiện gameplay, sử dụng chung `MainAudioMixer` và thiết lập âm lượng đã có từ Main Menu.

**Trạng thái:** Hoàn thành triển khai và Final QA theo kết quả kiểm thử của người thực hiện (28/10/2026).

## 2. Kiến trúc

```text
Bootstrap
└── AudioManager (Root GameObject, DontDestroyOnLoad)
    ├── AudioManager.cs         # BGM, playlist theo scene, volume
    ├── SFXManager.cs           # SFX registry, cooldown, AudioSource pooling
    ├── AudioSource             # Nhạc nền
    └── SFXPool (runtime)
        └── SFXSource_00...11   # 12 AudioSource tái sử dụng

MainAudioMixer
└── Master
    ├── Music
    └── SFX

PvE
└── AudioEventListeners
    ├── UnitCommandSFX
    ├── ResourceHarvestSFX
    └── GameStartSFX
```

- `AudioManager` và `SFXManager` cùng tồn tại trên Root GameObject do Bootstrap khởi tạo, duy trì khi chuyển scene.
- SFX dùng AudioSource riêng, không chiếm AudioSource của BGM.
- `SFXManager` ánh xạ `SFXType` đến clip và cấu hình thông qua `Dictionary`, giới hạn số nguồn phát đồng thời bằng pool cố định (mặc định 12).
- Cooldown theo từng `SFXType` hạn chế spam hiệu ứng khi nhiều đơn vị cùng hoạt động.
- Toàn bộ nguồn SFX được route đến AudioMixerGroup `SFX`; `Master`, `Music`, `SFX` sử dụng thiết lập âm lượng chung lưu qua `PlayerPrefs`.

## 3. Các hiệu ứng đã tích hợp

| Nhóm | SFX | Điểm kích hoạt |
|---|---|---|
| UI | `UIClick` | Mở Settings, Quit |
| UI | `UIHover` | Rê chuột vào button có tương tác |
| UI | `UIConfirm` | Chọn Play PvE |
| UI | `UICancel` | Back khỏi Settings |
| UI | `UIError` | Click đặt nhà không hợp lệ hoặc thất bại |
| Unit | `UnitSelect` | Chọn một hoặc nhiều unit thành công |
| Unit | `UnitMove` | Lệnh di chuyển được gửi tới nhóm unit hợp lệ |
| Building | `BuildingPlace` | Đặt công trình thành công |
| Building | `BuildingDestroy` | Phá công trình bằng Delete hoặc khi công trình hết HP |
| Resource | `WoodChop` | Một chu kỳ khai thác gỗ thành công |
| Resource | `GoldMine` | Một chu kỳ khai thác vàng thành công |
| Resource | `FoodGather` | Một chu kỳ thu thập thịt thành công |
| Game Event | `GameStart` | Bắt đầu scene PvE |

**Để dành:** `UINotification` đã chuẩn bị trong thư viện SFX nhưng chưa gắn Notification System. Các SFX chiến đấu sẽ tích hợp ở task combat tương ứng.

## 4. Luồng xử lý sự kiện

### 4.1 UI

- `UIHoverSFX` lắng nghe `IPointerEnterHandler`, chỉ phát khi Button đang interactable.
- `MainMenuController` gọi SFX theo chức năng button; không tự động gắn click SFX lên mọi button để tránh phát hai lần.

### 4.2 Chọn và điều khiển Unit

- `SelectionSystem.SelectionCompleted` phát một lần sau thao tác chọn có kết quả; `ClearSelection()` không phát.
- `RTSUnitManager.MoveCommandIssued` phát sau khi gửi lệnh di chuyển cho các `UnitController` hợp lệ, ngoài vòng lặp unit.
- `UnitCommandSFX` nhận hai sự kiện và gọi `SFXManager`.
- Không thay đổi A*, formation hoặc logic di chuyển riêng của từng unit.

### 4.3 Xây dựng và phá công trình

- `BuildingPlacementController.TryPlaceBuilding()` gọi `BuildingPlace` sau khi `BuildingPlacementService.TryPlace()` trả thành công.
- Click vào vị trí không hợp lệ hoặc service từ chối đặt nhà thì phát `UIError` (không phát trong `UpdatePreview()`).
- `BuildingDemolitionController.DemolishSelectedBuilding()` phát `BuildingDestroy` khi phá nhà hợp lệ bằng Delete.
- `BuildingBase.OnDestroyed()` phát `BuildingDestroy` khi HP giảm đến mức phá hủy; không phát trong `OnDestroy()` để tránh âm thanh giả khi unload scene.

### 4.4 Khai thác tài nguyên

- `WorkerFSM.HandleHarvestingState()` phát `OnAnyResourceHarvested(ResourceType, Vector3)` sau khi `ResourceDeposit.Harvest()` trả lượng thu hoạch thực tế lớn hơn 0.
- `ResourceHarvestSFX` chuyển loại tài nguyên Wood/Gold/Meat thành WoodChop/GoldMine/FoodGather rồi gọi `PlayAtPosition()`.
- Không phát cho trạng thái di chuyển, chờ hoặc khai thác thất bại.

### 4.5 Bắt đầu trận

- `GameStartSFX` phát hiệu ứng một lần khi component trong scene PvE bắt đầu; sau này có thể chuyển sang sự kiện `MatchStarted` khi Game Flow hoàn thiện.

## 5. Hướng dẫn sử dụng và yêu cầu

**Yêu cầu:**

1. Chạy từ scene `Bootstrap`; đối tượng `AudioManager` phải là Root GameObject để `DontDestroyOnLoad()` hoạt động.
2. Gắn `SFXManager` cùng GameObject với `AudioManager`; đặt `SFX Mixer Group = MainAudioMixer/SFX`.
3. Khai báo 14 mục `SFXEntry` trong Inspector, gán AudioClip và cooldown; pool mặc định 12, `Max Hearing Distance` mặc định 20.
4. Scene PvE cần các listener tương ứng với Unit, Harvest, GameStart; không đặt một bản sao `SFXManager` ở PvE.
5. Đảm bảo các script phát sự kiện trong gameplay đã được cập nhật đồng bộ với listener.

**API cơ bản:**

```csharp
SFXManager.Instance?.Play(SFXType.UIClick);
SFXManager.Instance?.PlayAtPosition(SFXType.WoodChop, transform.position);
```

Cả hai trả về `bool` trong trường hợp gọi trực tiếp để biết yêu cầu được chấp nhận hay bị bỏ qua (ví dụ vì cooldown, pool đầy hoặc thiếu clip).

## 6. Kết quả kiểm thử

Theo báo cáo của người thực hiện, đã hoàn thành kiểm thử cuối cùng đối với:

- Bootstrap/Main Menu/PvE và khả năng duy trì SFXManager xuyên scene.
- Phản hồi UI, Unit Selection/Move, đặt/phá công trình, khai thác 3 loại tài nguyên và GameStart.
- Pool 12 AudioSource, cooldown, phát BGM song song và âm lượng Master/SFX.
- Không có lỗi/Warning mới sau khi kiểm thử.

**Giới hạn kiểm thử:** Các luồng gameplay chỉ có thể kết luận cho những trường hợp đã thực sự chạy được trong project; trường hợp phá hủy công trình do HP phụ thuộc vào tình trạng tích hợp combat ở thời điểm test.

## 7. Lưu ý kỹ thuật và hướng mở rộng

- `UnitCommandSFX` đăng ký sự kiện trong `OnEnable()` có thể phụ thuộc thứ tự khởi tạo singleton ở scene PvE; nếu mất SFX ngẫu nhiên khi load scene, chuyển sang cơ chế bind sau khi manager sẵn sàng và hủy đăng ký tương ứng.
- `PlayAtPosition()` giảm âm lượng theo khoảng cách XY tới tâm camera, không phải spatial audio 3D; nên cân nhắc theo camera viewport nếu map rộng.
- Cooldown toàn cục theo loại có thể cố ý bỏ qua âm thanh của các hành động xảy ra sát nhau.
- Không tự tạo hoặc hủy AudioSource mỗi lần Play; khi pool đầy, âm thanh mới sẽ bị bỏ qua.
- Các clip ngắn nên import phù hợp với SFX (ví dụ Decompress On Load); giữ Unity `.meta` và kiểm tra giấy phép âm thanh trước khi đưa lên Git.
- Khi làm combat/notifications, mở rộng `SFXType` và thư viện thay vì tạo hệ thống âm thanh mới.

## 8. Checklist trước commit

- [x] SFXManager, Pooling, cooldown và mixer integration.
- [x] UI SFX, Unit SFX, Building SFX, Harvest SFX, GameStart.
- [x] Final QA theo kết quả báo cáo.
- [ ] Rà soát `git diff`, loại bỏ script/component debug không còn dùng.
- [ ] Xác nhận license asset âm thanh và commit đầy đủ `.meta`.
- [ ] Kiểm tra phạm vi code chung với teammate trước khi merge.

**Kết luận:** Hoàn thành task **Basic SFX System**. `UINotification` và âm thanh chiến đấu được giữ lại cho các task tiếp theo.
