using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;
using PetWAMS.Web.Services;

namespace PetWAMS.Web.Areas.Adopter.Controllers
{
    [Area("Adopter")]
    public class PreferencesController : Controller
    {
        private readonly PetWamsDbContext _db;

        private readonly NaturalLanguagePreferenceService
            _naturalLanguagePreferenceService;


        public PreferencesController(
            PetWamsDbContext db,
            NaturalLanguagePreferenceService
                naturalLanguagePreferenceService)
        {
            _db = db;

            _naturalLanguagePreferenceService =
                naturalLanguagePreferenceService;
        }


        // =====================================================
        // CHECK ADOPTER LOGIN
        // =====================================================
        private bool IsAdopter()
        {
            return
                HttpContext.Session.GetInt32("UserID") != null
                &&
                HttpContext.Session.GetString("UserType")
                    == "Adopter";
        }


        // =====================================================
        // NATURAL LANGUAGE PREFERENCE PAGE
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdopter())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new
                    {
                        area = ""
                    }
                );
            }


            int userId =
                HttpContext.Session
                    .GetInt32("UserID")!.Value;


            // =================================================
            // LOAD SAVED PREFERENCE
            // =================================================
            var preference =
                await _db.AdopterPreferences

                    .AsNoTracking()

                    .Include(p =>
                        p.PreferredCategory)

                    .FirstOrDefaultAsync(
                        p =>
                            p.UserID ==
                            userId
                    );


            // =================================================
            // TEXTBOX INPUT
            // =================================================
            ViewBag.PreferenceText =
                TempData["PreferenceInput"]?
                    .ToString()
                ??
                "";


            // =================================================
            // SAVED PREFERENCE
            // =================================================
            ViewBag.SavedPreference =
                preference;


            // =================================================
            // AI UNDERSTOOD SUMMARY
            // =================================================
            ViewBag.PreferenceSummary =
                TempData["PreferenceSummary"]?
                    .ToString()
                ??
                "";


            // =================================================
            // SUCCESS MESSAGE
            // =================================================
            ViewBag.Success =
                TempData["Success"]?
                    .ToString()
                ??
                "";


            // =================================================
            // ERROR MESSAGE
            // =================================================
            ViewBag.Error =
                TempData["Error"]?
                    .ToString()
                ??
                "";


            return View(
                preference
            );
        }


        // =====================================================
        // ANALYZE + SAVE NATURAL LANGUAGE PREFERENCE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            string? preferenceText)
        {
            if (!IsAdopter())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new
                    {
                        area = ""
                    }
                );
            }


            int userId =
                HttpContext.Session
                    .GetInt32("UserID")!.Value;


            // =================================================
            // EMPTY INPUT
            // =================================================
            if (string.IsNullOrWhiteSpace(
                preferenceText))
            {
                TempData["Error"] =
                    "Please write your pet preference.";


                return RedirectToAction(
                    nameof(Index)
                );
            }


            string cleanPreferenceText =
                preferenceText.Trim();


            // =================================================
            // NATURAL LANGUAGE AI PARSER
            // =================================================
            var parsed =
                await _naturalLanguagePreferenceService
                    .ParseAsync(
                        cleanPreferenceText
                    );


            // =================================================
            // AI COULD NOT UNDERSTAND
            //
            // In this case textbox keeps user's sentence
            // so the user can edit it.
            // =================================================
            if (!parsed.IsSuccess)
            {
                TempData["Error"] =
                    parsed.ErrorMessage;


                TempData["PreferenceInput"] =
                    cleanPreferenceText;


                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =================================================
            // FIND EXISTING PREFERENCE
            // =================================================
            var existing =
                await _db.AdopterPreferences

                    .FirstOrDefaultAsync(
                        p =>
                            p.UserID ==
                            userId
                    );


            // =================================================
            // FIRST TIME PREFERENCE
            // =================================================
            if (existing == null)
            {
                existing =
                    new AdopterPreference
                    {
                        UserID =
                            userId,


                        PreferredCategoryID =
                            parsed.PreferredCategoryID,


                        PreferredGender =
                            string.IsNullOrWhiteSpace(
                                parsed.PreferredGender)
                                ? "Any"
                                : parsed.PreferredGender,


                        MinimumAge =
                            parsed.MinimumAge,


                        MaximumAge =
                            parsed.MaximumAge,


                        PreferredBreed =
                            parsed.PreferredBreed,


                        // Existing model defaults
                        LivingType =
                            "House",


                        ActivityLevel =
                            "Medium",


                        PetExperience =
                            "Beginner",


                        HasChildren =
                            false,


                        // =====================================
                        // Original natural-language sentence
                        // database-এ থাকবে.
                        // =====================================
                        AdditionalNotes =
                            cleanPreferenceText,


                        UpdatedAt =
                            DateTime.Now
                    };


                _db.AdopterPreferences.Add(
                    existing
                );
            }
            else
            {
                // =================================================
                // UPDATE CATEGORY
                // =================================================
                existing.PreferredCategoryID =
                    parsed.PreferredCategoryID;


                // =================================================
                // UPDATE GENDER
                // =================================================
                existing.PreferredGender =
                    string.IsNullOrWhiteSpace(
                        parsed.PreferredGender)
                        ? "Any"
                        : parsed.PreferredGender;


                // =================================================
                // UPDATE AGE
                // =================================================
                existing.MinimumAge =
                    parsed.MinimumAge;


                existing.MaximumAge =
                    parsed.MaximumAge;


                // =================================================
                // UPDATE BREED
                // =================================================
                existing.PreferredBreed =
                    parsed.PreferredBreed;


                // =================================================
                // SAVE ORIGINAL USER SENTENCE
                // =================================================
                existing.AdditionalNotes =
                    cleanPreferenceText;


                // =================================================
                // OLD EMPTY VALUES SAFETY
                // =================================================
                if (string.IsNullOrWhiteSpace(
                    existing.LivingType))
                {
                    existing.LivingType =
                        "House";
                }


                if (string.IsNullOrWhiteSpace(
                    existing.ActivityLevel))
                {
                    existing.ActivityLevel =
                        "Medium";
                }


                if (string.IsNullOrWhiteSpace(
                    existing.PetExperience))
                {
                    existing.PetExperience =
                        "Beginner";
                }


                existing.UpdatedAt =
                    DateTime.Now;
            }


            // =================================================
            // SAVE DATABASE
            // =================================================
            await _db.SaveChangesAsync();


            // =================================================
            // SUCCESS
            // =================================================
            TempData["Success"] =
                "Your preference was understood and saved successfully.";


            // =================================================
            // SHOW AI INTERPRETATION
            // =================================================
            TempData["PreferenceSummary"] =
                parsed.Summary;


            

            return RedirectToAction(
                nameof(Index)
            );
        }
    }
}