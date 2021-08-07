namespace SqlServerCoverage
{
    internal struct OpenCoverOffsets
    {
        public OpenCoverOffsets(int offset, int length, string text)
        {
            StartLine = 0;
            EndLine = 0;
            StartColumn = 0;
            EndColumn = 0;

            int index = 0, column = 0, line = 1;
            while (index < text.Length)
            {
                if (text[index] == '\n')
                {
                    line++;
                    column = 0;
                    index += 1;
                    continue;
                }

                if (index == offset + length)
                {
                    EndLine = line;
                    EndColumn = column;
                    break;
                }

                if (index == offset)
                {
                    StartLine = line;
                    StartColumn = column;
                }

                column += 1;
                index += 1;
            }
        }

        public int StartLine { get; }
        public int EndLine { get; }
        public int StartColumn { get; }
        public int EndColumn { get; }
    }
}
