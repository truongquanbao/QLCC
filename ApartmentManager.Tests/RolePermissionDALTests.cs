using System.Linq;
using ApartmentManager.DAL;
using Xunit;

namespace ApartmentManager.Tests;

public class RolePermissionDALTests
{
    [Fact]
    public void UpdateRolePermissions_RemovingResidentPermission_PersistsAndCanBeRestored()
    {
        var residentRole = RolePermissionDAL.GetAllRoles()
            .FirstOrDefault(role => string.Equals(role.RoleName, "Resident", System.StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(residentRole);
        Assert.NotEmpty(residentRole!.PermissionIDs);

        var originalPermissionIds = residentRole.PermissionIDs
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        int removedPermissionId = originalPermissionIds[0];
        var reducedPermissionIds = originalPermissionIds
            .Where(id => id != removedPermissionId)
            .ToList();

        try
        {
            var updateSucceeded = RolePermissionDAL.UpdateRolePermissions(residentRole.RoleID, reducedPermissionIds);
            Assert.True(updateSucceeded, "Saving updated resident permissions should succeed.");

            var reloadedRole = RolePermissionDAL.GetRoleByID(residentRole.RoleID);
            Assert.NotNull(reloadedRole);
            Assert.DoesNotContain(removedPermissionId, reloadedRole!.PermissionIDs);
            Assert.Equal(reducedPermissionIds.OrderBy(id => id), reloadedRole.PermissionIDs.OrderBy(id => id));
        }
        finally
        {
            var restoreSucceeded = RolePermissionDAL.UpdateRolePermissions(residentRole.RoleID, originalPermissionIds);
            Assert.True(restoreSucceeded, "Restoring original resident permissions should succeed.");

            var restoredRole = RolePermissionDAL.GetRoleByID(residentRole.RoleID);
            Assert.NotNull(restoredRole);
            Assert.Equal(originalPermissionIds, restoredRole!.PermissionIDs.OrderBy(id => id).ToList());
        }
    }
}
