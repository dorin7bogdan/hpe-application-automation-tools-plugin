using System.Runtime.InteropServices;
using System;
using System.Security;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections;

namespace HpToolsLauncher.Utils
{
    internal static class Extensions
    {
        private const char PERCENT = '%';
        private const char DOLLAR = '$';
        private const char LEFT_CURLY_BRACKET = '{';
        private const char RIGHT_CURLY_BRACKET = '}';
        private static readonly char[] _percentDollar = new char[] { PERCENT, DOLLAR };
        private static readonly IDictionary<string, string> _caseInsensitiveLocalEnv;

        static Extensions()
        {
            IDictionary localEnv = Environment.GetEnvironmentVariables();
            _caseInsensitiveLocalEnv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in localEnv)
            {
                _caseInsensitiveLocalEnv[(string)entry.Key] = (string)entry.Value;
            }

        }
        public static SecureString ToSecureString(this string plainString)
        {
            if (plainString == null)
                return null;

            SecureString secureString = new SecureString();
            foreach (char c in plainString.ToCharArray())
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }
        public static string ToPlainString(this SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToBSTR(value);
                return Marshal.PtrToStringBSTR(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(valuePtr);
            }
        }
        public static bool EqualsIgnoreCase(this string s1, string s2)
        {
            if (s1 == null || s2 == null) return s1 == s2;
            return s1.Equals(s2, StringComparison.OrdinalIgnoreCase);
        }
        public static bool In(this string str, bool ignoreCase, params string[] values)
        {
            if (ignoreCase)
            {
                return values != null && values.Any((string s) => EqualsIgnoreCase(str, s));
            }
            return In(str, values);
        }

        public static bool In<T>(this T obj, params T[] values)
        {
            return values != null && values.Any((T o) => Equals(obj, o));
        }

        public static string ReplaceEx(this string str, string oldVal, string newVal, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldVal, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newVal);
                index += oldVal.Length;

                previousIndex = index;
                index = str.IndexOf(oldVal, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }

        public static HashSet<string> ExtractEnvVariableKeys(this string text)
        {
            HashSet<string> vars = new HashSet<string>();
            bool useChar = false, isPrevCharDollar = false;
            string varName = string.Empty;
            for (int x = 0; x < text.Length; x++)
            {
                char c = text[x];
                switch (c)
                {
                    case PERCENT:
                        if (useChar && varName.Trim().Length > 0) // end of var name
                        {
                            vars.Add(varName);
                            varName = string.Empty;
                        }
                        useChar ^= true;
                        isPrevCharDollar = false;
                        break;
                    case DOLLAR:
                        isPrevCharDollar = true;
                        break;
                    case LEFT_CURLY_BRACKET:
                        useChar = isPrevCharDollar;
                        isPrevCharDollar = false;
                        break;
                    case RIGHT_CURLY_BRACKET:
                        if (useChar && varName.Trim().Length > 0) // end of var name
                        {
                            vars.Add(varName);
                            varName = string.Empty;
                        }
                        useChar = false;
                        isPrevCharDollar = false;
                        break;
                    default:
                        if (useChar)
                            varName += c;
                        isPrevCharDollar = false;
                        break;
                }
            }
            return vars;
        }

        public static string ParseEnvVars(this string text, IDictionary<string, string> jenkinsEnvVars, bool escapeBackslashes = false)
        {
            const string PERCENT_FORMAT = "%{0}%";
            const string DOLLAR_FORMAT = "${{{0}}}";
            if (text.IndexOfAny(_percentDollar) > -1)
            {
                HashSet<string> vars = text.ExtractEnvVariableKeys();

                foreach (string rawKey in vars)
                {
                    string trimmedKey = rawKey.Trim();
                    if (_caseInsensitiveLocalEnv.ContainsKey(trimmedKey))
                    {
                        string value = (string)_caseInsensitiveLocalEnv[trimmedKey];
                        text = text.ReplaceEx(string.Format(PERCENT_FORMAT, rawKey), value)
                                   .ReplaceEx(string.Format(DOLLAR_FORMAT, rawKey), value);
                    }

                    if (jenkinsEnvVars != null && jenkinsEnvVars.ContainsKey(trimmedKey))
                    {
                        string value = jenkinsEnvVars[trimmedKey];
                        text = text.ReplaceEx(string.Format(PERCENT_FORMAT, rawKey), value)
                                   .ReplaceEx(string.Format(DOLLAR_FORMAT, rawKey), value);

                    }
                }

                if (escapeBackslashes)
                {
                    const char backslash = '\\';
                    const string dblBackslash = @"\\";
                    string[] items = text.Split(new char[] { backslash }, StringSplitOptions.RemoveEmptyEntries);
                    return string.Join(dblBackslash, items);
                }
            }
            return text.Trim();
        }
    }
}
