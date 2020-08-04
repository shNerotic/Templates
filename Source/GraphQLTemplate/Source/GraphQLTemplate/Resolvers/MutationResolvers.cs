namespace GraphQLTemplate.Resolvers
{
    using System;
    using System.Threading.Tasks;
    using GraphQLTemplate.Models;
    using GraphQLTemplate.Repositories;
    using GraphQLTemplate.Services;
    using HotChocolate.Resolvers;

    public class MutationResolvers
    {
        private readonly IClockService clockService;
        private readonly IHumanRepository humanRepository;

        public MutationResolvers(
            IClockService clockService,
            IHumanRepository humanRepository)
        {
            this.clockService = clockService;
            this.humanRepository = humanRepository;
        }

        public Task<Human> CreateHumanAsync(IResolverContext context, Human human)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (human is null)
            {
                throw new ArgumentNullException(nameof(human));
            }

            var now = this.clockService.UtcNow;
            human.Created = now;
            human.Modified = now;
            return this.humanRepository.AddHumanAsync(human, context.RequestAborted);
        }
    }
}
