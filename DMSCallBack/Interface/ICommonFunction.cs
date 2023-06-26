using Newtonsoft.Json.Linq;

namespace DMSCallBack
{
    public interface ICommonFunction
    {      
        public Task<string> getDocumentID(string requestId);
       
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
