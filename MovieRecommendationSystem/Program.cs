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
        static readonly Random random = new Random();

        static void Main(string[] args)
        {
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
            var recommendationSystem = new RecommendationSystem<Movie>(x => x.MovieId, x => x.UserId, x => x.Rating);
            elapsed = TimeUtilities.MeasureDuration(() => recommendationSystem.LoadModel(movies));
            Console.WriteLine($"Recommendation system trained in {elapsed.TotalMinutes} minute(s).");

            Console.WriteLine("Predicting how 5 random users would rate a random, unrated movie...");
            for (int i = 0; i < 5; i++)
            {
                int randomUser = 0;
                Movie unratedMovie = null;
                while (unratedMovie == null)
                {
                    var distinctUsers = movies.Select(x => x.UserId).Distinct().ToList();
                    randomUser = distinctUsers[random.Next(distinctUsers.Count())];
                    var ratedMovies = movies.Where(x => x.UserId == randomUser).Select(x => x.MovieId).ToList();
                    var unratedMovies = movies.Where(x => !ratedMovies.Contains(x.MovieId)).ToList();
                    if (unratedMovies.Count() > 0)
                    {
                        unratedMovie = unratedMovies[random.Next(unratedMovies.Count())];
                    }
                }
                var title = descriptions.FirstOrDefault(x => x.MovieId == unratedMovie.MovieId)?.Title ?? unratedMovie.MovieId.ToString();
                Console.WriteLine($"Predicting a rating for user \"{randomUser}\" and movie \"{title}\".");
                var predictedRating = recommendationSystem.PredictUserRating(randomUser, unratedMovie.MovieId);
                Console.WriteLine($"User \"{randomUser}\" would most likely rate the movie \"{title}\" {predictedRating}.");
            }
        }
    }
}
