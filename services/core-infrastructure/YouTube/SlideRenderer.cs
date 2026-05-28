namespace DevPulse.Infrastructure.YouTube;

internal static class SlideRenderer
{
    internal static string Render(SlideContent slide) => slide.Kind switch
    {
        SlideKind.Title    => TitleHtml(slide.Title, slide.Subtitle),
        SlideKind.Hook     => HookHtml(slide.Subtitle),
        SlideKind.Code     => CodeHtml(slide.Title, slide.Code, slide.SlideIndex, slide.TotalSlides),
        SlideKind.Takeaway => TakeawayHtml(slide.Subtitle),
        _                  => TitleHtml(slide.Title, slide.Subtitle),
    };

    private static string TitleHtml(string title, string? subtitle) =>
        Page("""
            .box{display:flex;flex-direction:column;align-items:center;justify-content:center;height:100%;padding:80px 60px;gap:40px;}
            .brand{color:#7c6af7;font-size:28px;font-weight:700;letter-spacing:6px;text-transform:uppercase;}
            .accent{width:80px;height:4px;background:#7c6af7;border-radius:2px;}
            .title{color:#e6edf3;font-size:64px;font-weight:800;text-align:center;line-height:1.2;}
            .sub{color:#8b949e;font-size:34px;text-align:center;line-height:1.4;}
            """,
            $$"""
            <div class="box">
              <div class="brand">DevPulse</div>
              <div class="accent"></div>
              <div class="title">{{E(title)}}</div>
              {{Wrap("sub", subtitle)}}
            </div>
            """);

    private static string HookHtml(string? anchor) =>
        Page("""
            .box{display:flex;flex-direction:column;align-items:center;justify-content:center;height:100%;padding:80px 60px;gap:48px;}
            .brand{color:#7c6af7;font-size:24px;font-weight:700;letter-spacing:6px;text-transform:uppercase;}
            .header{color:#7c6af7;font-size:40px;font-weight:700;text-align:center;}
            .divider{width:100%;height:1px;background:#30363d;}
            .anchor{color:#e6edf3;font-size:46px;font-weight:700;text-align:center;line-height:1.3;}
            """,
            $$"""
            <div class="box">
              <div class="brand">DevPulse</div>
              <div class="header">&#128161; You already know this</div>
              <div class="divider"></div>
              {{Wrap("anchor", anchor)}}
            </div>
            """);

    private static string CodeHtml(string title, string? code, int idx, int total) =>
        Page("""
            .box{display:flex;flex-direction:column;height:100%;padding:56px 48px;gap:28px;}
            .brand{color:#7c6af7;font-size:22px;font-weight:700;letter-spacing:6px;text-transform:uppercase;}
            .concept{color:#8b949e;font-size:28px;font-weight:600;}
            .code-block{background:#161b22;border:1px solid #30363d;border-radius:12px;padding:40px;flex:1;overflow:hidden;display:flex;}
            .nums{color:#484f58;font-family:'Cascadia Code','Fira Code',Consolas,monospace;font-size:26px;line-height:1.7;padding-right:24px;border-right:1px solid #30363d;margin-right:24px;text-align:right;white-space:pre;}
            pre{color:#e6edf3;font-family:'Cascadia Code','Fira Code',Consolas,monospace;font-size:26px;line-height:1.7;white-space:pre-wrap;word-break:break-word;margin:0;flex:1;}
            .dots{color:#484f58;font-size:36px;align-self:center;letter-spacing:8px;}
            """,
            $$"""
            <div class="box">
              <div class="brand">DevPulse</div>
              <div class="concept">{{E(title)}}</div>
              <div class="code-block">
                <div class="nums">{{LineNums(code)}}</div>
                <pre>{{E(code ?? "")}}</pre>
              </div>
              <div class="dots">{{Dots(idx, total)}}</div>
            </div>
            """);

    private static string TakeawayHtml(string? foreshadow) =>
        Page("""
            .box{display:flex;flex-direction:column;align-items:center;justify-content:center;height:100%;padding:80px 60px;gap:40px;}
            .brand{color:#7c6af7;font-size:28px;font-weight:700;letter-spacing:6px;text-transform:uppercase;}
            .check{font-size:80px;}
            .unlocked{color:#3fb950;font-size:36px;font-weight:700;}
            .divider{width:100%;height:1px;background:#30363d;}
            .label{color:#8b949e;font-size:28px;text-transform:uppercase;letter-spacing:4px;}
            .tomorrow{color:#e6edf3;font-size:42px;font-weight:700;text-align:center;line-height:1.3;}
            .cta{color:#7c6af7;font-size:28px;font-weight:600;}
            """,
            $$"""
            <div class="box">
              <div class="check">&#9989;</div>
              <div class="unlocked">Concept unlocked</div>
              <div class="divider"></div>
              <div class="label">Coming tomorrow</div>
              {{Wrap("tomorrow", foreshadow)}}
              <div class="divider"></div>
              <div class="cta">Follow for daily concepts</div>
              <div class="brand">DevPulse</div>
            </div>
            """);

    private static string Page(string css, string body) =>
        $$"""
        <!DOCTYPE html><html><head><meta charset="utf-8"><style>
          *{margin:0;padding:0;box-sizing:border-box;}
          body{width:1080px;height:1920px;background:#0d1117;font-family:'Segoe UI',system-ui,sans-serif;}
          {{css}}
        </style></head><body>{{body}}</body></html>
        """;

    private static string E(string s) => System.Web.HttpUtility.HtmlEncode(s);

    private static string Wrap(string cls, string? content) =>
        content is null ? "" : $"<div class=\"{cls}\">{E(content)}</div>";

    private static string LineNums(string? code)
    {
        if (string.IsNullOrEmpty(code)) return "";
        var count = code.Split('\n').Length;
        return string.Join('\n', Enumerable.Range(1, count).Select(n => n.ToString("D2")));
    }

    private static string Dots(int current, int total) =>
        string.Concat(Enumerable.Range(0, total).Select(i => i == current ? "●" : "○"));
}
