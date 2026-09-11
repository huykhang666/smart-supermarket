using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Desktop;

public static class ThemeManager
{
    public static Color Primary => AppTheme.PrimaryGreen;
    public static Color PrimaryHover => AppTheme.PrimaryGreenDark;
    public static Color NavyBrand => Color.FromArgb(11, 37, 69);
    public static Color TealBrand => AppTheme.InfoBlue;
    
    public static Color Success => AppTheme.SuccessGreen;
    public static Color Warning => AppTheme.WarningAmber;
    public static Color Danger => AppTheme.DangerRed;
    
    public static Color Background => AppTheme.BackgroundGray;
    public static Color CardBg => AppTheme.SurfaceWhite;
    public static Color Border => AppTheme.BorderLight;
    
    public static Color SidebarBg => Color.FromArgb(11, 37, 69); // KATQ Navy Brand
    public static Color SidebarHover => AppTheme.PrimaryGreen;    // KATQ Sky Blue
    public static Color SidebarActive => AppTheme.InfoBlue;       // KATQ Cyan Accent
    
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
    public static void ApplyRoundedCardPanel(Panel panel, int radius = 12) => AppTheme.ApplyCardPanel(panel, radius);
    public static void ApplyPrimaryButton(Button btn) => AppTheme.ApplyPrimaryButton(btn);
    public static void ApplySecondaryButton(Button btn) => AppTheme.ApplyOutlineButton(btn);
    public static void ApplyDangerButton(Button btn) => AppTheme.ApplyOutlineButton(btn);
    public static Panel CreateKpiStatCard(string title, string value, string badgeText, Color badgeColor) => AppTheme.CreateKpiCard(title, value, badgeText, badgeColor);
    public static GraphicsPath GetRoundedPath(Rectangle rect, int radius) => AppTheme.GetRoundedPath(rect, radius);
}
