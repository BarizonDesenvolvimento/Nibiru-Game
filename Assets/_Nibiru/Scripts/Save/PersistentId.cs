using System;
using UnityEngine;

namespace Nibiru.Save
{
    /// <summary>
    /// Identidade fixa de um objeto da cena (item no chão, checkpoint, porta...), usada para
    /// lembrar o que já mudou nele ao voltar de um checkpoint e, mais tarde, no save.
    /// O código é gerado sozinho; ao duplicar o objeto, a cópia ganha um código novo.
    /// </summary>
    [DisallowMultipleComponent]
    public class PersistentId : MonoBehaviour
    {
        [Tooltip("Gerado automaticamente. Não edite.")]
        [SerializeField] string id;

        public string Id => id;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying || !gameObject.scene.IsValid()) return;
            if (string.IsNullOrEmpty(id) || IsDuplicated())
            {
                id = Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        bool IsDuplicated()
        {
            foreach (PersistentId other in FindObjectsByType<PersistentId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (other != this && other.id == id && other.gameObject.scene == gameObject.scene)
                    return true;
            return false;
        }
#endif
    }
}
