namespace CoEdit.Common.Domain.Abstractions;

public interface IHtmlSanitizer
{
    string Sanitize(string html);
}