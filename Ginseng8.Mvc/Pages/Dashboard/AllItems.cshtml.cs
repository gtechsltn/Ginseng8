﻿using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Ginseng.Mvc.Queries;
using Ginseng.Mvc.Queries.SelectLists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Postulate.Base.Extensions;

namespace Ginseng.Mvc.Pages.Work
{
	[Authorize]
	public class AllItemsModel : DashboardPageModel
	{
		public AllItemsModel(IConfiguration config) : base(config)
		{
		}

		[BindProperty(SupportsGet = true)]
		public string Query { get; set; }

		[BindProperty(SupportsGet = true)]
		public int? FilterUserId { get; set; }

		[BindProperty(SupportsGet = true)]
		public int? FilterProjectId { get; set; }

		[BindProperty(SupportsGet = true)]
		public int? FilterMilestoneId { get; set; }

		[BindProperty(SupportsGet = true)]
		public int? FilterSizeId { get; set; }

		[BindProperty(SupportsGet = true)]
		public int? FilterActivityId { get; set; }

		[BindProperty(SupportsGet = true)]
		public bool? PastDue { get; set; }

		/// <summary>
		/// Projects found by a search
		/// </summary>
		public IEnumerable<ProjectInfoResult> Projects { get; set; }

		public SelectList UserSelect { get; set; }
		public SelectList ActivitySelect { get; set; }

		protected override async Task<RedirectResult> GetRedirectAsync(SqlConnection connection)
		{
			if (int.TryParse(Query, out int number))
			{
				if (connection.Exists("[dbo].[WorkItem] WHERE [OrganizationId]=@orgId AND [Number]=@number", new { orgId = OrgId, Number = number }))
				{
					return new RedirectResult($"/WorkItem/View/{number}");
				}
			}

			return await Task.FromResult<RedirectResult>(null);
		}

		protected override async Task OnGetInternalAsync(SqlConnection connection)
		{
			var userList = await new UserSelect() { OrgId = OrgId }.ExecuteItemsAsync(connection);
			userList.Insert(0, new SelectListItem() { Value = "0", Text = "- no assigned user -" });
			UserSelect = new SelectList(userList, "Value", "Text", FilterUserId);

			var activityList = await new ActivitySelect() { OrgId = OrgId }.ExecuteItemsAsync(connection);
			activityList.Insert(0, new SelectListItem() { Value = "0", Text = "- no current activity -" });
			ActivitySelect = new SelectList(activityList, "Value", "Text", FilterActivityId);
			

			if (!string.IsNullOrEmpty(Query))
			{
				// if a search was passed in, execute that on the project list
				Projects = await new ProjectInfo() { OrgId = OrgId, TitleAndBodySearch = Query, AppId = CurrentOrgUser.CurrentAppId, IsActive = true }.ExecuteAsync(connection);
			}
		}

		protected override OpenWorkItems GetQuery()
		{
			return new OpenWorkItems(QueryTraces)
			{
				OrgId = OrgId,
				AppId = CurrentOrgUser.CurrentAppId,
				ProjectId = FilterProjectId,
				LabelId = LabelId,				
				MilestoneId = FilterMilestoneId,
				SizeId = FilterSizeId,
				TitleAndBodySearch = Query,
				IsPastDue = PastDue,
				AssignedUserId = FilterUserId,
				ActivityId = FilterActivityId
			};
		}
	}
}