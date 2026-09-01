using UnityEngine;

namespace BunsKun.Upgrades
{
    /// <summary>
    /// A run-scoped upgrade offered on room clear. All bonuses are stored as simple
    /// deltas/percentages that RunUpgradeStats accumulates additively - no permanent
    /// meta-progression is stored anywhere.
    /// </summary>
    [CreateAssetMenu(menuName = "BunsKun/Upgrade", fileName = "NewUpgrade")]
    public class UpgradeData : ScriptableObject
    {
        public string upgradeId;
        public string upgradeName;
        [TextArea] public string description;
        public Color color = Color.white;

        public float maxHpBonus = 0f;
        public float moveSpeedPercent = 0f;
        public float damagePercent = 0f;
        public float delayReductionPercent = 0f;
        public float maxManaBonus = 0f;
        public float manaRegenPercent = 0f;
        public float projectileSpeedPercent = 0f;
        public float projectileSizePercent = 0f;
        public float critChanceBonus = 0f;
        public float damageTakenReductionPercent = 0f;

        public static UpgradeData Create(string id, string name, string desc, Color color)
        {
            var data = CreateInstance<UpgradeData>();
            data.upgradeId = id;
            data.upgradeName = name;
            data.description = desc;
            data.color = color;
            data.name = "Upgrade_" + id;
            return data;
        }
    }
}
