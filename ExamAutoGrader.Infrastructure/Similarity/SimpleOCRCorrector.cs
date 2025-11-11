namespace ExamAutoGrader.Infrastructure.Similarity
{
    public static class SimpleOCRCorrector
    {
        // 只校正已知的、频繁出现的错误
        private static readonly Dictionary<string, string> _commonErrors = new()
        {
            // 已知的特定错误
            { "频☰", "频繁" }
        };

        /// <summary>
        /// 简单替换已知的错误
        /// </summary>
        public static string CorrectKnownErrors(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
                return rawText;

            var result = rawText;

            // 按错误词长度从长到短替换，避免重复替换
            foreach (var error in _commonErrors.OrderByDescending(x => x.Key.Length))
            {
                result = result.Replace(error.Key, error.Value);
            }

            return result;
        }
    }
}
