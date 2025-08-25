# NoteFlix API - Backend

Bu proje, NoteFlix not alma uygulamasının backend API'sidir. .NET 9 Web API ve SQL Server kullanılarak geliştirilmiştir.

## 🚀 Özellikler

- **JWT Authentication**: Güvenli token tabanlı kimlik doğrulama
- **User Management**: Kullanıcı kaydı, girişi ve profil yönetimi
- **Premium Status**: Premium üyelik kontrolü
- **Refresh Token**: Otomatik token yenileme
- **SQL Server**: Entity Framework Core ile veritabanı yönetimi

## 📋 Gereksinimler

- .NET 9 SDK
- SQL Server LocalDB (veya SQL Server)
- Visual Studio 2022 veya VS Code

## 🛠️ Kurulum

### 1. Projeyi Klonlayın
```bash
git clone <repository-url>
cd NoteFlixApi/NoteFlixApi
```

### 2. Bağımlılıkları Yükleyin
```bash
dotnet restore
```

### 3. Veritabanını Oluşturun
```bash
dotnet ef database update
```

### 4. Uygulamayı Çalıştırın
```bash
dotnet run
```

API varsayılan olarak `https://localhost:7090` adresinde çalışacaktır.

## 🔧 Konfigürasyon

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NoteFlixDB;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-here-make-it-long-and-random-at-least-32-characters",
    "Issuer": "https://www.noteflix.co",
    "Audience": "https://www.noteflix.co",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

## 📚 API Endpoints

### Authentication
- `POST /api/auth/register` - Kullanıcı kaydı
- `POST /api/auth/login` - Kullanıcı girişi
- `POST /api/auth/refresh-token` - Token yenileme
- `POST /api/auth/logout` - Çıkış yapma

### User
- `GET /api/user/premium-status` - Premium durum kontrolü (Auth gerekli)
- `GET /api/user/profile` - Kullanıcı profili (Auth gerekli)

## 🔐 Güvenlik

- JWT token'ları 60 dakika geçerlidir
- Refresh token'ları 7 gün geçerlidir
- Şifreler SHA256 ile hash'lenir
- CORS tüm origin'lere açıktır (development için)

## 🗄️ Veritabanı

### Tablolar
- **Users**: Kullanıcı bilgileri
- **RefreshTokens**: Refresh token'ları

### Migration'lar
```bash
# Yeni migration oluştur
dotnet ef migrations add MigrationName

# Veritabanını güncelle
dotnet ef database update
```

## 🧪 Test

### Swagger UI
API dokümantasyonu için: `https://localhost:7090/swagger`

### Postman Collection
API testleri için Postman collection'ı hazırlanacak.

## 📁 Proje Yapısı

```
NoteFlixApi/
├── Controllers/          # API Controller'ları
│   ├── AuthController.cs
│   └── UserController.cs
├── Data/                # Entity Framework
│   └── NoteFlixDbContext.cs
├── DTOs/                # Data Transfer Objects
│   └── AuthDTOs.cs
├── Models/              # Entity Models
│   ├── User.cs
│   └── RefreshToken.cs
├── Services/            # Business Logic
│   └── JwtService.cs
├── Program.cs           # Application Entry Point
└── appsettings.json     # Configuration
```

## 🔄 Geliştirme

### Yeni Endpoint Ekleme
1. Controller oluşturun
2. DTO'ları tanımlayın
3. Swagger dokümantasyonunu güncelleyin

### Yeni Model Ekleme
1. Model sınıfını oluşturun
2. DbContext'e ekleyin
3. Migration oluşturun
4. Veritabanını güncelleyin

## 🚀 Deployment

### Production
1. Connection string'i güncelleyin
2. JWT secret'ı environment variable olarak ayarlayın
3. CORS policy'yi sınırlayın
4. HTTPS kullanın

### Docker (Gelecek)
```bash
docker build -t noteflix-api .
docker run -p 7090:7090 noteflix-api
```

## 📞 Destek

Herhangi bir sorun için issue açabilir veya iletişime geçebilirsiniz.

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.
