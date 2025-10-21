using System.Collections.Concurrent;
using System.Linq;

namespace ContosoUniversity.Helpers
{
    public static class LoginManager
    {
        private static readonly ConcurrentDictionary<int, string> _activeSessions = new ConcurrentDictionary<int, string>();

        public static bool TryAddSession(int userId, string sessionId)
        {
            return _activeSessions.TryAdd(userId, sessionId);
        }

        public static bool RemoveSession(int userId, string sessionId)
        {
            if (_activeSessions.TryGetValue(userId, out string currentSession) && currentSession == sessionId)
            {
                return _activeSessions.TryRemove(userId, out _);
            }
            return false;
        }

        public static bool IsUserLoggedIn(int userId)
        {
            return _activeSessions.ContainsKey(userId);
        }

        public static bool IsUserSessionValid(int userId, string sessionId)
        {
            return _activeSessions.TryGetValue(userId, out string currentSession) && currentSession == sessionId;
        }

        public static void ForceLogout(int userId)
        {
            _activeSessions.TryRemove(userId, out _);
        }

        public static string GetUserSession(int userId)
        {
            _activeSessions.TryGetValue(userId, out string sessionId);
            return sessionId;
        }

        public static int GetActiveUserCount()
        {
            return _activeSessions.Count;
        }

        public static void ClearAllSessions()
        {
            _activeSessions.Clear();
        }
    }
}