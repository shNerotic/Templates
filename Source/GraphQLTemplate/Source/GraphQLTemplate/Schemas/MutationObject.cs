namespace GraphQLTemplate.Schemas
{
    using System;
    using GraphQLTemplate.Repositories;
    using GraphQLTemplate.Services;
    using HotChocolate.Types;

    /// <summary>
    /// All mutations defined in the schema used to modify data.
    /// </summary>
    /// <example>
    /// This is an example mutation to create a new human.
    /// <c>
    /// mutation createHuman($human: HumanInput!) {
    ///   createHuman(human: $human)
    ///   {
    ///     id
    ///     name
    ///     dateOfBirth
    ///     appearsIn
    ///   }
    /// }
    /// This is an example JSON of the variables you also need to specify to create a new human:
    /// {
    ///  "human": {
    ///     "name": "Muhammad Rehan Saeed",
    ///     "homePlanet": "Earth",
    ///     "dateOfBirth": "2000-01-01",
    ///     "appearsIn": [ "NEWHOPE" ]
    ///   }
    /// }
    /// </c>
    /// These can be customized by the client.
    /// </example>
    public class MutationObject : ObjectType
    {
        private readonly IClockService clockService;
        private readonly IHumanRepository humanRepository;

        public MutationObject(IClockService clockService, IHumanRepository humanRepository)
        {
            this.clockService = clockService;
            this.humanRepository = humanRepository;

            this.Name = "Mutation";
            this.Description = "The mutation type, represents all updates we can make to our data.";
        }

        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            // this.FieldAsync<HumanObject, Human>(
            //     "createHuman",
            //     "Create a new human.",
            //     arguments: new QueryArguments(
            //         new QueryArgument<NonNullGraphType<HumanInputObject>>()
            //         {
            //             Name = "human",
            //             Description = "The human you want to create.",
            //         }),
            //     resolve: context =>
            //     {
            //         var human = context.GetArgument<Human>("human");
            //         var now = clockService.UtcNow;
            //         human.Created = now;
            //         human.Modified = now;
            //         return humanRepository.AddHumanAsync(human, context.CancellationToken);
            //     });
        }
    }
}
