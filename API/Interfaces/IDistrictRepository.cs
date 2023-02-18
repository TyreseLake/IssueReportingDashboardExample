using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;

namespace API.Interfaces
{
    public interface IDistrictRepository
    {
        Task<District> GetDistrictByName(string districtName);
        void AddUserDistrict(AppUser user, District district);
        Task<IEnumerable<District>> GetDistricts();
        Task<IEnumerable<string>> GetDistrictNames();
        // void AddUserDistrictByName(AppUser user, string districtName);
        // void RemoveUserDistrictByName(AppUser user, string districtName);
        void RemoveUserDistrict(UserDistrict userDistrict);
        // void RemoveAllUserDistricts(AppUser user);
        Task<bool> SaveAllAsync();
    }
}