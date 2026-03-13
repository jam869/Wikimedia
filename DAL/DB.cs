using EmailHandling;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using Models; 


namespace DAL
{
    public sealed class DB
    {
        #region singleton setup
        private static readonly DB instance = new DB();
        public static DB Instance { get { return instance; } }
        #endregion

        public static MediasRepository Medias { get; set; } = new MediasRepository();

        public static UsersRepository Users { get; set; } = new UsersRepository();
        public static LoginsRepository Logins { get; set; } = new LoginsRepository();
        public static NotificationsRepository Notifications { get; set; } = new NotificationsRepository();
        public static EventsRepository Events { get; set; } = new EventsRepository();

        public static Repository<UnverifiedEmail> UnverifiedEmails { get; set; } = new Repository<UnverifiedEmail>();
        public static Repository<RenewPasswordCommand> RenewPasswordCommands { get; set; } = new Repository<RenewPasswordCommand>();
    }
}