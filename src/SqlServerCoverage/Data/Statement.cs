namespace SqlServerCoverage.Data
{
    public class Statement
    {
        public string Text { get; }
        public int Offset { get; }
        public int OffsetEnd => Offset + Text.Length;
        public int HitCount { get; internal set; }

        internal Statement(string text, int offset)
        {
            Text = text;
            Offset = offset;
        }

        internal (int sl, int el, int sc, int ec) ToLineAndColumn(string objectText)
        {
            var startLine = 0;
            var startColumn = 0;

            int index = 0, column = 0, line = 1;
            while (index < objectText.Length && index < OffsetEnd)
            {
                if (index == Offset)
                {
                    startLine = line;
                    startColumn = column;
                }

                if (objectText[index] == '\n')
                {
                    line++;
                    column = -1;
                }

                column += 1;
                index += 1;
            }

            return (startLine, line, startColumn, column);
        }
    }
}
