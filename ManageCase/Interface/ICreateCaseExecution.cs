﻿namespace ManageCase
{
    public interface ICreateCaseExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public Task<CaseReturnParam> CreateCase(dynamic CaseData);
        public Task<CaseReturnParam> ValidateCreateCase(dynamic CaseData, string appkey);
        public Task<CaseListParam> getCaseList(dynamic CaseData, string appkey);
        public Task<CaseStatusRtParam> ValidategetCaseStatus(dynamic CaseData, string appkey);
    }
}