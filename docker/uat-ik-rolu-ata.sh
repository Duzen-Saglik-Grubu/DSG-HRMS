#!/usr/bin/env bash
#
# UAT: Insan Kaynaklari birimindeki aktif personele "IK Kimlik Islemleri" rolunu atar (#383).
#
# Kullanim (depo kokunde):
#   ./docker/uat-ik-rolu-ata.sh                              # yalnizca raporlar; hicbir sey yazmaz
#   ./docker/uat-ik-rolu-ata.sh --yalniz-sayi                # ad yazdirmadan yalnizca sayilar
#   ./docker/uat-ik-rolu-ata.sh --uygula --taahhut-alindi    # rolu atar
#
# Neden betik? Rol yonetimi ekrani T4'te gelir (KR-128, SYG-KMLK-074). O zamana kadar rol
# atamanin uygulamada bir yolu yoktur; atama veritabaninda yapilir.
#
# Ne yapar:
#   1. Birimdeki aktif kisileri bulur: aktif istihdam ve birim adi asagidaki birim olan acik atama
#   2. Her kisi icin hesabi var mi, aktif mi, rolu var mi raporlar
#   3. --uygula ile hesabi aktif olup rolu olmayanlara rolu atar. Atama, uygulamanin yazdigiyla
#      ayni bicimde denetim izine (audit.change_log, "UserRole" eklendi) AYNI ISLEMDE yazilir;
#      izleme kimligi bu betigin adidir. SQL ile yapilan atama boylece izsiz kalmaz (KR-060).
#
# Tekrar calistirmak guvenlidir: rolu olana yeniden atama yapilmaz. Hesabi olmayan kisi raporda
# "hesap yok" gorunur; uye olduktan sonra betik yeniden calistirilir.
#
# KVKK: IK rolu kan grubunu gorme iznini (personnel.person.view_sensitive) de icerir. Rol
# atanmadan once kisinin gizlilik taahhudu alinmis ve kaydi tutuluyor olmalidir (SYG-PERS-065,
# BK-29). --taahhut-alindi bunu acikca onaylamaktir; onsuz --uygula calismaz.
#
# Izinler her istekte veritabanindan okunur: rol hemen gecerli olur; kullanicinin ekraninda menu
# icin sayfayi yenilemesi yeterlidir.

set -euo pipefail

SERVER="${SERVER:-root@192.168.3.202}"
SECRETS_FILE="/opt/dsg-hrms/secrets/.env.uat"
DB_CONTAINER="dsg-hrms-uat-postgres"

# Hedef birim ve firmalar (LOGO'daki adlar). Birim baska bir firmada da varsa buraya eklenir.
UNIT_NAME='İNSAN KAYNAKLARI'
COMPANIES=('DÜZEN A.Ş.' 'DÜZEN LTD')
ROLE_CODE='hr-identity-operations'

fail() { printf '\nHATA: %s\n' "$1" >&2; exit 1; }

apply=false
consent=false
counts_only=false
for arg in "$@"; do
  case "$arg" in
    --uygula) apply=true ;;
    --taahhut-alindi) consent=true ;;
    --yalniz-sayi) counts_only=true ;;
    *) fail "Bilinmeyen secenek: $arg" ;;
  esac
done
if [[ "$apply" == true && "$consent" != true ]]; then
  fail "--uygula yalnizca --taahhut-alindi ile calisir: IK rolu kan grubunu gorme iznini icerir; once gizlilik taahhudu alinmalidir (SYG-PERS-065)."
fi

sql_literal() { printf "'%s'" "${1//\'/\'\'}"; }
company_list=$(for c in "${COMPANIES[@]}"; do sql_literal "$c"; printf ','; done)
company_list=${company_list%,}

if [[ "$counts_only" == true ]]; then
  report="SELECT count(*) AS aday,
       count(*) FILTER (WHERE account_id IS NOT NULL) AS hesabi_var,
       count(*) FILTER (WHERE has_role) AS rolu_var,
       count(*) FILTER (WHERE account_id IS NOT NULL AND status = 1 AND NOT has_role) AS atanacak
FROM candidate;"
else
  report="SELECT full_name AS \"Ad soyad\", company_name AS \"Firma\",
       CASE WHEN account_id IS NULL THEN 'hesap yok'
            WHEN status <> 1 THEN 'hesap pasif'
            ELSE 'hesap aktif' END AS \"Hesap\",
       CASE WHEN has_role THEN 'rol var'
            WHEN account_id IS NOT NULL AND status = 1 THEN 'atanacak'
            ELSE '-' END AS \"Rol\"
FROM candidate ORDER BY full_name;"
fi

if [[ "$apply" == true ]]; then
  write="WITH yeni AS (
  INSERT INTO identity.user_role (user_account_id, role_id, created_at, public_id)
  SELECT a.account_id, r.id, now(), gen_random_uuid()
  FROM candidate a CROSS JOIN identity.role r
  WHERE r.code = $(sql_literal "$ROLE_CODE") AND a.account_id IS NOT NULL AND a.status = 1 AND NOT a.has_role
  RETURNING public_id, role_id, user_account_id
)
INSERT INTO audit.change_log (occurred_at, user_account_id, entity_name, entity_id, operation, changes, trace_id)
SELECT now(), NULL, 'UserRole', y.public_id, 'Insert',
       jsonb_build_object(
         'RoleId', jsonb_build_object('new', y.role_id, 'old', NULL),
         'PublicId', jsonb_build_object('new', y.public_id, 'old', NULL),
         'UserAccountId', jsonb_build_object('new', y.user_account_id, 'old', NULL)),
       'uat-ik-rolu-ata.sh'
FROM yeni y;
SELECT count(*) AS \"atanan\" FROM audit.change_log WHERE trace_id = 'uat-ik-rolu-ata.sh' AND occurred_at = now();"
  finish='COMMIT;'
else
  write=''
  finish='ROLLBACK;'
fi

sql="\\set ON_ERROR_STOP on
BEGIN;
CREATE TEMP TABLE candidate ON COMMIT DROP AS
SELECT DISTINCT ON (p.id)
       p.first_name || ' ' || p.last_name AS full_name, c.name AS company_name,
       ua.id AS account_id, ua.status AS status,
       EXISTS (SELECT 1 FROM identity.user_role ur JOIN identity.role r ON r.id = ur.role_id
               WHERE ur.user_account_id = ua.id AND r.code = $(sql_literal "$ROLE_CODE")) AS has_role
FROM personnel.employment e
JOIN personnel.person p ON p.id = e.person_id
JOIN personnel.employment_assignment a ON a.employment_id = e.id AND a.valid_to IS NULL
JOIN organization.department d ON d.id = a.department_id
JOIN organization.company c ON c.id = a.company_id
LEFT JOIN identity.user_account ua ON ua.person_id = p.id
WHERE e.is_active AND d.name = $(sql_literal "$UNIT_NAME") AND c.name IN ($company_list)
ORDER BY p.id;
$report
$write
$finish"

if [[ "$apply" == true ]]; then
  echo "Rol atanacak: $ROLE_CODE (birim: $UNIT_NAME; firmalar: ${COMPANIES[*]})"
else
  echo "Yalnizca rapor; hicbir sey yazilmaz. Atamak icin: --uygula --taahhut-alindi"
fi

# Parola yazdirilmaz; baglanti sunucudaki sir dosyasindan okunur.
printf '%s\n' "$sql" | ssh "$SERVER" "set -a && . $SECRETS_FILE && set +a \
  && docker exec -i $DB_CONTAINER psql -U \"\$POSTGRES_USER\" -d \"\$POSTGRES_DB\" -q -X"
