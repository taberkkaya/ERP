using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ERPServer.Application.Features.Auth.Login;
using ERPServer.Application.Services;
using ERPServer.Domain.Entities;
using ERPServer.Infrastructure.Demo;
using ERPServer.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ERPServer.Infrastructure.Services
{
    internal class JwtProvider(
        UserManager<AppUser> userManager,
        IOptions<JwtOptions> jwtOptions) : IJwtProvider
    {
        public async Task<LoginCommandResponse> CreateToken(AppUser user)
        {
            List<Claim> claims =
            [
                new Claim("Id", user.Id.ToString()),
                new Claim("Name", user.FullName),
                new Claim("Email", user.Email ?? ""),
                new Claim("UserName", user.UserName ?? ""),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            ];

            DateTime expires = DateTime.UtcNow.AddDays(7);

            string token = Write(claims, expires);

            string refreshToken = Guid.NewGuid().ToString();
            DateTime refreshTokenExpires = expires.AddHours(1);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpires = refreshTokenExpires;

            await userManager.UpdateAsync(user);

            return new(token, refreshToken, refreshTokenExpires);
        }

        public string CreateDemoToken(Guid sessionId, string databaseName, string displayName, TimeSpan lifetime)
        {
            List<Claim> claims =
            [
                new Claim("Id", sessionId.ToString()),
                new Claim("Name", displayName),
                new Claim("Email", ""),
                new Claim("UserName", "demo"),

                // Demo ziyaretçisi yönetici değil: kullanıcı yönetimi ekranı
                // menüde de görünmüyor, uçları da politikayla kapalı.
                new Claim("IsAdmin", bool.FalseString),
                new Claim(DemoClaimTypes.SessionId, sessionId.ToString()),

                // İsteğin hangi sandbox'a bağlanacağı jetonda taşınıyor; imzalı
                // olduğu için ziyaretçi başka bir alanın verisine geçemiyor.
                new Claim(DemoClaimTypes.Database, databaseName)
            ];

            return Write(claims, DateTime.UtcNow.Add(lifetime));
        }

        private string Write(List<Claim> claims, DateTime expires)
        {
            SymmetricSecurityKey securityKey =
                new(Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey));

            JwtSecurityToken jwtSecurityToken = new(
                issuer: jwtOptions.Value.Issuer,
                audience: jwtOptions.Value.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512));

            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }
    }
}
