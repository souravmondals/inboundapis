using Newtonsoft.Json.Linq;

namespace CreateCase
{
    public interface IQueryParser
    {
        public Task<List<JObject>> HttpApiCall(string odataQuery, HttpMethod httpMethod, string parameterToPost = "");
    }
}
