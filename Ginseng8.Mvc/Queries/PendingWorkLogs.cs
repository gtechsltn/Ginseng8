﻿using Postulate.Base;
using Postulate.Base.Attributes;
using Postulate.Base.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;

namespace Ginseng.Mvc.Queries
{
	public class PendingWorkLogsResult
	{
		public int Id { get; set; }
		public int? ProjectId { get; set; }
		public int? WorkItemId { get; set; }
		public int UserId { get; set; }
		public DateTime Date { get; set; }
		public decimal Hours { get; set; }
		public string TextBody { get; set; }
		public string HtmlBody { get; set; }
		public int? SourceType { get; set; }
		public int? SourceId { get; set; }
		public string CreatedBy { get; set; }
		public DateTime DateCreated { get; set; }
		public string ModifiedBy { get; set; }
		public DateTime? DateModified { get; set; }
		public int OrganizationId { get; set; }
		public string Title { get; set; }
		public bool IsProject { get; set; }
		public int? WorkItemNumber { get; set; }
		public int Year { get; set; }
		public int WeekNumber { get; set; }
	}

	public class PendingWorkLogs : Query<PendingWorkLogsResult>, ITestableQuery
	{
		public PendingWorkLogs() : base(
			@"SELECT
				[wl].*,
				CASE
					WHEN [wl].[ProjectId] IS NOT NULL THEN [p].[Name]
					WHEN [wl].[WorkItemId] IS NOT NULL THEN [wi].[Title]
				END AS [Title],
				CONVERT(bit, CASE
					WHEN [wl].[ProjectId] IS NOT NULL THEN 1
					ELSE 0
				END) AS [IsProject],
				[wi].[Number] AS [WorkItemNumber],
				DATEPART(yyyy, [wl].[Date]) AS [Year],
				DATEPART(ww, [wl].[Date]) AS [WeekNumber]
			FROM
				[dbo].[PendingWorkLog] [wl]
				LEFT JOIN [dbo].[WorkItem] [wi] ON [wl].[WorkItemId]=[wi].[Id]
				LEFT JOIN [dbo].[Project] [p] ON [wl].[ProjectId]=[p].[Id]
			WHERE
				[wl].[OrganizationId]=@orgId {andWhere}")
		{
		}

		public int OrgId { get; set; }

		[Where("[wl].[UserId]=@userId")]
		public int? UserId { get; set; }

		[Where("DATEPART(yyyy, [wl].[Date])=@year")]
		public int? Year { get; set; }

		[Where("DATEPART(ww, [wl].[Date])=@weekNumber")]
		public int? WeekNumber { get; set; }

		public IEnumerable<dynamic> TestExecute(IDbConnection connection)
		{
			return TestExecuteHelper(connection);
		}

		public static IEnumerable<ITestableQuery> GetTestCases()
		{
			yield return new PendingWorkLogs() { OrgId = 0 };
			yield return new PendingWorkLogs() { Year = 2019 };
			yield return new PendingWorkLogs() { WeekNumber = 1 };
			yield return new PendingWorkLogs() { UserId = 1 };
		}
	}
}