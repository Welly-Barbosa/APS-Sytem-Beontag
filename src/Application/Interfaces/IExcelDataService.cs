using APSSystem.Core.Enums;
using System.Data;
using System.Threading.Tasks;

namespace APSSystem.Application.Interfaces;

public interface IExcelDataService
{
    Task PreloadScenarioDataAsync(CenarioTipo cenario);
    DataTable GetDataTable(string fileName);
}