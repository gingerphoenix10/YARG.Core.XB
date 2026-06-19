using System;
using System.Collections.Generic;

namespace YARG.Core.Chart
{
    internal static class CMUDict
    {
        private static Dictionary<string, string[]>? _dictionary;
        private static bool _initialized;

        public static bool IsInitialized => _initialized;

        public static void Initialize(string dictionaryText)
        {
            if (_initialized)
                return;

            // Count entries first for proper capacity
            int entryCount = 0;
            int lineStart = 0;
            while (lineStart < dictionaryText.Length)
            {
                int lineEnd = dictionaryText.IndexOf('\n', lineStart);
                if (lineEnd < 0) lineEnd = dictionaryText.Length;

                var line = dictionaryText.AsSpan(lineStart, lineEnd - lineStart);
                if (!StartsWith(line, ";;;") && !IsWhiteSpace(line) && !line.IsEmpty)
                    entryCount++;

                lineStart = lineEnd + 1;
            }

            _dictionary = new Dictionary<string, string[]>(entryCount);

            lineStart = 0;
            while (lineStart < dictionaryText.Length)
            {
                int lineEnd = dictionaryText.IndexOf('\n', lineStart);
                if (lineEnd < 0) lineEnd = dictionaryText.Length;

                var line = dictionaryText.AsSpan(lineStart, lineEnd - lineStart);
                lineStart = lineEnd + 1;

                if (StartsWith(line, ";;;") || IsWhiteSpace(line) || line.IsEmpty)
                    continue;

                // Find word (first whitespace-delimited token)
                int wordEnd = IndexOfAny(line, '\t', ' ');
                if (wordEnd <= 0)
                    continue;

                var word = line.Slice(0, wordEnd);

                // Handle alternate pronunciations like WORD(2)
                int parenIdx = word.IndexOf('(');
                if (parenIdx > 0)
                    word = word.Slice(0, parenIdx);

                // Parse phonemes (remaining tokens after word)
                var phonemeSpan = TrimStart(line.Slice(wordEnd));
                if (phonemeSpan.IsEmpty)
                    continue;

                // Count phonemes
                int phonemeCount = CountNonEmptySplits(phonemeSpan, ' ', '\t');

                var phonemes = new string[phonemeCount];
                int i = 0;
                int pos = 0;
                while (pos < phonemeSpan.Length && i < phonemeCount)
                {
                    // Skip whitespace
                    while (pos < phonemeSpan.Length && (phonemeSpan[pos] == ' ' || phonemeSpan[pos] == '\t'))
                        pos++;

                    if (pos >= phonemeSpan.Length)
                        break;

                    int start = pos;
                    while (pos < phonemeSpan.Length && phonemeSpan[pos] != ' ' && phonemeSpan[pos] != '\t')
                        pos++;

                    var phoneme = phonemeSpan.Slice(start, pos - start);

                    // Strip stress markers (0, 1, 2) from end
                    if (phoneme.Length > 0)
                    {
                        char last = phoneme[phoneme.Length - 1];
                        if (last == '0' || last == '1' || last == '2')
                            phoneme = phoneme.Slice(0, phoneme.Length - 1);
                    }

                    phonemes[i++] = phoneme.ToString();
                }

                var wordStr = word.ToString();
                if (!_dictionary.ContainsKey(wordStr))
                    _dictionary[wordStr] = phonemes;
            }

            _initialized = true;
        }

        private static bool StartsWith(ReadOnlySpan<char> span, string prefix)
        {
            if (span.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (span[i] != prefix[i]) return false;
            }
            return true;
        }

        private static bool IsWhiteSpace(ReadOnlySpan<char> span)
        {
            foreach (var c in span)
            {
                if (!char.IsWhiteSpace(c)) return false;
            }
            return true;
        }

        private static int IndexOfAny(ReadOnlySpan<char> span, char a, char b)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == a || span[i] == b) return i;
            }
            return -1;
        }

        private static ReadOnlySpan<char> TrimStart(ReadOnlySpan<char> span)
        {
            int i = 0;
            while (i < span.Length && char.IsWhiteSpace(span[i]))
                i++;
            return span.Slice(i);
        }

        private static int CountNonEmptySplits(ReadOnlySpan<char> span, char a, char b)
        {
            int count = 0;
            int i = 0;
            while (i < span.Length)
            {
                while (i < span.Length && (span[i] == a || span[i] == b))
                    i++;
                if (i >= span.Length) break;
                count++;
                while (i < span.Length && span[i] != a && span[i] != b)
                    i++;
            }
            return count;
        }

        public static IReadOnlyDictionary<string, string[]> GetDictionary()
        {
            if (!_initialized)
                throw new InvalidOperationException("CMUDict not initialized. Call Initialize() first.");
            return _dictionary!;
        }
    }
}
