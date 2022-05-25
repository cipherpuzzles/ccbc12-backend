using SqlSugar;

namespace Ccxc.Core.DbOrm
{
    public class SqlBaseClient : SqlSugarClient
    {
        public SqlBaseClient(string connStr, DbType dbtype) : base(new ConnectionConfig
        {
            DbType = dbtype,
            ConnectionString = connStr,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
        })
        {

        }

        public SqlBaseClient(string connStr, DbType dbtype, bool initKeyFromAttribute) : base(new ConnectionConfig
        {
            DbType = dbtype,
            ConnectionString = connStr,
            IsAutoCloseConnection = true,
            InitKeyType = initKeyFromAttribute ? InitKeyType.Attribute : InitKeyType.SystemTable,
        })
        {

        }

        public SqlBaseClient(ConnectionConfig connConfig) : base(connConfig)
        {

        }
    }
}
