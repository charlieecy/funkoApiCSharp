using FunkoApi.Models;
using HotChocolate.Types;

namespace FunkoApi.GraphQL.Types;

public class CategoryType: ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor.Name("Category");
        descriptor.Description("Entidad Category");
        
        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>().Description("ID de la categoría");
        descriptor.Field(c => c.Nombre).Type<NonNullType<StringType>>().Description("Nombre de la categoría");
        descriptor.Field(p => p.CreatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de creación");
        descriptor.Field(p => p.UpdatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de última actualización");
    }
}