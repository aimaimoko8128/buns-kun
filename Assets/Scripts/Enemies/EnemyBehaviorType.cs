namespace BunsKun.Enemies
{
    /// <summary>
    /// High-level movement/attack pattern an enemy follows. New enemy archetypes can be
    /// added by adding a data entry that reuses one of these behaviors, or by adding a
    /// new case to EnemyController without touching unrelated enemies.
    /// </summary>
    public enum EnemyBehaviorType
    {
        Melee,
        Fast,
        Ranged,
        Heavy,
        Flying,
        Boss
    }
}
