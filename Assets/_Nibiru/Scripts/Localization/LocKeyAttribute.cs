using UnityEngine;

namespace Nibiru.Localization
{
    /// <summary>
    /// Marca um campo que guarda a chave de um texto da tabela de idiomas. No Inspector, o campo aparece só para
    /// leitura, com o texto em pt-BR logo abaixo (veja <c>LocKeyDrawer</c>). Para mudar o texto, edite o pt-BR.json.
    /// </summary>
    public class LocKeyAttribute : PropertyAttribute
    {
    }
}
