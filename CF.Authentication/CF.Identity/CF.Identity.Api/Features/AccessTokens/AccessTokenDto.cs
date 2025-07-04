﻿using CF.Identity.Infrastructure.Features.AccessToken;
using IDFCR.Shared.Abstractions;

namespace CF.Identity.Api.Features.AccessTokens;

public class AccessTokenDto() : MappableBase<IAccessToken>, IEditableAccessToken, IAccessToken
{
    protected override IAccessToken Source => this;
    public string AccessToken { get; set; } = null!;
    public Guid ClientId { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public string ReferenceToken { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public string Type { get; set; } = null!;
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset? SuspendedTimestampUtc { get; set; }
    public string? RevokeReason { get; set; }
    public string? RevokedBy { get; set; }

    public override void Map(IAccessToken source)
    {
        ReferenceToken = source.ReferenceToken;
        AccessToken = source.AccessToken;
        RefreshToken = source.RefreshToken;
        ClientId = source.ClientId;
        Type = source.Type;
        ValidFrom = source.ValidFrom;
        ValidTo = source.ValidTo;
        Id = source.Id;
        UserId = source.UserId;
        SuspendedTimestampUtc = source.SuspendedTimestampUtc;
        RevokedBy = source.RevokedBy;
        RevokeReason = source.RevokeReason;
    }
}
