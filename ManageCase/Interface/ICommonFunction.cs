using Newtonsoft.Json.Linq;

namespace ManageCase
{
    public interface ICommonFunction
    {
       
        public Task<string> getCaseStatus(string CityCode);
        public Task<string> getAccountId(string BranchCode);
        public Task<string> getCustomerId(string CustomerCode);
        public Task<string> getCategoryId(string CustomerCode);
        public Task<string> getclassificationId(string CustomerCode);
        public Task<string> getSubCategoryId(string CustomerCode);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
        public Task<string> MeargeJsonString(string json1, string json2);

    }
}
