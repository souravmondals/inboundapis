using Newtonsoft.Json.Linq;

namespace DigiCustLead
{
    public interface ICommonFunction
    {
        public Task<string> getDDEFinalAccountIndvData(string AccountNumber);
        public Task<string> getDDEFinalAccountCorpData(string AccountNumber);
        public Task<string> getBranchId(string BranchCode);
        public Task<JArray> getContactData(string contact_id);
        public Task<string> getEntityID(string Entity);
        public Task<string> getSubentitytypeID(string Subentitytype);
        public Task<string> getPurposeID(string Purpose);
        public Task<Dictionary<string, string>> getProductId(string ProductCode);
        public Task<string> getTitleId(string Title);
        public Task<string> MeargeJsonString(string json1, string json2);
        public Task<JArray> getApplicentData(string Applicent_id);
        public Task<string> getEntityName(string EntityId);
        public Task<string> getRelationshipID(string relationshipCode);
        public Task<string> getAccRelationshipID(string accrelationshipCode);
        public Task<string> getCountryID(string CountryCode);
        public Task<string> getPincodeID(string PincodeCode);
        public Task<string> getCityID(string CityCode);
        public Task<string> getStateID(string StateCode);
        public Task<string> getAddressID(string DDEID);
        public  Task<string> getFatcaID(string DDEID);


    }
}
