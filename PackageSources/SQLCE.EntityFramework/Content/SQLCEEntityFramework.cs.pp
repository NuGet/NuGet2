using System.Data.Entity.Infrastructure;

namespace $rootnamespace$ {
    public static class SQLCEEntityFramework {
        public static void SetConnectionFactory() {
            Database.DefaultConnectionFactory = new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");
        }
    }
}
