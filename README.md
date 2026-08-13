# ÜvegVissza — .NET

Az ÜvegVissza alkalmazás .NET-es változata. A tervet, a döntések indoklásával
együtt, a [MAUI_TERV.md](MAUI_TERV.md) tartalmazza.

```
src/Vissza.Api      ASP.NET Core Minimal API + EF Core
src/Vissza.Shared   DTO-k és enumok, a szerver és a kliens közösen használja
src/Vissza.Maui     MAUI alkalmazás (egyelőre csak iOS)
```

## Első lépés: a workload set telepítése

A repo `global.json`-ja a `10.0.300.1` workload sethez van rögzítve, mert az
hozza azt a .NET iOS SDK-t, ami a gépen lévő Xcode 26.4-gyel párban van.
Amíg ez nincs telepítve, **minden build elhasal** `MSB4242` hibával — az
`src/Vissza.Api` is, nem csak a MAUI rész.

```bash
cd /Users/fustimolnarpatrick/vissza_maui && sudo dotnet workload restore
```

Rendszerszintű jogot kér, mert a .NET a `/usr/local/share/dotnet` alatt van.

## Az API futtatása

Az adatbázis-jelszó és a JWT titok **nincs a repóban**, és nem is kerül bele.
Első futtatás előtt állítsd be őket user secretsben:

```bash
cd src/Vissza.Api && dotnet user-secrets init
```

```bash
dotnet user-secrets set "ConnectionStrings:Vissza" "Server=<host>;Port=3306;Database=<db>;User Id=<user>;Password=<jelszó>;"
```

```bash
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
```

A meglévő értékek a régi projekt `backend/.env` fájljában vannak. A `Jwt:Secret`
legyen **ugyanaz**, mint ott, ha azt szeretnéd, hogy az átállás alatt a két API
ugyanazokat a tokeneket fogadja el.

Indítás:

```bash
dotnet run --project src/Vissza.Api
```

Ellenőrzés:

```bash
curl http://localhost:5000/api/health
```

### Ha nincs adatbázis-kapcsolat induláskor

Az API alapból lekérdezi a MySQL verzióját, hogy a generált SQL a valódi
szerverhez igazodjon. Offline fejlesztéshez ez kihagyható:

```bash
dotnet user-secrets set "Database:ServerVersion" "10.11.0-mariadb"
```

## Az adatbázisról

A séma a régi projekt `schema.sql`-je, és **nem változik**. Az EF modell ehhez
igazodik, migrációt szándékosan nem generálunk: amíg a régi Express backend is
ugyanezt az adatbázist használja, egy EF migráció alóla húzná ki a talajt.
