using System.ComponentModel;

namespace Entities.Scraping
{
    public class GetPageArgs
    {
        public string Cookie { get; set; }

        [Description("__VIEWSTATE")]
        public string ViewState { get; set; }

        [Description("__EVENTTARGET")]
        public string EventTarget { get; set; }

        [Description("__EVENTARGUMENT")]
        public string EventArgument { get; set; }

        [Description("metai")]
        public string Year { get; set; }

        [Description("leidimai")]
        public string ReportType { get; set; }

        [Description("sort")]
        public string SortBy { get; set; }

        [Description("padaliniai")]
        public string FilterType { get; set; }

        [Description("DropDownList3")]
        public string Enterprise { get; set; }

        [Description("CheckBox2")]
        public string ForestryFilterState { get; set; }

        [Description("DropDownList4")]
        public string Forestry { get; set; }

        [Description("Button1")]
        public string ButtonName { get; set; }
    }
}
