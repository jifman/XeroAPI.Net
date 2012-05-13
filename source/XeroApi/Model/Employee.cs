using System;
using System.ComponentModel.DataAnnotations;

namespace XeroApi.Model
{
    public class Employee : ModelBase
    {
        [Key]
        [ItemId]
        public Guid EmployeeID { get; set; }
        
        public string Status { get; set; }
        
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public Link ExternalLink { get; set; }

        [ItemUpdatedDate]
        public DateTime? UpdatedDateUTC { get; set; }

    }


    public class Employees : ModelList<Employee>
    {
    }
}
