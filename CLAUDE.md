# POS-Desktop — CLAUDE.md

## Loyiha haqida

WinForms asosidagi savdo nuqtasi (Point of Sale) desktop ilovasi. Clean Architecture pattern, .NET 8.

## Texnologiyalar

| Soha | Texnologiya |
|------|------------|
| Platform | .NET 8, WinForms |
| ORM | EF Core 8, SQL Server |
| UI library | Guna.UI2.WinForms |
| DI | Microsoft.Extensions.DependencyInjection 8 |
| Logging | Serilog (File + Console) |
| Validation | FluentValidation 11 |
| Mapping | AutoMapper 12 |
| Security | BCrypt.Net-Next 4 |
| Testing | NUnit 4, Moq, FluentAssertions |

## Loyiha tuzilishi

```
POS.sln
├── Desktop/              — WinForms UI (startup project)
│   ├── Admin/            — Admin panel formlari
│   ├── Auth/             — Login formi
│   ├── Seller/           — Sotuvchi panel
│   └── Program.cs        — DI, Serilog, config
├── src/
│   ├── Domain/           — Entitetlar, DbContext, Repository
│   ├── Application/      — Servicelar, DTOlar, Validatorlar
│   └── Infrastructure/   — BCrypt, Serilog configurer
└── tests/
    ├── Application.UnitTests/
    └── Domain.UnitTests/
```

## Muhim qoidalar

### Build
```bash
dotnet restore
dotnet build
dotnet test
```

### Migration yaratish
```bash
dotnet ef migrations add <MigrationName> --project src/Domain --startup-project src/Domain
```

### Konfiguratsiya

`Desktop/appsettings.json` — connection string shu yerda:
```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Database=PosDB;..."
  }
}
```

### Default login

| Maydon | Qiymat |
|--------|--------|
| Telefon | 998901234567 |
| Parol | Admin.123$ |
| Rol | SuperAdmin |

**MUHIM:** Birinchi kirishda parolni o'zgartiring!

## Arxitektura qarorlari

- **DbContext Scoped** — root scope dan resolve qilinadi (WinForms uchun optimal)
- **EF Core Migrations** — `Database.EnsureCreated()` emas, `Database.Migrate()` ishlatiladi
- **BCrypt work factor 11** — xavfsizlik va tezlik balansi
- **Serilog file sink** — `logs/pos-YYYYMMDD.log` formatida, 30 kun saqlanadi
- **FluentValidation** — `DependencyInjection.cs` da `AddValidatorsFromAssemblyContaining<>` orqali register
- **AutoMapper Profile** — `Application/Mappings/MappingProfile.cs`

## Foydalanuvchi rollari

| Rol | Imkoniyatlar |
|-----|-------------|
| SuperAdmin | Hamma narsani boshqaradi |
| Admin | Mahsulot, kategoriya, ombor |
| Seller | Faqat sotuv (SellerDashboard) |

## Loglar

`Desktop/logs/pos-YYYYMMDD.log` — har kuni yangi fayl yaratiladi.

## Testlar

37 ta unit test (34 Application + 3 Domain). Ishlatish:
```bash
dotnet test
```

## CI/CD

`.github/workflows/build-and-test.yml` — push va PR da avtomatik:
1. Build (Release)
2. Test
3. Publish artifact (faqat master branch)
