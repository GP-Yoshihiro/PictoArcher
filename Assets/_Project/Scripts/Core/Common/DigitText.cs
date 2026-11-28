using System.Globalization;
using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>画面に出す数字の文字列を作る。数字と記号（/ :）だけで、文字は出さない。</summary>
    public static class DigitText
    {
        /// <summary>進行（23/50）。</summary>
        public static string Progress(int current, int total) =>
            current.ToString(CultureInfo.InvariantCulture) + "/" + total.ToString(CultureInfo.InvariantCulture);

        /// <summary>時間（分:秒、23:45）。分に上限はなく、秒は切り捨てる。負と NaN は 0:00。</summary>
        public static string Duration(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f) seconds = 0f;
            int total = Mathf.FloorToInt(seconds);
            int minutes = total / 60;
            int rest = total % 60;
            return minutes.ToString(CultureInfo.InvariantCulture) + ":" + rest.ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>数（コインなど）。</summary>
        public static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
