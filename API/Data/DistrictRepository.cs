using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository for handling all district related requests
/// </summary>
namespace API.Data
{
    public class DistrictRepository : IDistrictRepository
    {
        private readonly DataContext _context;
        public DistrictRepository(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Function to fetch all districts from the database
        /// </summary>
        /// <returns>List of districts</returns>
        public async Task<IEnumerable<District>> GetDistricts()
        {
            return await _context.Districts.ToListAsync();
        }

        /// <summary>
        /// Get the names of all districts
        /// </summary>
        /// <returns>District names</returns>
        public async Task<IEnumerable<string>> GetDistrictNames()
        {
            var districts = await _context.Districts.ToListAsync();
            return districts.Select(d => d.Name).ToList();
        }

        // public async void AddUserDistrictByName(AppUser user, string districtName)
        // {
        //     var district = await _context.Districts.Where(x => x.Name.ToLower() == districtName.ToLower()).SingleOrDefaultAsync();
        //     var userDistrict = new UserDistrict 
        //     {
        //         District = district,
        //         User = user
        //     };
        //     _context.UserDistricts.Add(userDistrict);
        // }
        
        
        // public async void RemoveUserDistrictByName(AppUser user, string districtName)
        // {
        //     var district = await _context.Districts.Where(x => x.Name.ToLower() == districtName.ToLower()).SingleOrDefaultAsync();
        //     var userDistrict = new UserDistrict 
        //     {
        //         District = district,
        //         User = user
        //     };
        //     _context.UserDistricts.Remove(userDistrict);
        // }

        /// <summary>
        /// Removes a user's district from the database
        /// </summary>
        /// <param name="userDistrict">The district to be removed</param>
        public void RemoveUserDistrict(UserDistrict userDistrict)
        {
            _context.UserDistricts.Remove(userDistrict);
        }

        // /// <summary>
        // /// Removes all the districts for the particular user
        // /// </summary>
        // /// <param name="user">The user specified</param>
        // public void RemoveAllUserDistricts(AppUser user)
        // {
        //     foreach (var userDistrict in user.UserDistricts)
        //     {  
        //         user.UserDistricts.Remove(userDistrict);
        //         _context.UserDistricts.Remove(userDistrict);
        //     }
        // }

        /// <summary>
        /// Save the changes made to the District database table
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Get a district by name
        /// </summary>
        /// <param name="districtName">Specifies the name of the district to retrieve</param>
        /// <returns>A district </returns>
        public async Task<District> GetDistrictByName(string districtName)
        {
            return await _context.Districts.SingleOrDefaultAsync(d => d.Name.ToLower() == districtName.ToLower());
        }

        /// <summary>
        /// Add a district to a user
        /// </summary>
        /// <param name="user">Specify the user</param>
        /// <param name="district">Specify the district</param>
        public void AddUserDistrict(AppUser user, District district)
        {
            var did = district.Id;
            var uid = user.Id;
            var userDistrict = new UserDistrict 
            {
                DistrictId = did,
                UserId = uid
            };
            _context.UserDistricts.Add(userDistrict);
        }
    }
}