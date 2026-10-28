using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace PetWAMS.Web.Services
{
    // =========================================================
    // PARSED NATURAL LANGUAGE RESULT
    // =========================================================
    public class NaturalLanguagePreferenceResult
    {
        public string OriginalText { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        public int? PreferredCategoryID { get; set; }

        public string PreferredCategoryName { get; set; } = string.Empty;

        public string PreferredGender { get; set; } = "Any";

        public int? MinimumAge { get; set; }

        public int? MaximumAge { get; set; }

        public string PreferredBreed { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }


    // =========================================================
    // NATURAL LANGUAGE PREFERENCE SERVICE
    // =========================================================
    public class NaturalLanguagePreferenceService
    {
        private readonly PetWamsDbContext _db;


        public NaturalLanguagePreferenceService(
            PetWamsDbContext db)
        {
            _db = db;
        }


        // =====================================================
        // COMMON CATEGORY ALIASES
        // =====================================================
        private static readonly Dictionary<string, string[]>
            CategoryAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Dog",
                    new[]
                    {
                        "dog",
                        "dogs",
                        "puppy",
                        "puppies",
                        "doggy",
                        "doggie"
                    }
                },

                {
                    "Cat",
                    new[]
                    {
                        "cat",
                        "cats",
                        "kitten",
                        "kittens",
                        "kitty"
                    }
                },

                {
                    "Bird",
                    new[]
                    {
                        "bird",
                        "birds",
                        "parrot",
                        "parrots"
                    }
                },

                {
                    "Rabbit",
                    new[]
                    {
                        "rabbit",
                        "rabbits",
                        "bunny",
                        "bunnies"
                    }
                },

                {
                    "Fish",
                    new[]
                    {
                        "fish",
                        "fishes"
                    }
                }
            };


        // =====================================================
        // COMMON BREEDS
        //
        // Database breeds are also loaded automatically.
        // This list helps with common spelling mistakes.
        // =====================================================
        private static readonly string[] CommonBreeds =
        {
            // DOG
            "Labrador",
            "Golden Retriever",
            "German Shepherd",
            "Beagle",
            "Bulldog",
            "Poodle",
            "Pug",
            "Husky",
            "Siberian Husky",
            "Rottweiler",
            "Doberman",
            "Chihuahua",
            "Shih Tzu",
            "Cocker Spaniel",
            "Border Collie",
            "Great Dane",
            "Boxer",
            "Dalmatian",
            "Maltese",
            "Samoyed",
            "Pomeranian",

            // CAT
            "Persian",
            "Siamese",
            "Bengal",
            "Maine Coon",
            "British Shorthair",
            "Scottish Fold",
            "Ragdoll",
            "Sphynx",

            // BIRD
            "Cockatiel",
            "Budgerigar",
            "Lovebird",
            "Macaw",
            "African Grey",

            // RABBIT
            "Holland Lop",
            "Mini Lop",
            "Netherland Dwarf"
        };


        // =====================================================
        // WORDS THAT ARE NOT BREED NAMES
        // =====================================================
        private static readonly HashSet<string>
            StopWords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "i",
                "me",
                "my",
                "want",
                "wants",
                "wanted",
                "need",
                "needs",
                "needed",
                "looking",
                "look",
                "for",
                "a",
                "an",
                "the",

                "pet",
                "pets",

                "dog",
                "dogs",
                "puppy",
                "puppies",

                "cat",
                "cats",
                "kitten",
                "kittens",

                "bird",
                "birds",

                "rabbit",
                "rabbits",
                "bunny",
                "bunnies",

                "fish",

                "male",
                "female",
                "boy",
                "girl",
                "gender",

                "year",
                "years",
                "yr",
                "yrs",
                "old",
                "age",

                "please",
                "prefer",
                "preferred",
                "like",

                "with",
                "and",
                "or",
                "is",
                "be",
                "to",
                "of",
                "in"
            };


        // =====================================================
        // MAIN PARSER
        //
        // Example:
        //
        // I need a dog. 1 year. male. labador
        //
        // Result:
        // Dog
        // 1 Year
        // Male
        // Labrador
        // =====================================================
        public async Task<NaturalLanguagePreferenceResult>
            ParseAsync(string? preferenceText)
        {
            var result =
                new NaturalLanguagePreferenceResult
                {
                    OriginalText =
                        preferenceText?.Trim()
                        ?? string.Empty
                };


            // =================================================
            // EMPTY INPUT
            // =================================================
            if (string.IsNullOrWhiteSpace(
                preferenceText))
            {
                result.IsSuccess = false;

                result.ErrorMessage =
                    "Please write your pet preference.";

                return result;
            }


            string normalizedText =
                NormalizeText(
                    preferenceText
                );


            // =================================================
            // DETECT CATEGORY
            // =================================================
            await DetectCategoryAsync(
                normalizedText,
                result
            );


            // =================================================
            // DETECT GENDER
            // =================================================
            DetectGender(
                normalizedText,
                result
            );


            // =================================================
            // DETECT AGE
            // =================================================
            DetectAge(
                normalizedText,
                result
            );


            // =================================================
            // DETECT BREED
            // =================================================
            await DetectBreedAsync(
                normalizedText,
                result
            );


            // =================================================
            // SUCCESS WHEN AT LEAST SOMETHING IS UNDERSTOOD
            // =================================================
            result.IsSuccess =
                result.PreferredCategoryID.HasValue
                ||
                result.MinimumAge.HasValue
                ||
                result.MaximumAge.HasValue
                ||
                !result.PreferredGender.Equals(
                    "Any",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !string.IsNullOrWhiteSpace(
                    result.PreferredBreed);


            if (!result.IsSuccess)
            {
                result.ErrorMessage =
                    "I could not understand your pet preference. " +
                    "Try something like: I need a male Labrador dog, 1 year old.";

                return result;
            }


            // =================================================
            // CREATE HUMAN READABLE SUMMARY
            // =================================================
            result.Summary =
                BuildSummary(
                    result
                );


            return result;
        }


        // =====================================================
        // CATEGORY DETECTION
        // =====================================================
        private async Task DetectCategoryAsync(
            string normalizedText,
            NaturalLanguagePreferenceResult result)
        {
            var categories =
                await _db.PetCategories
                    .AsNoTracking()
                    .OrderBy(c =>
                        c.CategoryName)
                    .ToListAsync();


            if (categories.Count == 0)
            {
                return;
            }


            // =================================================
            // FIRST CHECK EXACT DATABASE CATEGORY
            // =================================================
            foreach (var category in categories)
            {
                string categoryName =
                    category.CategoryName?
                        .Trim()
                    ?? string.Empty;


                if (string.IsNullOrWhiteSpace(
                    categoryName))
                {
                    continue;
                }


                if (ContainsPhrase(
                    normalizedText,
                    NormalizeText(
                        categoryName)))
                {
                    result.PreferredCategoryID =
                        category.CategoryID;

                    result.PreferredCategoryName =
                        categoryName;

                    return;
                }
            }


            // =================================================
            // CHECK COMMON WORDS:
            // puppy -> Dog
            // kitten -> Cat
            // =================================================
            foreach (var aliasGroup
                in CategoryAliases)
            {
                bool aliasFound =
                    aliasGroup.Value.Any(
                        alias =>
                            ContainsPhrase(
                                normalizedText,
                                NormalizeText(
                                    alias)));


                if (!aliasFound)
                {
                    continue;
                }


                // =============================================
                // TRY TO FIND CORRESPONDING DATABASE CATEGORY
                // =============================================
                var databaseCategory =
                    categories
                        .Select(c =>
                            new
                            {
                                Category = c,

                                Score =
                                    Similarity(
                                        NormalizeText(
                                            c.CategoryName),
                                        NormalizeText(
                                            aliasGroup.Key))
                            })
                        .OrderByDescending(x =>
                            x.Score)
                        .FirstOrDefault();


                if (databaseCategory != null &&
                    databaseCategory.Score >= 0.60)
                {
                    result.PreferredCategoryID =
                        databaseCategory
                            .Category
                            .CategoryID;

                    result.PreferredCategoryName =
                        databaseCategory
                            .Category
                            .CategoryName;
                }


                return;
            }
        }


        // =====================================================
        // GENDER DETECTION
        // =====================================================
        private void DetectGender(
            string normalizedText,
            NaturalLanguagePreferenceResult result)
        {
            string[] femaleWords =
            {
                "female",
                "girl"
            };


            string[] maleWords =
            {
                "male",
                "boy"
            };


            if (femaleWords.Any(
                word =>
                    ContainsPhrase(
                        normalizedText,
                        word)))
            {
                result.PreferredGender =
                    "Female";

                return;
            }


            if (maleWords.Any(
                word =>
                    ContainsPhrase(
                        normalizedText,
                        word)))
            {
                result.PreferredGender =
                    "Male";

                return;
            }


            result.PreferredGender =
                "Any";
        }


        // =====================================================
        // AGE DETECTION
        //
        // Supported:
        //
        // 1 year
        // 1 year old
        // age 1
        // 1-3 years
        // 1 to 3 years
        // between 1 and 3 years
        // =====================================================
        private void DetectAge(
            string normalizedText,
            NaturalLanguagePreferenceResult result)
        {
            // =================================================
            // BETWEEN 1 AND 3 YEARS
            // =================================================
            Match betweenMatch =
                Regex.Match(
                    normalizedText,
                    @"\bbetween\s+(\d{1,2})\s+and\s+(\d{1,2})\s*(?:year|years|yr|yrs)?\b",
                    RegexOptions.IgnoreCase
                );


            if (betweenMatch.Success)
            {
                int first =
                    SafeInt(
                        betweenMatch
                            .Groups[1]
                            .Value);

                int second =
                    SafeInt(
                        betweenMatch
                            .Groups[2]
                            .Value);


                SetAgeRange(
                    result,
                    first,
                    second
                );

                return;
            }


            // =================================================
            // 1-3 YEARS / 1 TO 3 YEARS
            // =================================================
            Match rangeMatch =
                Regex.Match(
                    normalizedText,
                    @"\b(\d{1,2})\s*(?:-|to)\s*(\d{1,2})\s*(?:year|years|yr|yrs)\b",
                    RegexOptions.IgnoreCase
                );


            if (rangeMatch.Success)
            {
                int first =
                    SafeInt(
                        rangeMatch
                            .Groups[1]
                            .Value);

                int second =
                    SafeInt(
                        rangeMatch
                            .Groups[2]
                            .Value);


                SetAgeRange(
                    result,
                    first,
                    second
                );

                return;
            }


            // =================================================
            // AGE 1 / AGE IS 1
            // =================================================
            Match ageMatch =
                Regex.Match(
                    normalizedText,
                    @"\bage\s*(?:is|:|=)?\s*(\d{1,2})\b",
                    RegexOptions.IgnoreCase
                );


            if (ageMatch.Success)
            {
                int age =
                    SafeInt(
                        ageMatch
                            .Groups[1]
                            .Value);


                SetExactAge(
                    result,
                    age
                );

                return;
            }


            // =================================================
            // 1 YEAR / 1 YEAR OLD
            // =================================================
            Match yearMatch =
                Regex.Match(
                    normalizedText,
                    @"\b(\d{1,2})\s*(?:year|years|yr|yrs)(?:\s+old)?\b",
                    RegexOptions.IgnoreCase
                );


            if (yearMatch.Success)
            {
                int age =
                    SafeInt(
                        yearMatch
                            .Groups[1]
                            .Value);


                SetExactAge(
                    result,
                    age
                );
            }
        }


        // =====================================================
        // SET EXACT AGE
        // =====================================================
        private void SetExactAge(
            NaturalLanguagePreferenceResult result,
            int age)
        {
            if (age < 0 ||
                age > 50)
            {
                return;
            }


            result.MinimumAge =
                age;

            result.MaximumAge =
                age;
        }


        // =====================================================
        // SET AGE RANGE
        // =====================================================
        private void SetAgeRange(
            NaturalLanguagePreferenceResult result,
            int first,
            int second)
        {
            if (first < 0 ||
                second < 0 ||
                first > 50 ||
                second > 50)
            {
                return;
            }


            result.MinimumAge =
                Math.Min(
                    first,
                    second);

            result.MaximumAge =
                Math.Max(
                    first,
                    second);
        }


        // =====================================================
        // BREED DETECTION
        // =====================================================
        private async Task DetectBreedAsync(
            string normalizedText,
            NaturalLanguagePreferenceResult result)
        {
            // =================================================
            // LOAD ALL EXISTING PET BREEDS
            // =================================================
            var databaseBreeds =
                await _db.Pets
                    .AsNoTracking()
                    .Where(p =>
                        p.Breed != null &&
                        p.Breed.Trim() != "")
                    .Select(p =>
                        p.Breed!.Trim())
                    .Distinct()
                    .ToListAsync();


            // =================================================
            // DATABASE + COMMON BREEDS
            // =================================================
            var breeds =
                databaseBreeds
                    .Concat(
                        CommonBreeds)
                    .Where(b =>
                        !string.IsNullOrWhiteSpace(b))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();


            // =================================================
            // FIRST TRY EXACT MATCH
            // =================================================
            foreach (string breed
                in breeds)
            {
                if (ContainsPhrase(
                    normalizedText,
                    NormalizeText(
                        breed)))
                {
                    result.PreferredBreed =
                        CanonicalBreedName(
                            breed);

                    return;
                }
            }


            // =================================================
            // FIND POSSIBLE USER BREED WORDS
            //
            // Example input:
            //
            // i need a dog 1 year male labador
            //
            // After stop word removal:
            //
            // labador
            // =================================================
            string[] words =
                normalizedText
                    .Split(
                        ' ',
                        StringSplitOptions
                            .RemoveEmptyEntries)
                    .Where(word =>
                        !StopWords.Contains(
                            word)
                        &&
                        !int.TryParse(
                            word,
                            out _))
                    .ToArray();


            if (words.Length == 0)
            {
                return;
            }


            string? bestBreed =
                null;

            double bestScore =
                0;


            foreach (string breed
                in breeds)
            {
                string normalizedBreed =
                    NormalizeText(
                        breed);


                string[] breedWords =
                    normalizedBreed
                        .Split(
                            ' ',
                            StringSplitOptions
                                .RemoveEmptyEntries);


                int breedWordCount =
                    breedWords.Length;


                // =============================================
                // SINGLE / MULTI-WORD BREED COMPARISON
                // =============================================
                for (int i = 0;
                    i < words.Length;
                    i++)
                {
                    int available =
                        words.Length - i;


                    int take =
                        Math.Min(
                            breedWordCount,
                            available);


                    if (take <= 0)
                    {
                        continue;
                    }


                    string candidate =
                        string.Join(
                            " ",
                            words
                                .Skip(i)
                                .Take(take));


                    double score =
                        Similarity(
                            normalizedBreed,
                            candidate);


                    if (score > bestScore)
                    {
                        bestScore =
                            score;

                        bestBreed =
                            breed;
                    }
                }
            }


            // =================================================
            // ALLOW SMALL SPELLING ERRORS
            //
            // labador -> Labrador
            // =================================================
            if (bestBreed != null &&
                bestScore >= 0.72)
            {
                result.PreferredBreed =
                    CanonicalBreedName(
                        bestBreed);
            }
        }


        // =====================================================
        // CORRECT COMMON BREED NAMES
        // =====================================================
        private string CanonicalBreedName(
            string breed)
        {
            string normalized =
                NormalizeText(
                    breed);


            // Labrador spelling correction
            if (Similarity(
                    normalized,
                    "labrador") >= 0.72)
            {
                return "Labrador";
            }


            // German Shepherd correction
            if (normalized ==
                "german shepard")
            {
                return "German Shepherd";
            }


            return ToTitleCase(
                breed
            );
        }


        // =====================================================
        // BUILD NATURAL LANGUAGE SUMMARY
        // =====================================================
        private string BuildSummary(
            NaturalLanguagePreferenceResult result)
        {
            var summary =
                new StringBuilder();


            summary.Append(
                "You are looking for "
            );


            // =================================================
            // AGE
            // =================================================
            if (result.MinimumAge.HasValue &&
                result.MaximumAge.HasValue &&
                result.MinimumAge.Value ==
                result.MaximumAge.Value)
            {
                int age =
                    result.MinimumAge.Value;


                summary.Append(
                    $"a {age}-year-old "
                );
            }
            else if (
                result.MinimumAge.HasValue &&
                result.MaximumAge.HasValue)
            {
                summary.Append(
                    $"a {result.MinimumAge.Value} to " +
                    $"{result.MaximumAge.Value}-year-old "
                );
            }
            else
            {
                summary.Append(
                    "a "
                );
            }


            // =================================================
            // GENDER
            // =================================================
            if (!string.IsNullOrWhiteSpace(
                    result.PreferredGender)
                &&
                !result.PreferredGender.Equals(
                    "Any",
                    StringComparison.OrdinalIgnoreCase))
            {
                summary.Append(
                    result.PreferredGender
                        .ToLowerInvariant()
                );

                summary.Append(
                    " "
                );
            }


            // =================================================
            // BREED
            // =================================================
            if (!string.IsNullOrWhiteSpace(
                    result.PreferredBreed))
            {
                summary.Append(
                    result.PreferredBreed
                );

                summary.Append(
                    " "
                );
            }


            // =================================================
            // CATEGORY
            // =================================================
            if (!string.IsNullOrWhiteSpace(
                    result.PreferredCategoryName))
            {
                summary.Append(
                    result.PreferredCategoryName
                        .ToLowerInvariant()
                );
            }
            else
            {
                summary.Append(
                    "pet"
                );
            }


            summary.Append(
                "."
            );


            return summary.ToString();
        }


        // =====================================================
        // NORMALIZE TEXT
        // =====================================================
        private string NormalizeText(
            string? text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return string.Empty;
            }


            string value =
                text
                    .ToLowerInvariant()
                    .Replace(
                        "’",
                        "'")
                    .Replace(
                        "–",
                        "-")
                    .Replace(
                        "—",
                        "-");


            value =
                Regex.Replace(
                    value,
                    @"[^a-z0-9\-\s]",
                    " "
                );


            value =
                Regex.Replace(
                    value,
                    @"\s+",
                    " "
                );


            return value.Trim();
        }


        // =====================================================
        // WHOLE WORD / PHRASE CHECK
        // =====================================================
        private bool ContainsPhrase(
            string text,
            string phrase)
        {
            if (string.IsNullOrWhiteSpace(
                    text)
                ||
                string.IsNullOrWhiteSpace(
                    phrase))
            {
                return false;
            }


            string pattern =
                $@"(?:^|\s){Regex.Escape(phrase)}(?:$|\s)";


            return Regex.IsMatch(
                text,
                pattern,
                RegexOptions.IgnoreCase
            );
        }


        // =====================================================
        // FUZZY SIMILARITY
        // =====================================================
        private double Similarity(
            string? first,
            string? second)
        {
            string a =
                NormalizeText(
                    first);

            string b =
                NormalizeText(
                    second);


            if (string.IsNullOrWhiteSpace(a) ||
                string.IsNullOrWhiteSpace(b))
            {
                return 0;
            }


            if (a.Equals(
                b,
                StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }


            int distance =
                LevenshteinDistance(
                    a,
                    b);


            int maxLength =
                Math.Max(
                    a.Length,
                    b.Length);


            if (maxLength == 0)
            {
                return 1;
            }


            return
                1.0
                -
                (
                    (double)distance
                    /
                    maxLength
                );
        }


        // =====================================================
        // LEVENSHTEIN DISTANCE
        // =====================================================
        private int LevenshteinDistance(
            string source,
            string target)
        {
            int[,] matrix =
                new int[
                    source.Length + 1,
                    target.Length + 1
                ];


            for (int i = 0;
                i <= source.Length;
                i++)
            {
                matrix[i, 0] =
                    i;
            }


            for (int j = 0;
                j <= target.Length;
                j++)
            {
                matrix[0, j] =
                    j;
            }


            for (int i = 1;
                i <= source.Length;
                i++)
            {
                for (int j = 1;
                    j <= target.Length;
                    j++)
                {
                    int cost =
                        source[i - 1] ==
                        target[j - 1]
                            ? 0
                            : 1;


                    matrix[i, j] =
                        Math.Min(
                            Math.Min(
                                matrix[i - 1, j] + 1,
                                matrix[i, j - 1] + 1
                            ),

                            matrix[i - 1, j - 1]
                            + cost
                        );
                }
            }


            return matrix[
                source.Length,
                target.Length
            ];
        }


        // =====================================================
        // SAFE INTEGER
        // =====================================================
        private int SafeInt(
            string value)
        {
            if (int.TryParse(
                value,
                out int number))
            {
                return number;
            }


            return 0;
        }


        // =====================================================
        // TITLE CASE
        // =====================================================
        private string ToTitleCase(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return string.Empty;
            }


            string[] words =
                value
                    .Trim()
                    .ToLowerInvariant()
                    .Split(
                        ' ',
                        StringSplitOptions
                            .RemoveEmptyEntries);


            for (int i = 0;
                i < words.Length;
                i++)
            {
                if (words[i].Length == 1)
                {
                    words[i] =
                        words[i]
                            .ToUpperInvariant();

                    continue;
                }


                words[i] =
                    char.ToUpperInvariant(
                        words[i][0])
                    +
                    words[i]
                        .Substring(1);
            }


            return string.Join(
                " ",
                words
            );
        }
    }
}