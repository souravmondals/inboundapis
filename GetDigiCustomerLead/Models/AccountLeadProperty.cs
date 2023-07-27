using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CustomerLead
{
       
   
    
    public class CustomerLeadReturn
    {   
        public Applicent AccountApplicants { get; set; }
      
        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class Applicent
    {

    }

    public class AccountApplicantCorp : Applicent
    {
      
        public string Companynamepart1 { get; set; }
        public string Companynamepart2 { get; set; }
        public string Companynamepart3 { get; set; }
        public string pan { get; set; }

    }

    public class AccountApplicantIndv : Applicent
    {
      
        public string title { get; set; }
        public string firstname { get; set; }
        public string middlename { get; set; }
        public string lastname { get; set; }      
        public string pan { get; set; }
       

    }

   





}
