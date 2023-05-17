using Newtonsoft.Json.Linq;

namespace ManageCase
{
    public interface ICommonFunction
    {
       
        public Task<string> getCaseStatus(string CityCode);
        public Task<string> getAccountId(string BranchCode);
        public Task<string> getChannelId(string channelName);
        public Task<string> getCustomerId(string CustomerCode);
        public Task<string> getCategoryId(string CustomerCode);
        public Task<string> getclassificationId(string CustomerCode);
        public Task<string> getSubCategoryId(string CustomerCode, string CategoryId);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
        public Task<string> MeargeJsonString(string json1, string json2);
        public Task<List<MandatoryField>> getMandatoryFields(string subCategoryID);
        public Task<string> getIDfromMSDTable(string tablename, string idfield, string filterkey, string filtervalue);

    }
}
