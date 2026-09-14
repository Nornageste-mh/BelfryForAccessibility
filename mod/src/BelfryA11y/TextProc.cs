using System.Text.RegularExpressions;

namespace BelfryA11y
{
    /// <summary>
    /// 文本处理：把游戏里的原始字符串变成适合朗读的字符串。
    ///
    /// 《钟塔》的剧本是干净的纯文本（把 extracted/ 里五张表的 Dialogue 字段
    /// 全扫过一遍，没有任何富文本标签），所以这里要处理的只有三类东西：
    ///
    ///   1. 说话人名的包装。剧本里存的是 `【 韩冬 】`（全角方括号 + 两侧空格），
    ///      照读会变成「左黑括号 空格 韩冬 空格 右黑括号」，必须先剥掉。
    ///      全表统计：1256 行无名、1065 行韩冬、937 行许堇、481 行小堇、
    ///      26 行老妈、23 行 `【 ?  ? 】`、其余零散。
    ///
    ///   2. 未知说话人。`【 ?  ? 】` 是故意的遮蔽名，逐字念是噪音，
    ///      统一念成「？？？」。
    ///
    ///   3. 多人同框。`【 许堇/韩冬 】` / `【 小堇/许堇/韩冬 】` 共 3 行，
    ///      斜杠要给读屏一个停顿，念成「许堇、韩冬」。
    /// </summary>
    internal static class TextProc
    {
        // 【…】/［…］/[...]/(...) 之类包在名字外面的括号
        private static readonly Regex NameBracketRe =
            new Regex(@"^[\s【】\[\]（）()〔〕「」『』]+|[\s【】\[\]（）()〔〕「」『』]+$");

        // 连续空白（含全角空格）
        private static readonly Regex SpaceRe = new Regex(@"[ \t\u3000]+");

        // 连续的「?」「？」「?  ?」等遮蔽写法
        private static readonly Regex MaskRe = new Regex(@"^[?？\s]+$");

        /// <summary>说话人原名 → 朗读名。空名返回空串（旁白不加前缀）。</summary>
        public static string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            string s = NameBracketRe.Replace(raw, "");
            s = SpaceRe.Replace(s, " ").Trim();

            // 【 ?  ? 】→ ？？？
            if (MaskRe.IsMatch(s)) return "？？？";

            // 【 许堇/韩冬 】→ 许堇、韩冬
            s = s.Replace("/", "、");
            s = s.Replace("／", "、");

            return s;
        }

        /// <summary>原始台词 → 朗读文本。</summary>
        public static string ToSpeech(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            string s = raw.Replace("\r\n", "\n").Replace("\r", "\n");
            // 台词内部偶有换行；读屏遇到换行会停顿，但连续多个换行会读成空白，压掉
            s = Regex.Replace(s, @"\n{2,}", "\n");
            s = s.Replace("\n", "，");
            s = SpaceRe.Replace(s, " ");
            return s.Trim();
        }

        /// <summary>去掉「」书名号之类的包装，用于和 TMP 上显示的文字比对。</summary>
        public static string Fingerprint(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (char.IsWhiteSpace(c)) continue;
                if (c == '「' || c == '」' || c == '『' || c == '』') continue;
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>这句话是否只是标点/省略号（不值得单独朗读）。</summary>
        public static bool IsTrivial(string speech)
        {
            if (string.IsNullOrEmpty(speech)) return true;
            foreach (char c in speech)
            {
                if (char.IsLetterOrDigit(c)) return false;
                if (c >= 0x4E00 && c <= 0x9FFF) return false;
            }
            return true;
        }
    }
}
