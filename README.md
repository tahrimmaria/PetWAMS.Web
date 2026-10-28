# PetHope — ASP.NET Core MVC Version

## যা যা এখানে আছে

আপনার আগের static HTML সাইটের সব পেজ এখন dynamic MVC পেজ, database থেকে connect করা:

| আপনার আগের ফাইল | এখন কী | কী বদলেছে |
|---|---|---|
| `petdetails_Bella.html`, `_Max.html`, `_Luna.html`, `_Tommy.html`, `_Rio.html`, `_Buddy.html`, `_Milo.html`, `petdetail_Coco.html` | `/Pets/Details/1`, `/Pets/Details/2` ... `/Pets/Details/8` | ৮টা আলাদা ফাইলের বদলে **১টা** `Views/Pets/Details.cshtml`, ডাটা আসে DB থেকে |
| `pets.html` | `/Pets` | Dog/Cat/Bird ফিল্টার এখন query string দিয়ে কাজ করে |
| `managepets.html` | `/Shelter/Pets` | Edit/Delete বাটন এখন সত্যিই কাজ করে (আগে `href="#"` ছিল) |
| `shelterprofile.html` | `/Shelter/Profile` | Total Pets/Adoptions সংখ্যা এখন live count |
| `signin.html` | `/Account/SignIn` | role-toggle UI/JS হুবহু রাখা হয়েছে, submit করলে সার্ভারে email+role চেক করে |
| `signup.html` | `/Account/SignUp` | Adopter ফর্ম DB-তে insert করে (Shelter ফর্ম আপাতত UI-only) |
| `adminlogin.html`-এর `admin@gmail.com/admin123` hardcode | `/Account/AdminLogin` | এখন `Users` টেবিলে `UserType='Admin'` চেক করে |
| `admindashboard.html`-এর `sessionStorage.getItem("adminLoggedIn")` | সার্ভার-সাইড session চেক | JS দিয়ে client-side "লক" আগে বাইপাস করা যেত; এখন সার্ভার নিজে যাচাই করে |
| `dashboard.html` | `/Adopter/Dashboard` | "0" আর "No data yet" জায়গায় এখন real DB numbers/rows |

## চালানোর ধাপ

### ১) ডাটাবেস বানান
SSMS-এ ক্রমানুসারে রান করুন:
1. আপনার `PetWAMSDB_ddl.sql`
2. এই প্রজেক্টের `additional_schema.sql` (কিছু নতুন কলাম যোগ করে — Description, Vaccinated, Shelter Email/OpenHours)
3. `seed_data.sql` (আপনার ৮টা পোষা প্রাণীর ডাটা + shelter + health record বসিয়ে দেয়)

### ২) Connection String চেক করুন
`appsettings.json`-এ `Server=.` আপনার SQL Server instance নামের সাথে মিলছে কিনা দেখুন।

### ৩) Visual Studio-তে খুলুন
`PetWAMS.Web.csproj`-এ ডাবল ক্লিক → F5 চাপুন।

### ৪) টেস্ট করুন
- Homepage → **Browse Pets** ক্লিক করুন → dog/cat/bird ফিল্টার কাজ করছে দেখুন
- যেকোনো পেটে ক্লিক করে **View Details** — এখন `/Pets/Details/1` টাইপ URL হবে, কিন্তু দেখতে হুবহু আপনার আগের ডিজাইনের মতোই
- **Sign In** → Adopter সিলেক্ট করে `tahrim.maria@example.com` দিয়ে ঢুকুন → Dashboard-এ Bella-র request দেখবেন
- **Sign In** → Shelter/Staff সিলেক্ট করে `happypaws@gmail.com` দিয়ে ঢুকুন → Shelter Dashboard
- সরাসরি URL-এ `/Account/AdminLogin` গিয়ে `admin@gmail.com` দিয়ে ঢুকুন → Admin Dashboard

## গুরুত্বপূর্ণ সীমাবদ্ধতা (স্যার জিজ্ঞেস করলে বলার জন্য)

- **Password যাচাই এখনো নেই** — DDL-এর `Users` টেবিলে `Password` কলাম নেই, তাই এখন শুধু email+role মিললেই লগইন হয়ে যায়। Production-এ `PasswordHash` কলাম যোগ করে BCrypt দিয়ে verify করা উচিত।
- **Shelter Staff কোন নির্দিষ্ট Shelter-এর — এই লিংক নেই** — DDL-এ Users-Shelters সরাসরি সম্পর্ক নেই, তাই Shelter Dashboard/Profile আপাতত প্রথম shelter row দেখায়। প্রয়োজনে `Users`-এ `ShelterID` কলাম যোগ করুন।
- **Manage Pets-এ Add/Edit ফর্ম বেসিক** — শুধু মূল ফিল্ডগুলো আছে (name, breed, age, gender, color, description); চাইলে microchip, category dropdown ইত্যাদি যোগ করে দিতে পারি।

## ফাইল কী কাজ করে (এক নজরে)

```
Models/                    → প্রতিটা DB টেবিলের C# ক্লাস
Data/PetWamsDbContext.cs   → DB connection
Controllers/PetsController → Browse + Details (8 files → 1)
Controllers/AccountController → SignIn/SignUp/AdminLogin/Logout
Areas/Adopter/             → User Dashboard
Areas/Shelter/             → Shelter Dashboard + Manage Pets (CRUD) + Profile
Areas/Admin/                → Admin Dashboard
additional_schema.sql       → নতুন কলাম যোগ করার script
seed_data.sql               → demo data
```


## Premium HTML design merged into MVC
The supplied HTML designs have been converted to Razor/MVC views while keeping the same visual layout.

### Demo login accounts
- Adopter: `rahim@gmail.com` (any non-empty password for this class demo)
- Shelter/Staff: `tamim@gmail.com` (any non-empty password for this class demo)
- Admin: `admin@gmail.com` / `admin123`

### Database
Use `PetWAMSDB_WEB_SETUP.sql` once in SSMS. The app connection string should point to `PetWAMSDB`.
