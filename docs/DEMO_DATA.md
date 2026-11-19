# Demó mintaadatok

Development módban az API induláskor (üres `Producers` tábla esetén) létrehozza a mintaadatokat: `Extensions/DemoDataSeeder.cs`.

| Fiók | E-mail | Jelszó |
|---|---|---|
| Piacvezető | `admin@localmarket.test` | `Seed:AdminPassword` (alapból `Admin123!`) |
| Vásárló | `vevo@localmarket.test` | `Seed:CustomerPassword` (alapból `Customer123!`) |
| Termelő: Kovács Gazdaság | `kovacs.gazdasag@localmarket.test` | `Seed:CustomerPassword` |
| Termelő: Szabó Pékség | `szabo.pekseg@localmarket.test` | `Seed:CustomerPassword` |
| Termelő: Nagy Sajtműhely | `nagy.sajt@localmarket.test` | `Seed:CustomerPassword` |

Mindhárom árus jóváhagyott. Összesen 6 termék van allergénekkel és a piac következő nyitvatartási napjának hetére szóló készlettel, árusonként két átvételi idősávval (8-10 és 10-12 óra, 10 fős kapacitás).

Az adatok csak bemutatóra valók, éles környezetben nem jönnek létre.
