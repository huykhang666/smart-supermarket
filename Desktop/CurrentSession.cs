namespace Desktop;

public static class CurrentSession
{
    public static int UserId { get; set; } = 1;
    public static string Username { get; set; } = "admin";
    public static string FullName { get; set; } = "Quản trị viên";
    public static string Role { get; set; } = "Admin";
    public static string? Token { get; set; }
}
