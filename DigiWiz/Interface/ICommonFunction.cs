﻿using Newtonsoft.Json.Linq;

namespace DigiWiz
{
    public interface ICommonFunction
    {
        public Task<JArray> getAccountData(string AccountNumber);
        public Task<string> getProductCatName(string product_Cat_Id);
        public Task<JArray> getContactData(string contact_id);
        public Task<string> getEntityType(string EntityId);

        public Task<string> getSubEntityType(string subEntityId);


    }
}
