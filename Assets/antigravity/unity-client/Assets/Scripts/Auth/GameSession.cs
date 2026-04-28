namespace Antigravity.Auth
{
    public static class GameSession
    {
        public static string Token { get; set; }
        public static string Username { get; set; }
        public static string UserId { get; set; }
        public static string CurrentGameId { get; set; }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        public static void Logout()
        {
            Token = null;
            Username = null;
            UserId = null;
            CurrentGameId = null;
        }
    }
}
 