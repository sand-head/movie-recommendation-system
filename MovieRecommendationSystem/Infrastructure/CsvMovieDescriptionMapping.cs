using MovieRecommendationSystem.Models;
using TinyCsvParser.Mapping;

namespace MovieRecommendationSystem.Infrastructure
{
    public class CsvMovieDescriptionMapping : CsvMapping<MovieDescription>
    {
        public CsvMovieDescriptionMapping() : base()
        {
            MapProperty(0, x => x.MovieId);
            MapProperty(1, x => x.YearOfRelease);
            MapProperty(2, x => x.Title);
        }
    }
}
