using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Xunit;

namespace ApartmentManager.Tests;

public class LoginSmokeTests
{
    [Fact]
    public void SuperAdmin_Login_Should_Succeed()
    {
        var user = UserDAL.GetUserByUsername("superadmin");
        Assert.NotNull(user);
        Assert.False(string.IsNullOrWhiteSpace(user!.PasswordHash), "Missing password hash");
        Assert.True(
            PasswordHasher.VerifyPassword("Admin@123456", user.PasswordHash!),
            $"Stored hash does not verify: {user.PasswordHash}");

        var (success, message, session) = AuthenticationBLL.Login("superadmin", "Admin@123456");

        Assert.True(success, message);
        Assert.NotNull(session);
        Assert.Equal("superadmin", session!.Username);
    }
}
