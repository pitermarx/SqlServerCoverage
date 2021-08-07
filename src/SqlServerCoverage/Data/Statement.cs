namespace SqlServerCoverage.Data
{
    public class Statement
    {
        public string Text { get; }
        public int Offset { get; }
        public int Length { get; }
        public long HitCount { get; set; }

        internal Statement(string text, int offset)
        {
            Text = text;
            Offset = offset;
            Length = text.Length;

            Length = NormalizeStatement();
            int NormalizeStatement()
            {
                int chopOff = 0;
                var index = Length - 1;
                if (index <= 0 || Text.Length == 0)
                {
                    return Length;
                }

                while (ShouldChopOff(Text[index]))
                {
                    chopOff++;

                    if (index <= 0)
                        break;

                    index--;
                }

                return Length - chopOff;
            }
        }

        private bool ShouldChopOff(char c) => char.IsWhiteSpace(c) || char.IsControl(c) || c == ';';
    }
}
