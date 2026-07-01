using System.Collections.Generic;
using Dylanng.Core.Base;
using UnityEngine;

namespace Dylanng.Core.UI
{
    [CreateAssetMenu(fileName = "NewUiDatabase", menuName = "Dylanng/SO/UI Database")]
    public class UiDatabaseSO : ScriptableData
    {
        public List<UIBase> ListUI;
    }
}