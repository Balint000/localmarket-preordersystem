# LocalMarket – előrendelő rendszer helyi piacokhoz

Egyetemi projekt (Széchenyi István Egyetem, *Portálfejlesztés .NET-ben*). A rendszerben a vásárlók egy piacnap előtt
előrendelhetnek a helyi termelőktől, a termelők kezelik a kínálatukat és a beérkező rendeléseket, a piacvezető
(admin) pedig jóváhagyja az árusokat, eldönti a vitás ügyeket, és riportokat néz.

## Funkciók szerepkörönként

| Szerepkör | Mit tud |
|---|---|
| **Vásárló** | regisztráció, terméklista keresés/szűrés (kategória, allergén, piac, árus), a piac térképe az árus standjával, „Piacok” oldal (nyitvatartás, következő piacnap, időjárás, standok árusonként színezve), a rendeléseknél az átvételi stand kiemelve a térképen, kosár (több árusból), rendelés leadása árusonként egy átvételi időpontra, rendelések követése, lemondás, vitatás, rendelés exportja (JSON/CSV), időjárás-előrejelzés a piacnapra |
| **Termelő (árus)** | regisztráció (jóváhagyásra vár), termékek felvétele képpel, heti készlet és ár megadása, a piac nyitvatartásának megtekintése, átvételi időpontok kezelése (a nyitvatartáson belül), visszajelzés a lezárt hetek maradékáról, beérkező rendelések elfogadása / részleges teljesítése / elutasítása, indokolt lemondás, „kész”, „átvéve” és „nem jött el” jelölés, vitatás, szedési lista export |
| **Piacvezető (admin)** | árusok jóváhagyása/elutasítása, a piac felülnézeti elrendezése (standok húzással) és fix stand kiosztása az árusoknak, termékek törlése (az aktív rendelések indokolt lemondásával), felhasználói fiókok letiltása/engedélyezése, piacok (napok és nyitvatartási órák) és kategóriák kezelése, vitás rendelések eldöntése, rendelés lemondása indoklással, piacnapi szedési lista, riportok (top termékek, lemondási és lejárati arány, árusi bevétel, át nem vett rendelések), auditnapló |

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

* **Rendelés-állapotgép** a `Domain/Entity/Order.cs`-ben: `Pending → Accepted/PartiallyFulfilled/Rejected/Cancelled/Expired → Ready → PickedUp/NotPickedUp → Disputed`. Érvénytelen átmenet `DomainException` (HTTP 400). A vitatott rendelést kizárólag a piacvezető zárhatja le. Az időkorlátok (lemondási határidő, „átvéve” csak az átvétel napján, „nem jött el” csak az idősáv után, automatikus lejárat) a [`docs/DATA_MODEL.md`](docs/DATA_MODEL.md)-ben.
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
| `Orders:CustomerCancellationCutoffHours` | elfogadott rendelés vevői lemondási határideje az idősáv kezdete előtt (óra) | 12 |
| `Orders:ExpiryEnabled` / `ExpiryCheckMinutes` | függő rendelések automatikus lejárata / ellenőrzés gyakorisága (perc) | `true` / 15 |
| `Jwt:ExpiryMinutes` | token érvényessége (a letiltott fiók tokenje ettől függetlenül azonnal érvénytelen) | 60 |
| `Database:MigrateOnStartup` | migráció + kezdő adatok induláskor | `true` fejlesztésben |
| `Cors:Origins` | engedélyezett kliens-eredetek | `http://localhost:5219` |
| `App:TimeZone` | a piac helyi időzónája | `Europe/Budapest` |
| `ImageStorage:RootPath` | feltöltött képek mappája | lásd `appsettings` |
| `Seed:AdminPassword` | az admin kezdő jelszava (nem fejlesztői módban kötelező) | `Admin123!` fejlesztésben |
| `RateLimit:GeneralPerMinute` / `AuthPerMinute` | kérések/perc felhasználónként (vendégnél IP-nként) / bejelentkezés-regisztráció IP-nként | 1200 / 60 |
| `ASPNETCORE_ENVIRONMENT` (Docker: `.env`) | `Development`: demó adatok, OpenAPI; `Production`: ezek nélkül | `Development` |
| `Api:BaseUrl` (kliens) | az API címe a kliens szerverének | – (kötelező) |
| `Api:PublicBaseUrl` (kliens) | az API böngészőből elérhető címe (képekhez) | `Api:BaseUrl` |

## API röviden

Minden végpont `/api` alatt van; a védettek `Authorization: Bearer <token>` fejlécet kérnek. A lapozott listák (`page`, `pageSize` ≤ 100) `{ items, total, page, pageSize }` alakot adnak.

| Terület | Végpontok |
|---|---|
| Auth | `POST /auth/login`, `/auth/register-customer`, `/auth/register-producer` |
| Katalógus | `GET /products` (lapozott; `marketId`, `producerId`, `categoryId`, `text`, `excludeAllergies` szűrők), `GET /markets/{id}/layout`, `/products/{id}`, `/categories`, `/markets`, `/producers`, `/pickup-slots`, `/weather/markets/{id}` |
| Termelő | `POST/PUT/DELETE /products` (DELETE adminnak is, `?reason=`), `GET /producers/me/stock-review`, `PUT /products/{id}/stock`, `PUT/DELETE /products/{id}/image`, `POST/PUT/DELETE /pickup-slots`, `PUT /producers/me` |
| Kosár | `GET /cart`, `POST /cart/items`, `PUT/DELETE /cart/items/{productId}` |
| Rendelések | `POST /orders`, `GET /orders/my`, `/orders/producer`, `/orders/disputed`, `/orders/{id}`, `POST /orders/{id}/{cancel,accept,accept-partially,reject,ready,picked-up,not-picked-up,dispute,resolve-dispute}` |
| Export | `GET /export/orders/{id}?format=Json|Csv`, `/export/picking-list?date=…&format=…` |
| Admin | `PUT /markets/{id}/layout`, `PUT /producers/{id}/stall`, `/reports/*`, `GET /audit`, `GET /users`, `POST /users/{id}/{activate,deactivate}`, `POST /producers/{id}/{approve,reject}`, `/markets`, `/categories` |

## Tesztek és CI

```bash
dotnet test localmarket-preordersystem.Tests
```

A tesztek a rendelés-állapotgépet (a vitás rendelés védelmét is), a készlet- és idősáv-foglalást, a validációt és az időzóna-kezelést fedik le. Az `integration-smoke` CI-feladat valódi PostgreSQL-lel elindítja az API-t (migrációk, seed), és a `scripts/smoke-test.sh` végigviszi a teljes folyamatot (regisztráció, jóváhagyás, termék/készlet/idősáv/kép, kosár, rendelés, teljesítés, vita, döntés, szerkesztés/törlés, jogosultságok). Helyben: `docker compose up -d --build`, majd `scripts/smoke-test.sh http://localhost:5205`.

A GitHub Actions workflow (`.github/workflows/ci.yml`) minden push-nál és pull requestnél felépíti az API-t és a klienst, lefuttatja a teszteket és a füsttesztet.

## Ismert korlátok

* A Blazor Server kliens szerver oldalról hívja az API-t, ezért az API számára minden kérés ugyanarról az IP-ről érkezik. A bejelentkezett kéréseket ezért felhasználónként korlátozzuk; a bejelentkezés/regisztráció (`auth`) és a vendégforgalom limitje viszont továbbra is IP-alapú, vagyis a Blazor kliensen keresztül gyakorlatilag közös. Emiatt ezek a limitek bőkezűek és konfigurálhatók.
* A token-ellenőrzés minden hitelesített kérésnél egy könnyű adatbázis-lekérdezéssel megnézi a fiók állapotát (aktív-e, egyezik-e a szerepkör). Nagy forgalomnál ez rövid idejű gyorsítótárral kiváltható.
* A lapozás az auditnaplónál adatbázis-szinten történik; a termék- és rendelési listák (felhasználónként/árusonként korlátos méretűek) memóriában lapoznak.
* Vita csak az átadás lezárása (átvéve / nem jött el) után nyitható, ezért a vita lezárása nem hagyhat lefoglalva készletet vagy idősáv-helyet. Átadás előtti probléma esetén a vevő lemond, az árus indoklással lemond.
* A piac inaktiválása vagy egy nyitvatartási nap törlése a már leadott rendeléseket nem mondja le automatikusan (azokat a piacvezető indoklással lemondhatja), csak az új foglalásokat tiltja.
* A termékképek helyi lemezen tárolódnak (Dockerben volume-on); felhős tárhely nincs bekötve.
* Fizetés csak nyilvántartás (készpénz/kártya/átutalás jelölése), tényleges fizetési szolgáltató nincs integrálva.

## Hibák és javítások

A kódáttekintés során feltárt hibák és javításaik: [`docs/REVIEW.md`](docs/REVIEW.md).
