using System.Collections.Generic;
using Nibiru.Save;

namespace Nibiru.Powers
{
    /// <summary>
    /// Os despertares da Elena (a progressão sem XP nem nível). A demo tem um, disparado ao ler a Égide (passo 13); o
    /// sistema e os códigos são do ME7. No ME1 só existe o lugar onde eles moram (no <see cref="WorldState"/>, gravado
    /// no slot como "despertares"), vazio por enquanto.
    /// </summary>
    public static class Awakenings
    {
        public static bool Has(string code) => WorldState.HasAwakening(code);

        public static IEnumerable<string> All => WorldState.AwakeningCodes;

        public static int Count => WorldState.AwakeningCount;

        /// <summary>Marca um despertar (ME7). Sem efeito se já estava marcado.</summary>
        public static void Mark(string code) => WorldState.AddAwakening(code);
    }
}
