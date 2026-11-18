# Dokumentált tesztesetek

Automatikus: `dotnet test localmarket-preordersystem.Tests` (xUnit, 49 teszt) és `scripts/smoke-test.sh http://localhost:5205`.

Mintaadatok: seedelt admin (`admin@localmarket.test`), vásárló (`vevo@localmarket.test`), alap kategóriák, egy piac. A termelő regisztrációval vehető fel.

| # | Teszteset | Lépések | Elvárt eredmény |
|---|---|---|---|
| T1 | Árus regisztráció és jóváhagyás | Termelő regisztrál; admin jóváhagyja | Jóváhagyás előtt nem rendelhető tőle; után igen |
| T2 | Termék és heti készlet | Termék felvétele, készlet, ár, allergén, idősáv megadása | Megjelenik a katalógusban |
| T3 | Keresés/szűrés | Terméklista szűrése kategóriára és allergénre | Csak a találatok látszanak |
| T4 | Több árusos kosár | Két árustól termék a kosárba | Rendelés árusonként külön jön létre |
| T5 | Rendelés leadása | Idősáv választás, rendelés | Készlet és idősáv-hely lefoglalva |
| T6 | Validáció | Tört mennyiség darabos terméknél; túl hosszú szöveg | 400 ProblemDetails |
| T7 | Elfogadás | Termelő elfogadja | Állapot: Accepted |
| T8 | Részleges teljesítés | Termelő részlegesen elfogad | Állapot: PartiallyFulfilled, módosított tételek |
| T9 | Elutasítás/lemondás | Elutasítás vagy lemondás | Foglalás felszabadul |
| T10 | Átvétel | Kész, majd átvéve | PickedUp, fizetés teljesítve |
| T11 | Vita | Vásárló vitatja; sima lemondás/átvéve tiltott | Disputed; csak admin zárhat |
| T12 | Vita eldöntése | Admin dönt | Rendelés lezárul, naplóbejegyzés |
| T13 | Jogosultság | Vásárló hívja az admin végpontot | 403 |
| T14 | Konkurencia | Utolsó készlet két rendelésnél egyszerre | Az egyik 409 |
| T15 | Export | `GET /api/export/orders/{id}?format=Csv` és szedési lista | JSON/CSV letöltés |
| T16 | Időjárás | `GET /api/weather/markets/{id}` | Előrejelzés (Open-Meteo) vagy kezelt hiba |
| T17 | Riportok | `/reports/*` | Top termékek, lemondási arány, árusi forgalom, át nem vett rendelések |
| T18 | Audit | `GET /api/audit` adminként | Kritikus változások listázva |
