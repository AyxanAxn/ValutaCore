namespace ValutaCore.Core.Models.Currency
{
    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}