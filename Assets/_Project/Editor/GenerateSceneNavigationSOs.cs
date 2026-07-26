using UnityEngine;
using UnityEditor;
using TCO_CustomSceneController;
using System.IO;

public class GenerateSceneNavigationSOs : MonoBehaviour
{
    [MenuItem("Tools/Generate Scene Navigation SOs")]
    public static void GenerateSOs()
    {
        string folderPath = "Assets/_Project/Game/Features/Navigation/Data/SceneNavigations";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        // 1. Boot to Scene A
        var bootToA = ScriptableObject.CreateInstance<SceneNavigationSO>();
        bootToA.ActiveScene = new SceneSlotData { SlotKey = "Main", SceneName = "Scene_A" };
        bootToA.AdditiveScenes = new SceneSlotData[] 
        {
            new SceneSlotData { SlotKey = "Sub1", SceneName = "Scene_B" } // Preload B ẩn
        };
        bootToA.UnloadSceneSlots = new string[] { "Bootstrapper" };
        bootToA.HasOverlay = true;
        bootToA.ClearUnusedAssets = true;
        AssetDatabase.CreateAsset(bootToA, $"{folderPath}/BootToSceneA_SO.asset");

        // 2. A -> B (Bật B, giữ A ẩn, tải thêm C ẩn)
        var aToB = ScriptableObject.CreateInstance<SceneNavigationSO>();
        aToB.ActiveScene = new SceneSlotData { SlotKey = "Sub1", SceneName = "Scene_B" };
        aToB.AdditiveScenes = new SceneSlotData[] 
        {
            new SceneSlotData { SlotKey = "Main", SceneName = "Scene_A" }, // Giữ A ẩn
            new SceneSlotData { SlotKey = "Sub2", SceneName = "Scene_C" }  // Preload C ẩn
        };
        // Cố tình tắt Overlay để bạn thấy độ mượt và nhanh của Preload (chuyển ngay lập tức)
        aToB.HasOverlay = false; 
        AssetDatabase.CreateAsset(aToB, $"{folderPath}/SceneA_to_SceneB_SO.asset");

        // 3. B -> C (Bật C, giữ B ẩn, dọn dẹp A)
        var bToC = ScriptableObject.CreateInstance<SceneNavigationSO>();
        bToC.ActiveScene = new SceneSlotData { SlotKey = "Sub2", SceneName = "Scene_C" };
        bToC.AdditiveScenes = new SceneSlotData[] 
        {
            new SceneSlotData { SlotKey = "Sub1", SceneName = "Scene_B" } // Giữ B ẩn
        };
        bToC.UnloadSceneSlots = new string[] { "Main" }; // Hủy Scene A giải phóng RAM
        bToC.HasOverlay = false;
        AssetDatabase.CreateAsset(bToC, $"{folderPath}/SceneB_to_SceneC_SO.asset");

        // 4. B -> A (Bật A, giữ B ẩn, dọn dẹp C)
        var bToA = ScriptableObject.CreateInstance<SceneNavigationSO>();
        bToA.ActiveScene = new SceneSlotData { SlotKey = "Main", SceneName = "Scene_A" };
        bToA.AdditiveScenes = new SceneSlotData[] 
        {
            new SceneSlotData { SlotKey = "Sub1", SceneName = "Scene_B" } // Giữ B ẩn
        };
        bToA.UnloadSceneSlots = new string[] { "Sub2" }; // Hủy Scene C
        bToA.HasOverlay = false;
        AssetDatabase.CreateAsset(bToA, $"{folderPath}/SceneB_to_SceneA_SO.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[GenerateSceneNavigationSOs] Đã tạo lại 4 file cấu hình SO Preload tại: {folderPath}");
    }
}
