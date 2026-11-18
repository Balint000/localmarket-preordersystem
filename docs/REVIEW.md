# Kódáttekintés: feltárt hibák és javításaik

Súlyosság: **Magas** = hibás működés vagy összeomlás, **Közepes** = rossz eredmény vagy biztonsági/robusztussági hiány, **Alacsony** = tisztaság, karbantarthatóság.

## Domain

| # | Súly | Hiba | Javítás |
|---|---|---|---|
| 1 | Magas | A vitatott (`Disputed`) rendelést a vásárló a sima lemondással, az árus az „átvéve” jelöléssel megkerülhette, így a piacvezető döntése kihagyható volt | `Order.Cancel()` és `MarkPickedUp()` vitatott rendelésen `DomainException`; csak a `ResolveDispute*` zárhat |
| 2 | Közepes | A vitatott rendelés „átvettnek” döntésekor a függő fizetés függőben maradt | `ResolveDisputeAsPickedUp` teljesítettre állítja a fizetést (mint a `MarkPickedUp`) |
| 3 | Közepes | `ProductStock.Reserve` törtmennyiséget is elfogadott darabos termékre (pl. 1,5 db), és 3-nál több tizedesjegyet | Egész szám kötelező a darabos mértékegységeknél, legfeljebb 3 tizedes (`UnitsExtensions.IsCountable`) |
| 4 | Közepes | Inaktív piacra is lehetett árusként regisztrálni | `Producer.Register` elutasítja |
| 5 | Alacsony | `DishWashing` fizetési mód, DataAnnotations a domain entitáson (ellentmondott az EF konfigurációnak), alapértelmezett fizetési mód nélkül, halott `OrderItemStatusEntity` osztály | Eltávolítva / rendezve; `Payment.Method` alapértéke `Cash` |

## Application

| # | Súly | Hiba | Javítás |
|---|---|---|---|
| 6 | Magas | A heti készlet mennyisége nem lehetett 0, pedig a „elfogyott” állapotot ez jelenti (domain engedi, a DB-megszorítás tiltotta) | A validáció `NonNegative`, plusz felső korlát; a DB megszorítás `>= 0` (lásd 14.) |
| 7 | Közepes | Hiányzó hosszkorlátok (név, e-mail, telefon, jelszó, cím) → adatbázis-hiba (500) a validációs hiba helyett | `MaxLength` ellenőrzések a regisztrációnál és a piacoknál |
| 8 | Közepes | A kosárba törtmennyiség került darabos terméknél, a hiba csak rendeléskor derült ki | `AddToCart` korán validál |
| 9 | Közepes | A vita-döntés megjegyzése (1000) + a vitaindoklás összefűzve túlcsordulhatott a `DisputeNote` oszlopon | Döntési megjegyzés legfeljebb 900 karakter |
| 10 | Alacsony | `SetProductImageHandler` nullable-figyelmeztetés, junk `Remove` elemek a csproj-ban, elírt fájlnév (`ConcurencyException`) | Rendezve |
| 11 | Magas | A „ma” és az idősáv-határidők UTC-ben számolódtak, miközben a piac helyi idejében értendők (kb. 1–2 órás eltérés, rossz lejárat) | `ZonedTimeProvider` (`App:TimeZone`), a `TimeProvider` ezt adja |

## Infrastructure

| # | Súly | Hiba | Javítás |
|---|---|---|---|
| 12 | Magas | A `Products.ImageFileName` oszlop hiányzott a migrációból és a snapshotból, így friss adatbázison minden terméklekérdezés elbukott | Új migráció: `20261117090000_ProductImageAndStockPrecision` + Designer + snapshot |
| 13 | Közepes | A `ProductStocks` oszlopai `numeric(18,2)` típusúak maradtak, az EF konfiguráció `(12,3)`/`(12,2)`-t ír elő; a modell eltért a snapshottól (EF 10-ben a `Migrate()` ilyenkor kivételt dob) | A migráció átállítja a típusokat |
| 14 | Közepes | `CK_ProductStocks_Quantity` `> 0` volt, `CK_Payments_Amount` konfigurációja `>= 0`, a migrációval és a domainnel ellentétes | Egységesítve: készlet `>= 0`, fizetés `> 0` |
| 15 | Közepes | A kosár és a rendelés betöltésekor az árus (`Producer`) nem töltődött be (`ThenInclude` hiányzott) | `Include` kiegészítve |
| 16 | Közepes | A riportok dátumhatárait csak a „top termékek” alakította át helyi időből UTC-re, a többi UTC-nek vette; a „le nem vett rendelések” a helyi idősávot UTC-vel hasonlította | Minden riport `ToUtc`-t használ, a „most” helyi idő |
| 17 | Alacsony | Törölt osztályra hivatkozó `modelBuilder.Ignore<…>` | Eltávolítva |

## API

| # | Súly | Hiba | Javítás |
|---|---|---|---|
| 18 | Magas | A `[EnableRateLimiting("auth")]` hatástalan volt: nem volt rate limiter regisztrálva, a bejelentkezés korlátlanul próbálgatható | `AddRateLimiter` + `UseRateLimiter` (globális és „auth” policy, konfigurálható) |
| 19 | Magas | `GET /api/export/...` formátum nélkül mindig 400-at adott (az alapértelmezett Json soha nem érvényesült) | `ExportFormat?`, alapértelmezés Json |
| 20 | Közepes | A termelő nem vitathatta a rendelést, pedig a handler engedi | Új `CustomerOrProducer` policy a `dispute` végponton |
| 21 | Közepes | Nem volt végpont az auditnaplóhoz (a handler létezett) | `GET /api/audit` (admin, lapozott) |
| 22 | Közepes | Túl hosszú szöveg / számtúlcsordulás (PostgreSQL 22001/22003) 500-at adott | 400-ra képezve |
| 23 | Közepes | Éles környezetben is beégetett admin-jelszó, alap kategóriák nélkül indult az adatbázis | `Seed:AdminPassword` kötelező nem fejlesztői módban; alap kategóriák seedelése |
| 24 | Alacsony | Sablonmaradék `.http` fájl, üres `Folder` elem a csproj-ban | Törölve |

## Blazor kliens

| # | Súly | Hiba | Javítás |
|---|---|---|---|
| 25 | Magas | A lapozott végpontokat (termékek, rendelések) tömbként olvasta, így a terméklista és a rendelések oldalai hibára futottak | `GetAllPagesAsync` végigmegy az oldalakon |
| 26 | Magas | Az árus azonosítóját `producer_id` néven kereste, az API `producerId`-t ad, így az árusi oldalak nem találták a saját azonosítót | Claim név javítva |
| 27 | Közepes | Nem JSON válasz (pl. proxy hibaoldal) kezeletlen `JsonException`-t okozott | Elkapva, felhasználóbarát hiba |
| 28 | Közepes | A termékképek (API-képesség) nem jelentek meg és nem volt feltöltő felület | Képmegjelenítés a kártyán/részletoldalon, feltöltés és törlés az árusi szerkesztőben; `Api:PublicBaseUrl` |
| 29 | Közepes | Az admin nem látta az auditnaplót | Új „Napló” oldal |
| 30 | Alacsony | Sablonmaradékok (`Weather.razor`, `app.css`), duplikált `ManagerDisputes` oldal, kikommentezett duplikált `Program.cs` blokk, téves megjegyzés a kategóriákról | Törölve/javítva |

## Üzemeltetés és dokumentáció

| # | Hiba | Javítás |
|---|---|---|
| 31 | A `docker-compose.override.yml` Windows-specifikus (`APPDATA`) mountokat és HTTPS-portot adott a szolgáltatáshoz, és `docker compose up` automatikusan beolvasta, így Linuxon/macOS-en az API nem indult | Törölve (a `.dcproj`-ból is) |
| 32 | A compose nem adta át a kezdő jelszavakat, az időzónát és az API nyilvános címét | Környezeti változók felvéve |
| 33 | Nem volt `.env.example`, a README kevés információt adott | `.env.example`, teljes README |
| 34 | Nem voltak tesztek és CI | `localmarket-preordersystem.Tests` (49 teszteset), GitHub Actions workflow |

## Tudatosan nem javított pontok

Lásd a README „Ismert korlátok” szakaszát (foglalás-felszabadítás vitatott / át nem vett rendelésnél, IP-alapú rate limit a Blazor Server miatt, helyi képtárolás, fizetésintegráció hiánya).

## Ellenőrzés

* A Domain, az Application és a Blazor kliens offline lefordult hibák és figyelmeztetések nélkül.
* A teszteket egy xUnit-kompatibilis futtatóval ellenőriztem: 49 teszteset, mind sikeres.
* Az Infrastructure és az API csak szintaktikailag ellenőrizhető volt (az EF Core / Npgsql / JwtBearer csomagok nem voltak letölthetők), ezeknél a `dotnet build` és a `docker compose up --build` az első futtatás próbája.
