# Szerepkör- és jogosultsági mátrix

A policy-k: `api/Extensions/AuthorizationExtensions.cs`.

| Funkció | Vendég | Vásárló | Termelő | Piacvezető (Admin) |
|---|---|---|---|---|
| Regisztráció, bejelentkezés | igen | - | - | - |
| Termékek, piacok, időjárás megtekintése | igen | igen | igen | igen |
| Kosár kezelése | nem | igen | nem | nem |
| Rendelés leadása, saját rendelések, lemondás | nem | igen | nem | nem |
| Rendelés vitatása | nem | igen | igen | nem |
| Termék, heti készlet, ár, allergén, kép kezelése | nem | nem | igen (saját) | nem |
| Átvételi idősávok kezelése | nem | nem | igen (saját) | nem |
| Rendelés elfogadása / részleges / elutasítás / kész / átvéve | nem | nem | igen (saját) | nem |
| Szedési lista export | nem | nem | igen | igen |
| Rendelés export (JSON/CSV) | nem | igen (saját) | igen (saját) | igen |
| Árusok jóváhagyása / elutasítása | nem | nem | nem | igen |
| Piacok, nyitvatartási napok, kategóriák | nem | nem | nem | igen |
| Vitás rendelések eldöntése | nem | nem | nem | igen |
| Riportok, auditnapló | nem | nem | nem | igen |
