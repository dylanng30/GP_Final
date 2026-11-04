# Kế hoạch combat góc nhìn thứ 3 và hệ skill Ngũ Hành

Nhánh làm việc: `feature/tps-combat`. Tài liệu thiết kế gốc: `Game/Skill_Interaction.docx`.

## Quyết định đã chốt

Các giá trị này là mặc định được áp dụng khi bắt đầu Phase 0. Có thể đổi, nhưng đổi sau Phase 4 sẽ phải sửa dữ liệu.

| Chủ đề | Quyết định |
|---|---|
| Top-down và auto | Giữ. Tách thành hai trục `ViewMode` và `ControlMode` |
| Phạm vi góc nhìn thứ 3 | Dùng cả khi khám phá, không chỉ trong trận |
| Hero trên sân | Cả đội cùng chiến, người chơi điều khiển một hero, đồng đội do AI đánh |
| Sĩ số đội | 3, lưu trong dữ liệu. Nếu tăng lên 5 phải bật `MaxActiveLinks` |
| Bộ skill | 2 skill dùng cooldown và 1 ultimate dùng Nộ |
| Chạm chân dung | Đổi hero điều khiển, ultimate của đồng đội do AI dùng |
| Tương sinh | Bị động khi hai hero cùng đội, hiện trên màn xếp đội |
| Tương khắc | Phá status và cộng khoảng 15% sát thương của đòn đó, chỉnh được |
| Vai trò và hệ | Hai trục độc lập trong dữ liệu |
| Hệ của hero hiện có | `hero_000000` Hỏa, `000001` Mộc, `000002` Thổ, `000003` Kim, `000004` Thủy (tạm theo vòng tương sinh) |
| Nền tảng | PC trước, mobile màn ngang dùng chung Intent |

### Phần tài liệu thiết kế chưa rõ

| Điểm | Áp dụng |
|---|---|
| Quấn rễ là Trói hay Choáng | Root: không di chuyển, vẫn đánh được |
| Skill gây Hóa Đá | Thổ Skill 2 |
| Skill gây Phá Giáp | Kim Skill 1 |
| Thủy Skill 1 | Khiên. Hồi máu chuyển sang ultimate |
| Kim sinh Thủy (game không có mana) | Tăng hồi máu |

## Lộ trình

| Phase | Nội dung | Trạng thái |
|---|---|---|
| 0 | Chốt nền: nhánh, sandbox, checklist | Xong |
| 1 | Domain và test: ElementChart, DamagePipeline, asmdef | Xong |
| 2 | Tách `HeroInstance` thành các component | Chưa làm |
| 3 | Ability runtime: slot, Ultimate, Targeting, Delivery | Chưa làm |
| 4 | Status, Reaction, Synergy | Chưa làm |
| 5 | Điều khiển góc nhìn thứ 3 | Chưa làm |
| 6 | Nội dung 5 hero | Chưa làm |
| 7 | AI và HUD | Chưa làm |
| 8 | Dọn code cũ, chuẩn hóa namespace, profile mobile | Chưa làm |

## Sandbox

- `Tools/Combat/Play Sandbox`: game khởi động qua Bootstrapper rồi vào `Combat_Sandbox`. Nút Play thường vẫn vào Scene_A.
- `Tools/Combat/Run Smoke Test`: chạy tự động các bước chính, ghi kết quả vào `Logs/CombatSandboxSmoke.txt`.
- `Tools/Combat/Build Sandbox Scene`: dựng lại scene từ Scene_A, xóa mọi chỉnh sửa tay trong Sandbox.

Sandbox gồm 8 DummyBot (3 có sẵn từ Scene_A và 5 thêm mới, mỗi con một hệ), bỏ portal, thêm một tường và một dốc.

## Kiểm thử

- Unit test nằm ở `Game/Features/Combat/Tests/EditMode`, chạy trong Test Runner hoặc bằng `-runTests -testPlatform EditMode`.
- Logic combat thuần đặt trong assembly `Game.Combat.Domain` để test được mà không cần scene.

Chạy smoke test bằng dòng lệnh khi Editor không mở project này:

```bash
Unity -batchmode -projectPath <path> -executeMethod Game.Editor.Combat.CombatSandboxSmokeTest.Run -logFile smoke.log
```

Mã thoát: 0 là đạt, 1 là có mục lỗi, 2 là quá thời gian.

## Checklist chơi thử

Chạy trong Sandbox sau mỗi phase. Cột "Hiện tại" ghi hành vi trước refactor, kể cả lỗi đã biết. Smoke test phủ các mục 1–6, 8, 9, 12 và 13. Hiện chưa kiểm chứng tự động: các mục lỗi đã biết 7, 10, 11, phần hiển thị HUD, và việc bot đánh hero.

| # | Thao tác | Hiện tại |
|---|---|---|
| 1 | Vào game | Sinh hero trong đội, HUD hiện, không lỗi Console |
| 2 | Đi bằng WASD hoặc joystick | Hero đi theo hướng camera, đồng đội bám theo |
| 3 | Space, nút Dash | Nhảy, lộn né kèm miễn sát thương ngắn |
| 4 | Đứng yên gần địch khi bật auto | Cả đội tự đánh, Nộ tăng và tự dùng skill đầu |
| 5 | Tắt auto | Camera chuyển sang góc nhìn thứ 3, hiện nút hành động |
| 6 | Bấm nút đánh thường khi tắt auto | Đánh theo animation, số sát thương nổi lên |
| 7 | Dùng skill khi tắt auto | Lỗi đã biết: Nộ không tăng nên skill có `EnergyCost` không dùng được |
| 8 | Dùng Fireball | Bay vòng cung, nổ, gây sát thương và thiêu đốt |
| 9 | Chạm chân dung | Đổi hero điều khiển, camera đổi theo |
| 10 | Bot đánh hero | Hero mất máu. Lỗi đã biết: thanh máu HUD không đổi |
| 11 | Hero không được điều khiển bị stun | Lỗi đã biết: hero đó có thể nhận input sau khi hết stun |
| 12 | Đánh DummyBot | Trúng đòn, sát thương tính theo hệ của DummyBot |
| 13 | Đánh mục tiêu bị khắc hoặc khắc lại | Hệ số 1.2 hoặc 0.8, số nổi là sát thương cuối cùng |

## Lỗi đã thấy khi chạy baseline

Xử lý ở phase tương ứng:

| Lỗi | Phase |
|---|---|
| Scene có 2 EventSystem, Unity cảnh báo mỗi frame | 7 |
| Số nổi vẫn hiện khi đòn bị khiên, miễn nhiễm hoặc dash chặn | 2 |
| Đánh thường của mọi hero đang là hệ Physical | 6 |
| Khi thoát Play Mode, service bị xóa trước các object còn cần nó, gây lỗi `ServiceLocator` và `NullReferenceException` | 2 |

## Thay đổi cân bằng từ Phase 1

- DEF nay giảm sát thương theo công thức `100 / (100 + DEF)`, trước đây DEF không có tác dụng.
- Chí mạng dùng `CritRate` và `CritDamage` của nguồn, trước đây cố định 10–20% và không tăng sát thương.
- Kẻ địch không có chỉ số `CritRate` thì không chí mạng.

Các giá trị chỉnh tại `Game/Features/Combat/SOs/Rules/`.

## Quy tắc code

1. Domain không tham chiếu MonoBehaviour hoặc scene.
2. Effect không giữ trạng thái và không sửa field config lúc chạy.
3. Event chỉ báo việc đã xảy ra. Yêu cầu hành động thì gọi trực tiếp service.
4. Không dùng số cố định trong code. Giá trị nằm trong SO.
5. Không tắt component để khống chế. Dùng `ControlFlags`.
6. Không `switch` theo `GameElement` trong logic gameplay.
7. Thêm status, reaction hoặc skill mới không được phải sửa code cũ.
