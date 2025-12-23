
public static class AuthSession
{
    public static string Token { get; private set; }
    public static string UserName { get; private set; }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public static void Set(string token, string userName)
    {
        Token = token;
        UserName = userName;
    }

    public static void Clear()
    {
        Token = null;
        UserName = null;
    }
}
