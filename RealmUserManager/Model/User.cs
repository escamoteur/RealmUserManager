using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Realms;


namespace RealmUserManager.Model
{
    public class User : RealmObject 
    {
        public string UserName { get; set; }
        public string UserID { get; set; }

    }
}
