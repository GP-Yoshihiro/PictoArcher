using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>プレイヤーの見た目の表。武器ごとに1つの CharacterVisual を持つ。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/Visuals/PlayerVisual")]
    public sealed class PlayerVisual : ScriptableObject
    {
        [SerializeField] CharacterVisual _gun;
        [SerializeField] CharacterVisual _laser;
        [SerializeField] CharacterVisual _launcher;

        public CharacterVisual Gun => _gun;
        public CharacterVisual Laser => _laser;
        public CharacterVisual Launcher => _launcher;
    }
}
