# Kiến trúc Hệ thống Tìm đường Đa màn hình (Multi-Scene A* Pathfinding)

Để giải quyết bài toán nhân vật tự động tìm đường từ Scene A sang Scene C (đi qua Scene B), bạn không thể chỉ dùng thuật toán A* thông thường (Local Pathfinding) vì lưới A* (Grid/NavMesh) của Scene C chưa được load khi bạn đang ở Scene A.

Cách giải quyết hiệu quả, tối ưu và chuẩn xác nhất cho thể loại game này (như RPG, MMORPG) là sử dụng **Mô hình Tìm đường 2 Tầng (Two-Tier Pathfinding)**: Tầng Global (Thế giới) và Tầng Local (Cục bộ).

---

## 1. Nguyên lý hoạt động (The Two-Tier Model)

### Tầng 1: Global Pathfinding (Tìm đường liên map)
Bạn cần xây dựng một **World Graph (Đồ thị Thế giới)**. 
- **Node (Điểm/Nút):** Chính là các **Cổng dịch chuyển (Portals/Gates)**.
- **Edge (Cạnh):** 
  - Khoảng cách nối giữa 2 Portal **trong cùng một Scene** (ví dụ: Cổng Bắc nối Cổng Nam ở Scene B).
  - Kết nối giữa 2 Portal **khác Scene** (ví dụ: Cổng Bắc Scene A nối Cổng Nam Scene B). Chi phí (Cost) của cạnh này thường bằng 0.

Thuật toán A* ở tầng này sẽ chạy trên Đồ thị các Cổng.
*Ví dụ:* Đứng ở A, muốn đến C. Global A* sẽ trả về chuỗi các cổng cần đi qua: `[Cổng_A_Sang_B] -> [Cổng_B_Vào_Từ_A] -> [Cổng_B_Sang_C] -> [Cổng_C_Vào_Từ_B]`.

### Tầng 2: Local Pathfinding (Tìm đường trong map hiện tại)
Đây là thuật toán A* gốc của bạn (trên Grid hoặc NavMesh).
Nhiệm vụ của nó rất đơn giản: Tìm đường từ **Vị trí hiện tại** đến **Mục tiêu tiếp theo (Next Waypoint)** do Tầng 1 cung cấp.

---

## 2. Kiến trúc cụ thể (Folder & Code Structure)

Dựa trên cấu trúc tính năng (`Features`) mà chúng ta vừa vạch ra, bạn có thể thiết kế hệ thống như sau:

### 2.1. Cấu trúc Dữ liệu (Dùng ScriptableObject)
Lưu trữ đồ thị toàn cầu bằng `ScriptableObject` để dữ liệu này luôn tồn tại mà không phụ thuộc vào Scene nào.

```csharp
// Lưu trữ thông tin một cổng
[Serializable]
public class PortalConnection 
{
    public string fromPortalId;
    public string toSceneName;
    public string toPortalId;
    public float transitionCost = 0f; 
}

// Lưu khoảng cách giữa các cổng trong cùng 1 scene (Bake sẵn)
[Serializable]
public class IntraScenePath 
{
    public string portalId1;
    public string portalId2;
    public float distanceCost;
}

[CreateAssetMenu(fileName = "WorldMapGraph", menuName = "Dylanng/Pathfinding/WorldMapGraph")]
public class WorldMapGraphSO : ScriptableObject 
{
    public List<PortalConnection> interSceneConnections;
    public List<IntraScenePath> intraScenePaths;
    
    // Hàm này chạy A* trên đồ thị cổng
    public List<string> FindGlobalPath(string startScene, Vector3 startPos, string targetScene, Vector3 targetPos) 
    {
        // 1. Tìm cổng gần nhất với startPos trong startScene
        // 2. Tìm cổng gần nhất với targetPos trong targetScene
        // 3. Chạy A* để tìm đường từ Cổng Start -> Cổng Target
        // 4. Trả về List các Portal ID cần đi qua
    }
}
```

### 2.2. Xử lý trên Entity/Agent
Bạn tạo một component `MultiSceneNavigator` gắn lên nhân vật.

```csharp
public class MultiSceneNavigator : MonoBehaviour
{
    private WorldMapGraphSO worldGraph;
    private ILocalPathfinder localPathfinder; // Interface bọc thuật toán A* trong scene
    
    private Queue<string> globalPath; // Hàng đợi các cổng cần đi
    
    public void NavigateTo(string targetScene, Vector3 targetPos) 
    {
        if (CurrentScene == targetScene) 
        {
            // Cùng map -> Chỉ cần Local A*
            localPathfinder.SetDestination(targetPos);
        }
        else 
        {
            // Khác map -> Gọi Global A*
            globalPath = new Queue<string>(worldGraph.FindGlobalPath(CurrentScene, transform.position, targetScene, targetPos));
            GoToNextPortal();
        }
    }
    
    private void GoToNextPortal() 
    {
        if (globalPath.Count > 0) 
        {
            string nextPortalId = globalPath.Dequeue();
            Vector3 portalPos = PortalManager.Instance.GetPortalPosition(nextPortalId);
            localPathfinder.SetDestination(portalPos);
        }
    }
}
```

### 2.3. Quy trình Tương tác (Flow) khi chạm Cổng
Nhân vật cần giao tiếp với Hệ thống Scene (đã có `SceneController`, `SceneLoadManager` trong Project của bạn).

1. Nhân vật đi (Local A*) và chạm vào Trigger của `Portal_A`.
2. `Portal_A` bắn Event qua `EventBus`: `EventBus.Publish(new TeleportEvent(targetScene, targetPortalId))`.
3. `SceneLoadManager` nhận Event, tiến hành fade màn hình, load `Scene B`.
4. `Scene B` load xong (`SceneLoadedEvent` được bắn ra).
5. `SceneController` của Scene B tìm object `PortalManager`, lấy vị trí của `targetPortalId` và spawn/dịch chuyển nhân vật tới đó.
6. Nhân vật kiểm tra lại `MultiSceneNavigator`. Nhận thấy `globalPath` vẫn còn cổng tiếp theo, nó tiếp tục gọi `GoToNextPortal()` để chạy tới cổng bên kia của Scene B.

---

## 3. Các bước tối ưu & Dễ mở rộng (Best Practices)

1. **Pre-bake Intra-scene Costs (Tính toán trước chi phí trong map):**
   Đừng để game phải bật A* tính quãng đường giữa 2 cổng trong Scene B khi bạn đang đứng ở Scene A. Hãy tạo một Tool trên Editor: Mỗi khi lưu Scene, tool sẽ chạy A* giữa các cổng trong Scene đó, đo khoảng cách và lưu vào `WorldMapGraphSO` (`IntraScenePath`). Khi chạy thật, thuật toán Global A* chỉ việc đọc con số này (O(1)) -> Siêu mượt.

2. **Decoupling (Giảm phụ thuộc) bằng Interfaces:**
   Tạo interface `ILocalPathfinder`. Cho dù hôm nay bạn tự viết lưới A* (Grid A*), ngày mai bạn đổi sang Unity NavMesh, hay dùng thư viện A* Pathfinding Project của Aron Granberg, Tầng Global của bạn hoàn toàn không phải sửa code. Bạn chỉ cần viết 1 script Adapter kế thừa `ILocalPathfinder`.

3. **Gắn ID tĩnh cho các Portal:**
   Nên dùng chuỗi ID tĩnh (ví dụ: `SCENE_FOREST_PORTAL_NORTH`) hoặc ScriptableObject làm ID để định danh cổng, không nên dùng GameObject reference hay Name của object vì khi load khác scene sẽ bị mất reference.

4. **Sử dụng EventBus để chuyển Scene:**
   Hệ thống di chuyển không nên gọi trực tiếp `SceneLoadManager`. Khi chạm cổng, hãy ném ra một Event. Bất kỳ UI Loading, Hệ thống Save Game hay Hệ thống Analytics nào cần biết nhân vật vừa qua màn đều có thể lắng nghe Event này.

> [!TIP]
> Bạn có thể đưa cụm logic này vào folder `Game/Features/Navigation` (nếu làm tính năng mới) hoặc tích hợp vào phần `Game/Features/AI` (vì tìm đường là của AI/Nhân vật).
