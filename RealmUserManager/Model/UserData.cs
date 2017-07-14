using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Realms;


namespace RealmUserManager.Model
{
	public class UserData : RealmObject
	{
		public Guid Id { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
        public DateTimeOffset EndOfSubscription { get; set; }
		public bool active { get; internal set; }
        public string Language { get; set; }
	}
}
