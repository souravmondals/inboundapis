using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ManageCase
{
    
    public class LeadMsdProperty
    {
       // public string leadid { get; set; }
        public string caseorigincode { get; set; }
        public string eqs_casetype { get; set; }
        public string title { get; set; }
        public int prioritycode { get; set; }
        public string description { get; set; }


    }


    public class CaseProperty
    {
        public string ccs_classification { get; set; }
        public string eqs_customerid { get; set; }
        public string Accountid { get; set; }
        public string customerid { get; set; }
        public string CategoryId { get; set; }
        public string SubCategoryId { get; set; }


    }

    public class CaseReturnParam
    {
        public string ReturnCode { get; set; }      
        public string CaseID { get; set; }
        public int IsError { get; set; } 
        public string ErrorMessage { get; set; }
        public string InfoMessage { get; set; }
        public string Messages { get; set; }

    }

}
