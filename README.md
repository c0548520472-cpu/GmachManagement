# GmachManagement — מערכת ניהול גמ"ח והשאלות אישיות

פרויקט סיום ב־.NET 10: ניהול פריטים ועותקים פיזיים של גמ"ח, השאלות והחזרות, מעקב השאלות אישיות ותזכורות דוא"ל.

## ארכיטקטורה
- **Core** — ישויות, ממשקים, DTOs ו־Enums; ללא תלות בפרויקטים אחרים.
- **Data** — EF Core, SQL Server, Repositories, Migrations, תזכורות.
- **Service** — לוגיקה עסקית, אימות ומיפוי.
- **API** — Controllers, JWT, Middlewares, CORS; תלות ב־Core וב־Service בלבד. שירותי Data נטענים באמצעות Reflection.
- **Tests** — בדיקות xUnit/Moq ו־Concurrency.
- **Client** — HTML/CSS/JavaScript.

## המשאב המוגבל
כל `ItemCopy` מייצג עותק פיזי יחיד. בעת השאלה, עדכון סטטוס העותק ויצירת הרשומה מתבצעים בטרנזקציה; `RowVersion` משמש לבקרת תחרות. ניסיון להשאיל עותק שכבר נתפס מוחזר כ־HTTP 409.

## הרצה מקומית
1. נדרש .NET 10 SDK ושרת SQL Server מקומי.
2. הגדירו ב־User Secrets או במשתני סביבה את `ConnectionStrings:DefaultConnection`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key` (לפחות 32 בתים). אין להעלות סודות ל־Git.
3. החילו Migrations קיימות על מסד הנתונים.
4. הריצו את `GmachManagement.API` דרך Visual Studio או `dotnet run --project GmachManagement.API`.
5. מתוך `GmachManagement.Client` הריצו `python -m http.server 5500 --bind 127.0.0.1`, ופתחו `http://127.0.0.1:5500`.
6. כתובת ה־API מוגדרת ב־`GmachManagement.Client/app.js` כ־`https://localhost:7042/api`. אם פורט ה־API משתנה, עדכנו אותה.
7. בדיקות: `dotnet test GmachManagement.slnx`.

## תפקידי משתמש
- **User** — הרשמה, התחברות, צפייה בפריטים, השאלת עותקים, ניהול השאלות אישיות וסימון החזרה.
- **GmachAdmin** — הוספה ועריכה של פריטים, הוספת עותקים, השבתה/שחזור פריטים, צפייה בהשאלות פעילות, עדכון מועד החזרה וסימון החזרות.
- השבתת פריט משמרת את היסטוריית ההשאלות ואינה מוחקת נתונים.

## תזכורות במייל
שירות רקע בודק מדי שעה רשומות אישיות שהגיע מועד התזכורת שלהן. כדי לאפשר משלוח, הגדירו באופן מאובטח:
- `Email:SmtpHost`
- `Email:SmtpPort` (ברירת מחדל 587)
- `Email:From`
- `Email:Username`
- `Email:Password`

ללא הגדרות SMTP, לא יישלחו הודעות. מניעת משלוח כפול מבוססת כרגע על זיכרון התהליך בלבד; הפעלה מחדש עלולה לגרום לשליחה חוזרת. יש לאמת משלוח בפועל לפני שימוש.

## סטטוס פריסה
קיימת פריסה ציבורית נפרדת של הלקוח ב־Cloudflare Pages ושל השרת ב־Render. ענף `submission` מיועד להגשה ולבדיקות ואינו ענף הפריסה החיה. אין להפעיל פקודות מיגרציה או ניקוי נתונים על מסד הנתונים החי במסגרת בדיקות ההגשה.

## דמו מומלץ
הרשמה והתחברות; הצגת קטלוג וסינון; השאלת עותק; הדגמת HTTP 409 בשתי בקשות מתחרות; הוספת השאלה אישית; סימון החזרה; התחברות מנהל והוספת פריט/עותק, עדכון מועד החזרה.


## Gmach directory and owner areas

- Public directory: `GET /api/gmachs?city=...`, detail page: `GET /api/gmachs/{id}`.
- Registration accepts `isGmachOwner`, `gmachName`, and `city`. An owner gets a single gmach linked to their user account.
- Owners manage their own items through `/api/gmachs/mine` and `/api/gmachs/mine/items`; item and loan administration is ownership-scoped.
- Excel import: `POST /api/gmachs/mine/import` with multipart field `file`, `.xlsx` only, first worksheet; first row is headers, column A item name, B description, up to 500 items and 2 MB.
- Existing unassigned items remain unassigned; no automatic transfer of ownership.

### Required database upgrade (before using owner registration)

**Back up the existing Neon database first.** Run the additive PostgreSQL migration using the already-configured local Neon credentials:

```powershell
dotnet ef database update --context PostgreSqlMigrationsDbContext --project GmachManagement.Data --startup-project GmachManagement.API
```

For local SQL Server:

```powershell
dotnet ef database update --context AppDbContext --project GmachManagement.Data --startup-project GmachManagement.API
```

The API now applies pending PostgreSQL migrations on startup. Confirm Render logs show a successful startup and verify the new tables before testing owner registration, gmach search or Excel import. The manual command above remains available for local use.


## בדיקות הגשה בענף submission

- GitHub Actions: `.github/workflows/submission-tests.yml` מבצע restore, build ו־xUnit tests עם .NET 10 בכל push לענף `submission`.
- מקומית, ללא צורך במסד נתונים חי: `dotnet restore GmachManagement.slnx`, `dotnet build GmachManagement.slnx --no-restore`, `dotnet test GmachManagement.slnx --no-build`.
- דוח כיסוי: `dotnet test GmachManagement.Tests/GmachManagement.Tests.csproj --collect:"XPlat Code Coverage"`.
- בדיקת חבילות פגיעות: `dotnet list GmachManagement.slnx package --vulnerable --include-transitive`.
- בדיקת פרסום השרת בלי לפרוס: `dotnet publish GmachManagement.API/GmachManagement.API.csproj -c Release -o ./artifacts/api-publish`.
- בדיקת EF Core / migrations מול בסיס נתונים מקומי בלבד, ורק לאחר גיבוי. אין להריץ `database update` נגד Neon/Render.
- בדיקת תחרות: `ConcurrencyTests.TwoContexts_UpdatingSameCopy_SecondUpdateFails` משתמשת בשני DbContext נפרדים וב־SQLite in-memory. היא מאמתת זיהוי גרסה ישנה; יש להשלים גם בדיקות אינטגרציה מול ספק בסיס הנתונים של סביבת ההגשה.
- אין לכתוב סיסמאות, מפתחות JWT או פרטי SMTP ל־Git.
