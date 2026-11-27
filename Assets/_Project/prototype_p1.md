# Prototype P1: chơi thử trên map Elementia

Nhánh: `Prototype-P1`. Scene: `01_Scenes/Map/Elementia_Map.unity`.

## Chạy

1. `Tools/Elementia/Setup Prototype`: gắn lớp gameplay vào map. Chạy lại sau mỗi lần sửa map hoặc chạy `Tools/Elementia/Build Map`.
2. `Tools/Elementia/Play Prototype`: khởi động qua Bootstrapper rồi vào thẳng map. Nút Play thường vẫn vào Scene_A.

Điều khiển giống Combat Sandbox, xem `tps_combat_plan.md`.

## Setup Prototype làm gì

Tool không dựng lại map, nên giữ nguyên các chỉnh tay trong scene. Mọi thứ tool thêm vào nằm dưới `---PROTOTYPE---` và được dựng lại từ đầu mỗi lần chạy.

| Phần | Cách làm |
|---|---|
| Camera, EventSystem, FloatingTextManager | Chép từ Scene_A để giữ cấu hình. Camera của map bị thay, far clip giữ 1500 |
| Điểm spawn | Gắn `PlayerSpawnPoint` vào `Gameplay/PlayerSpawn` ở quảng trường làng. Đội xếp hàng ngang tại đó |
| Layer Ground | Collider trong nhóm `Terrain` và các mặt đường, sàn, cầu. Ngắm và chỉ báo skill dò mặt đất theo layer này |
| Checkpoint | Gắn `Checkpoint` vào mọi `Checkpoint_*`. Hero đang điều khiển đi vào bán kính 4 m là kích hoạt |
| Rơi khỏi đảo | `WorldRespawnService` đưa hero có Y thấp hơn Kill Y về checkpoint gần nhất. Kill Y tính từ đáy địa hình trừ 5 m (hiện là -42) |
| Cả đội ngã | Sau 3 giây, cả đội hồi sinh đầy máu ở checkpoint gần nhất |
| Chỗ đứng khi spawn, hồi sinh | `GroundPlacement` dò từ ngay trên marker xuống, rồi tìm quanh đó chỗ thân hero không chạm nhà, cây, đá |
| Quái | Crab theo marker, xem bảng dưới. Tên object có dạng `<Bậc>_<Hệ>_<Marker>`. Đặt trên layer Ground, dời ra chỗ trống nếu marker nằm trong gốc cây hay đá |
| Build Settings | Thêm map và `BootToElementiaMap_SO` |

Model `ScaleRef_Character (delete me)` ở điểm spawn bị xóa vì đội xuất hiện đúng chỗ đó.

### Quái theo marker

| Marker | Hệ |
|---|---|
| `EnemySpawn_Wilds_*` (đường nối) | Thổ |
| `EnemySpawn_JungleW_ForestEdge_*` | Mộc |
| `EnemySpawn_JungleW_MarshHeart_*` | Thủy |
| `EnemySpawn_JungleE_Trail_*` | Mộc |
| `EnemySpawn_JungleE_VineRuins_*` | Kim |
| `EnemySpawn_AshField_*` | Hỏa |
| `EnemySpawn_Ruins_*` | Thổ |
| `CombatArea_2_CrystalHollow_Elite` | Kim, tinh anh |
| `CombatArea_7_Jungle_MirrorLake_Elite` | Thủy, tinh anh |
| `CombatArea_10_Jungle_CanopyHollow_Elite` | Mộc, tinh anh |
| `BossSpawn_Stage1` (Heart of the Grove) | Mộc, boss |
| `BossSpawn` (Altar of Embers) | Hỏa, boss |

| Bậc | To hơn | Máu | Công | Tầm phát hiện, tầm đuổi | Hồi sinh |
|---|---|---|---|---|---|
| Thường | 1 | Giữ prefab | Giữ prefab | Giữ prefab | Giữ prefab |
| Tinh anh | 1.5 | x3 | x1.4 | x1.3, x1.4 | 30 giây |
| Boss | 2.5 | x10 | x1.8 | x1.8, x2 | Không |

Đổi hệ, hệ số hoặc thêm loại marker trong `Editor/Prototype/ElementiaPrototypeTools.cs` rồi chạy lại Setup.

## Cỏ cây và static batching

Shader `VegetationSingleColor`, `VegetationMultiColor` của Holotna tính gió theo vị trí object space. Static batching đưa đỉnh sang world space nên cỏ, cây phình thành tấm khổng lồ khi chạy game. Setup Prototype và Build Map bỏ cờ Batching Static, Occluder Static khỏi mọi renderer dùng hai shader này (hiện 14.380 renderer). Thêm cỏ cây mới bằng tay thì chạy lại Setup Prototype, hoặc đừng tick Batching Static cho chúng.

## Animation hero

`Tools/Hero/Fix Animator Transitions` nối lại transition cho mọi controller trong `Hero/Animation/Generated`, giữ nguyên clip đã gán. Hai generator controller cũng gọi bước này nên controller sinh mới được nối giống vậy.

| Trước | Sau |
|---|---|
| Chạy trên dốc, bậc thấp bị chuyển Fall rồi Hard Landing vì `isGrounded` nhấp nháy | Coi là đang rơi khi rời đất quá 0.15 giây và không còn mặt đất trong 0.4 m dưới chân |
| Nhảy thường cũng phát Hard Landing 1.5 giây, hero trượt trong tư thế tiếp đất | Chỉ phát Land khi rơi sâu hơn 4 m. Đang di chuyển thì thoát Land sau 20% clip |
| Clip Jump hết mà còn trên không thì đứng ở frame cuối | Hết clip Jump thì sang Fall |
| Đánh, skill, dash chỉ vào được từ Locomotion. Bấm lúc khác thì trigger treo, animation phát trễ 0.4 đến 1.2 giây | Vào từ AnyState. Trigger không được dùng sau 0.2 giây thì bị hủy |
| Skill làm chậm cả Animator, nên chạy, dash cũng chậm theo | Skill chậm qua parameter `ActionSpeed`, chỉ state Skill_N dùng |
| Đánh thường cắt ngang skill và dash | Không cắt skill trước 85% clip, không cắt dash trước 60% clip |

Các ngưỡng chỉnh trong Inspector của `HeroAnimatorController` trên prefab hero.

### Clip còn thiếu

| Hero | Thiếu |
|---|---|
| Tất cả trừ Fire | Death dùng tạm `Hit` |
| `hero_000000` Fire | Model chibi, xem mục dưới. State trống: Jump, Fall, Land, Roll, Death. Chưa có Walk (blend tree dùng Run từ ngưỡng 0.1) |
| `hero_000003` Mental | Walk (blend tree dùng Run từ ngưỡng 0.1) |
| `hero_000001`, `000002`, `000004` | Idle dùng tạm `Falling Idle`. Attack, Skill_0, Skill_1 dùng chung `Hook` |
| Quái Crab | Death dùng clip Idle ở tốc độ 0, MonsterInstance lật và lún xác bằng code |

Thay clip trong Animator Controller xong thì không cần chạy lại tool, transition giữ nguyên.

### Model Fire chibi

| Mục | Giá trị |
|---|---|
| FBX | `02_Art/3D/MainCharacters/Fire/2711_Fire_rig_anim.fbx`, rig `Chibi_Rig`, cao 1.68 m |
| Rig | Generic, không avatar, không root motion (animation chạy tại chỗ) |
| Clip | `Fire_Idle` (0.67 giây, lặp), `Fire_Run` (0.80 giây, lặp), `Fire_Attack` (0.23 giây), `Fire_Skill` (1.80 giây) |
| Event bắn | `OnAnimationTransitionEvent` ở khung súng giật: `Fire_Attack` 50%, `Fire_Skill` 56% (frame 30) |
| Controller | `hero_000000_Animator`: Skill_0 và Skill_1 cùng dùng `Fire_Skill`. Jump, Fall, Land, Roll, Death để trống |
| Material | Remap theo tên sang các `.mat` trong thư mục Fire |
| Điểm gắn | `Muzzle` ở đầu nòng, `ChargePoint` ở thân súng, con của xương `Weapon.R`, tính từ bindpose mesh súng |

Skill tụ lực bằng `ActionSpeed` để khung bắn trong `Fire_Skill` (giây 1.0) rơi đúng lúc skill phát:

| Skill | Cast | AnimationSpeed | Skill phát khi clip ở |
|---|---|---|---|
| FlameShot | 0.65 giây | 1.62 | 55% |
| InfernoBlast | 1.0 giây | 1.05 | 56% |
| MeteorRain | 1.0 giây | 1.05 | 56% |

Đổi clip skill thì tìm lại khung bắn, sửa event trong Import Settings và đặt `AnimationSpeed = giây bắn / (TotalCastDuration × 0.95)`.

## Cự Nhân Thép của Kim (Mental)

Ultimate `Metal_Ultimate_TitanForm` gắn trạng thái `Status_TitanForm` 8 giây. Trước đây trạng thái phóng to hero 1.5 lần. Giờ `HeroFormSwitcher` trên `Hero_Mental_000` đổi model `Metal_1` sang robot `Metal_2_Titan` (`METAL_2_2711.fbx`) và đổi lại khi hết.

| Mục | Giá trị |
|---|---|
| Model robot | `Model/Metal_2_Titan`, tắt sẵn. Animator dùng `hero_000003_Titan_Animator` (nhân bản từ controller Mental). Locomotion dùng `Metal_2_Idle`, `Metal_2_Run`, Attack dùng `Metal_2_Attack`. Các state khác để trống |
| Animation, đòn đánh | `HeroInstance.SwapAnimatorController` chuyển mọi lệnh animation sang model đang hiện |
| Thân va chạm | Cao 2.3, bán kính 0.55 khi là robot. Trả về 2 và 0.3 khi hết |
| Điểm gắn | `shoulder_R.003_end` thêm vào tay phải robot (`Bone.005_R.004`) để Chain Lightning bắn từ tay robot. Điểm gắn và hiệu ứng nảy khi trúng đòn chỉ tìm trên model đang hiện |
| Chết khi là robot | Trạng thái bị xóa, tự về `Metal_1` và phát Death trên model đó |

| Giai đoạn | VFX |
|---|---|
| Cast (0.6 giây, animation Skill_0 của Metal_1) | `MagicChargeYellow` |
| Skill phát | `MagicPillarBlastYellow` cột sáng |
| Đổi sang robot | `LightningExplosionYellow` ở giữa thân, vòng sét `NovaLightningYellow` dưới đất. Robot bật từ 60% lên 100% |
| Trong 8 giây | `MagicAuraYellow` x1.6, viền vàng, rung khi bước |
| Đổi về | `MagicSoftExplosionYellow`, bụi `DustDirtyPoof`. Metal_1 bật từ 130% về 100% |

Thêm dạng biến hình cho hero khác: thêm model con (tắt sẵn, có Animator và HeroAnimatorController), rồi khai báo trạng thái và model trong `HeroFormSwitcher`. Không phải sửa code.

## Chưa có

- Boss là crab phóng to, chưa có skill hay pha đánh riêng.
- Lava, nước, đầm lầy chưa gây sát thương hay làm chậm.
- Rương (`TreasureChest_*`) và bảng nhiệm vụ chỉ là mô hình.
- Chưa có NavMesh. Quái và đồng đội đi thẳng theo hướng tới mục tiêu nên có thể kẹt sau cây, nhà, đá.
- Chưa có UI báo đã qua checkpoint, chỉ ghi log.

## Đã kiểm tra (2026-11-27, qua Unity MCP)

- Play Prototype vào thẳng map. Hero xuất hiện giữa quảng trường, đứng trên mặt đất. Camera góc nhìn thứ 3 và HUD hoạt động.
- 28 quái và 18 vị trí hồi sinh (5 checkpoint và điểm spawn, mỗi chỗ 3 hero) đều trên mặt đất, không vướng vật cản.
- Đi tới đền ven đường thì checkpoint kích hoạt. Quái Thổ ở bãi Wilds tự lao vào đánh, số sát thương hiện đúng.
- Rơi xuống dưới Kill Y thì về checkpoint. Hero ngã thì sau 3 giây hồi sinh đầy máu ở checkpoint.

## Cần kiểm tra khi chơi tay

1. Camera không xuyên tường làng và nhà.
2. Đi bộ được từ làng ra đường nối, núi lửa và hai khu rừng, không bị chặn bởi collider lạ.
3. Đánh boss: boss to x2.5 có kẹt trong sàn đấu hay bị cây chặn không.
4. Đội 3 hero xếp hàng ở điểm spawn: lần thử chỉ có 1 hero trong đội.
5. FPS trên máy yếu: map có khoảng 5000 prefab.
