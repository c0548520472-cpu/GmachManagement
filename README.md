# GmachManagement — מערכת ניהול גמ"ח והשאלות אישיות

פרויקט סיום ב־.NET 10: ניהול פריטים ועותקים פיזיים של גמ"ח, השאלות והחזרות, מעקב השאלות אישיות ותזכורות דוא"ל.

## ארכיטקטורה
- **Core** — ישויות, ממשקים, DTOs ו־Enums; ללא תלות בפרויקטים אחרים.
- **Data** — EF Core, SQL Server או PostgreSQL, Repositories, Migrations, תזכורות.
- **Service** — לוגיקה עסקית, אימות ומיפוי.
- **API** — Controllers, JWT, Middlewares, CORS; תלות ב־Core וב־Service בלבד. שירותי Data נטענים באמצעות Reflection.
- **Tests** — בדיקות xUnit/Moq ו־Concurrency.
- **Client** — HTML/CSS/JavaScript במאגר נפרד: [גרסה תואמת להגשה](https://github.com/c0548520472-cpu/GmachManagement.Client/tree/2ee165c3be092eb37a0e35497705139fb9b77fe6).

## המשאב המוגבל
כל `ItemCopy` מייצג עותק פיזי יחיד. בעת השאלה, עדכון סטטוס העותק ויצירת הרשומה מתבצעים בטרנזקציה; ב־SQL Server נעשה שימוש ב־`RowVersion`, וב־PostgreSQL בעמודת `xmin` כבקרת תחרות. ניסיון להשאיל עותק שכבר נתפס מוחזר כ־HTTP 409.

## הרצה מקומית להגשה (ללא ענן)
1. התקינו .NET 10 SDK ו־**PostgreSQL מקומי**. בהגשה נבדקת הרצה מול PostgreSQL. התחילו בתיקיית השורש של ענף `submission`.
   ```sh
   dotnet dev-certs https --trust
   dotnet user-secrets set "DatabaseProvider" "PostgreSQL" --project GmachManagement.API
   dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Port=5432;Database=gmach_submission;Username=<local-role>;Password=<local-password>" --project GmachManagement.API
   dotnet user-secrets set "Jwt:Issuer" "GmachSubmission" --project GmachManagement.API
   dotnet user-secrets set "Jwt:Audience" "GmachSubmission" --project GmachManagement.API
   dotnet user-secrets set "Jwt:Key" "<local-generated-key-at-least-32-bytes>" --project GmachManagement.API
   ```
   החליפו את הערכים בסוגריים המשולשים בפרטים של PostgreSQL המקומי ובמפתח מקומי אקראי. תפקיד מסד הנתונים המקומי צריך להיות מורשה ליצור את מסד ההדגמה.
2. בפרויקט `GmachManagement.API` הגדירו **User Secrets** (או משתני סביבה), בלי להכניס סודות ל־Git:
   - `DatabaseProvider` = `PostgreSQL` או `SqlServer` (ברירת המחדל ב־`appsettings.json` היא SQL Server).
   - עבור PostgreSQL: `ConnectionStrings:PostgreSQL`; עבור SQL Server: `ConnectionStrings:DefaultConnection`.
   - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key` (מפתח של 32 בתים לפחות).
   - אל תשתמשו בחיבור למסד הנתונים של Neon או Render עבור ההגשה.
3. עבור **PostgreSQL**, השרת מפעיל migrations ממתינות בזמן האתחול באמצעות `PostgreSqlMigrationsDbContext`. עבור **SQL Server**, הפעילו migrations **על בסיס מקומי בלבד**:
   ```powershell
   dotnet ef database update --context AppDbContext --project GmachManagement.Data --startup-project GmachManagement.API
   ```
4. הריצו את השרת: `dotnet run --project GmachManagement.API --launch-profile https` (או דרך Visual Studio). בדקו ש־Swagger נפתח ב־`https://localhost:7042/swagger` כאשר זה הפורט המקומי.
5. שכפלו את המאגר הנפרד והפעילו את גרסת הלקוח התואמת מתיקיית המאגר.
   ```sh
   git clone --branch submission https://github.com/c0548520472-cpu/GmachManagement.Client.git
   cd GmachManagement.Client
   git checkout --detach 2ee165c3be092eb37a0e35497705139fb9b77fe6
   python -m http.server 5500 --bind 127.0.0.1
   ```
   פתחו `http://127.0.0.1:5500`. הלקוח פונה ל־`https://localhost:7042/api`.
6. בדיקות חובה: `dotnet test GmachManagement.slnx`. בדיקת תחרות ממוקדת: `dotnet test GmachManagement.Tests/GmachManagement.Tests.csproj --filter FullyQualifiedName~ConcurrencyTests`.
7. **משתמשי הדגמה ב־PostgreSQL המקומי.** צרו את החשבונות הבאים דרך מסך ההרשמה. בחרו לכל חשבון סיסמה מקומית של לפחות שמונה תווים, עם אות וספרה. ההתחברות משתמשת בסיסמה שבחרתם בהרשמה.

   | תפקיד | אימייל להדגמה | בחירה בהרשמה |
   | --- | --- | --- |
   | `User` | `demo-member@example.test` | הרשמה רגילה |
   | `GmachAdmin` | `demo-owner@example.test` | סמנו בעל גמ״ח ומלאו שם גמ״ח ועיר |
   | `SiteAdmin` | `demo-admin@example.test` | הרשמה רגילה עם כתובת מנהל ההדגמה |

   כדי להפעיל את תפקיד מנהל ההדגמה, הריצו `dotnet user-secrets set "SiteAdmin:BootstrapEmail" "demo-admin@example.test" --project GmachManagement.API` בסביבת ההגשה המקומית, ואתחלו את השרת לפני התחברות לחשבון זה.

### מאגרי ההגשה

- שרת (ענף הגשה): https://github.com/c0548520472-cpu/GmachManagement/tree/submission
- לקוח: [Commit תואם לגרסת ההגשה](https://github.com/c0548520472-cpu/GmachManagement.Client/tree/2ee165c3be092eb37a0e35497705139fb9b77fe6)
- אין צורך בתיקיית `GmachManagement.Client` במאגר השרת; קוד הלקוח הועבר למאגר הנפרד.

### הדגמה ממוקדת לפי דרישות המורה
- הרשמה והתחברות של משתמש רגיל ושל בעל גמ"ח; JWT והרשאות שונות.
- קטלוג פריטים עם חיפוש ו־pagination; הוספת פריט ועותק בידי בעל גמ"ח.
- יצירת בקשת השאלה דרך `POST /api/borrow-requests` ואישורה בידי בעל הגמ"ח דרך `PUT /api/borrow-requests/mine/{id}/approvals/current`.
- בקשה שכבר טופלה או מצב של חוסר זמינות מוחזרים כ־`409` עם הודעה ללקוח.
- בדיקת `ConcurrencyTests` מציגה שני `DbContext` נפרדים; ב־SQLite in-memory טריגר מקומי מחליף את ערך הגרסה אוטומטית בזמן העדכון.

## תפקידי משתמש
- **User** — הרשמה, התחברות, צפייה בפריטים, השאלת עותקים, ניהול השאלות אישיות וסימון החזרה.
- **GmachAdmin** — הוספה ועריכה של פריטים, הוספת עותקים, השבתה/שחזור פריטים, צפייה בהשאלות פעילות, עדכון מועד החזרה וסימון החזרות.
- **SiteAdmin** — ניהול האתר בהתאם להרשאות המוגדרות בשרת.
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

### בטיחות סביבת ההגשה

ההגשה מבוססת על שרת ולקוח מקומיים. אין צורך בענן, Docker או Google Sign-In. אין להריץ migrations, איפוס או בדיקות מול בסיס הנתונים החי. שינויי ההגשה מתבצעים בענף `submission` בלבד.

## בדיקות הגשה בענף submission

- GitHub Actions: `.github/workflows/submission-tests.yml` מבצע restore, build ו־xUnit tests עם .NET 10 בכל push לענף `submission`.
- מקומית, ללא צורך במסד נתונים חי: `dotnet restore GmachManagement.slnx`, `dotnet build GmachManagement.slnx --no-restore`, `dotnet test GmachManagement.slnx --no-build`.
- דוח כיסוי: `dotnet test GmachManagement.Tests/GmachManagement.Tests.csproj --collect:"XPlat Code Coverage"`.
- בדיקת חבילות פגיעות: `dotnet list GmachManagement.slnx package --vulnerable --include-transitive`.
- בדיקת פרסום השרת בלי לפרוס: `dotnet publish GmachManagement.API/GmachManagement.API.csproj -c Release -o ./artifacts/api-publish`.
- בדיקת EF Core / migrations מול בסיס נתונים מקומי בלבד, ורק לאחר גיבוי. אין להריץ `database update` נגד Neon/Render.
- בדיקת תחרות: `ConcurrencyTests.TwoContexts_UpdatingSameCopy_SecondUpdateFails` משתמשת בשני DbContext נפרדים וב־SQLite in-memory. היא מאמתת זיהוי גרסה ישנה; בבדיקת האינטגרציה של PostgreSQL אפשר לאשר במקביל שתי בקשות נפרדות לעותק הפיזי האחרון. הציפייה היא אישור אחד, תשובת 409 אחת והשאלה פעילה אחת.
- אין לכתוב סיסמאות, מפתחות JWT או פרטי SMTP ל־Git.
