/*using Ganns.Xss;

namespace CoEdit.Common.Infrastructure.Security;

public class HtmlSanitizerWrapper: IHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerWrapper()
    {
        _sanitizer = new HtmlSanitizer();
        // Customize allowed tags/attributes if needed
        // _sanitizer.AllowedAttributes.Add("class"); 
    }

    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        return _sanitizer.Sanitize(html);
    }
}*/