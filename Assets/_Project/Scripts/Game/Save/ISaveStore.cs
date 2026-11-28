using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>保存の読み書き。本番は JsonFileSaveStore。読めないときは SaveData.Empty() を返し、例外を外に出さない。</summary>
    public interface ISaveStore
    {
        SaveData Load();
        void Save(SaveData data);
    }
}
