namespace CreateLeads
{
    public interface ICreateLeadExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string _transactionID { set; get; }
        public Task<LeadReturnParam> ValidateLeade(dynamic LeadData, string appkey);
        public Task<LeadReturnParam> CreateLead(dynamic LeadData);
        public Task<string> EncriptRespons(string ResponsData);
    }
}
