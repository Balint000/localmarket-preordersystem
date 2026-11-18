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

login() { req POST /api/auth/login "" "{\"email\":\"$1\",\"password\":\"$2\"}"; expect 200 "bejelentkezés: $1"; echo "$BODY" | jq -r .token; }
order_status() { req GET "/api/orders/$2" "$1"; expect 200 "rendelés lekérése #$2"; echo "$BODY" | jq -r .status; }
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
req POST /api/auth/register-producer "" "{\"firstName\":\"Árus\",\"lastName\":\"Teszt\",\"email\":\"$PRODUCER_EMAIL\",\"password\":\"$PASSWORD\",\"phoneNumber\":\"+36301234567\",\"producerName\":\"Teszt Gazdaság $SUFFIX\",\"marketId\":$MARKET_ID,\"stallNumber\":7}"
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

echo "== Rendelés leadása és teljesítése"
req POST /api/cart/items "$CUSTOMER" "{\"productId\":$PRODUCT_ID,\"quantity\":2.5}"; expect 200 "kosárba tétel"
req GET /api/cart "$CUSTOMER"; expect 200 "kosár lekérése"
req POST /api/orders "$CUSTOMER" "{\"paymentMethod\":\"Cash\",\"pickups\":[{\"producerId\":$PRODUCER_ID,\"pickupSlotId\":$SLOT_ID}]}"
expect 200 "rendelés leadása"
ORDER_ID="$(echo "$BODY" | jq -r '(if type=="array" then .[0] else . end).id')"
assert_eq "$(order_status "$CUSTOMER" "$ORDER_ID")" Pending "a rendelés függő"
req GET "/api/export/orders/$ORDER_ID" "$CUSTOMER"; expect 200 "export formátum megadása nélkül (alapból JSON)"
req GET "/api/export/orders/$ORDER_ID?format=Csv" "$CUSTOMER"; expect 200 "export CSV-ben"
req POST "/api/orders/$ORDER_ID/accept" "$PRODUCER"; expect 200 "árus elfogadja"
req POST "/api/orders/$ORDER_ID/ready" "$PRODUCER"; expect 200 "kész az átvételre"
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

echo "== Elutasítás és részleges teljesítés"
req POST /api/cart/items "$CUSTOMER" "{\"productId\":$PRODUCT_ID,\"quantity\":4}"; expect 200 "kosárba tétel (2. rendelés)"
req POST /api/orders "$CUSTOMER" "{\"paymentMethod\":\"Cash\",\"pickups\":[{\"producerId\":$PRODUCER_ID,\"pickupSlotId\":$SLOT_ID}]}"
expect 200 "2. rendelés leadása"
ORDER2_ID="$(echo "$BODY" | jq -r '(if type=="array" then .[0] else . end).id')"
req POST "/api/orders/$ORDER2_ID/accept-partially" "$PRODUCER" "{\"lines\":[{\"productId\":$PRODUCT_ID,\"quantity\":1}]}"
expect 200 "részleges teljesítés"
assert_eq "$(order_status "$CUSTOMER" "$ORDER2_ID")" PartiallyFulfilled "a rendelés részben teljesített"
req POST "/api/orders/$ORDER2_ID/cancel" "$CUSTOMER"; expect 200 "vevő lemondja"

echo "== Admin"
req GET /api/audit "$ADMIN"; expect 200 "auditnapló"
[[ "$(echo "$BODY" | jq '.items | length')" -gt 0 ]] || { echo "HIBA: üres auditnapló"; exit 1; }
req GET /api/audit "$CUSTOMER"; expect 403 "az auditnapló vevőnek tiltott"
req GET /api/orders/disputed "$CUSTOMER"; expect 403 "a vitatott lista vevőnek tiltott"

echo
echo "Minden füstteszt sikeres."
