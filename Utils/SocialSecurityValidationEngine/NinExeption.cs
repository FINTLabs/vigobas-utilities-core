using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vigo.Bas.Utils.SocialSecurityNumber
{
    public enum Statuscode
    {
        Ok,
        IsNullOrEmpty,
        BadLength,
        BadCharacters,
        BadFirstDigit,
        BadDate,
        BadYearAndIndividualNumberCombination,
        BadCheckDigit,
        PatternIsNullOrEmpty,
        BadPatternLength,
        BadPattern,
        NoMatchFound,
        BadYear
    }

    public class NinException : Exception
    {
        public Statuscode Code { get; private set; }

        public NinException(Statuscode code, string message) : base(message)
        {
            Code = code;
        }
    }
}
