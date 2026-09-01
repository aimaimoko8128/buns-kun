namespace BunsKun.Ingredients
{
    /// <summary>
    /// Whether an ingredient fires a damaging attack itself, or buffs the attack(s)
    /// that come after it in the sequence. There is no third "combination" kind by design.
    /// </summary>
    public enum IngredientKind
    {
        Attack,
        Buff
    }
}
