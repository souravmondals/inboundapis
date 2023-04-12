﻿using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ManageCase
{
    
    public class LeadMsdProperty
    {      
        public string caseorigincode { get; set; }
        public string eqs_casetype { get; set; }
        public string title { get; set; }
        public int prioritycode { get; set; }
        public string description { get; set; }


    }


    public class CaseProperty
    {
        public string ccs_classification { get; set; }
        public string eqs_customerid { get; set; }
        public string Accountid { get; set; }
        public string customerid { get; set; }
        public string CategoryId { get; set; }
        public string SubCategoryId { get; set; }


    }

    public class CaseReturnParam
    {
        public string ReturnCode { get; set; }      
        public string CaseID { get; set; }
        public int IsError { get; set; } 
        public string ErrorMessage { get; set; }
        public string InfoMessage { get; set; }
        public string Messages { get; set; }

    }

    public class CaseStatusRtParam
    {
        public string ReturnCode { get; set; }
        public string CaseStatus { get; set; }       
        public string ErrorMessage { get; set; }
        public string InfoMessage { get; set; }
        public int IsError { get; set; }

    }

    public class CaseDetails
    {
        public string CaseID { get; set; }
        public string CaseStatus { get; set; }
        public string Description { get; set; }

        public string Casetype { get; set; }
        public string Subject { get; set; }
        public string Priority { get; set; }

    }

    public class CaseListParam
    {
        public string ReturnCode { get; set; }
        public List<CaseDetails> AllCases { get; set; }
        public string ErrorMessage { get; set; }
        public string InfoMessage { get; set; }
        public int IsError { get; set; }

    }

}
