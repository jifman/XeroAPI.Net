using System;
using System.ComponentModel.DataAnnotations;

namespace XeroApi.Model
{
    public class BrandingTheme : ModelBase
    {
        [Key]
        [ItemId]
        public Guid BrandingThemeID { get; set; }

        [ItemNumber]
        public string Name { get; set; }

        public int SortOrder { get; set; }
        
        [ItemUpdatedDate]
        public DateTime CreatedDateUTC { get; set; }
    }

    public class BrandingThemes : ModelList<BrandingTheme>
    {
    }
}
