using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using PetWAMS.Web.Data;
using PetWAMS.Web.MLModels;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Services
{
    // =========================================================
    // ML RECOMMENDATION RESULT
    // =========================================================
    public class MlPetRecommendationResult
    {
        public Pet? Pet { get; set; }

        public int MatchScore { get; set; }

        public List<string> Reasons { get; set; }
            = new List<string>();
    }


    // =========================================================
    // ML.NET PET RECOMMENDATION SERVICE
    // =========================================================
    public class MlPetRecommendationService
    {
        private readonly PetWamsDbContext _db;

        private readonly MLContext _mlContext;


        public MlPetRecommendationService(
            PetWamsDbContext db)
        {
            _db = db;

            _mlContext =
                new MLContext(
                    seed: 1
                );
        }


        // =====================================================
        // GET BEST PET FOR ADOPTER
        // =====================================================
        public async Task<MlPetRecommendationResult?>
            GetBestPetForAdopterAsync(
                int userId)
        {
            // =================================================
            // LOAD ADOPTER PREFERENCE
            // =================================================
            var adopterPreference =
                await _db.AdopterPreferences
                    .Include(p =>
                        p.PreferredCategory)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserID ==
                            userId
                    );


            if (adopterPreference == null)
            {
                return null;
            }


            // =================================================
            // GET CATEGORY NAME
            // =================================================
            string preferredCategory =
                await GetPreferredCategoryNameAsync(
                    adopterPreference
                );


            // =================================================
            // LOAD PETS
            //
            // IMPORTANT:
            //
            // Do NOT category-filter here.
            //
            // ML.NET needs both:
            // - matching examples
            // - non-matching examples
            //
            // This allows FastTree to learn positive and
            // negative training samples.
            // =================================================
            var allCandidatePets =
                await _db.Pets
                    .Include(p =>
                        p.Category)

                    .Where(p =>
                        !_db.Adoptions.Any(
                            a =>
                                a.PetID ==
                                p.PetID
                        )
                        &&
                        !_db.AdoptionRequests.Any(
                            r =>
                                r.PetID ==
                                p.PetID
                                &&
                                r.Status != null
                                &&
                                r.Status
                                    .Trim()
                                    .ToLower()
                                    == "approved"
                        )
                    )

                    .AsNoTracking()

                    .ToListAsync();


            // =================================================
            // KEEP ONLY AVAILABLE PETS
            //
            // Supports:
            //
            // Available
            // Available for Adoption
            // Available For Adoption
            //
            // =================================================
            var pets =
                allCandidatePets
                    .Where(p =>
                        IsAvailableStatus(
                            p.Status
                        )
                    )
                    .ToList();


            if (pets.Count == 0)
            {
                return null;
            }


            // =================================================
            // BUILD TRAINING DATA FROM ALL AVAILABLE PETS
            //
            // Category mismatches become negative examples.
            // =================================================
            var trainingData =
                BuildTrainingData(
                    adopterPreference,
                    preferredCategory,
                    pets
                );


            PredictionEngine<
                PetMatchInput,
                PetMatchPrediction>?
                predictionEngine =
                    null;


            // =================================================
            // FASTTREE NEEDS BOTH TRUE + FALSE LABELS
            // =================================================
            bool canTrain =
                trainingData.Count >= 2
                &&
                trainingData.Any(
                    x =>
                        x.Label == true
                )
                &&
                trainingData.Any(
                    x =>
                        x.Label == false
                );


            // =================================================
            // TRAIN ML.NET FASTTREE
            // =================================================
            if (canTrain)
            {
                try
                {
                    var dataView =
                        _mlContext.Data
                            .LoadFromEnumerable(
                                trainingData
                            );


                    var pipeline =
                        _mlContext.Transforms
                            .Categorical
                            .OneHotEncoding(
                                outputColumnName:
                                    "PreferredCategoryEncoded",

                                inputColumnName:
                                    nameof(
                                        PetMatchInput
                                            .PreferredCategory
                                    )
                            )


                        .Append(
                            _mlContext.Transforms
                                .Categorical
                                .OneHotEncoding(
                                    outputColumnName:
                                        "PreferredBreedEncoded",

                                    inputColumnName:
                                        nameof(
                                            PetMatchInput
                                                .PreferredBreed
                                        )
                                )
                        )


                        .Append(
                            _mlContext.Transforms
                                .Categorical
                                .OneHotEncoding(
                                    outputColumnName:
                                        "PreferredGenderEncoded",

                                    inputColumnName:
                                        nameof(
                                            PetMatchInput
                                                .PreferredGender
                                        )
                                )
                        )


                        .Append(
                            _mlContext.Transforms
                                .Categorical
                                .OneHotEncoding(
                                    outputColumnName:
                                        "PetCategoryEncoded",

                                    inputColumnName:
                                        nameof(
                                            PetMatchInput
                                                .PetCategory
                                        )
                                )
                        )


                        .Append(
                            _mlContext.Transforms
                                .Categorical
                                .OneHotEncoding(
                                    outputColumnName:
                                        "PetBreedEncoded",

                                    inputColumnName:
                                        nameof(
                                            PetMatchInput
                                                .PetBreed
                                        )
                                )
                        )


                        .Append(
                            _mlContext.Transforms
                                .Categorical
                                .OneHotEncoding(
                                    outputColumnName:
                                        "PetGenderEncoded",

                                    inputColumnName:
                                        nameof(
                                            PetMatchInput
                                                .PetGender
                                        )
                                )
                        )


                        .Append(
                            _mlContext.Transforms
                                .Categorical
                                .OneHotEncoding(
                                    outputColumnName:
                                        "PetStatusEncoded",

                                    inputColumnName:
                                        nameof(
                                            PetMatchInput
                                                .PetStatus
                                        )
                                )
                        )


                        .Append(
                            _mlContext.Transforms
                                .Concatenate(
                                    "Features",

                                    "PreferredCategoryEncoded",

                                    "PreferredBreedEncoded",

                                    "PreferredGenderEncoded",

                                    nameof(
                                        PetMatchInput
                                            .MinimumAge
                                    ),

                                    nameof(
                                        PetMatchInput
                                            .MaximumAge
                                    ),

                                    "PetCategoryEncoded",

                                    "PetBreedEncoded",

                                    "PetGenderEncoded",

                                    nameof(
                                        PetMatchInput
                                            .PetAge
                                    ),

                                    "PetStatusEncoded"
                                )
                        )


                        .Append(
                            _mlContext
                                .BinaryClassification
                                .Trainers
                                .FastTree(
                                    labelColumnName:
                                        nameof(
                                            PetMatchInput
                                                .Label
                                        ),

                                    featureColumnName:
                                        "Features"
                                )
                        );


                    var trainedModel =
                        pipeline.Fit(
                            dataView
                        );


                    predictionEngine =
                        _mlContext.Model
                            .CreatePredictionEngine<
                                PetMatchInput,
                                PetMatchPrediction>(
                                trainedModel
                            );
                }
                catch
                {
                    // =========================================
                    // If ML training fails,
                    // recommendation still works using
                    // preference rule score.
                    // =========================================
                    predictionEngine =
                        null;
                }
            }


            // =================================================
            // FIND BEST COMPATIBLE PET
            // =================================================
            MlPetRecommendationResult?
                bestResult =
                    null;


            foreach (var pet in pets)
            {
                // =============================================
                // IMPORTANT:
                //
                // We allowed non-matching categories into
                // TRAINING DATA.
                //
                // But final recommendation must still respect
                // adopter's pet-type preference.
                // =============================================
                if (
                    HasRealCategoryPreference(
                        preferredCategory
                    )
                    &&
                    !IsPreferredCategoryCompatible(
                        preferredCategory,
                        GetPetCategoryName(
                            pet
                        ),
                        pet.Breed ?? ""
                    )
                )
                {
                    continue;
                }


                // =============================================
                // RULE SCORE
                // =============================================
                int ruleScore =
                    CalculateRuleScore(
                        adopterPreference,
                        preferredCategory,
                        pet
                    );


                // =============================================
                // DEFAULT ML SCORE
                //
                // If model cannot train,
                // fallback safely to rule score.
                // =============================================
                int mlScore =
                    ruleScore;


                // =============================================
                // ML.NET PREDICTION
                // =============================================
                if (predictionEngine != null)
                {
                    try
                    {
                        var input =
                            CreateInput(
                                adopterPreference,
                                preferredCategory,
                                pet,
                                false
                            );


                        var prediction =
                            predictionEngine
                                .Predict(
                                    input
                                );


                        mlScore =
                            Convert.ToInt32(
                                Math.Round(
                                    prediction
                                        .Probability
                                    *
                                    100
                                )
                            );
                    }
                    catch
                    {
                        mlScore =
                            ruleScore;
                    }
                }


                // =============================================
                // HYBRID SCORE
                //
                // ML probability + preference rule protection
                // =============================================
                int finalScore =
                    CalculateFinalScore(
                        ruleScore,
                        mlScore
                    );


                var result =
                    new MlPetRecommendationResult
                    {
                        Pet =
                            pet,

                        MatchScore =
                            ClampScore(
                                finalScore
                            ),

                        Reasons =
                            BuildReasons(
                                adopterPreference,
                                preferredCategory,
                                pet
                            )
                    };


                // =============================================
                // KEEP HIGHEST-SCORE PET
                // =============================================
                if (
                    bestResult == null
                    ||
                    result.MatchScore >
                    bestResult.MatchScore
                )
                {
                    bestResult =
                        result;
                }
            }


            return bestResult;
        }


        // =====================================================
        // GET PREFERRED CATEGORY NAME
        // =====================================================
        private async Task<string>
            GetPreferredCategoryNameAsync(
                AdopterPreference preference)
        {
            // =================================================
            // NAVIGATION PROPERTY ALREADY LOADED
            // =================================================
            if (
                preference.PreferredCategory
                != null
                &&
                !string.IsNullOrWhiteSpace(
                    preference
                        .PreferredCategory
                        .CategoryName
                )
            )
            {
                return
                    preference
                        .PreferredCategory
                        .CategoryName
                        .Trim();
            }


            // =================================================
            // FALLBACK USING CATEGORY ID
            // =================================================
            if (
                preference
                    .PreferredCategoryID
                    .HasValue
            )
            {
                string? categoryName =
                    await _db.PetCategories
                        .AsNoTracking()

                        .Where(c =>
                            c.CategoryID
                            ==
                            preference
                                .PreferredCategoryID
                                .Value
                        )

                        .Select(c =>
                            c.CategoryName
                        )

                        .FirstOrDefaultAsync();


                return
                    categoryName?
                        .Trim()
                    ??
                    "";
            }


            return "";
        }


        // =====================================================
        // BUILD TRAINING DATA
        // =====================================================
        private List<PetMatchInput>
            BuildTrainingData(
                AdopterPreference adopterPreference,
                string preferredCategory,
                List<Pet> pets)
        {
            var trainingData =
                new List<PetMatchInput>();


            foreach (var pet in pets)
            {
                int score =
                    CalculateRuleScore(
                        adopterPreference,
                        preferredCategory,
                        pet
                    );


                // =============================================
                // 70+ = Good Match
                //
                // Below 70 = Negative example
                // =============================================
                bool label =
                    score >= 70;


                trainingData.Add(
                    CreateInput(
                        adopterPreference,
                        preferredCategory,
                        pet,
                        label
                    )
                );
            }


            return trainingData;
        }


        // =====================================================
        // CREATE ML INPUT
        // =====================================================
        private PetMatchInput CreateInput(
            AdopterPreference adopterPreference,
            string preferredCategory,
            Pet pet,
            bool label)
        {
            return new PetMatchInput
            {
                PreferredCategory =
                    preferredCategory
                    ??
                    "",


                PreferredBreed =
                    CleanPreferenceText(
                        adopterPreference
                            .PreferredBreed
                    ),


                PreferredGender =
                    CleanPreferenceText(
                        adopterPreference
                            .PreferredGender
                    ),


                MinimumAge =
                    adopterPreference
                        .MinimumAge
                    ??
                    0,


                MaximumAge =
                    adopterPreference
                        .MaximumAge
                    ??
                    0,


                PetCategory =
                    GetPetCategoryName(
                        pet
                    ),


                PetBreed =
                    pet.Breed
                    ??
                    "",


                PetGender =
                    pet.Gender
                    ??
                    "",


                PetAge =
                    pet.Age
                    ??
                    0,


                PetStatus =
                    pet.Status
                    ??
                    "",


                Label =
                    label
            };
        }


        // =====================================================
        // RULE SCORE
        // =====================================================
        private int CalculateRuleScore(
            AdopterPreference adopterPreference,
            string preferredCategory,
            Pet pet)
        {
            int score =
                0;


            var input =
                CreateInput(
                    adopterPreference,
                    preferredCategory,
                    pet,
                    false
                );


            // =================================================
            // CATEGORY = 45
            //
            // Wrong requested pet type receives 0 total score.
            // This creates negative training examples.
            // =================================================
            if (
                HasRealCategoryPreference(
                    input.PreferredCategory
                )
            )
            {
                bool categoryMatched =
                    IsPreferredCategoryCompatible(
                        input.PreferredCategory,
                        input.PetCategory,
                        input.PetBreed
                    );


                if (!categoryMatched)
                {
                    return 0;
                }


                score += 45;
            }


            // =================================================
            // BREED = 20
            // =================================================
            if (
                HasRealPreference(
                    input.PreferredBreed
                )
            )
            {
                if (
                    input.PreferredBreed
                        .Equals(
                            input.PetBreed,
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                )
                {
                    score += 20;
                }
                else if (
                    !string.IsNullOrWhiteSpace(
                        input.PetBreed
                    )
                    &&
                    (
                        input.PreferredBreed
                            .Contains(
                                input.PetBreed,
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                        ||
                        input.PetBreed
                            .Contains(
                                input.PreferredBreed,
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                    )
                )
                {
                    score += 10;
                }
            }


            // =================================================
            // GENDER = 20
            // =================================================
            if (
                HasRealPreference(
                    input.PreferredGender
                )
                &&
                !input.PreferredGender
                    .Equals(
                        "Any",
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            )
            {
                if (
                    input.PreferredGender
                        .Equals(
                            input.PetGender,
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                )
                {
                    score += 20;
                }
            }


            // =================================================
            // AGE = 15
            // =================================================
            bool ageMatched =
                true;


            if (
                input.MinimumAge > 0
                &&
                input.PetAge <
                input.MinimumAge
            )
            {
                ageMatched =
                    false;
            }


            if (
                input.MaximumAge > 0
                &&
                input.PetAge >
                input.MaximumAge
            )
            {
                ageMatched =
                    false;
            }


            if (ageMatched)
            {
                score += 15;
            }


            // =================================================
            // AVAILABILITY = 10
            // =================================================
            if (
                IsAvailableStatus(
                    input.PetStatus
                )
            )
            {
                score += 10;
            }


            return
                ClampScore(
                    score
                );
        }


        // =====================================================
        // HYBRID FINAL SCORE
        // =====================================================
        private int CalculateFinalScore(
            int ruleScore,
            int mlScore)
        {
            // =================================================
            // ML.NET contributes 60%
            // Preference matching contributes 40%
            //
            // This prevents a strange ML probability from
            // completely ignoring explicit adopter preference.
            // =================================================
            double combined =
                (mlScore * 0.60)
                +
                (ruleScore * 0.40);


            return
                ClampScore(
                    Convert.ToInt32(
                        Math.Round(
                            combined
                        )
                    )
                );
        }


        // =====================================================
        // RECOMMENDATION REASONS
        // =====================================================
        private List<string> BuildReasons(
            AdopterPreference adopterPreference,
            string preferredCategory,
            Pet pet)
        {
            var reasons =
                new List<string>();


            var input =
                CreateInput(
                    adopterPreference,
                    preferredCategory,
                    pet,
                    false
                );


            // =================================================
            // CATEGORY
            // =================================================
            if (
                HasRealCategoryPreference(
                    input.PreferredCategory
                )
                &&
                IsPreferredCategoryCompatible(
                    input.PreferredCategory,
                    input.PetCategory,
                    input.PetBreed
                )
            )
            {
                if (
                    TextMatches(
                        input.PreferredCategory,
                        input.PetCategory
                    )
                )
                {
                    reasons.Add(
                        $"Preferred pet type matched: {input.PetCategory}"
                    );
                }
                else
                {
                    reasons.Add(
                        $"Pet type matched with your preference: {input.PetCategory}"
                    );
                }
            }


            // =================================================
            // BREED
            // =================================================
            if (
                HasRealPreference(
                    input.PreferredBreed
                )
            )
            {
                if (
                    input.PreferredBreed
                        .Equals(
                            input.PetBreed,
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                )
                {
                    reasons.Add(
                        $"Breed matched: {input.PetBreed}"
                    );
                }
                else if (
                    !string.IsNullOrWhiteSpace(
                        input.PetBreed
                    )
                    &&
                    (
                        input.PreferredBreed
                            .Contains(
                                input.PetBreed,
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                        ||
                        input.PetBreed
                            .Contains(
                                input.PreferredBreed,
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                    )
                )
                {
                    reasons.Add(
                        $"Breed partially matched: {input.PetBreed}"
                    );
                }
            }


            // =================================================
            // GENDER
            // =================================================
            if (
                HasRealPreference(
                    input.PreferredGender
                )
                &&
                !input.PreferredGender
                    .Equals(
                        "Any",
                        StringComparison
                            .OrdinalIgnoreCase
                    )
                &&
                input.PreferredGender
                    .Equals(
                        input.PetGender,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            )
            {
                reasons.Add(
                    $"Gender matched: {input.PetGender}"
                );
            }


            // =================================================
            // AGE
            // =================================================
            bool ageMatched =
                true;


            if (
                input.MinimumAge > 0
                &&
                input.PetAge <
                input.MinimumAge
            )
            {
                ageMatched =
                    false;
            }


            if (
                input.MaximumAge > 0
                &&
                input.PetAge >
                input.MaximumAge
            )
            {
                ageMatched =
                    false;
            }


            if (ageMatched)
            {
                reasons.Add(
                    $"Age matched: {input.PetAge} years"
                );
            }


            // =================================================
            // AVAILABILITY
            // =================================================
            if (
                IsAvailableStatus(
                    input.PetStatus
                )
            )
            {
                reasons.Add(
                    "Pet is currently available for adoption"
                );
            }


            reasons.Add(
                "Pet is not already adopted or approved for another adopter"
            );


            if (reasons.Count == 0)
            {
                reasons.Add(
                    "ML.NET selected this pet as the closest available match."
                );
            }


            return reasons;
        }


        // =====================================================
        // AVAILABLE STATUS CHECK
        // =====================================================
        private bool IsAvailableStatus(
            string? status)
        {
            string value =
                NormalizeText(
                    status ?? ""
                );


            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return false;
            }


            return
                value == "available"
                ||
                value == "available for adoption"
                ||
                value == "ready for adoption";
        }


        // =====================================================
        // REAL CATEGORY PREFERENCE?
        // =====================================================
        private bool HasRealCategoryPreference(
            string value)
        {
            value =
                NormalizeText(
                    value
                );


            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return false;
            }


            return
                value != "any"
                &&
                value != "all"
                &&
                value != "all pet"
                &&
                value != "all pets"
                &&
                value != "other";
        }


        // =====================================================
        // REAL PREFERENCE?
        // =====================================================
        private bool HasRealPreference(
            string? value)
        {
            value =
                NormalizeText(
                    value ?? ""
                );


            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return false;
            }


            return
                value != "any"
                &&
                value != "all"
                &&
                value != "none"
                &&
                value != "no"
                &&
                value != "no breed"
                &&
                value != "not specific"
                &&
                value != "not specified";
        }


        // =====================================================
        // CLEAN PREFERENCE
        // =====================================================
        private string CleanPreferenceText(
            string? value)
        {
            if (
                !HasRealPreference(
                    value
                )
            )
            {
                return "";
            }


            return
                value?.Trim()
                ??
                "";
        }


        // =====================================================
        // CATEGORY COMPATIBILITY
        // =====================================================
        private bool IsPreferredCategoryCompatible(
            string preferredCategory,
            string petCategory,
            string petBreed)
        {
            preferredCategory =
                NormalizeText(
                    preferredCategory
                );


            petCategory =
                NormalizeText(
                    petCategory
                );


            petBreed =
                NormalizeText(
                    petBreed
                );


            if (
                !HasRealCategoryPreference(
                    preferredCategory
                )
            )
            {
                return true;
            }


            // =================================================
            // DIRECT MATCH
            // =================================================
            if (
                TextMatches(
                    preferredCategory,
                    petCategory
                )
                ||
                TextMatches(
                    preferredCategory,
                    petBreed
                )
            )
            {
                return true;
            }


            // =================================================
            // FAMILY MATCH
            //
            // Cat + Persian
            // Dog + Labrador
            // =================================================
            if (
                IsGenericPetType(
                    preferredCategory
                )
            )
            {
                string preferredFamily =
                    GetAnimalFamily(
                        preferredCategory
                    );


                string categoryFamily =
                    GetAnimalFamily(
                        petCategory
                    );


                string breedFamily =
                    GetAnimalFamily(
                        petBreed
                    );


                if (
                    !string.IsNullOrWhiteSpace(
                        preferredFamily
                    )
                    &&
                    (
                        preferredFamily
                        ==
                        categoryFamily
                        ||
                        preferredFamily
                        ==
                        breedFamily
                    )
                )
                {
                    return true;
                }
            }


            return false;
        }


        // =====================================================
        // TEXT MATCH
        // =====================================================
        private bool TextMatches(
            string preferred,
            string actual)
        {
            preferred =
                NormalizeText(
                    preferred
                );


            actual =
                NormalizeText(
                    actual
                );


            if (
                string.IsNullOrWhiteSpace(
                    preferred
                )
                ||
                string.IsNullOrWhiteSpace(
                    actual
                )
            )
            {
                return false;
            }


            if (
                preferred.Equals(
                    actual,
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return true;
            }


            if (
                actual.Contains(
                    preferred,
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return true;
            }


            if (
                preferred.Contains(
                    actual,
                    StringComparison
                        .OrdinalIgnoreCase
                )
                &&
                actual.Length >= 3
            )
            {
                return true;
            }


            return false;
        }


        // =====================================================
        // GENERIC PET TYPE
        // =====================================================
        private bool IsGenericPetType(
            string value)
        {
            value =
                NormalizeText(
                    value
                );


            return
                value == "dog"
                ||
                value == "cat"
                ||
                value == "bird"
                ||
                value == "rabbit"
                ||
                value == "fish"
                ||
                value == "hamster";
        }


        // =====================================================
        // ANIMAL FAMILY
        // =====================================================
        private string GetAnimalFamily(
            string value)
        {
            value =
                NormalizeText(
                    value
                );


            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return string.Empty;
            }


            // =================================================
            // CAT FAMILY
            // =================================================
            if (
                value.Contains("cat")
                ||
                value.Contains("persian")
                ||
                value.Contains("bengal")
                ||
                value.Contains("maine coon")
                ||
                value.Contains("british shorthair")
                ||
                value.Contains("siamese")
                ||
                value.Contains("ragdoll")
                ||
                value.Contains("sphynx")
                ||
                value.Contains("scottish fold")
            )
            {
                return "cat";
            }


            // =================================================
            // DOG FAMILY
            // =================================================
            if (
                value.Contains("dog")
                ||
                value.Contains("labrador")
                ||
                value.Contains("golden retriever")
                ||
                value.Contains("german shepherd")
                ||
                value.Contains("husky")
                ||
                value.Contains("beagle")
                ||
                value.Contains("poodle")
                ||
                value.Contains("rottweiler")
                ||
                value.Contains("pug")
                ||
                value.Contains("bulldog")
                ||
                value.Contains("doberman")
                ||
                value.Contains("chihuahua")
                ||
                value.Contains("pomeranian")
            )
            {
                return "dog";
            }


            // =================================================
            // BIRD FAMILY
            // =================================================
            if (
                value.Contains("bird")
                ||
                value.Contains("parrot")
                ||
                value.Contains("cockatiel")
                ||
                value.Contains("budgie")
                ||
                value.Contains("pigeon")
                ||
                value.Contains("lovebird")
                ||
                value.Contains("macaw")
            )
            {
                return "bird";
            }


            // =================================================
            // RABBIT
            // =================================================
            if (
                value.Contains("rabbit")
                ||
                value.Contains("bunny")
            )
            {
                return "rabbit";
            }


            // =================================================
            // HAMSTER
            // =================================================
            if (
                value.Contains("hamster")
            )
            {
                return "hamster";
            }


            // =================================================
            // FISH
            // =================================================
            if (
                value.Contains("fish")
                ||
                value.Contains("gold fish")
                ||
                value.Contains("goldfish")
            )
            {
                return "fish";
            }


            return value;
        }


        // =====================================================
        // NORMALIZE TEXT
        // =====================================================
        private string NormalizeText(
            string value)
        {
            value =
                (value ?? "")
                    .ToLowerInvariant()
                    .Trim();


            value =
                value.Replace(
                    "-",
                    " "
                );


            value =
                value.Replace(
                    "_",
                    " "
                );


            while (
                value.Contains(
                    "  "
                )
            )
            {
                value =
                    value.Replace(
                        "  ",
                        " "
                    );
            }


            // =================================================
            // SIMPLE PLURAL NORMALIZATION
            //
            // dogs -> dog
            // cats -> cat
            //
            // But do not damage words such as:
            // Persian
            // =================================================
            if (
                value.EndsWith("s")
                &&
                value.Length > 3
                &&
                !value.EndsWith("ss")
            )
            {
                value =
                    value.Substring(
                        0,
                        value.Length - 1
                    );
            }


            return value;
        }


        // =====================================================
        // GET PET CATEGORY NAME
        // =====================================================
        private string GetPetCategoryName(
            Pet pet)
        {
            if (
                pet.Category != null
                &&
                !string.IsNullOrWhiteSpace(
                    pet.Category
                        .CategoryName
                )
            )
            {
                return
                    pet.Category
                        .CategoryName
                        .Trim();
            }


            return string.Empty;
        }


        // =====================================================
        // SCORE LIMIT
        // =====================================================
        private int ClampScore(
            int score)
        {
            if (score < 0)
            {
                return 0;
            }


            if (score > 100)
            {
                return 100;
            }


            return score;
        }
    }
}