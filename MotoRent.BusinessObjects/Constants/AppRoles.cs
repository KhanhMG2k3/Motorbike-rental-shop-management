namespace MotoRent.BusinessObjects.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    public const string AdminOrStaff = Admin + "," + Staff;
}
