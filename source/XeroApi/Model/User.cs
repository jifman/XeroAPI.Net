﻿using System;
using System.ComponentModel.DataAnnotations;

namespace XeroApi.Model
{
    public class User : ModelBase
    {
        [Key]
        [ItemId]
        public Guid? UserID { get; set; }

        /*[ItemNumber]
        public string EmailAddress { get; set; }*/

        public string FirstName { get; set; }

        public string LastName { get; set; }

        [ReadOnly]
        public DateTime UpdatedDateUTC { get; set; }

        [ReadOnly]
        public bool IsSubscriber { get; set; }

        [ReadOnly]
        public string OrganisationRole { get; set; }


        public string FullName
        {
            get { return string.Concat(FirstName, " ", LastName); }
        }
    }

    public class Users : ModelList<User>
    {
    }

}
