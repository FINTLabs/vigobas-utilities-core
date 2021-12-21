using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Vigo.Bas.Utils.SocialSecurityNumber
{
    public class SocialSecurityNumberValidator
    {
        private  readonly ValidationKind _validationKind;

        //public Validator(ValidationKind validationKind)
        //{
        //    _validationKind = validationKind;
        //}

        public OperationResult ValidateOne(string number)
        {
            OperationResult result = Validate(number);
            return result;
        }

        public OperationResult RepeatValidation()
        {
            OperationResult result = new OperationResult { Code = Statuscode.Ok };
            string number = Console.ReadLine();
            while (!string.IsNullOrWhiteSpace(number))
            {
                result = Validate(number);
                Console.WriteLine(result.ToString());
                number = Console.ReadLine();
            }
            return result;
        }

        private OperationResult Validate(string number)
        {
            OperationResult result;
            switch (_validationKind)
            {
                case ValidationKind.BirthNumber:
                    result = ValidateBirthNumber(number);
                    break;
                case ValidationKind.DNumber:
                    result = ValidateDNumber(number);
                    break;
                default:
                    result = ValidateAnyIdNumber(number);
                    break;
            }
            return result;
        }


        private static OperationResult ValidateBirthNumber(string number)
        {
            try
            {
                BirthNumber birthNumber = new BirthNumber(number);
                return new OperationResult { Code = Statuscode.Ok };
            }
            catch (NinException ex)
            {
                return new OperationResult { Code = ex.Code, Message = ex.Message };
            }
        }

        private static OperationResult ValidateDNumber(string number)
        {
            try
            {
                DNumber dNumber = new DNumber(number);
                return new OperationResult { Code = Statuscode.Ok };
            }
            catch (NinException ex)
            {
                return new OperationResult { Code = ex.Code, Message = ex.Message };
            }
        }

        public static OperationResult ValidateAnyIdNumber(string number)
        {

            BirthNumber bn = BirthNumber.Create(number);
            if (bn != null)
            {
                return new OperationResult { Code = Statuscode.Ok, Message = "Personnummer" };
            }
            DNumber dn = DNumber.Create(number);
            if (dn != null)
            {
                return new OperationResult { Code = Statuscode.Ok, Message = "D-nummer" };
            }
            return new OperationResult { Code = Statuscode.NoMatchFound, Message = "Kan ikke valideres" };
        }

        public struct OperationResult
        {
            public Statuscode Code;
            public string Message;
            public override string ToString()
            {
                return string.Format("{0}: {1}", (int)Code, Message ?? "Ok");
            }
        }

        public enum ValidationKind { Any, OrganizationNumber, BirthNumber, DNumber };
        public enum GenerationKind { Unknown, OrganizationNumber, BirthNumber, DNumber };

    }
}

