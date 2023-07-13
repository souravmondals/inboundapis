using Newtonsoft.Json.Linq;

namespace FetchAccountLead
{
    public interface ICommonFunction
    {      
        public Task<string> getBranchId(string branch_Id);
        public Task<string> getAccRelationshipId(string AccRelationship_code);
        public Task<JArray> getApplicantDetails(string ApplicantId);
        public Task<JArray> getCustomerDetails(string CustomerId);
        public Task<Dictionary<string, string>> getProductId(string ProductCode);
        public Task<string> getEntityId(string Entity_code);
        public Task<string> getSubEntityId(string SubEntity_code);
        public Task<string> getRelationshipId(string Relationship_code);

        public Task<JArray> getContactData(string contact_id);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
        public Task<string> MeargeJsonString(string json1, string json2);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
