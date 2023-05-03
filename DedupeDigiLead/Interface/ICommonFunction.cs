using Newtonsoft.Json.Linq;

namespace DedupeDigiLead
{
    public interface ICommonFunction
    {
        public Task<JArray> getLeadData(string LeadID);
        public Task<JArray> getNLTRData(string Fullname);
        public Task<JArray> getNLData(string Fullname);

    }
}
