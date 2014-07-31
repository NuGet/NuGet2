using System;
using System.Globalization;
using System.Text;

namespace NuGet.Versioning
{
    public class VersionRangeFormatter : IFormatProvider, ICustomFormatter
    {
        private const string LessThanOrEqualTo = "\u2264";
        private const string GreaterThanOrEqualTo = "\u2265";

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            string formatted = null;
            Type argType = arg.GetType();

            if (argType == typeof(IFormattable))
            {
                formatted = ((IFormattable)arg).ToString(format, formatProvider);
            }
            else if (!String.IsNullOrEmpty(format))
            {
                VersionRange range = arg as VersionRange;

                if (range != null)
                {
                    // single char identifiers
                    if (format.Length == 1)
                    {
                        formatted = Format(format[0], range);
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder(format.Length);

                        for (int i = 0; i < format.Length; i++)
                        {
                            string s = Format(format[i], range);

                            if (s == null)
                            {
                                sb.Append(format[i]);
                            }
                            else
                            {
                                sb.Append(s);
                            }
                        }

                        formatted = sb.ToString();
                    }
                }
            }

            return formatted;
        }

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter)
                || formatType == typeof(VersionRange))
            {
                return this;
            }

            return null;
        }

        private static string Format(char c, VersionRange range)
        {
            string s = null;

            switch (c)
            {
                case 'P':
                    s = PrettyPrint(range);
                    break;
                case 'L':
                    s =  range.HasLowerBound ? String.Format(new VersionFormatter(), "{0:N}", range.MinVersion) : string.Empty;
                    break;
                case 'U':
                    s = range.HasUpperBound ? String.Format(new VersionFormatter(), "{0:N}", range.MaxVersion) : string.Empty;
                    break;
                case 'N':
                    s = GetToString(range);
                    break;
            }

            return s;
        }

        private static string GetToString(VersionRange range)
        {
            string s = null;
            VersionFormatter versionFormatter = new VersionFormatter();

            if (range.HasLowerBound && range.IsMinInclusive && !range.HasUpperBound)
            {
                s = String.Format(versionFormatter, "{0:N}", range.MinVersion);
            }
            else if(range.HasLowerAndUpperBounds && range.IsMinInclusive && range.IsMaxInclusive &&
                range.MinVersion.Equals(range.MaxVersion))
            {
                // TODO: Does this need a specific version comparision? Does metadata matter?

                s = String.Format(versionFormatter, "[{0:N}]", range.MinVersion);
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(range.HasLowerBound && range.IsMinInclusive ? '[' : '(');

                if (range.HasLowerBound)
                {
                    sb.AppendFormat(versionFormatter, "{0:N}", range.MinVersion);
                }

                sb.Append(", ");

                if (range.HasUpperBound)
                {
                    sb.AppendFormat(versionFormatter, "{0:N}", range.MaxVersion);
                }

                sb.Append(range.HasUpperBound && range.IsMaxInclusive ? ']' : ')');

                s = sb.ToString();
            }

            return s;
        }


        /// <summary>
        /// A pretty print representation of the VersionRange.
        /// </summary>
        private static string PrettyPrint(VersionRange range)
        {
            StringBuilder sb = new StringBuilder("(");
            VersionFormatter versionFormatter = new VersionFormatter();

            // no upper
            if (range.HasLowerBound && !range.HasUpperBound)
            {
                sb.Append(GreaterThanOrEqualTo);
                sb.AppendFormat(versionFormatter, " {0:N}", range.MinVersion);
            }
            // single version
            else if (range.HasLowerAndUpperBounds && range.MaxVersion.Equals(range.MinVersion) && range.IsMinInclusive && range.IsMaxInclusive)
            {
                sb.AppendFormat(versionFormatter, "= {0:N}", range.MinVersion);
            }
            else // normal range
            {
                if (range.HasLowerBound)
                {
                    if (range.IsMinInclusive)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", GreaterThanOrEqualTo);
                    }
                    else
                    {
                        sb.Append("> ");
                    }

                    sb.AppendFormat(versionFormatter, "{0:N}", range.MinVersion);
                }

                if (range.HasLowerAndUpperBounds)
                {
                    sb.Append(" && ");
                }

                if(range.HasUpperBound)
                {
                    if (range.IsMaxInclusive)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", LessThanOrEqualTo);
                    }
                    else
                    {
                        sb.Append("< ");
                    }

                    sb.AppendFormat(versionFormatter, "{0:N}", range.MaxVersion);
                }
            }

            sb.Append(")");

            // avoid ()
            if (sb.Length == 2)
            {
                sb.Clear();
            }

            return sb.ToString();
        }
    }
}
