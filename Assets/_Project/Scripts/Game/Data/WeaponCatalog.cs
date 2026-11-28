using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>武器3種の一覧（設計書 §4）。プレイの前に選んだ WeaponId から定義を引く。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/WeaponCatalog")]
    public sealed class WeaponCatalog : ScriptableObject
    {
        [SerializeField] WeaponDefinition[] _weapons = new WeaponDefinition[0];

        /// <summary>id の定義。ないなら null。</summary>
        public WeaponDefinition DefinitionOf(WeaponId id)
        {
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i] != null && _weapons[i].Id == id) return _weapons[i];
            }
            return null;
        }
    }
}
