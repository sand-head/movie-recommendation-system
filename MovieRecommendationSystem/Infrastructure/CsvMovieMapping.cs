using MovieRecommendationSystem.Models;
using TinyCsvParser.Mapping;

namespace MovieRecommendationSystem.Infrastructure
{
    public class CsvMovieMapping : CsvMapping<Movie>
    {
        public CsvMovieMapping() : base()
        {
            MapProperty(0, x => x.MovieId);
            MapProperty(1, x => x.UserId);
            MapProperty(2, x => x.Rating);
        }
    }
}
