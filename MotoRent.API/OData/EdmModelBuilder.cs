using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.BusinessObjects.DTOs.Rentals;

namespace MotoRent.API.OData;

/// <summary>
/// Mô hình dữ liệu OData. Expose DTO (không expose Entity) để không lộ dữ liệu nhạy cảm
/// như PasswordHash qua $expand.
/// </summary>
public static class EdmModelBuilder
{
    public static IEdmModel Build()
    {
        var builder = new ODataConventionModelBuilder();

        builder.EntitySet<MotorbikeDto>("Motorbikes").EntityType.HasKey(m => m.MotorbikeId);
        builder.EntitySet<RentalSummaryDto>("Rentals").EntityType.HasKey(r => r.RentalId);

        return builder.GetEdmModel();
    }
}
