using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 倒した敵から経験値とコインを落とし、プレイヤーが近づくと吸い寄せて拾う。ウェーブを全滅させると全部を吸い寄せる。
    /// 拾った合計を数える（レベルアップは 06、HUD の表示はそのあとのタスクで読む）。
    /// </summary>
    public sealed class PickupSystem : ITickable, IDisposable
    {
        sealed class Item
        {
            public PickupKind Kind;
            public int Amount;
            public PickupAttraction Motion;
            public PickupView View;
        }

        readonly PickupView _prefab;
        readonly PickupSettings _settings;
        readonly IEnemyEvents _enemies;
        readonly ITarget _player;
        readonly List<Item> _items = new List<Item>();
        bool _attractAllRequested;

        /// <summary>拾うたびに届く（種類と量）。</summary>
        public event Action<PickupKind, int> Collected;

        public PickupSystem(PickupView prefab, PickupSettings settings, IEnemyEvents enemies, ITarget player)
        {
            _prefab = prefab;
            _settings = settings;
            _enemies = enemies;
            _player = player;
            _enemies.Died += HandleDied;
        }

        /// <summary>このプレイで拾った経験値の合計。</summary>
        public int Experience { get; private set; }
        /// <summary>このプレイで拾ったコインの合計。</summary>
        public int Coins { get; private set; }
        /// <summary>まだ拾われていない数。</summary>
        public int ActiveCount => _items.Count;
        /// <summary>落ちていてまだ拾われていないコインの合計（クリアの瞬間に吸い寄せ中の分を結果に足すため）。</summary>
        public int PendingCoins
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < _items.Count; i++) if (_items[i].Kind == PickupKind.Coin) sum += _items[i].Amount;
                return sum;
            }
        }

        /// <summary>落ちているものを全部吸い寄せる。同じフレームにあとから落ちた分も含めるため、次の Tick で行う。</summary>
        public void AttractAll() => _attractAllRequested = true;

        public void Drop(PickupKind kind, int amount, Vector2 position)
        {
            if (amount <= 0) return;
            var view = UnityEngine.Object.Instantiate(_prefab);
            view.Init(_settings.VisualFor(kind), position);
            _items.Add(new Item { Kind = kind, Amount = amount, Motion = new PickupAttraction(position, _settings.Tuning), View = view });
        }

        public void Tick(float deltaTime)
        {
            if (_attractAllRequested)
            {
                _attractAllRequested = false;
                for (int i = 0; i < _items.Count; i++) _items[i].Motion.Attract();
            }
            // 死んだあとは拾わない（その場で止める）
            if (!_player.IsAlive) return;
            var target = _player.Position;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (!item.Motion.Tick(deltaTime, target))
                {
                    item.View.SetPosition(item.Motion.Position);
                    continue;
                }
                if (item.Kind == PickupKind.Coin) Coins += item.Amount;
                else Experience += item.Amount;
                if (item.View != null) UnityEngine.Object.Destroy(item.View.gameObject);
                _items.RemoveAt(i);
                Collected?.Invoke(item.Kind, item.Amount);
            }
        }

        public void Dispose()
        {
            _enemies.Died -= HandleDied;
            for (int i = 0; i < _items.Count; i++)
                if (_items[i].View != null) UnityEngine.Object.Destroy(_items[i].View.gameObject);
            _items.Clear();
        }

        // 経験値は左、コインは右に少しずらして、重ならないようにする
        void HandleDied(EnemyDeath death)
        {
            var spread = new Vector2(_settings.DropSpread, 0f);
            Drop(PickupKind.Experience, death.Experience, death.Position - spread);
            Drop(PickupKind.Coin, death.Coins, death.Position + spread);
        }
    }
}
