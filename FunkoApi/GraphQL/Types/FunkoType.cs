using FunkoApi.Models;
using HotChocolate.Types;

namespace FunkoApi.GraphQL.Types;

public class FunkoType : ObjectType<Funko>
{
    protected override void Configure(IObjectTypeDescriptor<Funko> descriptor)
    {
        descriptor.Name("Funko");
        descriptor.Description("Entidad Funko");

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>().Description("ID del funko");
        descriptor.Field(p => p.Nombre).Type<NonNullType<StringType>>().Description("Nombre del funko");
        descriptor.Field(p => p.Precio).Type<NonNullType<DecimalType>>().Description("Precio del funko");
        descriptor.Field(p => p.CategoryId).Type<NonNullType<StringType>>().Description("ID de la categoría");
        descriptor.Field(p => p.Category)
            .Type<CategoryType>()
            .Description("Categoría del Funko");
        descriptor.Field(p => p.Imagen).Type<StringType>().Description("URL de la imagen");
        descriptor.Field(p => p.CreatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de creación");
        descriptor.Field(p => p.UpdatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de última actualización");
        
    }
}
