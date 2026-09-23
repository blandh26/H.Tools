using Avalonia.Media;
using Avalonia.Media.Imaging;
using HTools.Windows.Imaging;

namespace HTools.App.Services;

/// <summary>
/// 设置页"语言"下拉框里每个语言前面的国旗小图标。
/// <para>
/// 为什么不用国旗 emoji（🇨🇳 🇺🇸 …）：Windows 自带的 Segoe UI Emoji 不包含国旗字形，
/// 在 Windows 上只会显示成两个字母（"CN"、"US"），所以这里改为内置 SVG，
/// 再用 <see cref="SvgRasterizer"/>（Svg.NET）栅格化为 PNG 交给 Avalonia 显示——
/// 与 <see cref="AppIcons"/> 相同的方案，避开与 Avalonia 12 不兼容的 Avalonia.Svg.Skia。
/// </para>
/// 所有国旗统一使用 30×20（3:2）的 viewBox，保证下拉框里图标尺寸一致。
/// </summary>
internal static class LanguageFlags
{
    // 五角星（外接圆半径 1、中心在原点）的 10 个顶点，供中国国旗通过 transform 缩放/平移复用
    private const string UnitStar =
        "0,-1 0.2245,-0.309 0.9511,-0.309 0.3633,0.118 0.5878,0.809 0,0.382 -0.5878,0.809 -0.3633,0.118 -0.9511,-0.309 -0.2245,-0.309";

    private static readonly Dictionary<string, string> Markup = new(StringComparer.OrdinalIgnoreCase)
    {
        // 中国：红底，左上一颗大星 + 四颗小星
        ["zh-CN"] = $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 30 20">
              <rect width="30" height="20" fill="#DE2910"/>
              <polygon points="{UnitStar}" fill="#FFDE00" transform="translate(5,5) scale(3)"/>
              <polygon points="{UnitStar}" fill="#FFDE00" transform="translate(10,2) scale(1)"/>
              <polygon points="{UnitStar}" fill="#FFDE00" transform="translate(12,4) scale(1)"/>
              <polygon points="{UnitStar}" fill="#FFDE00" transform="translate(12,7) scale(1)"/>
              <polygon points="{UnitStar}" fill="#FFDE00" transform="translate(10,9) scale(1)"/>
            </svg>
            """,

        // 美国：13 道红白条纹 + 左上蓝色星区（50 颗星在 24px 下看不清，用 5×6 的白点阵示意）
        ["en-US"] = BuildUsFlag(),

        // 日本：白底红日（圆直径为旗高的 3/5）
        ["ja-JP"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 30 20">
              <rect width="30" height="20" fill="#FFFFFF"/>
              <circle cx="15" cy="10" r="6" fill="#BC002D"/>
            </svg>
            """,

        // 韩国：白底太极 + 四角乾(☰)、坤(☷)、坎(☵)、离(☲) 四卦
        ["ko-KR"] = BuildKrFlag(),
    };

    private static readonly Dictionary<string, IImage?> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>返回语言代码对应的国旗图片；没有对应国旗或渲染失败时返回 null（下拉框只显示文字）。</summary>
    public static IImage? TryGet(string languageCode)
    {
        if (Cache.TryGetValue(languageCode, out var cached))
        {
            return cached;
        }

        IImage? image = null;
        if (Markup.TryGetValue(languageCode, out var svg))
        {
            try
            {
                // 显示尺寸 24×16，按 3 倍渲染，在 150%~300% 缩放的屏幕上依然清晰
                using var png = new MemoryStream(SvgRasterizer.RenderPng(svg, 72, 48));
                image = new Bitmap(png);
            }
            catch
            {
                // 国旗只是装饰，渲染失败不影响语言切换
            }
        }

        Cache[languageCode] = image;
        return image;
    }

    private static string BuildUsFlag()
    {
        const double stripe = 20d / 13;
        var parts = new List<string>
        {
            """<rect width="30" height="20" fill="#FFFFFF"/>""",
        };

        // 偶数条（0、2、…、12）为红色
        for (var i = 0; i < 13; i += 2)
        {
            parts.Add(FormattableString.Invariant($"""<rect y="{i * stripe:0.###}" width="30" height="{stripe:0.###}" fill="#B22234"/>"""));
        }

        // 星区：宽为旗宽的 2/5，高为 7 道条纹
        const double cantonWidth = 12, cantonHeight = 7 * stripe;
        parts.Add(FormattableString.Invariant($"""<rect width="{cantonWidth}" height="{cantonHeight:0.###}" fill="#3C3B6E"/>"""));
        for (var row = 0; row < 5; row++)
        {
            for (var col = 0; col < 6; col++)
            {
                var cx = 1 + col * 2;
                var cy = 1.1 + row * 2.1;
                parts.Add(FormattableString.Invariant($"""<circle cx="{cx}" cy="{cy:0.##}" r="0.45" fill="#FFFFFF"/>"""));
            }
        }

        return $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 30 20">{string.Concat(parts)}</svg>""";
    }

    private static string BuildKrFlag()
    {
        // 太极：先画整圆蓝色，再叠加红色的上半部分（左侧红色下探、右侧蓝色上探形成 S 形），
        // 整体沿旗面左上→右下对角线方向旋转 atan(20/30) ≈ 33.69°。
        const string taegeuk = """
            <g transform="rotate(33.69 15 10)">
              <circle cx="15" cy="10" r="5" fill="#0047A0"/>
              <path d="M10,10 A5,5 0 0 1 20,10 A2.5,2.5 0 0 0 15,10 A2.5,2.5 0 0 1 10,10 Z" fill="#CD2E3A"/>
            </g>
            """;

        // 四卦：true = 实线（阳爻），false = 中间断开（阴爻）；从靠近太极的一侧往外排列
        var trigrams = new[]
        {
            (X: 7.2, Y: 4.8, Angle: -56.31, Lines: new[] { true, true, true }),     // 乾 ☰ 左上
            (X: 22.8, Y: 15.2, Angle: -56.31, Lines: new[] { false, false, false }), // 坤 ☷ 右下
            (X: 22.8, Y: 4.8, Angle: 56.31, Lines: new[] { false, true, false }),    // 坎 ☵ 右上
            (X: 7.2, Y: 15.2, Angle: 56.31, Lines: new[] { true, false, true }),     // 离 ☲ 左下
        };

        var parts = new List<string>
        {
            """<rect width="30" height="20" fill="#FFFFFF"/>""",
            taegeuk,
        };

        foreach (var t in trigrams)
        {
            var bars = new List<string>();
            for (var i = 0; i < 3; i++)
            {
                var y = -1.25 + i * 1.25 - 0.42;
                bars.Add(t.Lines[i]
                    ? FormattableString.Invariant($"""<rect x="-2.5" y="{y:0.##}" width="5" height="0.84"/>""")
                    : FormattableString.Invariant($"""<rect x="-2.5" y="{y:0.##}" width="2.3" height="0.84"/><rect x="0.2" y="{y:0.##}" width="2.3" height="0.84"/>"""));
            }

            parts.Add(FormattableString.Invariant(
                $"""<g fill="#000000" transform="translate({t.X} {t.Y}) rotate({t.Angle})">{string.Concat(bars)}</g>"""));
        }

        return $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 30 20">{string.Concat(parts)}</svg>""";
    }
}
