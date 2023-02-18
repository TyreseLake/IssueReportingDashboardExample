using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers.Pagination;

namespace API.Interfaces
{
    public interface IIssueReportingRepository
    {
        void AddIssueStatus(IssueStatus issueStatus);
        void AddIssueReport(IssueReport issueReport);
        void AddIssueReports(IEnumerable<IssueReport> issueReports);
        Task<IssueStatus> GetIssueStatus(int issueStatusId);
        Task<IssueReport> GetIssueReport(int issueReportId);
        Task<List<IssueStatusReportDto>> AsyncGetIssueReports(IssueReportStatusParams issueReportStatusParams);
        string[] getPlatforms();
        Task<PagedList<IssueReportStatusDto>> AsyncGetPaginatedIssueReportStatuses(IssueReportStatusParams issueReportStatusParams, AppUser currentUser = null);
        Task<List<IssueReportStatusDto>> AsyncGetIssueReportStatuses(IssueReportStatusParams issueReportStatusParams);
        Task<IEnumerable<IssueReport>> GetUnreviewedWithCoordinatesByIssueType(int id);
        Task<IEnumerable<IssueReport>> GetIssueReportsByPlatform(string platform);
        Task<PagedList<IssueReportItemDto>> GetIssueReportsForStatus(IssueReportParams issueReportParams);
        void AddIssueStatusUpdate(StatusUpdate statusUpdate);
        Task<StatusUpdate> AsyncGetIssueStatusUpdateById(int statusUpdateId);
        Task<PagedList<IssueStatusUpdateDto>> AsyncGetIssueStatusUpdatesByIssueStatusId(IssueReportParams issueReportParams);
        Task<List<IssueStatusUpdateDto>> AsyncGetIssueStatusUpdates(int statusId);
        Task<IEnumerable<IssueReportStatusDto>> AsyncGetIssueStatusByDetails(IssueStatusSearchParams issueStatusSearchParams);
        Task<bool> SaveAllAsync();
        Task<IEnumerable<IssueReportStatusDto>> AsyncGetIssueStatusesByCoordinates(int issueTypeId, float locationLatitude, float locationLongitude, string completed, float eps);
        void DeleteIssueStatus(IssueStatus issueStatus);
        void ClearStatusUpdateApprovalItems(StatusUpdate statusUpdate);
        Task<IEnumerable<IssueStatus>> AsyncGetIssueStatuses();
        Task<Tuple<IEnumerable<IssueReportStatusDto>, int>> AsyncGetFilteredIssueStatuses(IssueReportStatusParams issueReportStatusParams = null, AppUser currentUser = null, int count = 10);
        Task<IEnumerable<Object>> AsyncGetIssueStatusTotals(string[] issueTypeAccess, string[] districtAccess, string type = null);
    }
}