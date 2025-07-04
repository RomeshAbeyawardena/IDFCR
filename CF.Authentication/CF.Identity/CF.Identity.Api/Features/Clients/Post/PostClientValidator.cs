﻿using FluentValidation;

namespace CF.Identity.Api.Features.Clients.Post;

public class PostClientValidator : AbstractValidator<PostClientCommand>
{
    public PostClientValidator()
    {
        RuleFor(x => x.Client.Id).Empty()
            .WithMessage("Client ID must be empty for new clients.");
    }
}
