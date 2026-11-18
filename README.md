# LocalMarket – előrendelő rendszer helyi piacokhoz

Egyetemi projekt (Széchenyi István Egyetem, *Portálfejlesztés .NET-ben*). A rendszerben a vásárlók egy piacnap előtt
előrendelhetnek a helyi termelőktől, a termelők kezelik a kínálatukat és a beérkező rendeléseket, a piacvezető
(admin) pedig jóváhagyja az árusokat, eldönti a vitás ügyeket, és riportokat néz.

## Funkciók szerepkörönként

| Szerepkör | Mit tud |
|---|---|
| **Vásárló** | regisztráció, terméklista keresés/szűrés (kategória, allergén), kosár (több árusból), rendelés leadása árusonként egy átvételi időpontra, rendelések követése, lemondás, vitatás, rendelés exportja (JSON/CSV), időjárás-előrejelzés a piacnapra |
| **Termelő (árus)** | regisztráció (jóváhagyásra vár), termékek felvétele képpel, heti készlet és ár megadása, átvételi időpontok kezelése, beérkező rendelések elfogadása / részleges teljesítése / elutasítása, „kész” és „átvéve” jelölés, szedési lista export |
| **Piacvezető (admin)** | árusok jóváhagyása/elutasítása, piacok és kategóriák kezelése, vitás rendelések eldöntése, riportok (top termékek, lemondási arány, árusi bevétel, át nem vett rendelések), auditnapló |

## Architektúra

Clean Architecture, függőségek befelé mutatnak:

```
Domain  <-  Application  <-  Infrastructure  <-  API (ASP.NET Core)        Blazor Server kliens --HTTP/JWT--> API
```

| Projekt | Szerep |
|---|---|
| `localmarket-preordersystem.Domain` | entitások és üzleti szabályok (rendelés-állapotgép, készletfoglalás, idősáv-foglalás); nincs külső függőség |
| `localmarket-preordersystem.Application` | use case-ek kézzel írt `*Handler` osztályokban, validáció, DTO-k, interfészek |
| `localmarket-preordersystem.Infrastructure` | EF Core + PostgreSQL, migrációk, repository-k, JWT, jelszó-hash, képtárolás, Open-Meteo hívások |
| `localmarket-preordersystem.api` | REST API: controllerek, JWT-hitelesítés, jogosultsági policy-k, hibakezelés (ProblemDetails), rate limit |
| `localmarket-preordersystem-blazerClient` | Blazor Server felhasználói felület |
| `localmarket-preordersystem.Tests` | xUnit tesztek a Domain és az Application rétegre |

Fontosabb tervezési döntések:

* **Rendelés-állapotgép** a `Domain/Entity/Order.cs`-ben: `Pending → Accepted/PartiallyFulfilled/Rejected/Cancelled → Ready → PickedUp/NotPickedUp → Disputed`. Érvénytelen átmenet `DomainException` (HTTP 400). A vitatott rendelést kizárólag a piacvezető zárhatja le.
* **Foglalás**: leadáskor a rendelés egyszerre foglalja a készletet és az idősáv egy helyét (mindent vagy semmit). Lemondás/elutasítás felszabadítja.
* **Optimista konkurencia**: a heti készlet (`xmin`) és az idősáv (`BookedCount`) konkurens módosításánál HTTP 409 jön.
* **Idő**: az idősávok dátuma és időpontja a piac helyi idejében értendő (`App:TimeZone`, alapból `Europe/Budapest`), az adatbázisba UTC kerül. A konténer UTC-ben fut, ezért az alkalmazás egy zónás `TimeProvider`-t használ.
* **Hibakezelés**: egységes `ProblemDetails` válasz (400 validáció/üzleti szabály, 401, 403, 404, 409, 429, 500).

## Gyors indítás Dockerrel

Előfeltétel: Docker és Docker Compose.

```bash
cp .env.example .env        # töltsd ki legalább a POSTGRES_PASSWORD, JWT_SECRET, PGADMIN_PASSWORD értékeket
docker compose up --build
```

| Szolgáltatás | Cím |
|---|---|
| Webes felület (Blazor) | http://localhost:5219 |
| API | http://localhost:5205 (állapot: `/health`, OpenAPI: `/openapi/v1.json`) |
| pgAdmin | http://localhost:5050 |

Az API induláskor lefuttatja a migrációkat (`MIGRATE_ON_STARTUP=true`), és létrehozza a kezdő adatokat: egy piac, alap kategóriák, egy admin és (fejlesztői módban) egy teszt vásárló.

| Felhasználó | E-mail | Jelszó (alapértelmezett) |
|---|---|---|
| Admin | `admin@localmarket.test` | `ADMIN_PASSWORD` (alapból `Admin123!`) |
| Vásárló | `vevo@localmarket.test` | `CUSTOMER_PASSWORD` (alapból `Customer123!`) |

A termelőt a regisztrációs oldalon lehet felvenni, majd az admin jóváhagyja az „Árusok” oldalon.
Az alapértelmezett jelszavak csak bemutatóra valók, éles környezetben állítsd át őket.

## Fejlesztés Docker nélkül

Szükséges: .NET 10 SDK és egy PostgreSQL (pl. `docker compose up db`).

```bash
# API (alapból http://localhost:5205)
cd localmarket-preordersystem.api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=localmarket;Username=localmarket;Password=<jelszo>"
dotnet user-secrets set "Jwt:Secret" "<legalabb-32-karakteres-kulcs>"
dotnet run

# Kliens (másik terminálban; az API címe: Api:BaseUrl az appsettings.json-ban)
cd localmarket-preordersystem-blazerClient
dotnet run
```

Új migráció (a modell módosítása után):

```bash
dotnet ef migrations add <Nev> -p localmarket-preordersystem.Infrastructure -s localmarket-preordersystem.api -o Persistence/Migrations
```

## Konfiguráció

| Kulcs (környezeti változóként `__`-val) | Jelentés | Alapérték |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL kapcsolat | – (kötelező) |
| `Jwt:Secret` | JWT aláíró kulcs, min. 32 karakter | – (kötelező) |
| `Jwt:ExpiryMinutes` | token érvényessége | 60 |
| `Database:MigrateOnStartup` | migráció + kezdő adatok induláskor | `true` fejlesztésben |
| `Cors:Origins` | engedélyezett kliens-eredetek | `http://localhost:5219` |
| `App:TimeZone` | a piac helyi időzónája | `Europe/Budapest` |
| `ImageStorage:RootPath` | feltöltött képek mappája | lásd `appsettings` |
| `Seed:AdminPassword` | az admin kezdő jelszava (nem fejlesztői módban kötelező) | `Admin123!` fejlesztésben |
| `RateLimit:GeneralPerMinute` / `AuthPerMinute` | kérések/perc IP-nként / bejelentkezés-regisztráció | 1200 / 60 |
| `Api:BaseUrl` (kliens) | az API címe a kliens szerverének | – (kötelező) |
| `Api:PublicBaseUrl` (kliens) | az API böngészőből elérhető címe (képekhez) | `Api:BaseUrl` |

## API röviden

Minden végpont `/api` alatt van; a védettek `Authorization: Bearer <token>` fejlécet kérnek. A lapozott listák (`page`, `pageSize` ≤ 100) `{ items, total, page, pageSize }` alakot adnak.

| Terület | Végpontok |
|---|---|
| Auth | `POST /auth/login`, `/auth/register-customer`, `/auth/register-producer` |
| Katalógus | `GET /products` (lapozott), `/products/{id}`, `/categories`, `/markets`, `/producers`, `/pickup-slots`, `/weather/markets/{id}` |
| Termelő | `POST/PUT/DELETE /products`, `PUT /products/{id}/stock`, `PUT/DELETE /products/{id}/image`, `POST/PUT/DELETE /pickup-slots`, `PUT /producers/me` |
| Kosár | `GET /cart`, `POST /cart/items`, `PUT/DELETE /cart/items/{productId}` |
| Rendelések | `POST /orders`, `GET /orders/my`, `/orders/producer`, `/orders/disputed`, `/orders/{id}`, `POST /orders/{id}/{cancel,accept,accept-partially,reject,ready,picked-up,not-picked-up,dispute,resolve-dispute}` |
| Export | `GET /export/orders/{id}?format=Json|Csv`, `/export/picking-list?date=…&format=…` |
| Admin | `/reports/*`, `GET /audit`, `POST /producers/{id}/{approve,reject}`, `/markets`, `/categories` |

## Tesztek és CI

```bash
dotnet test localmarket-preordersystem.Tests
```

A tesztek a rendelés-állapotgépet (a vitás rendelés védelmét is), a készlet- és idősáv-foglalást, a validációt és az időzóna-kezelést fedik le. A GitHub Actions workflow (`.github/workflows/ci.yml`) minden push-nál és pull requestnél felépíti az API-t és a klienst, majd lefuttatja a teszteket.

## Ismert korlátok

* A Blazor Server kliens szerver oldalról hívja az API-t, ezért az API számára minden kérés ugyanarról az IP-ről érkezik: az IP-alapú rate limit gyakorlatilag globális, ezért a limitek bőkezűek és konfigurálhatók.
* Ha egy elfogadott vagy kész rendelés vitatottá válik, majd a piacvezető a lemondás mellett dönt, a rendelés már nem tart foglalást, ezért a készlet és az idősáv-hely nem szabadul fel automatikusan. A `NotPickedUp` állapotba kerülő rendelés foglalása sem szabadul fel magától. Ezek a szabályok a jelenlegi állapotgép továbbfejlesztési pontjai.
* A termékképek helyi lemezen tárolódnak (Dockerben volume-on); felhős tárhely nincs bekötve.
* Fizetés csak nyilvántartás (készpénz/kártya/átutalás jelölése), tényleges fizetési szolgáltató nincs integrálva.
* Az Infrastructure/API réteg a fejlesztői környezetben csak statikusan ellenőrizhető volt (NuGet nélkül); a `dotnet test` és a `docker compose up --build` az első futtatásnál visszaigazolja.

## Hibák és javítások

A kódáttekintés során feltárt hibák és javításaik: [`docs/REVIEW.md`](docs/REVIEW.md).
