using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Desktop;

public static class ThemeManager
{
    public static Color Primary => AppTheme.Primary;
    public static Color PrimaryHover => AppTheme.PrimaryHover;
    public static Color NavyBrand => AppTheme.SidebarBg;
    public static Color TealBrand => AppTheme.Primary;
    
    public static Color Success => AppTheme.Success;
    public static Color Warning => AppTheme.Warning;
    public static Color Danger => AppTheme.Danger;
    
    public static Color Background => AppTheme.BackgroundGray;
    public static Color CardBg => AppTheme.SurfaceWhite;
    public static Color Border => AppTheme.BorderLight;
    
    public static Color SidebarBg => AppTheme.SidebarBg;         // #1B1F23 Dark Charcoal Black
    public static Color SidebarHover => AppTheme.SidebarHover;     // #2D3338
    public static Color SidebarActive => AppTheme.SidebarActive;   // #0F6CBD
    
    public static Color TextPrimary => AppTheme.TextPrimary;
    public static Color TextSecondary => AppTheme.TextSecondary;

    public static Font TitleFont => AppTheme.FontTitle;
    public static Font HeaderFont => AppTheme.FontH1;
    public static Font SubtitleFont => AppTheme.FontH2;
    public static Font BodyFont => AppTheme.FontBody;
    public static Font BodyBold => AppTheme.FontBodyBold;
    public static Font SmallFont => AppTheme.FontCaption;

    public static void ApplyGridStyle(DataGridView dgv) => AppTheme.ApplyGridStyle(dgv);
    public static void ApplyCardPanel(Panel panel) => AppTheme.ApplyCardPanel(panel);
    public static void ApplyRoundedCardPanel(Panel panel, int radius = 8) => AppTheme.ApplyCardPanel(panel, radius);
    public static void ApplyPrimaryButton(Button btn) => AppTheme.ApplyPrimaryButton(btn);
    public static void ApplySecondaryButton(Button btn) => AppTheme.ApplySecondaryButton(btn);
    public static void ApplyDangerButton(Button btn) => AppTheme.ApplyDangerButton(btn);
    public static Panel CreateKpiStatCard(string title, string value, string badgeText, Color badgeColor) => AppTheme.CreateKpiCard(title, value, badgeText, badgeColor);
    public static GraphicsPath GetRoundedPath(Rectangle rect, int radius) => AppTheme.GetRoundedPath(rect, radius);
}
