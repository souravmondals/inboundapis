namespace DedupeDigiLead
{
    public interface IDedupDgLdNLExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        
        public Task<dynamic> ValidateDedupDgLdNL(dynamic CaseData, string appkey, string type);
        public Task<dynamic> getDedupDgLdNLStatus(dynamic RequestData, string type);        
    }
}
