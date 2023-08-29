﻿using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DigiCustLead
{

    public class CustLeadElement
    {
        public string eqs_crmproductcategorycode { get; set; }
        public int leadsourcecode { get; set; }
        public string firstname { get; set; }
        public string middlename { get; set; }
        public string lastname { get; set; }
        public string mobilephone { get; set; }
        public string yomifullname
        {
            get
            {
                return firstname + " " + middlename + " " + lastname;
            }

        }

        public string eqs_pincode { get; set; }
        public string  eqs_dob { get; set; }
        public string eqs_internalpan { get; set; }
        public string eqs_voterid { get; set; }
        public string eqs_dlnumber { get; set; }
        public string eqs_passportnumber { get; set; }
        public string eqs_ckycnumber { get; set; }

    }

    public class CustLeadElementCorp
    {
        public string eqs_crmproductcategorycode { get; set; }
        public int leadsourcecode { get; set; }
        public string eqs_companynamepart1 { get; set; }
        public string eqs_companynamepart2 { get; set; }
        public string eqs_companynamepart3 { get; set; }
        public string eqs_contactmobile { get; set; }
        public string eqs_contactperson { get; set; }
        public string eqs_cinnumber { get; set; }
        public string eqs_tannumber { get; set; }
        public string eqs_gstnumber { get; set; }
        public string eqs_cstvatnumber { get; set; }
 
        public string eqs_internalpan { get; set; }
    }

    public class CreateCustLeadReturn
    {
        public string AccountapplicantID { get; set; }
        public string LeadID { get; set; }

        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class UpdateCustLeadReturn
    {
        public string IndividualDDEID { get; set; }
        public string CorporateDDEID { get; set; }
        public string AddressID { get; set; }
        public string FATCAID { get; set; }
        public List<string> Documents { get; set; }

        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }





}
