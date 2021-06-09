using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.CmsKit.Admin.Menus;

namespace Volo.CmsKit.Admin.Web.Pages.CmsKit.Menus.MenuItems
{
    public class UpdateModalModel : CmsKitAdminPageModel
    {
        protected IMenuAdminAppService MenuAdminAppService { get; }
     
        public UpdateModalModel(IMenuAdminAppService menuAdminAppService)
        {
            MenuAdminAppService = menuAdminAppService;
        }

        [BindProperty]
        public MenuItemUpdateViewModel ViewModel { get; set; }

        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid MenuId { get; set; }

        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        public async Task OnGetAsync()
        {
            var menuItemDto = await MenuAdminAppService.GetMenuItemAsync(MenuId, Id);

            ViewModel = ObjectMapper.Map<MenuItemDto, MenuItemUpdateViewModel>(menuItemDto);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var input = ObjectMapper.Map<MenuItemUpdateViewModel, MenuItemUpdateInput>(ViewModel);
            
            var result = await MenuAdminAppService.UpdateMenuItemAsync(MenuId, Id, input);

            return new OkObjectResult(result);
        }
        
        [AutoMap(typeof(MenuItemDto))]
        [AutoMap(typeof(MenuItemUpdateInput), ReverseMap = true)]
        public class MenuItemUpdateViewModel
        {
            [Required]
            public string DisplayName { get; set; }

            public bool IsActive { get; set; }
            
            public string Url { get; set; }

            public string Icon { get; set; }

            public string Target { get; set; }

            public string ElementId { get; set; }

            public string CssClass { get; set; }

            public string RequiredPermissionName { get; set; }

            public Guid? PageId { get; set; }
        }
    }
}