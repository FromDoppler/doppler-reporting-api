using System.Data;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public interface IDatabaseConnectionFactory
    {
        IDbConnection GetConnection();
    }
}
