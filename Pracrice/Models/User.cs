namespace Pracrice.Models;

public enum UserRole
{
    Administrator = 0,
    Director = 1,
    Manager = 2
}

public partial class User
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public UserRole Role { get; set; }

    public string FullName { get; set; } = null!;
}
