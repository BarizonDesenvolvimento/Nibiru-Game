namespace Nibiru.Combat
{
    /// <summary>
    /// Algo que muda o dano recebido (uma armadura que só cede quando o inimigo está
    /// atordoado, por exemplo). Devolve o multiplicador: 1 = normal, 0,25 = um quarto.
    /// </summary>
    public interface IDamageModifier
    {
        float DamageMultiplier(DamageInfo info);
    }
}
