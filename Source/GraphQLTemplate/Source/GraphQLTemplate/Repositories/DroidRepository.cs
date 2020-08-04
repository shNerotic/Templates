namespace GraphQLTemplate.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using GraphQLTemplate.Models;

    public class DroidRepository : IDroidRepository
    {
        public Task<Droid> GetDroidAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Database.Droids.FirstOrDefault(x => x.Id == id));

        public Task<IEnumerable<Droid>> GetDroidsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult(Database.Droids.Where(x => ids.Contains(x.Id)));

        public Task<List<Droid>> GetDroidsAsync(
            int? first,
            DateTime? createdAfter,
            CancellationToken cancellationToken) =>
            Task.FromResult(Database
                .Droids
                .If(createdAfter.HasValue, x => x.Where(y => y.Manufactured > createdAfter.Value))
                .If(first.HasValue, x => x.Take(first.Value))
                .ToList());

        public Task<List<Droid>> GetDroidsReverseAsync(
            int? last,
            DateTime? createdBefore,
            CancellationToken cancellationToken) =>
            Task.FromResult(Database
                .Droids
                .If(createdBefore.HasValue, x => x.Where(y => y.Manufactured < createdBefore.Value))
                .If(last.HasValue, x => x.TakeLast(last.Value))
                .ToList());

        public Task<bool> GetHasNextPageAsync(
            int? first,
            DateTime? createdAfter,
            CancellationToken cancellationToken) =>
            Task.FromResult(Database
                .Droids
                .If(createdAfter.HasValue, x => x.Where(y => y.Manufactured > createdAfter.Value))
                .Skip(first.Value)
                .Any());

        public Task<bool> GetHasPreviousPageAsync(
            int? last,
            DateTime? createdBefore,
            CancellationToken cancellationToken) =>
            Task.FromResult(Database
                .Droids
                .If(createdBefore.HasValue, x => x.Where(y => y.Manufactured < createdBefore.Value))
                .SkipLast(last.Value)
                .Any());

        public Task<List<Character>> GetFriendsAsync(Droid droid, CancellationToken cancellationToken) =>
            Task.FromResult(Database.Characters.Where(x => droid.Friends.Contains(x.Id)).ToList());

        public Task<int> GetTotalCountAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Database.Droids.Count);
    }
}
