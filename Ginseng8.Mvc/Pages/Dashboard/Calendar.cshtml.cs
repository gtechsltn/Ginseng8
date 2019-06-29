﻿using Ginseng.Models;
using Ginseng.Mvc.Classes;
using Ginseng.Mvc.Queries;
using Ginseng.Mvc.Queries.SelectLists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Postulate.SqlServer.IntKey;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Ginseng.Models.Extensions;
using System.Data;

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
        public ILookup<YearMonth, CalendarProjectMetricsResult> Metrics { get; set; }

        public IEnumerable<SelectListItem> ProjectItems { get; set; }

        public bool ShowTeamNames
        {
            get { return !CurrentOrgUser.CurrentTeamId.HasValue && !CurrentOrgUser.CurrentAppId.HasValue; }
        }

        public bool ShowAppNames
        {
            get { return !CurrentOrgUser.CurrentAppId.HasValue; }
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
                ProjectItems = await new ProjectSelect() { AppId = CurrentOrgUser.CurrentAppId, TeamId = CurrentOrgUser.CurrentTeamId }.ExecuteItemsAsync(cn);

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

                    var metrics = await new CalendarProjectMetrics() { OrgId = OrgId }.ExecuteAsync(cn);
                    Metrics = metrics.ToLookup(row => new YearMonth(row.Year, row.Month));
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

        public SelectList GetProjectSelect(IEnumerable<DevCalendarProjectsResult> excludeProjects)
        {
            var excludeProjectIds = excludeProjects.Select(prj => prj.ProjectId.ToString());
            var items = ProjectItems.Where(prj => !excludeProjectIds.Contains(prj.Value));
            return new SelectList(items, "Value", "Text");
        }

        public async Task<RedirectResult> OnPostAddProjectAsync(int year, int month, int projectId)
        {
            var dates = GetMilestoneDates(year, month);

            using (var cn = Data.GetConnection())
            {
                var milestones = await GetMilestonesAsync(cn, projectId, dates);
                foreach (var ms in milestones)
                {

                }

            }

            return Redirect("/Calendar");
        }

        private IEnumerable<DateTime> GetMilestoneDates(int year, int month)
        {
            var firstDay = new DateTime(year, month, 1);            
            int weeks = 0;

            do
            {                
                var milestoneDate = firstDay.NextDayOfWeek(DayOfWeek.Friday, weeks);
                weeks++;
                if (milestoneDate.Month != month) break;
                yield return milestoneDate;                
            } while (true);            
        }

        private async Task<IEnumerable<Milestone>> GetMilestonesAsync(IDbConnection connection, int projectId, IEnumerable<DateTime> dates)
        {
            var prj = await connection.FindAsync<Project>(projectId);

            List<Milestone> results = new List<Milestone>();

            foreach (var date in dates)
            {
                var ms = 
                    await connection.FindWhereAsync<Milestone>(new { TeamId = prj.TeamId, Date = date }) ??
                    new Milestone(date)
                    {
                        TeamId = prj.TeamId,
                        ApplicationId = prj.ApplicationId,
                        ProjectId = prj.Id
                    };

                if (ms.Id == 0) await connection.SaveAsync(ms);

                results.Add(ms);
            }

            return results;
        }

        private async Task CreatePlaceholderItemAsync(SqlConnection cn, Milestone record)
        {
            const string text = @"Placeholder item created with milestone. This enables you to filter for this project on the milestone dashboard. This will automatically close when you add another work item to this milestone.";

            var team = await cn.FindAsync<Team>(record.TeamId);

            var workItem = new Ginseng.Models.WorkItem()
            {
                OrganizationId = team.OrganizationId,
                TeamId = team.Id,
                ApplicationId = record.ApplicationId,
                MilestoneId = record.Id,
                ProjectId = record.ProjectId,
                Title = "Placeholder item created with milestone",
                HtmlBody = $"<p>{text}</p>",
                TextBody = text
            };

            await workItem.SetNumberAsync(cn);

            if (await Data.TrySaveAsync(cn, workItem))
            {
                var wil = new WorkItemLabel()
                {
                    WorkItemId = workItem.Id,
                    LabelId = await GetPlaceholderLabelIdAsync(cn)
                };

                await Data.TrySaveAsync(cn, wil);
            }
        }

        private async Task<int> GetPlaceholderLabelIdAsync(SqlConnection cn)
        {
            var label =
                await cn.FindWhereAsync<Label>(new { OrganizationId = OrgId, Name = Label.PlaceholderLabel }) ??
                new Label() { OrganizationId = OrgId, Name = Label.PlaceholderLabel, BackColor = "#B8B8B8", ForeColor = "black", IsActive = false };

            if (label.Id == 0) await cn.SaveAsync(label, CurrentUser);

            return label.Id;
        }
    }
}