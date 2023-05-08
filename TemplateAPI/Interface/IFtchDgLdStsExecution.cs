namespace DigiLead
{
    public interface IFtchDgLdStsExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public Task<FtchDgLdStsReturn> getDigiLeadStatus(dynamic CaseData);
        public Task<FtchDgLdStsReturn> ValidateFtchDgLdSts(dynamic CaseData, string appkey);
        
       
    }
}
