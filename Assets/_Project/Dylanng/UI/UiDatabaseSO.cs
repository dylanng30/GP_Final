using System.Collections.Generic;
using UnityEngine;

namespace Dylanng
{
    [CreateAssetMenu(fileName = "NewUiDatabase", menuName = "Dylanng/SO/UI Database")]
    public class UiDatabaseSO : ScriptableData
    {
        public List<UIBase> ListUI;
    }
}