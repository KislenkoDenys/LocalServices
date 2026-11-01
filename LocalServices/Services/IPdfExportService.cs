namespace LocalServices.Services;

public interface IPdfExportService
{
    byte[] GenerateFacilityPriceListPdf(Models.Facility facility);
}

