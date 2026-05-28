namespace DevPulse.Infrastructure.YouTube;

public enum SlideKind { Title, Hook, Code, Takeaway }

public record SlideContent(
    SlideKind Kind,
    string    Title,
    string?   Subtitle    = null,
    string?   Code        = null,
    string?   Language    = null,
    int       SlideIndex  = 0,
    int       TotalSlides = 0);
