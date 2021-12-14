namespace DreamScene2
{
    /// <summary>
    /// Allow strings to be truncated
    /// </summary>
    public static class TruncateExtensions
    {
        /// <summary>
        /// Truncate the string
        /// </summary>
        /// <param name="input">The string to be truncated</param>
        /// <param name="length">The length to truncate to</param>
        /// <param name="truncationString">The string used to truncate with</param>
        /// <param name="from">The enum value used to determine from where to truncate the string</param>
        /// <returns>The truncated string</returns>
        public static string Truncate(this string input, int length, string truncationString = "…", TruncateFrom from = TruncateFrom.Middle)
        {
            if (input.Length > length)
            {
                if (from == TruncateFrom.Left)
                {
                    string str = input.Substring(input.Length + truncationString.Length - length, length - truncationString.Length);
                    return truncationString + str;
                }
                else if (from == TruncateFrom.Middle)
                {
                    int len = (length - truncationString.Length) / 2;
                    string str1 = input.Substring(0, len);
                    string str2 = input.Substring(input.Length - len, len);
                    return str1 + truncationString + str2;
                }
                else if (from == TruncateFrom.Right)
                {
                    string str = input.Substring(0, length - truncationString.Length);
                    return str + truncationString;
                }
            }
            return input;
        }
    }

    /// <summary>
    /// Truncation location for humanizer
    /// </summary>
    public enum TruncateFrom
    {
        /// <summary>
        /// Truncate letters from the left (start) of the string
        /// </summary>
        Left,
        Middle,
        /// <summary>
        /// Truncate letters from the right (end) of the string
        /// </summary>
        Right
    }
}
