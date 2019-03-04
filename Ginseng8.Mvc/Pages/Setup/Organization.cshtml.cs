﻿using Ginseng.Models;
using Ginseng.Mvc.Attributes;
using Ginseng.Mvc.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ginseng.Mvc.Pages.Setup
{
	[Authorize]
	[OrgNotRequired]
	public class OrganizationModel : AppPageModel
	{				
		public OrganizationModel(IConfiguration config) : base(config)
		{			
		}

		[BindProperty]
		public Organization Organization { get; set; }

		public bool MustCreateOrg { get; set; }

		public IEnumerable<Organization> MyOrgs { get; set; }

		public void OnGet(bool mustCreate = false)
		{
			if (mustCreate) MustCreateOrg = true;

			Organization = CurrentOrg ?? new Organization();
			using (var cn = Data.GetConnection())
			{
				MyOrgs = new MyOrgs() { UserId = CurrentUser.UserId }.Execute(cn);
			}				
		}

		public async Task<ActionResult> OnPostAsync()
		{
			await Data.TrySaveAsync(Organization, successMessage: "Organization was updated succesfully.");			
			return RedirectToPage("/Setup/Organization");
		}

		public async Task<ActionResult> OnPostSave(Organization org)
		{
			org.OwnerUserId = CurrentUser.UserId;
			await Data.TrySaveAsync(org);
			return RedirectToPage("/Setup/Organization");
		}

		public async Task<ActionResult> OnPostDelete(int id)
		{
			await Data.TryDelete<Organization>(id);
			return RedirectToPage("/Setup/Organization");
		}
	}
}