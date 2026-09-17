using Microsoft.AspNetCore.Identity;

namespace ERPServer.Domain.Entities
{
    public sealed class AppUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => string.Join(" ", FirstName, LastName);

        /// <summary>Kullanıcı yönetimi ekranlarına yalnızca yöneticiler erişebilir.</summary>
        public bool IsAdmin { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpires { get; set; }
    }
}
