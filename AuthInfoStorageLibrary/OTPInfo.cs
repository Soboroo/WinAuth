using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthInfoStorageLibrary
{
    public class OTPInfo
    {
        private string _accountName;
        private string _type;
        private string _secret;
        private string _issuer;
        private string _algorithm = "SHA1";
        private string _digits = "6";
        private string _period = "30";
        private string _counter = "0";

        public string AccountName { get { return _accountName; } set { _accountName = value; } }
        public string Type 
        { 
            get { return _type; } 
            set 
            {
                if (value == "totp" || value == "hotp")
                {
                    _type = value;
                }
                else
                {
                    throw new ArgumentException("Only types \"totp\" and \"hotp\" are allowed.");
                }
            } 
        }
        public string Secret { get { return _secret; } set { _secret = value; } }
        public string Issuer { get { return _issuer; } set { _issuer = value; } }
        public string Algorithm 
        { 
            get 
            { 
                return _algorithm; 
            } 
            set 
            {
                if (value == "SHA1" || value == "SHA256" || value == "SHA512")
                {
                    _algorithm = value;
                }
            } 
        }
        public string Digits 
        { 
            get 
            { 
                return _digits; 
            } 
            set 
            {
                if (value.All(char.IsDigit))
                {
                    _digits = value;
                }
            } 
        }
        public string Period 
        {
            get 
            {
                return _period;
            } 
            set 
            { 
                if (value.All(char.IsDigit))
                {
                    _period = value;
                }
            } 
        }
        public string Counter 
        {
            get 
            {
                return _counter;
            }
            set 
            {
                if (value.All(char.IsDigit))
                {
                    _counter = value;
                }
            } 
        }
    }
}
