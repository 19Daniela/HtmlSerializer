using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        var html = await Load("https://example.com");
        Console.WriteLine(html);
    }

    public static async Task<string> Load(string url)
    {
        HttpClient client = new HttpClient();
        var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        return html;
    }

    public static List<string> SplitHtml(string html)
    {
        var tags = new List<string>();
        var regex = new Regex(@"<[^>]+>");
        var matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            tags.Add(match.Value);
        }

        return tags;
    }

    public static HtmlElement BuildTree(List<string> tags)
    {
        var root = new HtmlElement { Name = "root" };
        var current = root;
        foreach (var tag in tags)
        {
            if (tag.StartsWith("</"))
            {
                current = current.Parent;
            }
            else
            {
                var element = new HtmlElement { Name = tag };
                current.Children.Add(element);
                element.Parent = current;
                if (!tag.EndsWith("/>") && !HtmlHelper.Instance.SelfClosingTags.Contains(tag))
                {
                    current = element;
                }
            }
        }
        return root;
    }
}

public class HtmlElement
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> Attributes { get; set; } = new List<string>();
    public List<string> Classes { get; set; } = new List<string>();
    public string InnerHtml { get; set; }
    public HtmlElement Parent { get; set; }
    public List<HtmlElement> Children { get; set; } = new List<HtmlElement>();

    public IEnumerable<HtmlElement> Descendants()
    {
        var queue = new Queue<HtmlElement>();
        queue.Enqueue(this);
        while (queue.Count > 0)
        {
            var element = queue.Dequeue();
            yield return element;
            foreach (var child in element.Children)
            {
                queue.Enqueue(child);
            }
        }
    }

    public IEnumerable<HtmlElement> Ancestors()
    {
        var current = this;
        while (current.Parent != null)
        {
            yield return current.Parent;
            current = current.Parent;
        }
    }
}

public class HtmlHelper
{
    public string[] AllTags { get; private set; }
    public string[] SelfClosingTags { get; private set; }

    private HtmlHelper()
    {
        AllTags = LoadTags("allTags.json");
        SelfClosingTags = LoadTags("selfClosingTags.json");
    }

    private string[] LoadTags(string fileName)
    {
        var json = File.ReadAllText(fileName);
        return JsonSerializer.Deserialize<string[]>(json);
    }

    private static HtmlHelper _instance;
    public static HtmlHelper Instance => _instance ??= new HtmlHelper();
}

public class Selector
{
    public string TagName { get; set; }
    public string Id { get; set; }
    public List<string> Classes { get; set; } = new List<string>();
    public Selector Parent { get; set; }
    public Selector Child { get; set; }
}

public static class HtmlElementExtensions
{
    public static List<HtmlElement> FindElementsBySelectorWithList(this HtmlElement element, Selector selector)
    {
        var result = new List<HtmlElement>();
        FindElementsBySelector(element, selector, result);
        return result;
    }

    private static void FindElementsBySelector(HtmlElement element, Selector selector, List<HtmlElement> result)
    {
        var descendants = element.Descendants();
        var filtered = descendants.Where(e => e.Name == selector.TagName && e.Id == selector.Id && selector.Classes.All(cls => e.Classes.Contains(cls)));

        if (selector.Child != null)
        {
            foreach (var child in filtered)
            {
                FindElementsBySelector(child, selector.Child, result);
            }
        }
        else
        {
            result.AddRange(filtered);
        }
    }

    public static List<HtmlElement> FindElementsBySelectorWithHashSet(this HtmlElement element, Selector selector)
    {
        var result = new HashSet<HtmlElement>();
        FindElementsBySelector(element, selector, result);
        return result.ToList();
    }

    private static void FindElementsBySelector(HtmlElement element, Selector selector, HashSet<HtmlElement> result)
    {
        var descendants = element.Descendants();
        var filtered = descendants.Where(e => e.Name == selector.TagName && e.Id == selector.Id && selector.Classes.All(cls => e.Classes.Contains(cls)));

        if (selector.Child != null)
        {
            foreach (var child in filtered)
            {
                FindElementsBySelector(child, selector.Child, result);
            }
        }
        else
        {
            result.UnionWith(filtered);
        }
    }
}
