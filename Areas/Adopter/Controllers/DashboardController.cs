using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;
using PetWAMS.Web.Services;

namespace PetWAMS.Web.Areas.Adopter.Controllers
{
    [Area("Adopter")]
    public class DashboardController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly MlPetRecommendationService _mlRecommendationService;


        public DashboardController(
            PetWamsDbContext db,
            MlPetRecommendationService mlRecommendationService)
        {
            _db = db;
            _mlRecommendationService = mlRecommendationService;
        }


        // =====================================================
        // ADOPTER DASHBOARD
        // =====================================================
        public async Task<IActionResult> Index()
        {
            // =================================================
            // LOGIN CHECK
            // =================================================
            int? userId =
                HttpContext.Session.GetInt32("UserID");

            string? userType =
                HttpContext.Session.GetString("UserType");


            if (userId == null ||
                string.IsNullOrWhiteSpace(userType) ||
                !userType.Equals(
                    "Adopter",
                    StringComparison.OrdinalIgnoreCase))
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


            int adopterUserId =
                userId.Value;


            // =================================================
            // LOAD USER
            // =================================================
            var user =
                await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserID ==
                            adopterUserId
                    );


            ViewBag.User =
                user;


            ViewBag.FullName =
                user?.FullName
                ??
                HttpContext.Session.GetString("FullName")
                ??
                "Adopter";


            ViewBag.Email =
                user?.Email
                ??
                HttpContext.Session.GetString("Email")
                ??
                "";


            ViewBag.Phone =
                user?.Phone
                ??
                HttpContext.Session.GetString("Phone")
                ??
                "";


            ViewBag.Address =
                user?.Address
                ??
                HttpContext.Session.GetString("Address")
                ??
                "";


            // =================================================
            // LOAD ADOPTION REQUESTS
            // =================================================
            var myRequests =
                await _db.AdoptionRequests
                    .Include(r => r.Pet)

                    .Where(r =>
                        r.UserID ==
                        adopterUserId)

                    .OrderByDescending(r =>
                        r.RequestDate)

                    .ToListAsync();


            // =================================================
            // DASHBOARD STATISTICS
            // =================================================
            ViewBag.TotalRequests =
                myRequests.Count;


            ViewBag.PendingCount =
                myRequests.Count(
                    r =>
                        !string.IsNullOrWhiteSpace(
                            r.Status)
                        &&
                        r.Status.Equals(
                            "Pending",
                            StringComparison.OrdinalIgnoreCase)
                );


            ViewBag.ApprovedCount =
                myRequests.Count(
                    r =>
                        !string.IsNullOrWhiteSpace(
                            r.Status)
                        &&
                        r.Status.Equals(
                            "Approved",
                            StringComparison.OrdinalIgnoreCase)
                );


            ViewBag.PendingReview =
                ViewBag.PendingCount;


            ViewBag.SuccessfulAdoptions =
                ViewBag.ApprovedCount;


            // =================================================
            // ACTIVE REQUEST CHECK
            //
            // IMPORTANT:
            // This is IN-MEMORY because myRequests is already
            // loaded with ToListAsync().
            //
            // Therefore StringComparison is safe here.
            // =================================================
            bool hasActiveRequest =
                myRequests.Any(
                    r =>
                        !string.IsNullOrWhiteSpace(
                            r.Status)
                        &&
                        (
                            r.Status.Equals(
                                "Pending",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            r.Status.Equals(
                                "Approved",
                                StringComparison.OrdinalIgnoreCase)
                        )
                );


            ViewBag.HasActiveRequest =
                hasActiveRequest;


            // =================================================
            // LOAD NATURAL LANGUAGE PREFERENCE
            // =================================================
            var currentPreference =
                await _db.AdopterPreferences
                    .AsNoTracking()

                    .Include(p =>
                        p.PreferredCategory)

                    .FirstOrDefaultAsync(
                        p =>
                            p.UserID ==
                            adopterUserId
                    );


            // =================================================
            // SEND PREFERENCE TO VIEW
            // =================================================
            ViewBag.CurrentPreference =
                currentPreference;


            ViewBag.HasSavedPreference =
                currentPreference != null;


            ViewBag.PreferenceText =
                currentPreference?.AdditionalNotes
                ??
                "";


            ViewBag.PreferredCategory =
                currentPreference?
                    .PreferredCategory?
                    .CategoryName
                ??
                "";


            ViewBag.PreferredGender =
                currentPreference?
                    .PreferredGender
                ??
                "Any";


            ViewBag.PreferredBreed =
                currentPreference?
                    .PreferredBreed
                ??
                "";


            ViewBag.PreferredMinimumAge =
                currentPreference?
                    .MinimumAge;


            ViewBag.PreferredMaximumAge =
                currentPreference?
                    .MaximumAge;


            // =================================================
            // NO SAVED PREFERENCE
            // =================================================
            if (currentPreference == null)
            {
                ViewBag.IsNewRecommendation =
                    false;


                ViewBag.LatestRecommendation =
                    null;


                ViewBag.HasAiRecommendation =
                    false;


                ViewBag.NoRecommendationMessage =
                    "Please save your pet preference first. After saving it, ML.NET AI will compare your preference with available pets.";


                return View(
                    myRequests
                );
            }


            // =================================================
            // REMOVE OLD GENERATED RECOMMENDATIONS
            // =================================================
            var oldRecommendations =
                await _db.PetRecommendations

                    .Where(r =>
                        r.UserID ==
                        adopterUserId)

                    .ToListAsync();


            if (oldRecommendations.Count > 0)
            {
                _db.PetRecommendations
                    .RemoveRange(
                        oldRecommendations
                    );


                await _db.SaveChangesAsync();
            }


            // =================================================
            // GENERATE NEW AI RECOMMENDATION
            // =================================================
            PetRecommendation? latestRecommendation =
                null;


            try
            {
                var aiResult =
                    await _mlRecommendationService
                        .GetBestPetForAdopterAsync(
                            adopterUserId
                        );


                // =================================================
                // CHECK PET IS STILL VALID
                // =================================================
                if (aiResult != null &&
                    aiResult.Pet != null)
                {
                    // =============================================
                    // ALREADY ADOPTED?
                    // =============================================
                    bool alreadyAdopted =
                        await _db.Adoptions
                            .AnyAsync(
                                a =>
                                    a.PetID ==
                                    aiResult.Pet.PetID
                            );


                    // =============================================
                    // ALREADY APPROVED FOR SOMEONE?
                    //
                    // IMPORTANT FIX:
                    //
                    // Do NOT use:
                    //
                    // .Equals(
                    //   "Approved",
                    //   StringComparison.OrdinalIgnoreCase
                    // )
                    //
                    // inside EF Core query.
                    // =============================================
                    bool alreadyApproved =
                        await _db.AdoptionRequests
                            .AnyAsync(
                                r =>
                                    r.PetID ==
                                    aiResult.Pet.PetID
                                    &&
                                    r.Status != null
                                    &&
                                    r.Status
                                        .ToLower()
                                        == "approved"
                            );


                    // =============================================
                    // IF PET CANNOT BE USED
                    // =============================================
                    if (alreadyAdopted ||
                        alreadyApproved)
                    {
                        aiResult =
                            null;
                    }
                }


                // =================================================
                // SAVE AI RESULT
                // =================================================
                if (aiResult != null &&
                    aiResult.Pet != null)
                {
                    string recommendationReason =
                        "ML.NET AI generated based on your saved natural-language pet preference.";


                    if (aiResult.Reasons != null &&
                        aiResult.Reasons.Count > 0)
                    {
                        recommendationReason =
                            "ML.NET AI generated: "
                            +
                            string.Join(
                                " | ",
                                aiResult.Reasons
                            );
                    }


                    var newRecommendation =
                        new PetRecommendation
                        {
                            UserID =
                                adopterUserId,

                            PetID =
                                aiResult.Pet.PetID,

                            MatchScore =
                                aiResult.MatchScore,

                            RecommendationReason =
                                recommendationReason,

                            IsViewed =
                                false,

                            CreatedAt =
                                DateTime.Now
                        };


                    _db.PetRecommendations
                        .Add(
                            newRecommendation
                        );


                    await _db.SaveChangesAsync();


                    // =============================================
                    // LOAD COMPLETE PET DETAILS
                    // =============================================
                    latestRecommendation =
                        await _db.PetRecommendations

                            .Include(r =>
                                r.Pet)
                                .ThenInclude(p =>
                                    p.Category)

                            .Include(r =>
                                r.Pet)
                                .ThenInclude(p =>
                                    p.Shelter)

                            .FirstOrDefaultAsync(
                                r =>
                                    r.RecommendationID ==
                                    newRecommendation
                                        .RecommendationID
                            );
                }
            }
            catch (Exception ex)
            {
                latestRecommendation =
                    null;


                ViewBag.AiRecommendationError =
                    ex.Message;
            }


            // =================================================
            // SEND AI RESULT TO VIEW
            // =================================================
            ViewBag.LatestRecommendation =
                latestRecommendation;


            ViewBag.HasAiRecommendation =
                latestRecommendation != null;


            ViewBag.IsNewRecommendation =
                latestRecommendation != null
                &&
                !latestRecommendation.IsViewed;


            // =================================================
            // NO RESULT MESSAGE
            // =================================================
            if (latestRecommendation == null)
            {
                string? aiError =
                    ViewBag.AiRecommendationError
                        as string;


                if (!string.IsNullOrWhiteSpace(
                    aiError))
                {
                    ViewBag.NoRecommendationMessage =
                        "AI recommendation could not be generated. Error: "
                        +
                        aiError;
                }
                else
                {
                    ViewBag.NoRecommendationMessage =
                        "No suitable available pet match was found for your saved preference.";
                }
            }
            else
            {
                ViewBag.NoRecommendationMessage =
                    "";
            }


            // =================================================
            // MARK AS VIEWED
            // =================================================
            if (latestRecommendation != null &&
                !latestRecommendation.IsViewed)
            {
                latestRecommendation.IsViewed =
                    true;


                await _db.SaveChangesAsync();
            }


            return View(
                myRequests
            );
        }
    }
}