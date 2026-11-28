# Szerepkör- és jogosultsági mátrix

A policy-k: `api/Extensions/AuthorizationExtensions.cs`.

| Funkció | Vendég | Vásárló | Termelő | Piacvezető (Admin) |
|---|---|---|---|---|
| Regisztráció, bejelentkezés | igen | - | - | - |
| Termékek, piacok, időjárás megtekintése | igen | igen | igen | igen |
| Kosár kezelése | nem | igen | nem | nem |
| Rendelés leadása, saját rendelések | nem | igen | nem | nem |
| Rendelés lemondása | nem | igen (saját, lemondási határidőig) | igen (saját, indoklással, idősáv végéig) | igen (bármelyik, indoklással) |
| Rendelés vitatása (átvétel / „nem jött el” után) | nem | igen (saját) | igen (saját) | nem |
| Termék, heti készlet, ár, allergén, kép kezelése | nem | nem | igen (saját) | nem |
| Átvételi idősávok kezelése | nem | nem | igen (saját) | nem |
| Rendelés elfogadása / részleges / elutasítás / kész / átvéve | nem | nem | igen (saját) | nem |
| Szedési lista export | nem | nem | igen (saját) | igen (egy árus vagy a teljes piacnap) |
| Rendelés export (JSON/CSV) | nem | igen (saját) | igen (saját) | igen |
| Termék törlése | nem | nem | igen (saját, aktív rendelés nélkül) | igen (bármelyik; az aktív rendeléseket indoklással lemondja) |
| Piac nyitvatartásának megtekintése | igen | igen | igen | igen |
| Piac térképe (standok, kiosztott árusok) | igen | igen | igen | igen (a függő árusokat is látja) |
| Piacok áttekintő oldal (`/markets`) | igen | igen | igen | igen |
| Átvételi stand a saját rendelésnél (térképen kiemelve) | nem | igen (saját) | nem | nem |
| Piac elrendezése, fix stand kiosztása | nem | nem | nem (a saját standját látja, nem módosíthatja) | igen |
| Termékek szűrése piacra / árusra | igen | igen | igen | igen |
| Heti maradék-visszajelzés (lezárt hetek) | nem | nem | igen (saját) | nem |
| Árusok jóváhagyása / elutasítása | nem | nem | nem | igen |
| Felhasználói fiókok listázása, letiltása / engedélyezése (saját fiók és az utolsó aktív admin nem tiltható) | nem | nem | nem | igen |
| Piacok, nyitvatartási napok és órák, kategóriák | nem | nem | nem | igen |
| Vitás rendelések eldöntése | nem | nem | nem | igen |
| Riportok, auditnapló | nem | nem | nem | igen |

A tokenben lévő szerepkört az API minden kérésnél összeveti az adatbázissal: letiltott fiók, vagy azóta megváltozott szerepkörű/árus-profilú felhasználó tokenje azonnal érvénytelen (401), a kliens ilyenkor kijelentkeztet.
