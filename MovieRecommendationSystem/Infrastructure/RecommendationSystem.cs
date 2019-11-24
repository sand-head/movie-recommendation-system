using MovieRecommendationSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MovieRecommendationSystem.Infrastructure
{
    public class RecommendationSystem<T>
    {
        private IEnumerable<T> _itemRatings;
        private Dictionary<(int X, int Y), double> _itemSimilarityMatrix;
        private Func<T, int> _itemIdFunc, _userIdFunc;
        private Func<T, double> _ratingFunc;

        public RecommendationSystem(Func<T, int> itemIdFunc, Func<T, int> userIdFunc, Func<T, double> ratingFunc)
        {
            _itemIdFunc = itemIdFunc;
            _userIdFunc = userIdFunc;
            _ratingFunc = ratingFunc;
        }

        public void LoadModel(IEnumerable<T> itemRatings)
        {
            _itemRatings = itemRatings;
            _itemSimilarityMatrix = new Dictionary<(int X, int Y), double>();
            var distinctItem = itemRatings.Select(_itemIdFunc).Distinct().ToList();

            int currentProgress = 0, totalCount = distinctItem.Count();
            Console.WriteLine($"Building model based off of {totalCount} items...");
            Parallel.ForEach(distinctItem, firstItemId =>
            {
                var elapsed = TimeUtilities.MeasureDuration(() =>
                {
                    var firstItemRatings = itemRatings.Where(x => _itemIdFunc(x) == firstItemId);
                    var firstItemAverageRating = firstItemRatings.Sum(_ratingFunc) / firstItemRatings.Count();

                    // for every user who rated First Item, add all other rated items to List paired with First Item
                    List<(int UserId, T FirstItem, T SecondItem)> itemPairs = firstItemRatings
                        .SelectMany(ratings => itemRatings.Where(x => _userIdFunc(x) == _userIdFunc(ratings) && _itemIdFunc(x) != firstItemId)
                        .Select(x => (_userIdFunc(x), itemRatings.First(x => _itemIdFunc(x) == firstItemId), x))).ToList();

                    // for every Second Item, calculate and store the similarity in the matrix
                    foreach (var secondItemId in itemPairs.Select(x => _itemIdFunc(x.SecondItem)).Distinct())
                    {
                        var secondItemRatings = itemRatings.Where(x => _itemIdFunc(x) == secondItemId);
                        var secondItemAverageRating = secondItemRatings.Sum(_ratingFunc) / secondItemRatings.Count();

                        var secondItemPairs = itemPairs.Where(x => _itemIdFunc(x.SecondItem) == secondItemId);
                        var similarity = secondItemPairs.Sum(x => (_ratingFunc(x.FirstItem) - firstItemAverageRating) * (_ratingFunc(x.SecondItem) - secondItemAverageRating)) /
                            (Math.Sqrt(secondItemPairs.Sum(x => Math.Pow(_ratingFunc(x.FirstItem), 2))) * Math.Sqrt(secondItemPairs.Sum(x => Math.Pow(_ratingFunc(x.SecondItem), 2))));
                        _itemSimilarityMatrix.Add((firstItemId, secondItemId), similarity);
                    }
                });

                Interlocked.Increment(ref currentProgress);
                Console.WriteLine($" [{currentProgress}/{totalCount}] Finished work on item ID \"{firstItemId}\" in {elapsed.TotalMinutes} minute(s).");
            });
        }

        public double PredictUserRating(int userId, int itemId)
        {
            if (_itemRatings == null || _itemSimilarityMatrix == null)
                throw new Exception("There is no data to generate recommendations from.");
            var existingRating = _itemRatings.FirstOrDefault(x => _itemIdFunc(x) == itemId && _userIdFunc(x) == userId);
            if (existingRating != null)
                return _ratingFunc(existingRating);

            var firstItemRatings = _itemRatings.Where(x => _itemIdFunc(x) == itemId);
            var firstItemAverageRating = firstItemRatings.Sum(_ratingFunc) / firstItemRatings.Count();

            var ratings = _itemRatings.Where(x => _userIdFunc(x) == userId);
            var distinctRatedItems = ratings.Select(_itemIdFunc).Distinct();

            IEnumerable<(double WeightedRating, double AbsoluteSimilarity)> itemData = distinctRatedItems.Select(secondItemId =>
            {
                var secondItemRatings = _itemRatings.Where(x => _itemIdFunc(x) == secondItemId);
                var secondItemAverageRating = secondItemRatings.Sum(_ratingFunc) / secondItemRatings.Count();
                return !_itemSimilarityMatrix.TryGetValue((itemId, secondItemId), out var similarity) ? (0, 0) :
                    ((_ratingFunc(ratings.First(x => _itemIdFunc(x) == secondItemId)) - secondItemAverageRating) * similarity, Math.Abs(similarity));
            });
            return firstItemAverageRating + itemData.Sum(x => x.WeightedRating) / itemData.Sum(x => x.AbsoluteSimilarity);
        }
    }
}
