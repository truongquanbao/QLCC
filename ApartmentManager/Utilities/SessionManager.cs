using System;

namespace ApartmentManager.Utilities;

/// <summary>
/// Manages user session information
/// </summary>
public static class SessionManager
{
    private static UserSession? _currentSession;

    /// <summary>
    /// Set the current user session
    /// </summary>
    public static void SetSession(UserSession session)
    {
        _currentSession = session;
        _currentSession.LoginTime = DateTime.Now;
    }

    /// <summary>
    /// Get the current user session
    /// </summary>
    public static UserSession? GetSession()
    {
        return _currentSession;
    }

    /// <summary>
    /// Check if user is logged in
    /// </summary>
    public static bool IsLoggedIn()
    {
        return _currentSession != null;
    }

    /// <summary>
    /// Clear the current session (logout)
    /// </summary>
    public static void ClearSession()
    {
        _currentSession = null;
    }

    /// <summary>
    /// Get current user ID from session
    /// </summary>
    public static int? GetCurrentUserID()
    {
        return _currentSession?.UserID;
    }

    /// <summary>
    /// Check if user has permission
    /// </summary>
    public static bool HasPermission(string permissionName)
    {
        return _currentSession?.HasPermission(permissionName) ?? false;
    }

    /// <summary>
    /// Update last activity time
    /// </summary>
    public static void UpdateActivity()
    {
        if (_currentSession != null)
        {
            _currentSession.LastActivityTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Get session timeout status
    /// </summary>
    public static bool IsSessionExpired(int timeoutMinutes = 30)
    {
        if (_currentSession == null)
            return true;

        if (_currentSession.LastActivityTime == null)
            return false;

        var timeSinceLastActivity = DateTime.Now - _currentSession.LastActivityTime.Value;
        return timeSinceLastActivity.TotalMinutes > timeoutMinutes;
    }
}
