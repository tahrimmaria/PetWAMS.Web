using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AIRecommendationController : Controller
    {
        private readonly PetWamsDbContext _db;

        public AIRecommendationController(
            PetWamsDbContext db)
        {
            _db = db;
        }


        // =========================================
        // CHECK ADMIN LOGIN
        // =========================================
        private bool IsAdmin()
        {
            return HttpContext.Session
                .GetString("UserType") == "Admin";
        }


        // =========================================
        // SELECT ADOPTER PAGE
        // =========================================
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }


            ViewBag.Adopters =
                await _db.Users
                    .Where(u =>
                        u.UserType == "Adopter")
                    .OrderBy(u =>
                        u.FullName)
                    .ToListAsync();


            return View();
        }


        // =========================================
        // FIND AI BEST PET
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Recommend(
            int userId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }


            // =========================================
            // GET ADOPTER
            // =========================================
            var adopter =
                await _db.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserID == userId
                        &&
                        u.UserType == "Adopter");


            if (adopter == null)
            {
                return NotFound();
            }


            // =========================================
            // GET ADOPTER PREFERENCES
            // =========================================
            var preference =
                await _db.AdopterPreferences

                    .Include(p =>
                        p.PreferredCategory)

                    .FirstOrDefaultAsync(p =>
                        p.UserID == userId);


            if (preference == null)
            {
                ViewBag.Adopter =
                    adopter;


                ViewBag.Error =
                    "This adopter has not completed the pet preference profile yet.";


                return View("Result");
            }


            // =========================================
            // GET ALL AVAILABLE PETS
            // =========================================
            var availablePets =
                await _db.Pets

                    .Include(p =>
                        p.Category)

                    .Include(p =>
                        p.Shelter)

                    .Include(p =>
                        p.HealthRecords)

                    .Where(p =>
                        p.Status != null
                        &&
                        p.Status.ToLower() == "available")

                    .ToListAsync();


            if (availablePets.Count == 0)
            {
                ViewBag.Adopter =
                    adopter;


                ViewBag.Preference =
                    preference;


                ViewBag.Error =
                    "There are currently no available pets in the system.";


                return View("Result");
            }


            // =========================================
            // ANALYSE EVERY AVAILABLE PET
            // =========================================
            var results =
                new List<PetMatchResult>();


            foreach (var pet in availablePets)
            {
                var result =
                    CalculateCompatibility(
                        pet,
                        preference
                    );


                results.Add(
                    result
                );
            }


            // =========================================
            // SELECT ONLY ONE BEST PET
            // =========================================
            var bestMatch =
                results

                    .OrderByDescending(r =>
                        r.Score)

                    .ThenBy(r =>
                        r.Pet.PetID)

                    .First();


            // =========================================
            // SEND RESULT TO VIEW
            // =========================================
            ViewBag.Adopter =
                adopter;


            ViewBag.Preference =
                preference;


            ViewBag.BestPet =
                bestMatch.Pet;


            ViewBag.MatchScore =
                bestMatch.Score;


            ViewBag.Reasons =
                bestMatch.Reasons;


            ViewBag.TotalPetsAnalyzed =
                availablePets.Count;


            return View("Result");
        }


        // =========================================
        // SEND AI RECOMMENDATION
        // TO ADOPTER
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRecommendation(
            int userId,
            int petId,
            int matchScore,
            string? recommendationReason)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }


            // =========================================
            // CHECK ADOPTER
            // =========================================
            var adopter =
                await _db.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserID == userId
                        &&
                        u.UserType == "Adopter");


            if (adopter == null)
            {
                return NotFound();
            }


            // =========================================
            // CHECK PET
            // =========================================
            var pet =
                await _db.Pets
                    .FirstOrDefaultAsync(p =>
                        p.PetID == petId);


            if (pet == null)
            {
                return NotFound();
            }


            // =========================================
            // PET MUST STILL BE AVAILABLE
            // =========================================
            if (!string.Equals(
                    pet.Status,
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This pet is no longer available.";


                return RedirectToAction(
                    "Recommend",
                    new
                    {
                        userId = userId
                    }
                );
            }


            // =========================================
            // VALIDATE SCORE
            // =========================================
            matchScore =
                Math.Clamp(
                    matchScore,
                    0,
                    100
                );


            // =========================================
            // REASON MAX 1000 CHARACTERS
            // =========================================
            if (!string.IsNullOrWhiteSpace(
                    recommendationReason)
                &&
                recommendationReason.Length > 1000)
            {
                recommendationReason =
                    recommendationReason
                        .Substring(
                            0,
                            1000
                        );
            }


            // =========================================
            // CHECK SAME PET ALREADY
            // RECOMMENDED TO SAME ADOPTER
            // =========================================
            var existingRecommendation =
                await _db.PetRecommendations
                    .FirstOrDefaultAsync(r =>
                        r.UserID == userId
                        &&
                        r.PetID == petId);


            if (existingRecommendation != null)
            {
                // =====================================
                // DON'T CREATE DUPLICATE ROW
                // UPDATE EXISTING RECOMMENDATION
                // =====================================

                existingRecommendation.MatchScore =
                    matchScore;


                existingRecommendation.RecommendationReason =
                    recommendationReason;


                // আবার recommendation পাঠানো হয়েছে
                // তাই আবার unread/unviewed
                existingRecommendation.IsViewed =
                    false;


                existingRecommendation.CreatedAt =
                    DateTime.Now;


                await _db.SaveChangesAsync();


                TempData["Success"] =
                    $"{pet.PetName} recommendation for {adopter.FullName} has been updated successfully.";
            }
            else
            {
                // =====================================
                // FIRST TIME RECOMMENDATION
                // =====================================

                var recommendation =
                    new PetRecommendation
                    {
                        UserID =
                            adopter.UserID,

                        PetID =
                            pet.PetID,

                        MatchScore =
                            matchScore,

                        RecommendationReason =
                            recommendationReason,

                        IsViewed =
                            false,

                        CreatedAt =
                            DateTime.Now
                    };


                _db.PetRecommendations.Add(
                    recommendation
                );


                await _db.SaveChangesAsync();


                TempData["Success"] =
                    $"{pet.PetName} has been recommended to {adopter.FullName} successfully.";
            }


            // =========================================
            // RETURN TO AI RESULT
            // =========================================
            return RedirectToAction(
                "Recommend",
                new
                {
                    userId = adopter.UserID
                }
            );
        }


        // =========================================
        // AI COMPATIBILITY ENGINE
        // =========================================
        private PetMatchResult CalculateCompatibility(
            Pet pet,
            AdopterPreference preference)
        {
            double earnedPoints = 0;

            double possiblePoints = 0;


            var reasons =
                new List<string>();


            string petText =
                BuildPetText(
                    pet
                );


            // =========================================
            // 1. CATEGORY MATCH
            // WEIGHT = 30
            // =========================================
            if (preference
                .PreferredCategoryID
                .HasValue)
            {
                possiblePoints += 30;


                if (pet.CategoryID ==
                    preference.PreferredCategoryID)
                {
                    earnedPoints += 30;


                    reasons.Add(
                        "Preferred pet category matches perfectly."
                    );
                }
            }


            // =========================================
            // 2. GENDER MATCH
            // WEIGHT = 10
            // =========================================
            if (!string.IsNullOrWhiteSpace(
                    preference.PreferredGender)
                &&
                !preference.PreferredGender
                    .Equals(
                        "Any",
                        StringComparison.OrdinalIgnoreCase
                    ))
            {
                possiblePoints += 10;


                if (!string.IsNullOrWhiteSpace(
                        pet.Gender)
                    &&
                    pet.Gender.Equals(
                        preference.PreferredGender,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    earnedPoints += 10;


                    reasons.Add(
                        "Preferred gender matches."
                    );
                }
            }


            // =========================================
            // 3. AGE MATCH
            // WEIGHT = 20
            // =========================================
            if (preference.MinimumAge.HasValue
                ||
                preference.MaximumAge.HasValue)
            {
                possiblePoints += 20;


                if (pet.Age.HasValue)
                {
                    bool minimumOkay =
                        !preference.MinimumAge.HasValue
                        ||
                        pet.Age.Value >=
                        preference.MinimumAge.Value;


                    bool maximumOkay =
                        !preference.MaximumAge.HasValue
                        ||
                        pet.Age.Value <=
                        preference.MaximumAge.Value;


                    if (minimumOkay &&
                        maximumOkay)
                    {
                        earnedPoints += 20;


                        reasons.Add(
                            "Pet age is within the adopter's preferred range."
                        );
                    }
                    else
                    {
                        int distance = 0;


                        if (preference.MinimumAge.HasValue
                            &&
                            pet.Age.Value <
                            preference.MinimumAge.Value)
                        {
                            distance =
                                preference.MinimumAge.Value
                                -
                                pet.Age.Value;
                        }


                        if (preference.MaximumAge.HasValue
                            &&
                            pet.Age.Value >
                            preference.MaximumAge.Value)
                        {
                            distance =
                                pet.Age.Value
                                -
                                preference.MaximumAge.Value;
                        }


                        // Slightly outside preferred age
                        if (distance == 1)
                        {
                            earnedPoints += 8;
                        }
                    }
                }
            }


            // =========================================
            // 4. BREED MATCH
            // WEIGHT = 15
            // =========================================
            if (!string.IsNullOrWhiteSpace(
                    preference.PreferredBreed))
            {
                possiblePoints += 15;


                if (!string.IsNullOrWhiteSpace(
                        pet.Breed)
                    &&
                    (
                        pet.Breed.Contains(
                            preference.PreferredBreed,
                            StringComparison.OrdinalIgnoreCase
                        )

                        ||

                        preference.PreferredBreed.Contains(
                            pet.Breed,
                            StringComparison.OrdinalIgnoreCase
                        )
                    ))
                {
                    earnedPoints += 15;


                    reasons.Add(
                        "Preferred breed matches."
                    );
                }
            }


            // =========================================
            // 5. LIVING ENVIRONMENT
            // WEIGHT = 5
            // =========================================
            if (!string.IsNullOrWhiteSpace(
                    preference.LivingType))
            {
                possiblePoints += 5;


                // House is generally suitable
                // for wider range of pets
                if (preference.LivingType.Equals(
                    "House",
                    StringComparison.OrdinalIgnoreCase))
                {
                    earnedPoints += 5;


                    reasons.Add(
                        "Suitable for the adopter's home environment."
                    );
                }


                else if (
                    preference.LivingType.Equals(
                        "Apartment",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (ContainsAny(
                        petText,
                        "apartment",
                        "indoor",
                        "small",
                        "calm",
                        "quiet",
                        "gentle"))
                    {
                        earnedPoints += 5;


                        reasons.Add(
                            "Pet characteristics appear suitable for apartment living."
                        );
                    }
                    else
                    {
                        // Neutral score
                        earnedPoints += 2.5;
                    }
                }
            }


            // =========================================
            // 6. ACTIVITY LEVEL
            // WEIGHT = 5
            // =========================================
            if (!string.IsNullOrWhiteSpace(
                    preference.ActivityLevel))
            {
                possiblePoints += 5;


                bool activityMatched =
                    false;


                // HIGH ACTIVITY
                if (preference.ActivityLevel.Equals(
                    "High",
                    StringComparison.OrdinalIgnoreCase))
                {
                    activityMatched =
                        ContainsAny(
                            petText,
                            "active",
                            "energetic",
                            "playful",
                            "exercise",
                            "outdoor"
                        );
                }


                // LOW ACTIVITY
                else if (
                    preference.ActivityLevel.Equals(
                        "Low",
                        StringComparison.OrdinalIgnoreCase))
                {
                    activityMatched =
                        ContainsAny(
                            petText,
                            "calm",
                            "quiet",
                            "gentle",
                            "relaxed",
                            "low energy"
                        );
                }


                // MEDIUM ACTIVITY
                else
                {
                    activityMatched =
                        ContainsAny(
                            petText,
                            "friendly",
                            "playful",
                            "moderate",
                            "social",
                            "gentle"
                        );
                }


                if (activityMatched)
                {
                    earnedPoints += 5;


                    reasons.Add(
                        "Pet personality fits the adopter's activity level."
                    );
                }
                else
                {
                    // Neutral if information
                    // is not enough
                    earnedPoints += 2.5;
                }
            }


            // =========================================
            // 7. PET EXPERIENCE
            // WEIGHT = 5
            // =========================================
            if (!string.IsNullOrWhiteSpace(
                    preference.PetExperience))
            {
                possiblePoints += 5;


                if (preference.PetExperience.Equals(
                    "Experienced",
                    StringComparison.OrdinalIgnoreCase))
                {
                    earnedPoints += 5;


                    reasons.Add(
                        "Adopter's experience level is suitable for this pet."
                    );
                }
                else
                {
                    bool beginnerFriendly =
                        ContainsAny(
                            petText,
                            "friendly",
                            "gentle",
                            "calm",
                            "easy",
                            "trained",
                            "trainable"
                        );


                    if (beginnerFriendly)
                    {
                        earnedPoints += 5;


                        reasons.Add(
                            "Pet appears suitable for the adopter's experience level."
                        );
                    }
                    else
                    {
                        earnedPoints += 2.5;
                    }
                }
            }


            // =========================================
            // 8. CHILDREN COMPATIBILITY
            // WEIGHT = 5
            // =========================================
            if (preference.HasChildren == true)
            {
                possiblePoints += 5;


                bool childFriendly =
                    ContainsAny(
                        petText,
                        "child",
                        "children",
                        "family",
                        "friendly",
                        "gentle"
                    );


                if (childFriendly)
                {
                    earnedPoints += 5;


                    reasons.Add(
                        "Pet information indicates good family compatibility."
                    );
                }
                else
                {
                    earnedPoints += 2.5;
                }
            }


            // =========================================
            // 9. HEALTH CONDITION
            // WEIGHT = 5
            // =========================================
            possiblePoints += 5;


            var latestHealth =
                pet.HealthRecords?

                    .OrderByDescending(h =>
                        h.CheckupDate)

                    .FirstOrDefault();


            if (latestHealth == null)
            {
                // No health data
                // gets neutral score
                earnedPoints += 2.5;
            }
            else
            {
                string healthStatus =
                    latestHealth.HealthStatus
                    ?? "";


                if (ContainsAny(
                    healthStatus,
                    "healthy",
                    "good",
                    "stable",
                    "normal",
                    "fit"))
                {
                    earnedPoints += 5;


                    reasons.Add(
                        "Latest health information is positive."
                    );
                }
                else if (!ContainsAny(
                    healthStatus,
                    "critical",
                    "serious",
                    "severe",
                    "ill"))
                {
                    earnedPoints += 2.5;
                }
            }


            // =========================================
            // FINAL MATCH PERCENTAGE
            // =========================================
            double percentage;


            if (possiblePoints <= 0)
            {
                percentage = 0;
            }
            else
            {
                percentage =
                    earnedPoints
                    /
                    possiblePoints
                    *
                    100;
            }


            int finalScore =
                (int)Math.Round(
                    percentage
                );


            // Ensure score always 0 - 100
            finalScore =
                Math.Clamp(
                    finalScore,
                    0,
                    100
                );


            // =========================================
            // FALLBACK EXPLANATION
            // =========================================
            if (reasons.Count == 0)
            {
                reasons.Add(
                    "This pet has the highest overall compatibility among currently available pets."
                );
            }


            return new PetMatchResult
            {
                Pet =
                    pet,

                Score =
                    finalScore,

                Reasons =
                    reasons
            };
        }


        // =========================================
        // BUILD TEXT FOR
        // PET CHARACTERISTIC ANALYSIS
        // =========================================
        private string BuildPetText(
            Pet pet)
        {
            var healthText =
                pet.HealthRecords == null

                ? ""

                : string.Join(
                    " ",
                    pet.HealthRecords
                        .Select(h =>
                            $"{h.HealthStatus} {h.MedicalNotes}")
                );


            string completeText =
                $"{pet.PetName} " +
                $"{pet.Breed} " +
                $"{pet.Gender} " +
                $"{pet.Color} " +
                $"{pet.Description} " +
                $"{pet.AIRecommendation} " +
                $"{healthText}";


            return completeText
                .ToLower();
        }


        // =========================================
        // KEYWORD ANALYSIS
        // =========================================
        private bool ContainsAny(
            string text,
            params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(
                    text))
            {
                return false;
            }


            foreach (var keyword in keywords)
            {
                if (text.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            return false;
        }


        // =========================================
        // INTERNAL AI RESULT CLASS
        // =========================================
        private class PetMatchResult
        {
            public Pet Pet { get; set; }
                = null!;


            public int Score { get; set; }


            public List<string> Reasons
            {
                get;
                set;
            }
                = new List<string>();
        }
    }
}