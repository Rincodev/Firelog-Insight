namespace FireLog
{
    internal static class TextUtil
    {
        public static bool ContainsIgnoreCase(string? text, string token)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token)) return false;
            return text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
