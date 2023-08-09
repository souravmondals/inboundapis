using Newtonsoft.Json.Linq;

namespace CRMConnect
{
    public interface IQueryParser
    {
        public Task<List<JObject>> HttpApiCall(string odataQuery, HttpMethod httpMethod, string parameterToPost = "");
        public Task<string> HttpCBSApiCall(string odataQuery, HttpMethod httpMethod, string parameterToPost = "");
        public Task<string> PayloadEncryption(string V_requestData, string V_requestedID);
        public Task<string> getOptionSetTextToValue(string tableName, string fieldName, string OptionText);
        public Task<bool> DeleteFromTable(string tablename, string tableid = "", string filter = "", string filtervalu = "", string tableselecter = "");
        public Task<string> PayloadDecryption(string V_requestData);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
    }
}
