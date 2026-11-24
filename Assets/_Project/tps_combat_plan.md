# Combat góc nhìn thứ 3 và hệ skill Ngũ Hành

Nhánh làm việc: `feature/tps-combat`. Tài liệu thiết kế gốc: `Game/Skill_Interaction.docx`.

## Trạng thái

Cả 9 phase đã có code và dữ liệu. Chưa có người chơi thử, xem mục "Cần kiểm tra khi chơi thật".

| Phase | Nội dung |
|---|---|
| 0 | Nhánh, Sandbox, checklist |
| 1 | Assembly Domain, ElementChart, DamagePipeline, unit test |
| 2 | Tách máu, khiên, Nộ, di chuyển khỏi `HeroInstance` |
| 3 | `AbilityRunner`, `AbilityContext`, slot Ultimate, mở khóa theo cấp |
| 4 | Status, ControlFlags, tương khắc, tương sinh |
| 5 | Intent, camera, chọn mục tiêu, ngắm, combo, hitbox cận chiến |
| 6 | 15 skill cho 5 hero, tường đá, dạng khổng lồ của Kim |
| 7 | AI dùng đủ skill, HUD, chữ phản ứng, nhãn liên kết |
| 8 | Xóa code cũ, chuẩn hóa namespace, bỏ log thừa |

## Điều khiển

| Hành động | PC | Mobile |
|---|---|---|
| Di chuyển | WASD | Joystick |
| Xoay camera | Chuột, sau khi khóa con trỏ | Vuốt vùng trống trên màn hình |
| Zoom | Con lăn | Chụm hai ngón |
| Đánh thường | Chuột trái, giữ để đánh liên tục | Giữ nút đánh |
| Skill 1, Skill 2, Ultimate | E, R, Q | Nút skill |
| Dash | Left Shift | Nút Dash |
| Nhảy | Space | Nút Nhảy |
| Lock-on | Chuột giữa | Nút Khóa |
| Đổi hero | 1, 2, 3 | Chạm chân dung |
| Tập hợp đội | G | Nút Tập hợp |
| Đổi góc nhìn | V | Nút Đổi camera |
| Khóa con trỏ | Bấm vùng trống, hoặc Left Alt | Không cần |
| Hiện con trỏ để bấm UI | Left Alt hoặc Esc | Không cần |

Mặc định chơi tay ở góc nhìn thứ 3. Nút Auto trên HUD chỉ đổi chế độ tự đánh, không đổi camera. Các nút trên màn hình dùng được cả trên PC.

### Test PC và mobile

Game tự nhận nền tảng qua `UnityEngine.Device.Application.isMobilePlatform`. Dòng nhắc ở cuối màn hình cho biết đang ở chế độ nào, dòng này và phím F9 chỉ có trong Editor và Development Build.

- **PC:** bấm Play, con trỏ vẫn hiện để bấm UI. Bấm vùng trống để khóa con trỏ và xoay camera.
- **Mobile bằng Game view:** trong Play Mode bấm F9. Bàn phím bị tắt, dùng chuột thay ngón tay: kéo vùng trống để xoay, kéo joystick để đi, bấm các nút trên HUD. Không giả lập được hai ngón.
- **Mobile bằng Device Simulator:** đổi Game view sang Simulator trước khi Play, game tự vào chế độ mobile.
- **Đổi hero bất kỳ:** Tab thay hero đang điều khiển bằng hero kế tiếp chưa có trong đội. Bảng "Đổi hero" ở góc phải chọn thẳng một hero, bấm được bằng chuột hoặc chạm. Hero mới đứng đúng chỗ hero cũ, hai hero còn lại giữ nguyên máu và hồi chiêu. Chỉ có trong Editor và Development Build.

## Quyết định thiết kế

| Chủ đề | Quyết định |
|---|---|
| Top-down và auto | Giữ, độc lập với nhau |
| Phạm vi góc nhìn thứ 3 | Dùng cả khi khám phá |
| Hero trên sân | Cả đội cùng chiến, người chơi điều khiển một hero, đồng đội do AI đánh |
| Sĩ số đội | 3. Nếu tăng lên 5 nên đặt `maxActiveLinks` trong `CombatRules` |
| Bộ skill | 2 skill dùng cooldown, 1 ultimate dùng Nộ |
| Chạm chân dung | Đổi hero điều khiển, ultimate của đồng đội do AI dùng |
| Tương sinh | Bị động khi hai hero cùng đội, hiện trên màn xếp đội |
| Tương khắc | Phá trạng thái và cộng sát thương bằng 15% ATK, chỉnh trong từng rule |
| Hệ của hero | `hero_000000` Hỏa, `000001` Mộc, `000002` Thổ, `000003` Kim, `000004` Thủy (tạm, cần đổi cho đúng model) |
| Nền tảng | PC trước, mobile màn ngang dùng chung Intent |

### Phần tài liệu thiết kế chưa rõ

| Điểm | Áp dụng |
|---|---|
| Quấn rễ là Trói hay Choáng | Root: không di chuyển, vẫn đánh được |
| Skill gây Hóa Đá | Thổ Skill 2 |
| Skill gây Phá Giáp | Kim Skill 1, áp lên mọi mục tiêu tia sét nảy trúng |
| Thủy Skill 1 | Khiên. Hồi máu chuyển sang ultimate |
| Kim sinh Thủy (game không có mana) | Tăng hồi máu |

### Khác với kế hoạch ban đầu

- Camera tự viết tiếp trên `CameraController`, chưa dùng Cinemachine. Thêm package và rig vào 4 scene mà không kiểm tra được cảm giác camera là rủi ro lớn.
- Input vẫn dùng Input Manager cũ. Mọi input đã đi qua `IPlayerIntentSource`, nên đổi sang Input System chỉ cần viết nguồn mới.
- Chưa có hit-stop, vì đổi `Time.timeScale` xung đột với `PauseSystem`. Hiện có rung camera khi trúng đòn.
- Chỉ phần Domain là asmdef riêng. Phần Runtime vẫn nằm trong `Assembly-CSharp`.

## Chỉnh nội dung

| Muốn đổi | Sửa ở đâu |
|---|---|
| Hệ số khắc, vòng sinh, vòng khắc | `Combat/SOs/Rules/ElementChart.asset` |
| Công thức DEF, sát thương tối thiểu, số liên kết tối đa | `Combat/SOs/Rules/CombatRules.asset` |
| Trạng thái | `Combat/SOs/Status/` |
| Luật tương khắc | `Combat/SOs/Reactions/`, rồi thêm vào `CombatRules` |
| Liên kết tương sinh | `Combat/SOs/Synergies/`, rồi thêm vào `CombatRules` |
| Skill, combo, đánh thường | `Combat/SOs/Abilities/Heroes/` |
| Màu, kích thước thanh máu | `02_Prefabs/Combat/ActorHealthBar.prefab` |
| Hình tia sét, màu sóng kiếm của hệ Kim | `02_Prefabs/Combat/LightningArc.prefab`, `ConeWave.prefab` |
| Độ cao số sát thương | Inspector của `FloatingTextManager` trong từng scene |

`Tools/Combat/Build Combat Content` ghi đè dữ liệu trên theo giá trị trong `Editor/CombatContentBuilder*.cs`, trừ phần `FloatingTextManager`. Sau khi chỉnh tay trong Inspector, đừng chạy lại lệnh này, hoặc sửa luôn trong builder.

Thanh máu vẽ bằng quad và shader `WorldProgressBar`, không dùng Canvas, và luôn quay theo camera. Muốn thêm cho actor khác, kéo `ActorHealthBar.prefab` làm con của actor có `ITargetable` rồi chỉnh độ cao. Hiện mới gắn vào `DummyBot`.

## Sandbox và kiểm thử

- `Tools/Combat/Play Sandbox`: khởi động qua Bootstrapper rồi vào `Combat_Sandbox`. Nút Play thường vẫn vào Scene_A.
- `Tools/Combat/Run Smoke Test`: chạy tự động các bước chính, ghi kết quả vào `Logs/CombatSandboxSmoke.txt`.
- `Tools/Combat/Build Sandbox Scene`: dựng lại scene từ Scene_A, xóa mọi chỉnh sửa tay trong Sandbox.
- Unit test nằm ở `Game/Features/Combat/Tests/EditMode`.

Sandbox có 8 DummyBot (3 từ Scene_A và 5 con, mỗi con một hệ), một tường và một dốc.

```bash
Unity -batchmode -projectPath <path> -executeMethod Game.Editor.Combat.CombatSandboxSmokeTest.Run -logFile smoke.log
```

Mã thoát: 0 là đạt, 1 là có mục lỗi, 2 là quá thời gian.

## Cần kiểm tra khi chơi thật

Các mục này chưa thể kiểm chứng tự động:

1. Cảm giác camera, độ nhạy chuột, zoom và lúc camera chạm tường.
2. Nhịp combo, tầm đánh, độ lao và độ nặng của đòn.
3. Hiển thị của HUD: vị trí nút Ultimate, bảng thông tin, tâm ngắm, chữ tiếng Việt trên số nổi.
4. VFX của từng skill: kích thước, vị trí, màu.
5. Tường đá: chặn đạn, chặn di chuyển của hero và quái.
6. Cân bằng: sát thương, cooldown, Nộ, tốc độ giết quái.
7. Bot đánh hero và việc thanh máu trên HUD giảm đúng.
8. Chuyển scene qua portal, EventSystem còn hoạt động sau khi đổi scene.
9. Mobile: chạm, vuốt, hiệu năng. Chưa profile trên thiết bị thật.

## Việc còn lại

- Animation: combo và ultimate đang dùng chung clip `Attack`, `Skill_0`, `Skill_1`. Chưa có phản ứng trúng đòn, strafe, cutscene ultimate.
- VFX riêng cho Mộc Skill 2 và hai skill của Thổ: đang dùng tạm prefab Epic Toon FX.
- Model dạng khổng lồ của Kim đang là phóng to model hiện có. Gọi pet chưa làm.
- Hóa Đá, Trói, Choáng chưa có animation đứng yên riêng.
- UI: `CombatInfoPanel` và nhãn liên kết dựng bằng code, chưa có icon trạng thái hay art.
- VFX tạo bằng Instantiate, nên chuyển sang pool nếu thấy tụt khung hình.

## Thay đổi cân bằng

- DEF giảm sát thương theo công thức `100 / (100 + DEF)`.
- Chí mạng dùng `CritRate`, `CritDamage` của nguồn. Kẻ địch không có `CritRate` thì không chí mạng.
- Đánh thường trúng đích tăng Nộ theo `EnergyPerHit`, cả khi chơi tay.
- Skill thường không tốn Nộ, ultimate tốn 100.

## Quy tắc code

1. Domain không tham chiếu MonoBehaviour hoặc scene.
2. Effect không giữ trạng thái và không sửa field config lúc chạy.
3. Event chỉ báo việc đã xảy ra. Yêu cầu hành động thì gọi trực tiếp service.
4. Không dùng số cố định trong code. Giá trị nằm trong SO.
5. Không tắt component để khống chế. Dùng `ControlFlags`.
6. Không `switch` theo `GameElement` trong logic gameplay.
7. Thêm status, reaction hoặc skill mới không được phải sửa code cũ.
