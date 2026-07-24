using System.Collections;
using UnityEngine;

namespace TCO_CustomSceneController
{
    public abstract class BaseLoadingOverlay : MonoBehaviour
    {
        public abstract IEnumerator StartLoad();
        public abstract IEnumerator EndLoad();
    }
}