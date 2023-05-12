using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PhoneNumbers;
using Vigo.Bas.ManagementAgent.Log;

namespace Vigo.Bas.Utils.Phonenumber
{
    public class PhoneNumberUtils
    {
        public static string CleanAndFormat(string phoneNumber, PhonenumberType type)
        {
            string cleanedPhoneNumber = Regex.Replace(phoneNumber, @"[^0-9]", "").Trim(".".ToCharArray());

            ////If cleanphoneumber don't contain any numbers ,just return original input
            //if (cleanedPhoneNumber.Length == 0)
            //{
            //    return phoneNumber;
            //}

            if (cleanedPhoneNumber.Length < 8)
            {
                Logger.Log.ErrorFormat($"Phonenumber {phoneNumber} not valid: It has less than 8 digits");

                return "NOTVALIDPHONENUMBER";
            }
            //if cleanphnenumber starts with two zeros ,strip them off
            if (cleanedPhoneNumber.StartsWith("00"))
            {
                cleanedPhoneNumber = cleanedPhoneNumber.Substring(2);
            }

            //if phonenumber is 8 numbers long ,asume it's norwegian and add 47
            if (cleanedPhoneNumber.Length == 8)
            {
                cleanedPhoneNumber = string.Format("47{0}", cleanedPhoneNumber);
            }

            //Phonnumbers should now be in format countrycode Phonenumber ,no pluss sign og leading zeros

            string formatedPhoneNumber;

            PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
            PhoneNumber numberProto = new PhoneNumber();

            string countryCode = string.Empty;
            string nationalNumber = string.Empty;

            try
            {
                phoneNumber = "+" + cleanedPhoneNumber;
                numberProto = phoneUtil.Parse(phoneNumber, "");

                countryCode = numberProto.CountryCode.ToString();
                nationalNumber = numberProto.NationalNumber.ToString();
            }
            catch (NumberParseException e)
            {
                Logger.Log.ErrorFormat($"Phonenumber {phoneNumber} not valid. NumberParseException was thrown: {e.ToString()}");

                return "NOTVALIDPHONENUMBER";
            }
            switch (type)
            {
                case PhonenumberType.HumanReadable:
                    {
                        formatedPhoneNumber = phoneUtil.Format(numberProto, PhoneNumberFormat.INTERNATIONAL);
                        break;
                    }
                case PhonenumberType.HumanReadableNational:
                    {
                        formatedPhoneNumber = phoneUtil.Format(numberProto, PhoneNumberFormat.NATIONAL);
                        break;
                    }
                case PhonenumberType.Mobile:

                    if (cleanedPhoneNumber.Length == 10)
                    {
                        formatedPhoneNumber = string.Format("{0:+## ### ## ###}", Convert.ToInt64(cleanedPhoneNumber));
                    }
                    else if (cleanedPhoneNumber.Length == 11)
                    {
                        formatedPhoneNumber = string.Format("{0:+## ## ### ####}", Convert.ToInt64(cleanedPhoneNumber));
                    }
                    else
                    {
                        formatedPhoneNumber = cleanedPhoneNumber;
                    }
                    break;

                case PhonenumberType.Work:
                case PhonenumberType.Private:

                    if (cleanedPhoneNumber.Length == 10)
                    {
                        formatedPhoneNumber = string.Format("{0:+## ## ## ## ##}", Convert.ToInt64(cleanedPhoneNumber));
                    }
                    else if (cleanedPhoneNumber.Length == 11)
                    {
                        formatedPhoneNumber = string.Format("{0:+## ## ### ####}", Convert.ToInt64(cleanedPhoneNumber));
                    }
                    else
                    {
                        formatedPhoneNumber = cleanedPhoneNumber;
                    }
                    break;

                case PhonenumberType.MachineReadableType1:
                    formatedPhoneNumber = string.Format("00{0}", cleanedPhoneNumber);
                    break;
                case PhonenumberType.MachineReadableType2:
                    formatedPhoneNumber = cleanedPhoneNumber;
                    break;
                case PhonenumberType.MachineReadableType3:
                    formatedPhoneNumber = string.Format("+{0}", cleanedPhoneNumber);
                    break;
                case PhonenumberType.AzureReadable:
                    {
                        formatedPhoneNumber = string.Format($"+{countryCode} {nationalNumber}");
                        break;
                    }
                default:
                    formatedPhoneNumber = cleanedPhoneNumber;
                    break;
            }

            return formatedPhoneNumber;

        }

        public enum PhonenumberType
        {
            Mobile,
            Work,
            Private,
            MachineReadableType1,
            MachineReadableType2,
            MachineReadableType3,
            AzureReadable,
            HumanReadable, 
            HumanReadableNational
        }
    }
}

