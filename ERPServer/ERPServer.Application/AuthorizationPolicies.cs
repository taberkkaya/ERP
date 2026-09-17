namespace ERPServer.Application;

public static class AuthorizationPolicies
{
    /// <summary>
    /// Kullanıcı yönetimi sıradan kullanım değil yönetim işi; giriş yapmış olmak
    /// yetmiyor, jetonun taşıdığı yönetici işareti aranıyor.
    /// </summary>
    public const string Admin = "Admin";
}
