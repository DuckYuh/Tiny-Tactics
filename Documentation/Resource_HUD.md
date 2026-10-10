# Task: Resource HUD - Tiny Tactics

## 1. Mục tiêu

Tạo giao diện HUD hiển thị tài nguyên người chơi:

- Meat
- Wood
- Gold

HUD kết nối với `ResourceManager` và cập nhật khi tài nguyên thay đổi.

---

## 2. Cơ chế hoạt động

```text
Farmer
  ↓
Deposit Resource
  ↓
ResourceManager.AddResource()
  ↓
OnResourceChanged
  ↓
ResourceHUD cập nhật
```

HUD sử dụng **Event-driven**, không kiểm tra tài nguyên mỗi frame.

---

## 3. Cơ chế hoạt động

Đặt ResourceHUD.prefab vào Canvas của Scene. Kiểm tra các reference Meat Amount, Wood Amount, Gold Amount trong Inspector. Khi ResourceManager thay đổi tài nguyên, HUD sẽ tự động cập nhật.

Yêu cầu:

- Managers phải có ResourceManager
- ResourceHUD phải nằm trong Canvas

---

## 4. Cấu trúc

```text
ResourceHUD
└── ResourceContainer
    ├── MeatItem
    │   ├── Icon
    │   └── Amount
    ├── WoodItem
    │   ├── Icon
    │   └── Amount
    └── GoldItem
        ├── Icon
        └── Amount
```

Icon:

```text
Meat → Icon_04
Wood → Icon_01
Gold → Icon_03
```

---

## 5. Script

File:

```text
Assets/Scripts/UI/ResourceHUD.cs
```

`ResourceHUD` thực hiện:

- Lấy dữ liệu ban đầu từ `ResourceManager`.
- Đăng ký `OnResourceChanged`.
- Cập nhật đúng loại tài nguyên khi có thay đổi.
- Hủy đăng ký event khi bị destroy.

---

## 6. Prefab

Prefab:

```text
Assets/Prefabs/UI/ResourceHUD.prefab
```

Prefab chỉ chứa UI, không chứa `ResourceManager`.

Có thể tái sử dụng cho PvE/PvP và các Scene khác.

---

## 7. Kiểm thử

- [x] Hiển thị Meat / Wood / Gold
- [x] Cập nhật khi `AddResource()`
- [x] Farmer deposit cập nhật HUD
- [x] Prefab hoạt động bình thường
- [x] Không ảnh hưởng Farmer / Pathfinding / Unit Manager

---

## 8. Commit

Chỉ commit:

```text
Assets/Scripts/UI/ResourceHUD.cs
Assets/Prefabs/UI/ResourceHUD.prefab
```

**Status: COMPLETED**
