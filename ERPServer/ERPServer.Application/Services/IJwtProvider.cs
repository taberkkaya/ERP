using ERPServer.Application.Features.Auth.Login;
using ERPServer.Domain.Entities;

namespace ERPServer.Application.Services
{
    public interface IJwtProvider
    {
        Task<LoginCommandResponse> CreateToken(AppUser user);

        /// <summary>
        /// Demo ziyaretçisi için jeton üretir. Sandbox'ta gerçek bir kullanıcı kaydı
        /// olmadığı için Identity'ye hiç uğramıyor; jeton oturumu ve bağlanılacak
        /// veritabanını taşıyor ve oturum süresi kadar yaşıyor.
        /// </summary>
        string CreateDemoToken(Guid sessionId, string databaseName, string displayName, TimeSpan lifetime);
    }
}
