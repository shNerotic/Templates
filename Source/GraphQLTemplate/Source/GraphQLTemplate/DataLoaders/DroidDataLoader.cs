namespace GraphQLTemplate.DataLoaders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using GraphQLTemplate.Models;
    using GraphQLTemplate.Repositories;
    using GreenDonut;

    public class DroidDataLoader : DataLoaderBase<Guid, Droid>
    {
        private readonly IDroidRepository repository;

        public DroidDataLoader(IDroidRepository repository)
            : base(new DataLoaderOptions<Guid>()) =>
            this.repository = repository;

        protected override async Task<IReadOnlyList<Result<Droid>>> FetchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken)
        {
            var droids = await this.repository.GetDroidsAsync(keys, cancellationToken).ConfigureAwait(false);
            return droids.Select(x => Result<Droid>.Resolve(x)).ToList();
        }
    }
}
