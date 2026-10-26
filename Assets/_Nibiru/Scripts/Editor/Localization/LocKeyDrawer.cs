using Nibiru.Localization;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Localization
{
    /// <summary>
    /// Desenha os campos de chave de tradução (<see cref="LocKeyAttribute"/>) só para leitura: a chave e, embaixo, o texto
    /// em pt-BR que ela tem na tabela (ou um aviso, se a chave está vazia ou sem texto). Não grava nada.
    /// </summary>
    [CustomPropertyDrawer(typeof(LocKeyAttribute))]
    sealed class LocKeyDrawer : PropertyDrawer
    {
        const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight + Gap + PreviewHeight(property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.TextField(line, label, string.IsNullOrEmpty(property.stringValue) ? "(sem chave)" : property.stringValue);

            string key = property.stringValue;
            var box = new Rect(position.x + EditorGUIUtility.labelWidth, line.yMax + Gap, position.width - EditorGUIUtility.labelWidth, PreviewHeight(property));
            if (string.IsNullOrEmpty(key))
                EditorGUI.HelpBox(box, "Sem chave: rode Nibiru > Localização > Extrair textos dos arquivos de dados.", MessageType.None);
            else if (!Loc.Has(key))
                EditorGUI.HelpBox(box, "A chave não tem texto no pt-BR.", MessageType.Warning);
            else
                EditorGUI.HelpBox(box, Loc.Get(key), MessageType.None);
        }

        static float PreviewHeight(SerializedProperty property)
        {
            string key = property.stringValue;
            string text = string.IsNullOrEmpty(key) ? "Sem chave: rode Nibiru > Localização > Extrair textos dos arquivos de dados."
                : Loc.Has(key) ? Loc.Get(key) : "A chave não tem texto no pt-BR.";
            float width = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 50f);
            return Mathf.Clamp(EditorStyles.helpBox.CalcHeight(new GUIContent(text), width), 22f, 220f);
        }
    }
}
