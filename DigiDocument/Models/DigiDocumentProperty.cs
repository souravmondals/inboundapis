using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DigiDocument
{

    public class productFilter
    {
        public string gender { get; set; }
        public string age { get; set; }
        public string subentity { get; set; }
        public string customerSegment { get; set; }
        public string IsStaff { get; set; }
        public string productCategory { get; set; }
    }

    
    public class DgDocDtlReturn
    {
       
       
        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

}
