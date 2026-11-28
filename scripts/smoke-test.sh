#!/usr/bin/env bash
# Végigviszi a teljes üzleti folyamatot a futó API-n (valódi PostgreSQL-lel):
# regisztráció -> árus jóváhagyás -> termék/készlet/idősáv -> kosár -> rendelés -> teljesítés -> vita -> döntés.
# Használat: scripts/smoke-test.sh [API_BASE_URL]   (alapból http://localhost:5205)
# Környezeti változó: ADMIN_PASSWORD (alapból Admin123!)
set -euo pipefail

BASE="${1:-http://localhost:5205}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin123!}"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
SUFFIX="$(date +%s)$RANDOM"
PASSWORD="Teszt1234!"
STATUS=""; BODY=""

req() { # req METHOD PATH [TOKEN] [JSON]
  local method="$1" path="$2" token="${3:-}" body="${4:-}"
  local args=(-sS -o "$TMP/body" -w '%{http_code}' -X "$method" "$BASE$path" -H 'Content-Type: application/json')
  [[ -n "$token" ]] && args+=(-H "Authorization: Bearer $token")
  [[ -n "$body" ]] && args+=(-d "$body")
  STATUS="$(curl "${args[@]}")"
  BODY="$(cat "$TMP/body")"
}

expect() { # expect STATUS LEIRAS
  if [[ "$STATUS" != "$1" ]]; then
    echo "HIBA: $2 (várt: $1, kapott: $STATUS)"; echo "$BODY"; exit 1
  fi
  echo "ok  $2"
}

expect_4xx() { # expect_4xx LEIRAS
  if [[ ! "$STATUS" =~ ^4 ]]; then echo "HIBA: $1 (várt: 4xx, kapott: $STATUS)"; echo "$BODY"; exit 1; fi
  echo "ok  $1 ($STATUS)"
}

login() { req POST /api/auth/login "" "{\"email\":\"$1\",\"password\":\"$2\"}"; expect 200 "bejelentkezés: $1" >&2; echo "$BODY" | jq -r .token; }
order_status() { req GET "/api/orders/$2" "$1"; expect 200 "rendelés lekérése #$2" >&2; echo "$BODY" | jq -r .status; }
assert_eq() { if [[ "$1" != "$2" ]]; then echo "HIBA: $3 (várt: $2, kapott: $1)"; exit 1; fi; echo "ok  $3"; }

echo "== Egészség és alapadatok"
req GET /health; expect 200 "health"
req GET /api/markets; expect 200 "piacok listája"
MARKET_ID="$(echo "$BODY" | jq -r '.[0].id')"
req GET /api/categories; expect 200 "kategóriák listája"
CATEGORY_ID="$(echo "$BODY" | jq -r '.[0].id')"
[[ "$MARKET_ID" != "null" && "$CATEGORY_ID" != "null" ]] || { echo "HIBA: nincs seedelt piac/kategória"; exit 1; }

echo "== Regisztráció"
PRODUCER_EMAIL="arus$SUFFIX@example.com"; CUSTOMER_EMAIL="vevo$SUFFIX@example.com"
req POST /api/auth/register-producer "" "{\"firstName\":\"Árus\",\"lastName\":\"Teszt\",\"email\":\"$PRODUCER_EMAIL\",\"password\":\"$PASSWORD\",\"phoneNumber\":\"+36301234567\",\"producerName\":\"Teszt Gazdaság $SUFFIX\",\"marketId\":$MARKET_ID,\"stallNumber\":0}"
expect 200 "árus regisztráció"
req POST /api/auth/register-customer "" "{\"firstName\":\"Vevő\",\"lastName\":\"Teszt\",\"email\":\"$CUSTOMER_EMAIL\",\"password\":\"$PASSWORD\",\"phoneNumber\":\"+36301234568\"}"
expect 200 "vevő regisztráció"
req POST /api/auth/register-customer "" "{\"firstName\":\"\",\"lastName\":\"X\",\"email\":\"rossz\",\"password\":\"1\",\"phoneNumber\":\"1\"}"
expect 400 "érvénytelen regisztráció 400-at ad"
req POST /api/auth/login "" "{\"email\":\"$CUSTOMER_EMAIL\",\"password\":\"rossz-jelszo\"}"
expect 401 "rossz jelszó 401-et ad"

echo "== Árus jóváhagyása"
ADMIN="$(login admin@localmarket.test "$ADMIN_PASSWORD")"
req GET "/api/producers?status=Pending" "$ADMIN"; expect 200 "függő árusok"
PRODUCER_ID="$(echo "$BODY" | jq -r --arg n "Teszt Gazdaság $SUFFIX" '[.[] | select(.name==$n)][0].id')"
[[ "$PRODUCER_ID" != "null" ]] || { echo "HIBA: az új árus nem látszik a függők között"; exit 1; }
req POST "/api/producers/$PRODUCER_ID/approve" "$ADMIN"; expect 200 "árus jóváhagyása"

echo "== Termék, készlet, idősáv, kép"
PRODUCER="$(login "$PRODUCER_EMAIL" "$PASSWORD")"
CUSTOMER="$(login "$CUSTOMER_EMAIL" "$PASSWORD")"
SAT="$(date -u -d 'next saturday' +%F)"
req POST /api/products "$PRODUCER" "{\"name\":\"Teszt alma\",\"description\":\"Friss\",\"categoryIds\":[$CATEGORY_ID],\"allergies\":[]}"
expect 200 "termék létrehozása"
PRODUCT_ID="$(echo "$BODY" | jq -r .id)"
req PUT "/api/products/$PRODUCT_ID/stock" "$PRODUCER" "{\"weekDate\":\"$SAT\",\"quantity\":20,\"unit\":\"Kilogram\",\"unitPrice\":500}"
expect 200 "heti készlet megadása"
req PUT "/api/products/$PRODUCT_ID/stock" "$PRODUCER" "{\"weekDate\":\"$SAT\",\"quantity\":-1,\"unit\":\"Kilogram\",\"unitPrice\":500}"
expect 400 "negatív készlet 400-at ad"
req POST /api/pickup-slots "$PRODUCER" "{\"date\":\"$SAT\",\"startTime\":\"09:00:00\",\"endTime\":\"12:00:00\",\"capacity\":5}"
expect 200 "átvételi idősáv létrehozása"
SLOT_ID="$(echo "$BODY" | jq -r .id)"

# 1x1-es PNG feltöltése, majd letöltése a statikus kiszolgálón át
printf 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==' | base64 -d > "$TMP/kep.png"
STATUS="$(curl -sS -o "$TMP/body" -w '%{http_code}' -X PUT "$BASE/api/products/$PRODUCT_ID/image" -H "Authorization: Bearer $PRODUCER" -F "file=@$TMP/kep.png;type=image/png")"
BODY="$(cat "$TMP/body")"; expect 200 "termékkép feltöltése"
IMAGE_URL="$(echo "$BODY" | jq -r .imageUrl)"
[[ "$IMAGE_URL" != "null" ]] || { echo "HIBA: a válaszban nincs imageUrl"; exit 1; }
assert_eq "$(curl -sS -o /dev/null -w '%{http_code}' "$BASE/${IMAGE_URL#/}")" 200 "a feltöltött kép elérhető"

req GET "/api/products?text=alma" ; expect 200 "terméklista (lapozott)"
assert_eq "$(echo "$BODY" | jq '[.items[] | select(.id=='"$PRODUCT_ID"')] | length')" 1 "az új termék a találatok között van"

echo "== Mai piacnap a teljes folyamathoz"
# Az átvétel csak az átvétel napján rögzíthető, ezért a mai napot (a piac helyi idejében) felvesszük a
# nyitvatartásba, és egész napos idősávot hirdetünk rá. A végén a nyitvatartás visszaáll.
MARKET_TZ="${MARKET_TZ:-Europe/Budapest}"
TODAY="$(TZ="$MARKET_TZ" date +%F)"
TODAY_DOW="$(LC_ALL=C TZ="$MARKET_TZ" date +%A)"
req GET /api/markets; expect 200 "piac adatai"
MARKET_ORIGINAL="$(echo "$BODY" | jq -c --argjson id "$MARKET_ID" '.[] | select(.id==$id) | {name, address, openingDays, latitude, longitude, isActive, opensAt, closesAt}')"
# egész napos nyitvatartás a mai, egész napos idősávhoz
MARKET_WITH_TODAY="$(echo "$MARKET_ORIGINAL" | jq -c --arg d "$TODAY_DOW" '.openingDays = ((.openingDays + [$d]) | unique) | .opensAt = "00:00:00" | .closesAt = "23:59:00"')"
req PUT "/api/markets/$MARKET_ID" "$ADMIN" "$MARKET_WITH_TODAY"; expect 200 "a mai nap felvétele a nyitvatartásba"
req PUT "/api/products/$PRODUCT_ID/stock" "$PRODUCER" "{\"weekDate\":\"$TODAY\",\"quantity\":20,\"unit\":\"Kilogram\",\"unitPrice\":500}"
expect 200 "heti készlet a mai hétre"
req POST /api/pickup-slots "$PRODUCER" "{\"date\":\"$TODAY\",\"startTime\":\"00:00:00\",\"endTime\":\"23:59:00\",\"capacity\":5}"
expect 200 "mai, egész napos idősáv"
TODAY_SLOT_ID="$(echo "$BODY" | jq -r .id)"

place_order() { # place_order SLOT_ID QUANTITY -> rendelés-azonosító
  req POST /api/cart/items "$CUSTOMER" "{\"productId\":$PRODUCT_ID,\"quantity\":$2}"; expect 200 "kosárba tétel ($2)" >&2
  req POST /api/orders "$CUSTOMER" "{\"paymentMethod\":\"Cash\",\"pickups\":[{\"producerId\":$PRODUCER_ID,\"pickupSlotId\":$1}]}"
  expect 200 "rendelés leadása (idősáv #$1)" >&2
  echo "$BODY" | jq -r '(if type=="array" then .[0] else . end).id'
}

echo "== Rendelés leadása és teljesítése"
req POST /api/cart/items "$CUSTOMER" "{\"productId\":$PRODUCT_ID,\"quantity\":2.5}"; expect 200 "kosárba tétel"
req GET /api/cart "$CUSTOMER"; expect 200 "kosár lekérése"
req POST /api/orders "$CUSTOMER" "{\"paymentMethod\":\"Cash\",\"pickups\":[{\"producerId\":$PRODUCER_ID,\"pickupSlotId\":$TODAY_SLOT_ID}]}"
expect 200 "rendelés leadása"
ORDER_ID="$(echo "$BODY" | jq -r '(if type=="array" then .[0] else . end).id')"
assert_eq "$(order_status "$CUSTOMER" "$ORDER_ID")" Pending "a rendelés függő"
req GET "/api/export/orders/$ORDER_ID" "$CUSTOMER"; expect 200 "export formátum megadása nélkül (alapból JSON)"
req GET "/api/export/orders/$ORDER_ID?format=Csv" "$CUSTOMER"; expect 200 "export CSV-ben"
req POST "/api/orders/$ORDER_ID/accept" "$PRODUCER"; expect 200 "árus elfogadja"
req POST "/api/orders/$ORDER_ID/not-picked-up" "$PRODUCER"; expect 400 "„nem jött el” az idősáv vége előtt nem jelölhető"
req POST "/api/orders/$ORDER_ID/dispute" "$CUSTOMER" '{"note":"Korai vita"}'; expect 400 "átadás előtt nem nyitható vita"
req POST "/api/orders/$ORDER_ID/ready" "$PRODUCER"; expect 200 "kész az átvételre"
req GET "/api/export/picking-list?date=$TODAY&format=Csv" "$PRODUCER"; expect 200 "szedési lista (árus)"
req GET "/api/export/picking-list?date=$TODAY&producerId=999999" "$PRODUCER"; expect 403 "más árus szedési listája tiltott"
req GET "/api/export/picking-list?date=$TODAY" "$ADMIN"; expect 200 "szedési lista (piacvezető, teljes piacnap)"
assert_eq "$(echo "$BODY" | jq --argjson o "$ORDER_ID" '[.Lines[] | select(.OrderId==$o)] | length')" 1 "az elfogadott rendelés a szedési listán van"
req POST "/api/orders/$ORDER_ID/picked-up" "$PRODUCER"; expect 200 "átvéve"
assert_eq "$(order_status "$CUSTOMER" "$ORDER_ID")" PickedUp "a rendelés átvett"
req GET /api/orders/my "$CUSTOMER"; expect 200 "saját rendelések (lapozott)"
assert_eq "$(echo "$BODY" | jq '.items | length')" 1 "egy saját rendelés van"

echo "== Vita és piacvezetői döntés"
req POST "/api/orders/$ORDER_ID/dispute" "$CUSTOMER" '{"note":"Nem kaptam meg a terméket"}'; expect 200 "vevő vitatja"
assert_eq "$(order_status "$CUSTOMER" "$ORDER_ID")" Disputed "a rendelés vitatott"
req POST "/api/orders/$ORDER_ID/cancel" "$CUSTOMER"; expect 400 "a vitatott rendelés nem mondható le a sima úton"
req POST "/api/orders/$ORDER_ID/picked-up" "$PRODUCER"; expect 400 "a vitatott rendelés nem jelölhető átvettnek a sima úton"
req GET /api/orders/disputed "$ADMIN"; expect 200 "vitatott rendelések"
req POST "/api/orders/$ORDER_ID/resolve-dispute" "$ADMIN" '{"decision":"Cancelled","note":"Jogos a panasz"}'; expect 200 "a piacvezető dönt"
assert_eq "$(order_status "$CUSTOMER" "$ORDER_ID")" Cancelled "a rendelés lemondva"

echo "== Elutasítás, részleges teljesítés, lemondások"
ORDER2_ID="$(place_order "$SLOT_ID" 4)"
req POST "/api/orders/$ORDER2_ID/accept-partially" "$PRODUCER" "{\"lines\":[{\"productId\":$PRODUCT_ID,\"quantity\":1}]}"
expect 200 "részleges teljesítés"
assert_eq "$(order_status "$CUSTOMER" "$ORDER2_ID")" PartiallyFulfilled "a rendelés részben teljesített"
req POST "/api/orders/$ORDER2_ID/picked-up" "$PRODUCER"; expect 400 "átvett az átvétel napja előtt nem jelölhető"
# a vevő az elfogadott rendelést az idősáv kezdete előtt 12 órával (alapbeállítás) mondhatja le utoljára
if [[ "$(TZ="$MARKET_TZ" date +%u)" == 5 && "$(TZ="$MARKET_TZ" date +%H)" -ge 21 ]]; then
  req POST "/api/orders/$ORDER2_ID/cancel" "$CUSTOMER"; expect 400 "a lemondási határidő lejárt (péntek este)"
  req POST "/api/orders/$ORDER2_ID/cancel" "$PRODUCER" '{"reason":"Teszt lezárás"}'; expect 200 "árus lemondja"
else
  req POST "/api/orders/$ORDER2_ID/cancel" "$CUSTOMER"; expect 200 "vevő lemondja a határidőn belül"
fi

ORDER3_ID="$(place_order "$SLOT_ID" 1)"
req POST "/api/orders/$ORDER3_ID/accept" "$PRODUCER"; expect 200 "árus elfogadja (3. rendelés)"
req POST "/api/orders/$ORDER3_ID/cancel" "$PRODUCER"; expect 400 "árusi lemondás indoklás nélkül 400"
req POST "/api/orders/$ORDER3_ID/cancel" "$PRODUCER" '{"reason":"Elfagyott a termés"}'; expect 200 "árus lemondja indoklással"
req GET "/api/orders/$ORDER3_ID" "$CUSTOMER"; expect 200 "vevő látja a lemondott rendelést"
assert_eq "$(echo "$BODY" | jq -r .rejectionReason)" "Elfagyott a termés" "a vevő látja a lemondás okát"

ORDER4_ID="$(place_order "$SLOT_ID" 1)"
req POST "/api/orders/$ORDER4_ID/cancel" "$ADMIN"; expect 400 "piacvezetői lemondás indoklás nélkül 400"
req POST "/api/orders/$ORDER4_ID/cancel" "$ADMIN" '{"reason":"Elmarad a piacnap"}'; expect 200 "piacvezető lemondja"

echo "== Nyitvatartás változása után a meghirdetett idősáv nem foglalható"
if [[ "$TODAY_DOW" != "Saturday" ]]; then
  MARKET_ONLY_TODAY="$(echo "$MARKET_ORIGINAL" | jq -c --arg d "$TODAY_DOW" '.openingDays = [$d]')"
  req PUT "/api/markets/$MARKET_ID" "$ADMIN" "$MARKET_ONLY_TODAY"; expect 200 "a szombat kivétele a nyitvatartásból"
  req GET "/api/pickup-slots?producerId=$PRODUCER_ID"; expect 200 "idősávok listája"
  assert_eq "$(echo "$BODY" | jq --argjson s "$SLOT_ID" '[.[] | select(.id==$s)][0].isBookable')" false "a szombati idősáv nem foglalható"
  req POST /api/cart/items "$CUSTOMER" "{\"productId\":$PRODUCT_ID,\"quantity\":1}"; expect 200 "kosárba tétel"
  req POST /api/orders "$CUSTOMER" "{\"paymentMethod\":\"Cash\",\"pickups\":[{\"producerId\":$PRODUCER_ID,\"pickupSlotId\":$SLOT_ID}]}"
  expect 400 "zárt napra nem adható le rendelés"
  req DELETE "/api/cart/items/$PRODUCT_ID" "$CUSTOMER"; expect 200 "kosár ürítése"
fi
req PUT "/api/markets/$MARKET_ID" "$ADMIN" "$MARKET_ORIGINAL"; expect 200 "a nyitvatartás visszaállítása"

echo "== Szerkesztés és törlés"
req POST /api/categories "$ADMIN" "{\"name\":\"Teszt kategória $SUFFIX\",\"description\":\"x\"}"; expect 200 "kategória létrehozása"
CAT2_ID="$(echo "$BODY" | jq -r .id)"
req PUT "/api/categories/$CAT2_ID" "$ADMIN" "{\"name\":\"Átnevezett $SUFFIX\",\"description\":\"y\"}"; expect 200 "kategória módosítása"
req DELETE "/api/categories/$CAT2_ID" "$ADMIN"; expect 204 "üres kategória törlése"
req DELETE "/api/categories/$CATEGORY_ID" "$ADMIN"; expect_4xx "használatban lévő kategória nem törölhető"
req POST /api/categories "$CUSTOMER" '{"name":"Tiltott","description":""}'; expect 403 "kategóriát vevő nem hozhat létre"

req POST /api/pickup-slots "$PRODUCER" "{\"date\":\"$SAT\",\"startTime\":\"12:00:00\",\"endTime\":\"14:00:00\",\"capacity\":2}"; expect 400 "nyitvatartáson (07–13) kívüli idősáv 400"
req POST /api/pickup-slots "$PRODUCER" "{\"date\":\"$SAT\",\"startTime\":\"12:00:00\",\"endTime\":\"12:30:00\",\"capacity\":2}"; expect 200 "második idősáv"
SLOT2_ID="$(echo "$BODY" | jq -r .id)"
req POST /api/pickup-slots "$PRODUCER" "{\"date\":\"$SAT\",\"startTime\":\"11:00:00\",\"endTime\":\"12:15:00\",\"capacity\":2}"; expect 409 "átfedő idősáv nem hozható létre"
req PUT "/api/pickup-slots/$SLOT2_ID" "$PRODUCER" "{\"date\":\"$SAT\",\"startTime\":\"12:00:00\",\"endTime\":\"13:00:00\",\"capacity\":3}"; expect 200 "idősáv módosítása"
req DELETE "/api/pickup-slots/$SLOT2_ID" "$PRODUCER"; expect 204 "foglalatlan idősáv törlése"
req DELETE "/api/pickup-slots/$SLOT_ID" "$PRODUCER"; expect_4xx "foglalt idősáv nem törölhető"

req POST /api/products "$PRODUCER" "{\"name\":\"Törlendő termék\",\"description\":\"\",\"categoryIds\":[$CATEGORY_ID],\"allergies\":[]}"; expect 200 "második termék"
PRODUCT2_ID="$(echo "$BODY" | jq -r .id)"
req DELETE "/api/products/$PRODUCT2_ID" "$PRODUCER"; expect 200 "rendelés nélküli termék törlése"
assert_eq "$(echo "$BODY" | jq -r .archived)" false "rendelés nélküli termék fizikailag törlődik"

echo "== Termék törlése piacvezetőként (aktív rendelés lemondásával)"
req POST /api/products "$PRODUCER" "{\"name\":\"Admin törli $SUFFIX\",\"description\":\"\",\"categoryIds\":[$CATEGORY_ID],\"allergies\":[]}"; expect 200 "harmadik termék"
PRODUCT3_ID="$(echo "$BODY" | jq -r .id)"
req PUT "/api/products/$PRODUCT3_ID/stock" "$PRODUCER" "{\"weekDate\":\"$SAT\",\"quantity\":10,\"unit\":\"Kilogram\",\"unitPrice\":800}"; expect 200 "készlet a harmadik termékhez"
req POST /api/cart/items "$CUSTOMER" "{\"productId\":$PRODUCT3_ID,\"quantity\":0.3}"; expect 400 "kg csak 0,5-ösével rendelhető"
req POST /api/cart/items "$CUSTOMER" "{\"productId\":$PRODUCT3_ID,\"quantity\":1.5}"; expect 200 "1,5 kg kosárba"
req POST /api/orders "$CUSTOMER" "{\"paymentMethod\":\"Cash\",\"pickups\":[{\"producerId\":$PRODUCER_ID,\"pickupSlotId\":$SLOT_ID}]}"; expect 200 "rendelés a harmadik termékre"
ORDER5_ID="$(echo "$BODY" | jq -r '(if type=="array" then .[0] else . end).id')"
req DELETE "/api/products/$PRODUCT3_ID" "$PRODUCER"; expect 409 "árus nem törölheti az aktív rendeléses terméket"
req DELETE "/api/products/$PRODUCT3_ID" "$ADMIN"; expect 400 "piacvezetői törlés indoklás nélkül 400 (aktív rendelés van)"
req DELETE "/api/products/$PRODUCT3_ID?reason=Teszt%20visszah%C3%ADv%C3%A1s" "$ADMIN"; expect 200 "piacvezető törli a terméket"
assert_eq "$(echo "$BODY" | jq -r .archived)" true "rendeléses termék archiválódik"
assert_eq "$(echo "$BODY" | jq '.cancelledOrderIds | length')" 1 "az aktív rendelés lemondva"
assert_eq "$(order_status "$CUSTOMER" "$ORDER5_ID")" Cancelled "a vevő rendelése lemondott"
req GET "/api/products?producerId=$PRODUCER_ID"; expect 200 "árus termékei"
assert_eq "$(echo "$BODY" | jq --argjson p "$PRODUCT3_ID" '[.items[] | select(.id==$p)] | length')" 0 "az archivált termék nincs a kínálatban"
req GET /api/producers/me/stock-review "$PRODUCER"; expect 200 "heti maradék-visszajelzés"

echo "== Piac-elrendezés és fix stand"
req GET "/api/markets/$MARKET_ID/layout"; expect 200 "piac térképe (nyilvános)"
LAYOUT="$BODY"
if [[ "$(echo "$LAYOUT" | jq '.stalls | length')" -eq 0 ]]; then
  # régi adatbázis, még nincs elrendezés: egy kis, 4 standos rács a teszthez
  req PUT "/api/markets/$MARKET_ID/layout" "$ADMIN" '{"columns":8,"rows":4,"stalls":[{"number":1,"x":0,"y":0,"width":2,"height":2},{"number":2,"x":2,"y":0,"width":2,"height":2},{"number":3,"x":4,"y":0,"width":2,"height":2},{"number":4,"x":6,"y":0,"width":2,"height":2}]}'
  expect 200 "elrendezés mentése"
  LAYOUT="$BODY"
fi
req PUT "/api/markets/$MARKET_ID/layout" "$PRODUCER" '{"columns":8,"rows":4,"stalls":[]}'; expect 403 "árus nem módosíthatja az elrendezést"
req PUT "/api/markets/$MARKET_ID/layout" "$ADMIN" '{"columns":8,"rows":4,"stalls":[{"number":1,"x":0,"y":0,"width":3,"height":2},{"number":2,"x":2,"y":0,"width":2,"height":2}]}'
expect 400 "átfedő standok 400"
FREE_STALL="$(echo "$LAYOUT" | jq '[.stalls[] | select(.producerId == null)][0].number')"
if [[ "$FREE_STALL" != "null" ]]; then
  req PUT "/api/producers/$PRODUCER_ID/stall" "$ADMIN" "{\"stallNumber\":$FREE_STALL}"; expect 200 "fix stand kiosztása"
  assert_eq "$(echo "$BODY" | jq .stallNumber)" "$FREE_STALL" "az árus megkapta a standot"
  req PUT /api/producers/me "$PRODUCER" "{\"name\":\"Teszt Gazdaság $SUFFIX\",\"stallNumber\":999}"; expect 403 "az árus nem írhatja át a standját"
  req GET "/api/markets/$MARKET_ID/layout"; expect 200 "térkép a kiosztás után"
  assert_eq "$(echo "$BODY" | jq --argjson n "$FREE_STALL" -r '.stalls[] | select(.number==$n) | .producerId')" "$PRODUCER_ID" "a térképen az árus a standján"
  TAKEN_STALL="$(echo "$BODY" | jq --argjson p "$PRODUCER_ID" '[.stalls[] | select(.producerId != null and .producerId != $p)][0].number')"
  if [[ "$TAKEN_STALL" != "null" ]]; then
    req PUT "/api/producers/$PRODUCER_ID/stall" "$ADMIN" "{\"stallNumber\":$TAKEN_STALL}"; expect 409 "foglalt stand nem osztható ki csere nélkül"
  fi
fi
req PUT "/api/producers/$PRODUCER_ID/stall" "$ADMIN" '{"stallNumber":998}'; expect 409 "nem létező stand nem osztható ki"
req GET "/api/products?marketId=$MARKET_ID&producerId=$PRODUCER_ID"; expect 200 "termékek piac és árus szerint"
req GET "/api/products?marketId=999999"; expect 200 "ismeretlen piac"
assert_eq "$(echo "$BODY" | jq '.items | length')" 0 "más piac szűrője üres"

echo "== Admin"
req GET /api/audit "$ADMIN"; expect 200 "auditnapló"
[[ "$(echo "$BODY" | jq '.items | length')" -gt 0 ]] || { echo "HIBA: üres auditnapló"; exit 1; }
req GET /api/audit "$CUSTOMER"; expect 403 "az auditnapló vevőnek tiltott"
req GET /api/orders/disputed "$CUSTOMER"; expect 403 "a vitatott lista vevőnek tiltott"

echo "== Fiók letiltása (a még érvényes token is azonnal érvénytelen)"
req GET /api/me "$CUSTOMER"; expect 200 "saját profil"
CUSTOMER_ID="$(echo "$BODY" | jq -r .id)"
req POST "/api/users/$CUSTOMER_ID/deactivate" "$CUSTOMER"; expect 403 "vevő nem tilthat le fiókot"
req POST "/api/users/$CUSTOMER_ID/deactivate" "$ADMIN"; expect 200 "vevő fiókjának letiltása"
req GET /api/me "$CUSTOMER"; expect 401 "a letiltott fiók régi tokenje 401-et ad"
req POST /api/auth/login "" "{\"email\":\"$CUSTOMER_EMAIL\",\"password\":\"$PASSWORD\"}"; expect 401 "letiltott fiók nem tud belépni"
req GET /api/me "$ADMIN"; expect 200 "admin saját profil"
ADMIN_ID="$(echo "$BODY" | jq -r .id)"
req POST "/api/users/$ADMIN_ID/deactivate" "$ADMIN"; expect 409 "a saját fiók nem tiltható le"
req POST "/api/users/$CUSTOMER_ID/activate" "$ADMIN"; expect 200 "vevő fiókjának újraengedélyezése"
CUSTOMER="$(login "$CUSTOMER_EMAIL" "$PASSWORD")"
req GET /api/me "$CUSTOMER"; expect 200 "újraengedélyezés után a belépés működik"

echo
echo "Minden füstteszt sikeres."
