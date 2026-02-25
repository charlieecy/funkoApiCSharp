using FunkoApi.GraphQL.Events;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace FunkoApi.GraphQL.Subscriptions;

public class FunkoSubscriptions
{
    [Subscribe]
    [Topic("onCreatedFunko")]
    public FunkoCreatedEvent OnCreatedFunko([EventMessage] FunkoCreatedEvent message) => message;
    
    [Subscribe]
    [Topic("onUpdatedFunko")]
    public FunkoUpdatedEvent OnUpdatedFunko([EventMessage] FunkoUpdatedEvent message) => message;
    
    [Subscribe]
    [Topic("onDeletedFunko")]
    public FunkoDeletedEvent OnDeletedFunko([EventMessage] FunkoDeletedEvent message) => message;
}