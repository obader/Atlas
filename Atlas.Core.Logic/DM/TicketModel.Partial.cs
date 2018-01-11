using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PinPayObjects.BaseObjects;

namespace Atlas.Core.Logic.DM
{
    public partial class TicketingEntities
    {
        private TicketingEntities(string connectionString)
            : base(connectionString)
        {
        }

        public static TicketingEntities ConnectToSqlServer(DBConnectionInfo pDbConnectionInfo)
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = pDbConnectionInfo.Host,
                InitialCatalog = pDbConnectionInfo.Catalog,
                PersistSecurityInfo = true,
                IntegratedSecurity = pDbConnectionInfo.WinAuth,
                MultipleActiveResultSets = true,
                UserID = pDbConnectionInfo.User,
                Password = pDbConnectionInfo.Password,
            };

            var entityConnectionStringBuilder = new EntityConnectionStringBuilder
            {
                Provider = "System.Data.SqlClient",
                ProviderConnectionString = sqlBuilder.ConnectionString,
                Metadata = "res://*/DM.TicketModel.csdl|res://*/DM.TicketModel.ssdl|res://*/DM.TicketModel.msl",
            };

            return new TicketingEntities(entityConnectionStringBuilder.ConnectionString);
        }

        public static TicketingEntities ConnectToSqlServer(string host, string catalog, string user, string pass, bool winAuth)
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = host,
                InitialCatalog = catalog,
                PersistSecurityInfo = true,
                IntegratedSecurity = winAuth,
                MultipleActiveResultSets = true,

                UserID = user,
                Password = pass,
            };

            var entityConnectionStringBuilder = new EntityConnectionStringBuilder
            {
                Provider = "System.Data.SqlClient",
                ProviderConnectionString = sqlBuilder.ConnectionString,
                Metadata = "res://*/DM.TicketModel.csdl|res://*/DM.TicketModel.ssdl|res://*/DM.TicketModel.msl",
            };

            return new TicketingEntities(entityConnectionStringBuilder.ConnectionString);
        }

        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
            }
        }
    }
}