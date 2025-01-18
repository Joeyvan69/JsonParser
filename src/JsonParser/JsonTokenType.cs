namespace LowGCJsonParser
{
    public enum JsonTokenType
    {
        None,
        ObjectStart,
        ObjectEnd,
        ArrayStart,
        ArrayEnd,
        String,
        Number,
        Boolean,
        Null,
        Comma,
        Colon,
    }
}