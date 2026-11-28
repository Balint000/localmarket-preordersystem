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
| T19 | Fiók letiltása | Admin letiltja a vásárlót, majd a vásárló a korábbi (még nem lejárt) tokenjével hív | 401, a kliens kijelentkeztet; a belépés is 401; az újraengedélyezés után működik |
| T20 | Letiltási védelmek | Admin a saját fiókját, illetve az utolsó aktív admint próbálja letiltani | 409 |
| T21 | Átfedő idősáv | Árus 9–12 mellé 11–13:30-as idősávot hirdet ugyanarra a napra | 409; a csak érintkező 12–14 megengedett |
| T22 | Lemondási határidő | Vevő az elfogadott rendelést 12 órán belül, illetve azon kívül mondja le | Határidőn túl 400, előtte lemondva |
| T23 | Árusi / piacvezetői lemondás | Árus vagy admin indoklás nélkül, majd indoklással mond le | 400, majd lemondva; a vevő látja az okot |
| T24 | Időkorlátok | „Átvéve” az átvétel napja előtt, „nem jött el” az idősáv vége előtt | 400 |
| T25 | Vita átadás előtt | Vevő elfogadott, még át nem adott rendelést vitat | 400 (vita csak átvéve / nem jött el után) |
| T26 | Automatikus lejárat | Függő rendelés idősávja véget ér | `Expired`, készlet és hely felszabadul, naplóbejegyzés |
| T27 | Nyitvatartás változása | Admin kiveszi a napot a nyitvatartásból, vevő a meghirdetett idősávra rendel | Az idősáv `isBookable=false`, a rendelés 400 |
| T28 | Szedési lista (admin) | `GET /api/export/picking-list?date=…` adminként, árussal és anélkül | 200; csak az elfogadott rendelések; a függők száma jelezve |
| T29 | Nyitvatartási órák | Árus a piac nyitvatartásán kívüli idősávot hirdet (pl. 12–14, ha 13-kor zár) | 400; a nyitvatartás az árusi idősáv-oldalon látszik |
| T30 | Rendelési lépték | Vevő 0,3 kg-ot, majd 1,5 kg-ot tesz kosárba | 0,3 kg: 400; 1,5 kg: siker; a léptető 0,5-ösével lép |
| T31 | Lezárt hét | A hét utolsó piacnapjának zárása után a heti kínálatú termék | Nem tehető kosárba (a következő hét kínálata lép életbe, ha van) |
| T32 | Maradék-visszajelzés | Árus megnyitja a Termékeim oldalt egy lezárt hét után | Termékenként meghirdetve / rendelve / maradt, „Elfogyott” / „Sok maradt” javaslattal |
| T33 | Vita lezáratlan átadásnál | Az árus nem jelölte az átvételt, az idősáv lejárt; a vevő vitát nyit | Disputed; a piacvezető dönt |
| T34 | Admin termék-törlés | Admin aktív rendeléses terméket töröl indoklás nélkül, majd indoklással | 400, majd a rendelések lemondva, a termék archiválva és eltűnik a kínálatból |
| T35 | Árusi törlés aktív rendeléssel | Árus aktív rendeléses terméket töröl | 409 |
| T36 | Szűrés piacra / árusra | `GET /api/products?marketId=…&producerId=…`; a Termékek oldalon a piac- és árusválasztó | Csak az adott piac / árus termékei; piac választása után csak annak árusai választhatók |
| T37 | Piac-elrendezés | Admin új piacot vesz fel → a szerkesztőben standokat helyez el (kattintás, húzás, sablon), átfedő standdal ment | Új piac után a szerkesztő nyílik; átfedésnél 400; helyes mentés után a térkép frissül |
| T38 | Fix stand kiosztása | Admin szabad, foglalt, illetve nem létező standot oszt ki | Szabad: siker; foglalt: 409 (csere opcióval áthelyez); nem létező: 409 |
| T39 | Stand megszűnése | Admin töröl egy kiosztott standot és ment | Az árus kiosztás nélkül marad, a válasz felsorolja |
| T40 | Árus nem írhatja át a standját | `PUT /api/producers/me` más standszámmal | 403; a profilban a stand csak olvasható |
| T41 | Térkép a vevőnek | A termékoldalon az árus standja kiemelve | A piac térképén az árus standja kiemelt színnel |
| T42 | Átvételi stand a rendelésnél | Vevő megnyit egy aktív rendelést, amelynek árusa standot kapott | „Itt veheted át” kártya: árus neve az árus színével, standszám-tábla, a térképen a stand kiemelve |
| T43 | Következő átvétel | Vevőnek több árusnál van rendelése ugyanarra a piacnapra | A Rendeléseim tetején a standok időrendben, a térképen mind kiemelve |
| T44 | Piacok oldal | Vevő vagy vendég megnyitja a Piacok oldalt | Piacnapok, nyitvatartás, következő piacnap, időjárás, termelők száma, térkép árusonként színezve; „A piac kínálata” a szűrt terméklistára visz |
| T45 | Csökkentett mozgás | Az operációs rendszerben a „csökkentett mozgás” be van kapcsolva | A stand-felvillanás, a kosárszámláló és a cédula animációja nem játszódik le |
