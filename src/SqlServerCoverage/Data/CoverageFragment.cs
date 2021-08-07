namespace SqlServerCoverage.Data
{
    internal class CoverageFragment
    {
        private readonly int offset;
        private readonly int offsetEnd;

        public int ObjectId { get; }
        private bool IsLastFragment => offsetEnd == -1;

        public CoverageFragment(int objectId, int offset, int offsetEnd)
        {
            ObjectId = objectId;
            this.offset = offset;
            this.offsetEnd = offsetEnd;
        }

        public bool Includes(Statement statement)
        {
            var fragmentStart = offset / 2; // why divide by 2?
            var statementStart = statement.Offset;

            var fragmentEnd = (offsetEnd / 2) + 2; // add 2?
            var statementEnd = statement.Offset + statement.Length;

            return statementStart >= fragmentStart &&
                  (statementEnd <= fragmentEnd || IsLastFragment);
        }
    }
}