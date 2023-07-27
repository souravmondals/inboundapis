using Newtonsoft.Json.Linq;

namespace CustomerLead
{
    public interface ICommonFunction
    {      
        public Task<string> getDocCategoryId(string Category_code);
        public Task<string> getDocSubcatId(string Subcat_code);
        public Task<JArray> getApplicentsSetails(string LeadAccId);
        public Task<JArray> getPreferences(string applicantid);
        public Task<string> getProductId(string Productcode);
        public Task<string> getProductCategoryId(string Productcatcode);
        public Task<string> getDocTypeId(string Type_code);
        public Task<string> getRelationShipId(string RelationShipCode);
        public Task<string> getCustomerType(string City_code);
        public Task<string> getApplicentCustId(string ApplicantId);
        public Task<string> getTitle(string TitleId);

        public Task<JArray> getLeadDetails(string contact_id);
     
        public Task<JArray> getCustomerDetails(string CustId);
        public Task<string> MeargeJsonString(string json1, string json2);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
