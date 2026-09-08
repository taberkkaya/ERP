namespace ERPServer.Domain.Demo;

/// <summary>"Demo" yapılandırma bölümünden bağlanır.</summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>Kapalıyken demo uçları hiç eşlenmez ve sandbox hazırlanmaz.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Hazır bekletilen, önceden migrate edilmiş sandbox veritabanı sayısı. Aynı
    /// zamanda eşzamanlı ziyaretçi tavanı: bir ziyaretçi tam olarak bir slot tutar.
    /// </summary>
    public int SlotCount { get; set; } = 3;

    public string DatabaseNamePrefix { get; set; } = "Tezgah_Demo_";

    /// <summary>
    /// Sandbox veritabanlarının açılacağı SQL Server örneği. Boş bırakılırsa ana
    /// bağlantı dizesindeki sunucu ve kimlik bilgileri kullanılır; tek sunuculu bir
    /// kurulumun istediği de budur.
    /// </summary>
    public string? DatabaseServer { get; set; }

    public string? DatabaseUsername { get; set; }

    public string? DatabasePassword { get; set; }

    /// <summary>Bir oturumun harcayabileceği yazma işlemi (create/update/delete) sayısı.</summary>
    public int WriteLimit { get; set; } = 40;

    /// <summary>Bu sayıdan sonra ziyaretçi iletişim sayfasına davet edilir; oturum kapanmaz.</summary>
    public int NudgeAfterWrites { get; set; } = 18;

    /// <summary>Ziyaretçi bu kadar süre hiçbir şey yapmazsa sandbox geri alınır.</summary>
    public int IdleTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Oturumun toplam ömrü. Havuzda az sayıda sandbox olduğu için kısa tutuluyor:
    /// uzun bir oturum, sırada bekleyen ziyaretçiyi dışarıda bırakıyor.
    /// </summary>
    public int AbsoluteTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Çalışma kümesi tavanı. Aşıldığında en uzun süredir dokunulmayan oturumlar
    /// serbest bırakılır; gözetimsiz bir demoyu sınırlı tutan da budur.
    /// </summary>
    public int MemoryThresholdMegabytes { get; set; } = 1024;

    public int JanitorIntervalSeconds { get; set; } = 30;

    public const string DefaultContactUrl = "https://ataberkkaya.com";

    /// <summary>Oturumu biten ziyaretçinin yönlendirileceği adres.</summary>
    public string ContactUrl { get; set; } = DefaultContactUrl;

    /// <summary>Demo şeridinde ve jetonda görünen çalışma alanı adı.</summary>
    public string WorkspaceName { get; set; } = "Demo Üretim A.Ş.";

    public string UserName { get; set; } = "demo";

    public string Email { get; set; } = "demo@tezgah.local";
}
