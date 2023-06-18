using Newtonsoft.Json.Linq;

namespace GFSProduct
{
    public interface ICommonFunction
    {
        public Task<JArray> getAccountData(string AccountNumber);
        public Task<string> getProductCatName(string product_Cat_Id);
        public Task<JArray> getContactData(string contact_id);

        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
