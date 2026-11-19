# Adatmodell

A modell a `localmarket-preordersystem.Domain/Entity` osztályaira és az `Infrastructure/Persistence/Configurations` EF konfigurációkra épül (PostgreSQL).

```mermaid
erDiagram
    Market ||--o{ Producer : "piacon árul"
    User ||--o| Producer : "árus fiók"
    Category ||--o{ Product : "kategória"
    Producer ||--o{ Product : "kínál"
    Product ||--o{ ProductStock : "heti készlet"
    Producer ||--o{ PickupSlot : "átvételi idősáv"
    User ||--o| Cart : "kosár"
    Cart ||--o{ CartItem : "tételek"
    Product ||--o{ CartItem : ""
    User ||--o{ Order : "vásárló"
    Producer ||--o{ Order : "árusonként egy rendelés"
    PickupSlot ||--o{ Order : "foglalás"
    Order ||--o{ OrderItem : "tételsorok"
    Product ||--o{ OrderItem : ""
    Order ||--o| Payment : "fizetés (készpénz/kártya/átutalás)"
    User ||--o{ AuditLog : "állapotváltozás-napló"
```

| Entitás | Szerep |
|---|---|
| User | felhasználó, szerepkör: Customer / Producer / Admin |
| Market | piac, nyitvatartási napok (admin állítja) |
| Producer | árus adatlap, jóváhagyási állapot |
| Category, Product | termékek, mértékegység, allergének |
| ProductStock | heti készlet és ár (`xmin` optimista konkurencia) |
| PickupSlot | átvételi idősáv, `BookedCount` |
| Cart, CartItem | több árusból álló kosár |
| Order, OrderItem | rendelés és tételsorok, állapotgép |
| Payment | fizetési mód és állapot |
| Logs (AuditLog) | kritikus módosítások naplója |

## Rendelés-állapotgép

`Pending -> Accepted | PartiallyFulfilled | Rejected | Cancelled -> Ready -> PickedUp | NotPickedUp -> Disputed -> (admin döntés)`
