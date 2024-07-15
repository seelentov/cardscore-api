using System.Globalization;
using System.Text.RegularExpressions;

namespace cardscore_api.Services
{
    public class FormatService
    {
        public FormatService() { }

        public async Task<DateTime> DateTimeRounding(DateTime dateTime)
        {
            int minutes = dateTime.Minute;

            if (minutes.ToString().EndsWith("9"))
            {
                return dateTime.AddMinutes(1);
            }
            else if (minutes.ToString().EndsWith("1"))
            {
                return dateTime.AddMinutes(-1);
            }
            return dateTime;

        }


        public bool CanParseDDMM(string dateString)
        {
            try
            {
                ParseDateTimeDDMM(dateString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool CanParseDDMMYY(string dateString)
        {
            try
            {
                ParseDateTimeDDMMYY(dateString);
                return true;
            }
            catch
            {
                return false;
            }
        }


        public string SubtractTime(string timeString, int hours, int minutes = 0)
        {
            TimeSpan timeSpan = TimeSpan.Parse(timeString);

            TimeSpan newTimeSpan = timeSpan.Subtract(new TimeSpan(hours, minutes, 0));

            return newTimeSpan.ToString(@"hh\:mm");
        }

        public DateTime ParseDateTimeDDMM(string input)
        {

            const string format = "dd.MM, HH:mm";

            return DateTime.ParseExact(input, format, CultureInfo.InvariantCulture);
        }

        public DateTime ParseDateTimeDDMMYY(string input)
        {
            const string format = "dd.MM.yy, HH:mm";

            return DateTime.ParseExact(input, format, CultureInfo.InvariantCulture);
        }

        public DateTime ParseDateTimeDDMMYYYY(string input)
        {
            const string format = "dd.MM.yyyy";

            return DateTime.ParseExact(input, format, CultureInfo.InvariantCulture);
        }

        public DateTime ParseDateTimeDDMMYYYYHHMM(string input)
        {
            const string format = "dd.MM.yyyy HH:mm";

            return DateTime.ParseExact(input, format, CultureInfo.InvariantCulture);
        }



        public bool TryParseDateTime(string dateString)
        {
            try
            {
                ParseDateTime(dateString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public DateTime ParseDateTime(string input)
        {
            string[] parts = input.Split(',');
            string datePart = parts[parts.Length - 1].Trim();

            const string format1 = "dd.MM.yyyy HH:mm";
            const string format2 = "dd.MM.yyyy";

            if (datePart.Contains(':'))
            {
                return DateTime.ParseExact(datePart, format1, CultureInfo.InvariantCulture);
            }
            else
            {
                return DateTime.ParseExact(datePart, format2, CultureInfo.InvariantCulture);
            }
        }

        public string ClearString(string str)
        {
            var clearedStr = str.Replace("\n", string.Empty).Replace("\t", string.Empty);
            return Regex.Replace(clearedStr, "<.*?>", string.Empty).Trim();
        }

        public int ToInt(string str)
        {
            int result;
            int.TryParse(string.Join("", str.Where(c => char.IsDigit(c))), out result);
            return result;
        }

        public string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

    }
}
