# Hướng dẫn chi tiết: Setup Tự động tìm đường Đa màn hình (Hexa Grid)

Dưới đây là kịch bản bạn yêu cầu: **Hero ở Scene A -> Chạy tới cổng -> Sang Scene B -> Chạy tới cổng khác ở Scene B -> Sang Scene C -> Chạy tới đích ở Scene C**.

Để làm được điều này một cách trơn tru, bạn cần thực hiện tuần tự các bước cấu hình sau trên Unity Editor.

---

## BƯỚC 1: Cấu hình Dữ liệu Thế giới (World Map Graph)
Đây là "bộ não" để thuật toán A* biết được đường đi từ Scene A đến Scene C phải thông qua Scene B.

1. Click chuột phải vào cửa sổ Project, chọn `Create > Dylanng > Pathfinding > WorldMapGraph`. Đặt tên file là `MainWorldGraph`.
2. Chọn file `MainWorldGraph` vừa tạo, nhìn sang **Inspector**:
   - Ở mục **Inter Scene Connections**, tạo 2 phần tử mới:
     - *Phần tử 1:* `From Portal Id` = **Portal_A_Out** | `To Scene Name` = **SceneB** | `To Portal Id` = **Portal_B_In_A**
     - *Phần tử 2:* `From Portal Id` = **Portal_B_Out_C** | `To Scene Name` = **SceneC** | `To Portal Id` = **Portal_C_In_B**
   - Ở mục **Intra Scene Paths**, bạn định nghĩa quãng đường phải đi *trong cùng Scene B* (từ cổng vừa bước vào, tới cổng đi ra):
     - *Phần tử 1:* `Portal Id 1` = **Portal_B_In_A** | `Portal Id 2` = **Portal_B_Out_C** | `Distance Cost` = 10 (Hoặc tùy độ lớn bản đồ của bạn).

---

## BƯỚC 2: Thiết lập Scene A (Điểm khởi hành)
Mở **Scene A** lên và thực hiện:

1. **Tạo lưới Hex Grid:**
   - Tạo một Empty GameObject tên là `Map_SceneA` ở tọa độ (0,0,0).
   - Gắn script `HexGrid` vào nó. Chỉnh Width/Height. Bấm **Generate/Reset Grid**.
   - Dùng cọ vẽ trong Inspector để tô các vật cản (Đỏ) nếu muốn.
2. **Tạo Cổng (Portal):**
   - Tạo một khối Cube (đánh dấu `Is Trigger` = true trong BoxCollider). Đặt tên `Portal_A_Out`.
   - Gắn script `Portal` vào Cube này. Cấu hình:
     - `Portal Id`: **Portal_A_Out**
     - `Target Scene`: **SceneB**
     - `Target Portal Id`: **Portal_B_In_A**
3. **Quản lý Cổng:**
   - Tạo một Empty GameObject tên `PortalManager`. Gắn script `PortalManager` vào nó (Script này sẽ tự động tìm tất cả các cổng trong Scene).
4. **Cấu hình Hero:**
   - Kéo prefab Hero vào Scene A.
   - Gắn các script sau vào Hero: 
     - `HexLocalPathfinder`
     - `MultiSceneNavigator` (Kéo file `MainWorldGraph` SO vào biến `World Graph`).
     - **`MultiSceneTransitionHandler`**: (Đây là script tôi vừa mới viết thêm cho bạn. Nó sẽ giúp nhân vật tự động nhận diện khi đổi scene, đặt đúng vị trí cổng mới và ra lệnh đi tiếp).

---

## BƯỚC 3: Thiết lập Scene B (Trạm trung chuyển)
Lưu Scene A, mở **Scene B** lên và thực hiện:

1. **Tạo lưới Hex Grid:** (Làm tương tự Bước 2).
2. **Tạo Cổng Nhận (Từ A qua):**
   - Tạo khối Cube (Is Trigger = true). Tên: `Portal_B_In_A`.
   - Gắn script `Portal`. `Portal Id`: **Portal_B_In_A**. (Không cần Target Scene vì đây là cổng nhận).
3. **Tạo Cổng Đi (Sang C):**
   - Tạo khối Cube khác (Is Trigger = true). Tên: `Portal_B_Out_C`.
   - Gắn script `Portal`. Cấu hình: 
     - `Portal Id`: **Portal_B_Out_C**
     - `Target Scene`: **SceneC**
     - `Target Portal Id`: **Portal_C_In_B**
4. **Quản lý Cổng:** Tạo GameObject gắn `PortalManager`.

*(Lưu ý: Bạn không cần thả Hero vào Scene B, vì Hero sẽ tự mang qua bằng thuộc tính DontDestroyOnLoad có sẵn trong `MultiSceneTransitionHandler`).*

---

## BƯỚC 4: Thiết lập Scene C (Điểm đến cuối cùng)
Lưu Scene B, mở **Scene C** lên và thực hiện:

1. **Tạo lưới Hex Grid:** (Làm tương tự Bước 2).
2. **Tạo Cổng Nhận (Từ B qua):**
   - Tạo khối Cube (Is Trigger = true). Tên: `Portal_C_In_B`.
   - Gắn script `Portal`. `Portal Id`: **Portal_C_In_B**.
3. **Quản lý Cổng:** Tạo GameObject gắn `PortalManager`.

---

## BƯỚC 5: Gọi mã thực thi di chuyển
Sau khi bạn đã setup xong 3 Scene (và add cả 3 Scene vào **Build Settings**), hãy quay lại Scene A. Bấm nút Play.

Bạn gọi đoạn code sau từ bất kỳ đâu (ví dụ UI Button hoặc một script test):

```csharp
// Lấy tham chiếu tới Hero của bạn
MultiSceneNavigator heroNavigator = heroObject.GetComponent<MultiSceneNavigator>();

// Ra lệnh đi tới 1 vị trí mục tiêu ở Scene C
Vector3 finalTargetPositionInSceneC = new Vector3(10f, 0f, 15f);
heroNavigator.NavigateTo("SceneC", finalTargetPositionInSceneC);
```

### Luồng hoạt động ngầm (Hệ thống sẽ làm gì?):
1. Nhận lệnh `NavigateTo`, `GlobalPathfinder` đọc `MainWorldGraph`. Nó tính ra lộ trình: `Cần đi qua Portal_A_Out -> Sang Portal_B_Out_C`.
2. `HexLocalPathfinder` nhận lệnh dẫn Hero lách qua lưới lục giác tới `Portal_A_Out`.
3. Hero chạm `Portal_A_Out`, event đổi scene được bắn ra. `SceneLoadManager` load Scene B.
4. Scene B load xong, `MultiSceneTransitionHandler` bắt được event, lập tức dịch chuyển (Teleport) Hero đặt vào đúng tọa độ của cổng `Portal_B_In_A`. Đồng thời bảo Navigator hãy tiếp tục lộ trình.
5. `HexLocalPathfinder` ở Scene B nhận điểm đến mới là cổng `Portal_B_Out_C`. Hero tìm đường xuyên qua lưới lục giác Scene B tới cổng đó.
6. Chạm cổng thứ hai, tiếp tục quy trình load Scene C.
7. Khi sang Scene C, hàng đợi Portal đã hết, Navigator ra lệnh cho `HexLocalPathfinder` đi tới vị trí đích cuối cùng (`finalTargetPositionInSceneC`). 

> [!IMPORTANT]
> **Chú ý quan trọng:** Script `MultiSceneTransitionHandler` sẽ tự động cấu hình `DontDestroyOnLoad(gameObject)` cho nhân vật của bạn, để giữ nguyên trạng thái nhân vật khi bay qua lại giữa các Scene. Đảm bảo rằng kiến trúc hệ thống game của bạn không tự xóa nhân vật khi tải màn chơi mới.
