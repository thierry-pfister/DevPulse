using DevPulse.Application.Episodes;
using DevPulse.Domain.Episodes;
using DevPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace DevPulse.Infrastructure.Episodes;

public class EpisodeRepository(AppDbContext db) : IEpisodeRepository
{
    public async Task<FSharpOption<Episode>> FindById(EpisodeId id)
    {
        var entity = await db.Episodes.FirstOrDefaultAsync(e => e.Id == id.Item);
        return entity is null ? FSharpOption<Episode>.None : new FSharpOption<Episode>(EpisodeMapper.ToDomain(entity));
    }

    public async Task<FSharpOption<Episode>> FindDraft()
    {
        var entity = await db.Episodes.FirstOrDefaultAsync(e => e.Status == "Draft");
        return entity is null ? FSharpOption<Episode>.None : new FSharpOption<Episode>(EpisodeMapper.ToDomain(entity));
    }

    public async Task<FSharpList<Episode>> FindAll(FSharpOption<EpisodeStatus> statusFilter)
    {
        IQueryable<EpisodeEntity> query = db.Episodes;

        if (statusFilter is not null)
        {
            var statusStr = EpisodeMapper.StatusToString(statusFilter.Value);
            query = query.Where(e => e.Status == statusStr);
        }

        var entities = await query.OrderBy(e => e.EpisodeNumber).ToListAsync();
        return ListModule.OfSeq(entities.Select(EpisodeMapper.ToDomain));
    }

    public async Task Save(Episode episode)
    {
        var incoming = EpisodeMapper.ToEntity(episode);
        var existing = await db.Episodes.FirstOrDefaultAsync(e => e.Id == incoming.Id);
        if (existing is null)
            db.Episodes.Add(incoming);
        else
            db.Entry(existing).CurrentValues.SetValues(incoming);
        await db.SaveChangesAsync();
    }

    public async Task<int> NextNumber()
    {
        var max = await db.Episodes.MaxAsync(e => (int?)e.EpisodeNumber);
        return (max ?? 0) + 1;
    }

    public async Task<FSharpList<Episode>> FindRecentPublishedByTag(string tag, int count)
    {
        var entities = await db.Episodes
            .Where(e => e.Tag == tag && e.Status == "Published" && e.PublishedAt != null)
            .OrderByDescending(e => e.PublishedAt)
            .Take(count)
            .ToListAsync();
        return ListModule.OfSeq(entities.Select(EpisodeMapper.ToDomain));
    }
}
