# راهنمای کامل استقرار (Deploy) SmartCall

<div dir="rtl">

این سند تمام مراحل استقرار SmartCall را از صفر تا اجرای کامل روی سرور توضیح می‌دهد. دو روش پشتیبانی می‌شود: **Docker (پیشنهادی)** و **نصب دستی**.

---

## ۱. پیش‌نیازها

### سخت‌افزار پیشنهادی
| مورد | حداقل | پیشنهادی |
|---|---|---|
| CPU | ۲ هسته | ۴ هسته |
| RAM | ۲ گیگابایت | ۴ گیگابایت |
| دیسک | ۲۰ گیگابایت | ۵۰+ گیگابایت (برای فایل‌های ضبط‌شده) |

### نرم‌افزار
- سرور لینوکس (Ubuntu 22.04 یا بالاتر پیشنهاد می‌شود)
- یک **دامنه** با رکورد DNS اشاره‌کننده به IP سرور (مثلاً `call.example.com`)
- **HTTPS الزامی است** — مرورگرها بدون HTTPS اجازهٔ دسترسی به دوربین/میکروفن (WebRTC) را نمی‌دهند
- کلید API معتبر OpenAI (یا هر Endpoint سازگار با OpenAI)

### روش Docker
- Docker Engine نسخهٔ ۲۴ به بالا و Docker Compose v2:

```bash
curl -fsSL https://get.docker.com | sh
```

### روش دستی
- .NET 8 SDK/Runtime
- Node.js 20+ (فقط برای Build فرانت‌اند)
- PostgreSQL 16
- nginx

---

## ۲. استقرار با Docker (روش پیشنهادی)

### ۲.۱. دریافت کد

```bash
git clone <آدرس مخزن>
cd smartcall
```

### ۲.۲. تنظیم متغیرهای محیطی

```bash
cp .env.example .env
nano .env
```

مقادیر زیر را **حتماً** تغییر دهید:

| متغیر | توضیح |
|---|---|
| `DB_PASSWORD` | پسورد PostgreSQL |
| `JWT_SECRET` | رشتهٔ تصادفی حداقل ۳۲ کاراکتری برای امضای توکن‌ها |
| `DATA_KEY` | کلید رمزنگاری مقادیر حساس (کلید OpenAI و پسورد SMTP) در دیتابیس |
| `PUBLIC_URL` | آدرس عمومی سایت، مثلاً `https://call.example.com` |
| `TURN_PASSWORD` | پسورد سرور TURN |

برای تولید مقادیر تصادفی امن:

```bash
openssl rand -base64 48
```

### ۲.۳. اجرا

```bash
docker compose up -d --build
```

چهار کانتینر بالا می‌آید:

| سرویس | نقش |
|---|---|
| `db` | PostgreSQL 16 (دادهٔ ماندگار در Volume به‌نام `db-data`) |
| `backend` | API با .NET 8 (فایل‌های ضبط در Volume `recordings`) |
| `frontend` | nginx + خروجی Build شدهٔ React |
| `coturn` | سرور TURN برای عبور WebRTC از NAT/فایروال |

بررسی سلامت:

```bash
docker compose ps
docker compose logs -f backend
curl http://localhost/api/public/install-status
```

### ۲.۴. فعال‌سازی HTTPS

سادگی‌ترین راه، قرار دادن یک Reverse Proxy با گواهی Let's Encrypt جلوی کانتینر frontend است. نمونه با **Caddy** (خودش گواهی می‌گیرد و تمدید می‌کند):

```bash
# در .env مقدار HTTP_PORT را به 8081 تغییر دهید و docker compose up -d بزنید
sudo apt install -y caddy
```

فایل `/etc/caddy/Caddyfile`:

```
call.example.com {
    reverse_proxy 127.0.0.1:8081
}
```

```bash
sudo systemctl restart caddy
```

یا با **nginx + certbot**:

```bash
sudo apt install -y nginx certbot python3-certbot-nginx
sudo certbot --nginx -d call.example.com
```

نمونهٔ کانفیگ nginx (توجه به بخش WebSocket برای SignalR):

```nginx
server {
    server_name call.example.com;
    listen 443 ssl;
    # ... مسیر گواهی‌ها که certbot اضافه می‌کند ...

    client_max_body_size 100m;

    location / {
        proxy_pass http://127.0.0.1:8081;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /hubs/ {
        proxy_pass http://127.0.0.1:8081;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_read_timeout 3600s;
    }
}
```

### ۲.۵. باز کردن پورت‌های فایروال

```bash
sudo ufw allow 80/tcp     # HTTP (ریدایرکت به HTTPS)
sudo ufw allow 443/tcp    # HTTPS
sudo ufw allow 3478/tcp   # TURN
sudo ufw allow 3478/udp   # TURN
sudo ufw allow 49152:65535/udp   # محدودهٔ Relay سرور TURN
```

### ۲.۶. ویزارد نصب (مانند وردپرس)

آدرس سایت را در مرورگر باز کنید. چون هنوز نصبی انجام نشده، **ویزارد نصب** به‌صورت خودکار نمایش داده می‌شود:

1. **اتصال دیتابیس:** اگر از docker-compose استفاده می‌کنید مقادیر پیش‌فرض این‌هاست:
   - Host: `db`  — Port: `5432` — Database: `smartcall` — Username: `smartcall` — Password: همان `DB_PASSWORD`
2. روی «**تست اتصال**» بزنید و منتظر تأیید بمانید.
3. **حساب سوپر ادمین:** نام، ایمیل و گذرواژهٔ ادمین اولیه را وارد کنید.
4. روی «**نصب و راه‌اندازی**» بزنید — جدول‌ها ساخته، داده‌های اولیه (زبان‌ها، فونت‌ها، محتوای صفحهٔ اصلی) Seed و حساب ادمین ایجاد می‌شود.

> **نکته:** اگر متغیر `ConnectionStrings__Default` در docker-compose تنظیم شده باشد، Backend مستقیماً به همان دیتابیس وصل می‌شود؛ ویزارد فقط برای Migration/Seed و ساخت ادمین استفاده می‌شود.

### ۲.۷. پیکربندی پس از نصب (پنل سوپر ادمین)

با حساب ادمین وارد شوید و به **پنل مدیریت** بروید:

1. **تنظیمات هوش مصنوعی:**
   - کلید API و در صورت نیاز Base URL (برای پروکسی یا Azure OpenAI) را وارد کنید.
   - نام مدل‌های STT، ترجمهٔ متن، TTS و Realtime را وارد کنید (نام مدل‌ها عمداً در کد Hardcode نشده‌اند).
   - روش فعال (Cascade یا Realtime) را انتخاب کنید.
   - روی «**تست اتصال**» بزنید — فهرست مدل‌های در دسترس نمایش داده می‌شود.
2. **سرور ایمیل (SMTP):** مشخصات SMTP را وارد و با «ارسال ایمیل تست» بررسی کنید (برای «فراموشی پسورد» لازم است).
3. **زبان و عمومی:** زبان پیش‌فرض داشبرد و آدرس سرورهای STUN/TURN را تنظیم کنید. برای coturn این compose:

```json
[
  { "urls": "stun:call.example.com:3478" },
  { "urls": "turn:call.example.com:3478", "username": "smartcall", "credential": "TURN_PASSWORD شما" }
]
```

---

## ۳. استقرار نسخهٔ جدید (بدون از دست دادن داده)

وقتی کد جدید منتشر می‌شود:

```bash
cd smartcall
git pull
docker compose up -d --build
```

سپس **یکی** از دو راه:

- در صفحهٔ نصب (`/install`) روی دکمهٔ «**استقرار نسخهٔ جدید برنامه**» بزنید — فقط Migration های جدید (Incremental) اجرا می‌شود و هیچ داده‌ای پاک نمی‌شود؛ **یا**
- از خط فرمان:

```bash
curl -X POST https://call.example.com/api/install/upgrade \
  -H "Content-Type: application/json" \
  -d '{"host":"db","port":5432,"database":"smartcall","username":"smartcall","password":"DB_PASSWORD"}'
```

> دادهٔ دیتابیس در Volume ماندگار `db-data` و فایل‌های ضبط در `recordings` نگهداری می‌شوند و با rebuild کانتینرها از بین نمی‌روند. **هرگز** `docker compose down -v` اجرا نکنید مگر بخواهید همه‌چیز پاک شود.

---

## ۴. نصب دستی (بدون Docker)

### ۴.۱. دیتابیس

```bash
sudo apt install -y postgresql-16
sudo -u postgres psql -c "CREATE USER smartcall WITH PASSWORD 'رمز-قوی';"
sudo -u postgres psql -c "CREATE DATABASE smartcall OWNER smartcall;"
```

### ۴.۲. Backend

```bash
cd backend
dotnet publish src/SmartCall.Api/SmartCall.Api.csproj -c Release -o /opt/smartcall/api
```

فایل `/opt/smartcall/api/appsettings.Production.json`:

```json
{
  "ConnectionStrings": { "Default": "Host=localhost;Port=5432;Database=smartcall;Username=smartcall;Password=رمز-قوی" },
  "Jwt": { "Secret": "رشتهٔ تصادفی ۳۲+ کاراکتری" },
  "SMARTCALL_DATA_KEY": "رشتهٔ تصادفی دیگر",
  "Storage": { "RootPath": "/var/lib/smartcall/recordings" },
  "Cors": { "AllowedOrigins": "https://call.example.com" }
}
```

سرویس systemd — فایل `/etc/systemd/system/smartcall.service`:

```ini
[Unit]
Description=SmartCall API
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/smartcall/api
ExecStart=/usr/bin/dotnet /opt/smartcall/api/SmartCall.Api.dll
Environment=ASPNETCORE_URLS=http://127.0.0.1:8080
Restart=always
User=www-data

[Install]
WantedBy=multi-user.target
```

```bash
sudo mkdir -p /var/lib/smartcall/recordings && sudo chown www-data /var/lib/smartcall/recordings
sudo systemctl enable --now smartcall
```

### ۴.۳. Frontend

```bash
cd frontend
npm install
npm run build
sudo cp -r dist/* /var/www/smartcall/
```

nginx را مطابق نمونهٔ بخش ۲.۴ تنظیم کنید (ریشهٔ سایت `/var/www/smartcall`، پروکسی `/api/` و `/hubs/` به `127.0.0.1:8080`).

### ۴.۴. TURN (coturn)

```bash
sudo apt install -y coturn
```

فایل `/etc/turnserver.conf`:

```
listening-port=3478
realm=smartcall
user=smartcall:رمز-TURN
lt-cred-mech
fingerprint
```

```bash
sudo systemctl enable --now coturn
```

سپس ویزارد نصب را مانند بخش ۲.۶ اجرا کنید.

---

## ۵. Migration های EF Core (برای توسعه‌دهندگان)

پس از تغییر Entity ها، Migration جدید بسازید تا مسیر «استقرار نسخهٔ جدید» بتواند به‌صورت Incremental اجرا شود:

```bash
cd backend
dotnet tool install --global dotnet-ef
dotnet ef migrations add NameOfChange \
  --project src/SmartCall.Infrastructure \
  --startup-project src/SmartCall.Api
```

> اگر هیچ Migration کامپایل‌شده‌ای وجود نداشته باشد، نصب اولیه Schema را مستقیماً از مدل می‌سازد (`EnsureCreated`)، اما برای بروزرسانی‌های بعدی حتماً Migration اضافه کنید.

---

## ۶. پشتیبان‌گیری

```bash
# دیتابیس
docker compose exec db pg_dump -U smartcall smartcall | gzip > backup-$(date +%F).sql.gz

# فایل‌های ضبط‌شده
docker run --rm -v smartcall_recordings:/data -v $(pwd):/backup alpine \
  tar czf /backup/recordings-$(date +%F).tar.gz -C /data .
```

بازیابی دیتابیس:

```bash
gunzip -c backup-2026-01-01.sql.gz | docker compose exec -T db psql -U smartcall smartcall
```

---

## ۷. عیب‌یابی

| مشکل | علت محتمل / راه‌حل |
|---|---|
| دوربین/میکروفن کار نمی‌کند | سایت باید حتماً HTTPS باشد (به‌جز `localhost`) |
| تماس وصل نمی‌شود (ویدیو سیاه) | سرور TURN در دسترس نیست؛ پورت‌های 3478 و محدودهٔ UDP را در فایروال باز کنید و JSON سرورهای ICE را در پنل ادمین بررسی کنید |
| ترجمه خطای «API key is not configured» می‌دهد | کلید OpenAI را در پنل ادمین وارد و «تست اتصال» را اجرا کنید |
| ترجمه خطای «No STT model configured» می‌دهد | نام مدل‌ها در پنل ادمین خالی است؛ آن‌ها را تنظیم کنید |
| ایمیل ریست پسورد نمی‌رسد | تنظیمات SMTP را در پنل ادمین وارد و «ارسال ایمیل تست» را بزنید |
| SignalR مدام قطع می‌شود | پروکسی باید WebSocket را پاس بدهد (هدرهای `Upgrade`/`Connection` در بخش ۲.۴) و `proxy_read_timeout` بالا باشد |
| خطای 413 هنگام ضبط | `client_max_body_size` در nginx را افزایش دهید |

لاگ‌ها:

```bash
docker compose logs -f backend    # لاگ کامل Serilog
docker compose logs -f coturn
```

---

## ۸. چک‌لیست امنیتی نهایی

- [ ] HTTPS فعال است و HTTP به HTTPS ریدایرکت می‌شود
- [ ] `JWT_SECRET` و `DATA_KEY` تصادفی و طولانی‌اند و در Git نیستند
- [ ] پسورد دیتابیس قوی است و پورت 5432 از بیرون بسته است
- [ ] پسورد TURN تغییر کرده است
- [ ] پشتیبان‌گیری خودکار (cron) از دیتابیس و فایل‌های ضبط تنظیم شده است
- [ ] قوانین حریم خصوصی مربوط به ضبط مکالمه و ورود ادمین به تماس، مطابق بازار هدف بررسی شده است

</div>
