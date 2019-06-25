﻿using Ginseng.Mvc.Classes;
using Ginseng.Mvc.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ginseng.Mvc.Pages.Dashboard
{
    [Authorize]
    public class CalendarModel : AppPageModel
    {
        public CalendarModel(IConfiguration config) : base(config)
        {
        }

        public IEnumerable<YearMonth> MonthCells { get; set; }
        public ILookup<YearMonth, DevCalendarProjectsResult> Projects { get; set; }
        public ILookup<YearMonth, DevMilestoneWorkingHoursResult> WorkingHours { get; set; }

        public bool ShowTeams
        {
            get { return !CurrentOrgUser.CurrentTeamId.HasValue && !CurrentOrgUser.CurrentAppId.HasValue; }
        }

        public int? GetBalance(YearMonth month, int appId, int developerId)
        {
            try
            {                
                var work = WorkingHours[month].Where(row => row.ApplicationId == appId).ToLookup(row => row.DeveloperId);
                return work[developerId].Sum(row => row.AvailableHours);
            }
            catch 
            {
                return null;
            }
        }

        public string GetBalanceBackColor(int? balance)
        {
            if (!balance.HasValue) return "auto";
            if (balance < 0) return "red";
            if (balance < 5) return "orange";
            return "lightgreen";
        }

        public string GetBalanceForeColor(int? balance)
        {
            if (!balance.HasValue) return "auto";
            if (balance < 0) return "white";
            return "auto";
        }

        public async Task OnGetAsync()
        {
            using (var cn = Data.GetConnection())
            {
                var projects = await new DevCalendarProjects()
                {
                    OrgId = OrgId,
                    TeamId = CurrentOrgUser.CurrentTeamId,
                    AppId = CurrentOrgUser.CurrentAppId
                }.ExecuteAsync(cn);
                Projects = projects.ToLookup(row => new YearMonth(row.Year, row.Month));

                if (projects.Any())
                {
                    var userIds = projects.GroupBy(row => row.DeveloperUserId).Select(grp => grp.Key).ToArray();

                    DateTime startDate = projects.Min(row => row.GetMonthStartDate());
                    DateTime endDate = projects.Max(row => row.GetMonthEndDate());

                    var workingHours = await new DevMilestoneWorkingHours()
                    {
                        OrgId = OrgId,
                        StartMilestoneDate = startDate,
                        EndMilestoneDate = endDate
                    }.ExecuteAsync(cn);
                    WorkingHours = workingHours.ToLookup(row => new YearMonth(row.Year, row.Month));
                }
                
                MonthCells = AppendMonths(Projects.Select(grp => grp.Key), 4);
            }
        }

        private IEnumerable<YearMonth> AppendMonths(IEnumerable<YearMonth> months, int count)
        {
            if (!months.Any())
            {
                var start = new YearMonth();
                return Enumerable.Range(1, count).Select(i => start + i);
            }

            var last = months.Last();
            var list = months.ToList();
            list.AddRange(Enumerable.Range(1, count).Select(i => last + i));
            return list;
        }
    }
}