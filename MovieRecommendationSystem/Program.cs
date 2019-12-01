using MovieRecommendationSystem.Infrastructure;
using MovieRecommendationSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TinyCsvParser;

namespace MovieRecommendationSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists("data/data-full.txt"))
            {
                Console.WriteLine("Could not find file \"data-full.txt\" in the relative directory \"data\". Please make sure the required data is located in said directory.");
                Console.ReadKey();
                return;
            }

            var csvOptions = new CsvParserOptions(true, ',');
            var csvMovieMapping = new CsvMovieMapping();
            var csvMovieDescriptionMapping = new CsvMovieDescriptionMapping();
            var movieParser = new CsvParser<Movie>(csvOptions, csvMovieMapping);
            var descriptionParser = new CsvParser<MovieDescription>(csvOptions, csvMovieDescriptionMapping);

            Console.WriteLine("Reading movie data in from \"data/data-full.txt\"...");
            var elapsed = TimeUtilities.MeasureDuration(() => movieParser.ReadFromFile("data/data-full.txt", Encoding.ASCII).ToList(), out var data);
            var movies = data.Where(x => x.IsValid).Select(x => x.Result).ToList();
            Console.WriteLine($"Data loaded in {elapsed.TotalSeconds} second(s).");

            List<MovieDescription> descriptions = new List<MovieDescription>();
            if (File.Exists("data/movie_names.txt"))
            {
                Console.WriteLine("Reading optional movie description data in from \"data/movie_names.txt\"...");
                elapsed = TimeUtilities.MeasureDuration(() => descriptionParser.ReadFromFile("data/movie_names.txt", Encoding.ASCII).ToList(), out var descriptionData);
                descriptions.AddRange(descriptionData.Where(x => x.IsValid).Select(x => x.Result));
                Console.WriteLine($"Data loaded in {elapsed.TotalSeconds} second(s).");
            }

            Console.WriteLine("Training the recommendation system...");
            var recommendationSystem = new RecommendationSystem<Movie>(x => x.MovieId, x => x.UserId, x => x.Rating,
                x => descriptions.FirstOrDefault(y => y.MovieId == x)?.Title ?? x.ToString());
            elapsed = TimeUtilities.MeasureDuration(() => recommendationSystem.LoadModel(movies));
            Console.WriteLine($"Recommendation system trained in {elapsed.TotalMinutes} minute(s).");

            var continueLoop = true;
            while (continueLoop)
            {
                Console.WriteLine("\nEnter a user ID to predict a rating for:");
                var userParseSuccess = int.TryParse(Console.ReadLine(), out var userId);
                Console.WriteLine("Enter a movie ID to predict a rating for:");
                var movieParseSuccess = int.TryParse(Console.ReadLine(), out var movieId);

                var rating = recommendationSystem.PredictUserRating(userId, movieId);
                Console.WriteLine($"User \"{userId}\" would most likely rate the movie " +
                    $"\"{descriptions.FirstOrDefault(x => x.MovieId == movieId)?.Title ?? movieId.ToString()}\" {Math.Round(rating, 2)} out of 5.");

                Console.WriteLine("Enter another? (Y/n)");
                continueLoop = Console.ReadLine().Trim().ToUpperInvariant() == "Y";
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
