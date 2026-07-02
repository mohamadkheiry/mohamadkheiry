# SmartCall — پلتفرم مکالمهٔ تصویری با ترجمهٔ صوتی بلادرنگ

<div dir="rtl">

SmartCall یک پلتفرم مکالمهٔ تصویری دونفره (۱:۱) است که صدای هر طرف را به‌صورت بلادرنگ به زبان انتخابی طرف مقابل ترجمه و پخش می‌کند. هر شرکت‌کننده **مستقل از طرف دیگر** زبانی را که می‌خواهد بشنود انتخاب می‌کند.

## امکانات

- 🎥 تماس تصویری/صوتی دونفره با WebRTC (سیگنالینگ با SignalR)
- 🌐 ترجمهٔ صوتی بلادرنگ با دو روش قابل انتخاب از پنل ادمین:
  - **Cascade:** گفتار → متن (STT) → ترجمهٔ متن → گفتار (TTS) — با زیرنویس زنده
  - **Realtime:** ترجمهٔ گفتار‌به‌گفتار مستقیم با OpenAI Realtime API
- 🔊 کنترل مستقل صدای اصلی و صدای ترجمه‌شدهٔ طرف مقابل (قطع/وصل + دو اسلایدر Volume)
- 🖥️ اشتراک‌گذاری صفحه
- ⏺️ ضبط مکالمه با آپلود تدریجی روی سرور و دسترسی سوپر ادمین
- 🛠️ پنل سوپر ادمین کامل: تنظیمات OpenAI (کلید/Base URL/نام همهٔ مدل‌ها + تست اتصال)، مدیریت زبان‌های ترجمه، مدیریت تماس‌ها و ورود مستقیم به تماس زنده، گزارش مصرف توکن، مدیریت فونت (۱۰ فارسی + ۱۰ انگلیسی)، CMS صفحهٔ اصلی، تنظیمات SMTP با ایمیل تست
- 🧙 نصب‌کنندهٔ وردپرس‌مانند + دکمهٔ «استقرار نسخهٔ جدید» بدون از دست رفتن داده
- 🌍 دوزبانه: فارسی (RTL، پیش‌فرض) و انگلیسی (LTR)، کاملاً Responsive

## پشتهٔ فناوری

| لایه | فناوری |
|---|---|
| Backend | .NET 8، MediatR (CQRS)، EF Core، SignalR، PostgreSQL |
| Frontend | React 18 + TypeScript، Vite، react-i18next، Lucide Icons |
| AI | OpenAI API (قابل تعویض با هر Endpoint سازگار) |
| زیرساخت | Docker، nginx، coturn (TURN) |

## ساختار پروژه

```
smartcall/
├── backend/
│   ├── src/
│   │   ├── SmartCall.Domain/          # موجودیت‌ها
│   │   ├── SmartCall.Application/     # Command/Query های MediatR
│   │   ├── SmartCall.Infrastructure/  # EF Core، OpenAI، SMTP، Installer
│   │   └── SmartCall.Api/             # کنترلرها، SignalR Hub
│   └── tests/SmartCall.Tests/         # تست‌های واحد
├── frontend/                          # React + TypeScript
├── docker-compose.yml
├── DEPLOYMENT.md                      # ← آموزش کامل دیپلوی
└── .env.example
```

## اجرای سریع (Docker)

```bash
cp .env.example .env   # مقادیر را تغییر دهید
docker compose up -d --build
```

سپس `http://localhost` را باز کنید؛ ویزارد نصب اجرا می‌شود.

## اجرای محیط توسعه

```bash
# Backend (نیازمند .NET 8 SDK و PostgreSQL)
cd backend
dotnet run --project src/SmartCall.Api

# Frontend
cd frontend
npm install
npm run dev   # http://localhost:5173
```

## تست‌ها

```bash
cd backend && dotnet test
```

راهنمای کامل استقرار در [DEPLOYMENT.md](DEPLOYMENT.md) آمده است.

</div>
