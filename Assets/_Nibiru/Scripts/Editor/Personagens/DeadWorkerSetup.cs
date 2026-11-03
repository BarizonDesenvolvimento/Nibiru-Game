using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Nibiru.EditorTools.Content;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// O corpo do trabalhador executado no heliponto do Acampamento (`Corpo_Trabalhador`, cápsula tombada do
    /// CampContent): o modelo do TrabalhadorEscavacao deitado, parado (o clipe deitado do Yusuf no primeiro quadro,
    /// sem a respiração), entra como "Visual" da cápsula pelo ContentKit.UseModel, como o Yusuf.
    /// Menu: Nibiru > Personagens > Corpo do trabalhador na cena aberta. Rode depois do menu do Acampamento e do
    /// Montar personagens da Aldeia e da equipe.
    /// </summary>
    static class DeadWorkerSetup
    {
        const string Character = "TrabalhadorEscavacao";
        const string Placeholder = "Corpo_Trabalhador";
        static string PrefabPath => VillageTeamSetup.PrefabFolder + Character + "_Morto.prefab";
        static string ControllerPath => $"{VillageTeamSetup.ArtRoot}{Character}/{Character}_Morto.controller";

        [MenuItem("Nibiru/Personagens/Corpo do trabalhador na cena aberta")]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes.");
                return;
            }
            var lying = AnimationLibrary.Clip("Yusuf_Deitado");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(VillageTeamSetup.PrefabPath(Character));
            if (lying == null || source == null)
            {
                Debug.LogError("Corpo do trabalhador: rode antes Nibiru > Personagens > Montar personagens da Aldeia e da equipe.");
                return;
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("Morto");
            state.motion = lying;
            state.speed = 0f;
            EditorUtility.SetDirty(controller);

            var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                var model = root.transform.Find("Modelo");
                model.GetComponent<Animator>().runtimeAnimatorController = controller;
                float lowest = CharacterHeights.Extent($"{VillageTeamSetup.ArtRoot}{Character}/{Character}.fbx", lying).min;
                model.localPosition = new Vector3(0f, -lowest, 0f);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            GameObject body = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == Placeholder && t.gameObject.scene.IsValid())
                    body = t.gameObject;
            if (body == null)
            {
                Debug.LogWarning($"Corpo do trabalhador: não achei {Placeholder} na cena (rode o menu do Acampamento).");
                return;
            }
            // A cápsula está no chão do heliponto (y = 0), como o CampContent a põe.
            ContentKit.UseModel(body, PrefabPath, 0f, flatten: true);
            EditorSceneManager.MarkSceneDirty(body.scene);
            Debug.Log("Corpo do trabalhador: modelo deitado no lugar da cápsula. Salve a cena.");
        }
    }
}
