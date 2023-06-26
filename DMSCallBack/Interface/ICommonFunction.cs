using Newtonsoft.Json.Linq;

namespace DMSCallBack
{
    public interface ICommonFunction
    {      
        public Task<string> getDocumentID(string requestId);
       
        public Task<JArray> getApplicantDetails(string ApplicantId);
        public Task<JArray> getCustomerDetails(string CustomerId);
        public Task<JArray> getContactData(string contact_id);

        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
