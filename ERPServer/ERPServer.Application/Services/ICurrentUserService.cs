namespace ERPServer.Application.Services;

/// <summary>
/// İsteği yapan kullanıcının kimliği. Kullanıcı yönetiminde bir kişinin kendi
/// hesabını silmesini engellemek için gerekiyor; silme başarılı olsaydı oturum
/// anında geçersizleşir ve kişi dışarıda kalırdı.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
}
