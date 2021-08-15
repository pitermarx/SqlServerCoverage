namespace SqlServerCoverage.Data
{
    internal class CoverageFragment
    {
        public int ObjectId { get; }
        private bool IsLastFragment => offsetEnd == -1;
        private readonly int offset;
        private readonly int offsetEnd;

        public CoverageFragment(int objectId, int offset, int offsetEnd)
        {
            ObjectId = objectId;
            this.offset = offset;
            this.offsetEnd = offsetEnd;
        }

        public bool Covers(Statement statement)
        {
            return statement.Offset >= offset &&
                  (statement.OffsetEnd <= offsetEnd || IsLastFragment);
        }
    }
}