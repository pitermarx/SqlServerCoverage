namespace SqlServerCoverage.Data
{
    public enum SourceObjectType
    {
        InlineFunction,
        ScalarFunction,
        TableFunction,
        Procedure,
        View,
        Trigger,
        Unknown
    }
}