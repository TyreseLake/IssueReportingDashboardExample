using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;

namespace API.Interfaces
{
    public interface IIssueTypeRepository
    {
        Task<IssueType> GetIssueTypeByName(string issueTypeName);
        void AddUserIssueType(AppUser user, IssueType issueType);
        Task<List<IssueType>> GetIssueTypes();
        Task<List<string>> GetIssueTypeNames();
        // void AddUserIssueTypeByName(AppUser user, string issueTypeName);
        // void RemoveUserIssueTypeByName(AppUser user, string issueTypeName);
        void RemoveUserIssueType(UserIssueType userIssueType);
        // void RemoveAllUserIssueTypes(AppUser user);
        Task<bool> SaveAllAsync();
    }
}