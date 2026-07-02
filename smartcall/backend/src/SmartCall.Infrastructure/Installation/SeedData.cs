using SmartCall.Domain.Entities;

namespace SmartCall.Infrastructure.Installation;

public static class SeedData
{
    /// <summary>
    /// Initial language list based on languages commonly supported by OpenAI
    /// audio models. Admin can add/remove entries as OpenAI's support evolves.
    /// </summary>
    public static IEnumerable<TranslationLanguage> DefaultLanguages()
    {
        var list = new (string Code, string En, string Native, bool Rtl)[]
        {
            ("fa", "Persian", "فارسی", true),
            ("en", "English", "English", false),
            ("ar", "Arabic", "العربية", true),
            ("de", "German", "Deutsch", false),
            ("fr", "French", "Français", false),
            ("es", "Spanish", "Español", false),
            ("it", "Italian", "Italiano", false),
            ("pt", "Portuguese", "Português", false),
            ("ru", "Russian", "Русский", false),
            ("tr", "Turkish", "Türkçe", false),
            ("zh", "Chinese", "中文", false),
            ("ja", "Japanese", "日本語", false),
            ("ko", "Korean", "한국어", false),
            ("hi", "Hindi", "हिन्दी", false),
            ("ur", "Urdu", "اردو", true),
            ("nl", "Dutch", "Nederlands", false),
            ("pl", "Polish", "Polski", false),
            ("sv", "Swedish", "Svenska", false),
            ("uk", "Ukrainian", "Українська", false),
            ("el", "Greek", "Ελληνικά", false),
            ("he", "Hebrew", "עברית", true),
            ("id", "Indonesian", "Bahasa Indonesia", false),
            ("ms", "Malay", "Bahasa Melayu", false),
            ("th", "Thai", "ไทย", false),
            ("vi", "Vietnamese", "Tiếng Việt", false),
            ("cs", "Czech", "Čeština", false),
            ("da", "Danish", "Dansk", false),
            ("fi", "Finnish", "Suomi", false),
            ("hu", "Hungarian", "Magyar", false),
            ("no", "Norwegian", "Norsk", false),
            ("ro", "Romanian", "Română", false),
            ("az", "Azerbaijani", "Azərbaycanca", false),
            ("hy", "Armenian", "Հայերեն", false),
            ("ka", "Georgian", "ქართული", false),
            ("kk", "Kazakh", "Қазақша", false),
            ("bg", "Bulgarian", "Български", false),
            ("hr", "Croatian", "Hrvatski", false),
            ("sk", "Slovak", "Slovenčina", false),
            ("sl", "Slovenian", "Slovenščina", false),
            ("sr", "Serbian", "Српски", false),
            ("lt", "Lithuanian", "Lietuvių", false),
            ("lv", "Latvian", "Latviešu", false),
            ("et", "Estonian", "Eesti", false),
            ("sw", "Swahili", "Kiswahili", false),
            ("tl", "Tagalog", "Tagalog", false),
            ("bn", "Bengali", "বাংলা", false),
            ("ta", "Tamil", "தமிழ்", false),
            ("mr", "Marathi", "मराठी", false),
            ("ne", "Nepali", "नेपाली", false),
            ("ps", "Pashto", "پښتو", true),
        };

        return list.Select((l, i) => new TranslationLanguage
        {
            Id = Guid.NewGuid(),
            Code = l.Code,
            EnglishName = l.En,
            NativeName = l.Native,
            IsRtl = l.Rtl,
            IsActive = true,
            SortOrder = i
        });
    }

    /// <summary>10 Persian fonts (B Nazanin and B Yekan required) + 10 English fonts.</summary>
    public static IEnumerable<Font> DefaultFonts()
    {
        var persian = new (string Name, string Family)[]
        {
            ("B Nazanin", "'B Nazanin', 'Vazirmatn', sans-serif"),
            ("B Yekan", "'B Yekan', 'Vazirmatn', sans-serif"),
            ("Vazirmatn", "'Vazirmatn', sans-serif"),
            ("IRANSans", "'IRANSans', 'Vazirmatn', sans-serif"),
            ("Shabnam", "'Shabnam', 'Vazirmatn', sans-serif"),
            ("Sahel", "'Sahel', 'Vazirmatn', sans-serif"),
            ("Samim", "'Samim', 'Vazirmatn', sans-serif"),
            ("Estedad", "'Estedad', 'Vazirmatn', sans-serif"),
            ("Yekan Bakh", "'Yekan Bakh', 'Vazirmatn', sans-serif"),
            ("Dana", "'Dana', 'Vazirmatn', sans-serif"),
        };

        var english = new (string Name, string Family)[]
        {
            ("Inter", "'Inter', system-ui, sans-serif"),
            ("Roboto", "'Roboto', system-ui, sans-serif"),
            ("Open Sans", "'Open Sans', system-ui, sans-serif"),
            ("Lato", "'Lato', system-ui, sans-serif"),
            ("Montserrat", "'Montserrat', system-ui, sans-serif"),
            ("Poppins", "'Poppins', system-ui, sans-serif"),
            ("Source Sans 3", "'Source Sans 3', system-ui, sans-serif"),
            ("Nunito", "'Nunito', system-ui, sans-serif"),
            ("Work Sans", "'Work Sans', system-ui, sans-serif"),
            ("IBM Plex Sans", "'IBM Plex Sans', system-ui, sans-serif"),
        };

        return persian.Select(f => new Font { Id = Guid.NewGuid(), Name = f.Name, Language = "fa", FontFamily = f.Family })
            .Concat(english.Select(f => new Font { Id = Guid.NewGuid(), Name = f.Name, Language = "en", FontFamily = f.Family }));
    }

    public static IEnumerable<LandingPageContent> DefaultLandingContent()
    {
        var fa = new (string Key, string Content)[]
        {
            ("hero.title", "با هر زبانی، با همه صحبت کنید"),
            ("hero.subtitle", "مکالمهٔ تصویری زنده با ترجمهٔ صوتی بلادرنگ مبتنی بر هوش مصنوعی — مانع زبانی را برای همیشه فراموش کنید."),
            ("hero.cta", "شروع رایگان"),
            ("features.title", "امکانات SmartCall"),
            ("features.items", """[
                {"icon":"video","title":"تماس تصویری باکیفیت","text":"مکالمهٔ تصویری دونفره با کیفیت و پایداری بالا، روی موبایل و دسکتاپ."},
                {"icon":"languages","title":"ترجمهٔ صوتی بلادرنگ","text":"صدای طرف مقابل را به زبان انتخابی خودتان بشنوید؛ هر طرف مستقل زبان خودش را انتخاب می‌کند."},
                {"icon":"monitor-up","title":"اشتراک‌گذاری صفحه","text":"صفحهٔ خود را در حین مکالمه به اشتراک بگذارید."},
                {"icon":"disc","title":"ضبط مکالمه","text":"تماس‌ها را ضبط و بعداً مرور کنید."},
                {"icon":"sliders-horizontal","title":"کنترل کامل صدا","text":"بلندی صدای اصلی و صدای ترجمه‌شده را جداگانه تنظیم کنید."},
                {"icon":"shield-check","title":"امن و خصوصی","text":"ارتباط رمزنگاری‌شده و مدیریت متمرکز از پنل ادمین."}
            ]"""),
            ("how.title", "چطور کار می‌کند؟"),
            ("how.items", """[
                {"step":1,"title":"مکالمه بسازید","text":"وارد شوید و با یک کلیک لینک دعوت بسازید."},
                {"step":2,"title":"لینک را بفرستید","text":"طرف مقابل بدون نصب برنامه از مرورگر وارد می‌شود."},
                {"step":3,"title":"ترجمه را شروع کنید","text":"زبان دلخواه را انتخاب و روی «شروع ترجمه» بزنید."}
            ]"""),
            ("cta.title", "همین حالا اولین مکالمهٔ بدون مرز خود را شروع کنید"),
            ("cta.button", "ساخت حساب کاربری"),
            ("footer.contact", "تماس با ما: info@smartcall.example"),
        };

        var en = new (string Key, string Content)[]
        {
            ("hero.title", "Speak to anyone, in any language"),
            ("hero.subtitle", "Live video calls with real-time AI voice translation — forget the language barrier forever."),
            ("hero.cta", "Start for free"),
            ("features.title", "SmartCall features"),
            ("features.items", """[
                {"icon":"video","title":"High-quality video calls","text":"Stable 1:1 video calls on mobile and desktop."},
                {"icon":"languages","title":"Real-time voice translation","text":"Hear the other side in the language you choose; each side picks independently."},
                {"icon":"monitor-up","title":"Screen sharing","text":"Share your screen during the call."},
                {"icon":"disc","title":"Call recording","text":"Record calls and review them later."},
                {"icon":"sliders-horizontal","title":"Full audio control","text":"Adjust original and translated voice volumes separately."},
                {"icon":"shield-check","title":"Secure & private","text":"Encrypted communication with centralized admin management."}
            ]"""),
            ("how.title", "How it works"),
            ("how.items", """[
                {"step":1,"title":"Create a call","text":"Log in and generate an invite link in one click."},
                {"step":2,"title":"Send the link","text":"The other side joins from the browser, no install needed."},
                {"step":3,"title":"Start translating","text":"Pick your language and hit “Start translation”."}
            ]"""),
            ("cta.title", "Start your first borderless conversation now"),
            ("cta.button", "Create an account"),
            ("footer.contact", "Contact us: info@smartcall.example"),
        };

        return fa.Select(c => new LandingPageContent { Id = Guid.NewGuid(), SectionKey = c.Key, Language = "fa", Content = c.Content })
            .Concat(en.Select(c => new LandingPageContent { Id = Guid.NewGuid(), SectionKey = c.Key, Language = "en", Content = c.Content }));
    }

    public static IEnumerable<AppSetting> DefaultSettings() =>
    [
        new() { Key = SettingKeys.OpenAiBaseUrl, Value = "https://api.openai.com/v1" },
        new() { Key = SettingKeys.ActiveTranslationMethod, Value = "cascade" },
        new() { Key = SettingKeys.DefaultDashboardLanguage, Value = "fa" },
        new() { Key = SettingKeys.AllowUserLanguageSwitch, Value = "true" },
        new() { Key = SettingKeys.StunTurnServers, Value = """[{"urls":"stun:stun.l.google.com:19302"}]""" },
        // Model names are intentionally left empty — the super admin sets them
        // in the panel; they are never hardcoded.
        new() { Key = SettingKeys.OpenAiSttModel, Value = "" },
        new() { Key = SettingKeys.OpenAiTranslationModel, Value = "" },
        new() { Key = SettingKeys.OpenAiTtsModel, Value = "" },
        new() { Key = SettingKeys.OpenAiTtsVoice, Value = "alloy" },
        new() { Key = SettingKeys.OpenAiRealtimeModel, Value = "" },
    ];
}
